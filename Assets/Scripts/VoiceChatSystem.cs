using Unity.Netcode;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System;

public class VoiceChatSystem : NetworkBehaviour
{
    private int FREQUENCY = 48000; // Use 48 kHz for better quality
    private int length = 1; // Recording length in seconds
    private AudioClip recordedClip;
    private bool isRecording;

    private List<byte[]> audioChunks = new List<byte[]>();
    private List<byte[]> receivedChunks = new List<byte[]>();
    private Queue<float> audioBuffer = new Queue<float>(); // Circular buffer for real-time playback

    private bool isInProximity; // To track if the player is close to others
    private float proximityDistance = 10f; // Distance for close-range voice chat
    private float longRangeDistance = 20f; // Distance for long-range voice chat
    private AudioSource audioSource;

    private KeyCode localPushToTalkKey = KeyCode.C; // Key for local voice chat
    private KeyCode radioPushToTalkKey = KeyCode.B; // Key for radio voice chat

    public AudioLowPassFilter lowPassFilter; // To apply radio effect

    private void Start()
    {
        if (GetComponent<AudioSource>() == null)
        {
            gameObject.AddComponent<AudioSource>();
        }
        audioSource = GetComponent<AudioSource>();

       
        lowPassFilter.enabled = false; 
    }

    void FixedUpdate()
    {
        if (IsOwner)
        {
            HandleRecordingInput();
            CheckProximityToOtherPlayers();
        }
    }

    // Handles the push-to-talk button input and proximity-based recording logic
    private void HandleRecordingInput()
    {
        // Local voice chat push-to-talk (V key)
        if (Input.GetKey(localPushToTalkKey) && !isRecording && isInProximity)
        {
            StartRecording();
        }

        // Radio voice chat push-to-talk (B key)
        if (Input.GetKey(radioPushToTalkKey) && !isRecording)
        {
            StartRecording(); // Radio mode when B is pressed
            EnableRadioEffect();
        }

        // Stop recording when button is released
        if (Input.GetKeyUp(localPushToTalkKey) && isRecording && isInProximity)
        {
            StopRecording();
        }

        if (Input.GetKeyUp(radioPushToTalkKey) && isRecording)
        {
            StopRecording();
            DisableRadioEffect(); // Disable radio effect when B is released
        }

        if (!Microphone.IsRecording(null) && isRecording)
        {
            StopRecording();
        }
    }

    // Enables the radio effect (low-pass filter) for the global VC
    private void EnableRadioEffect()
    {
        lowPassFilter.enabled = true; // Apply low-pass filter for radio effect
        lowPassFilter.cutoffFrequency = 5000; // Adjust this for the radio effect quality
    }

    // Disables the radio effect (low-pass filter)
    private void DisableRadioEffect()
    {
        lowPassFilter.enabled = false; // Turn off low-pass filter for normal voice chat
    }

    // Checks the proximity of other players to determine if the local voice chat is active
    private void CheckProximityToOtherPlayers()
    {
        float distanceToOtherPlayer = Mathf.Infinity;
        // Iterate through the networked players to find the closest one
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject == this.gameObject) continue; // Skip the current player

            float distance = Vector3.Distance(transform.position, client.PlayerObject.transform.position);
            if (distance < distanceToOtherPlayer)
            {
                distanceToOtherPlayer = distance;
            }
        }

        // Determine if the player is within range for local voice chat
        isInProximity = distanceToOtherPlayer <= proximityDistance;

        // If in proximity, apply volume attenuation based on distance
        if (isInProximity)
        {
            float volume = Mathf.Clamp01(1 - (distanceToOtherPlayer / proximityDistance)); // Volume decreases as the player moves away
            audioSource.volume = volume;
        }
        else
        {
            audioSource.volume = 0; // Mute when out of range
        }
    }

    // Starts the recording process
    private void StartRecording()
    {
        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("No microphone found!");
            return;
        }

        recordedClip = Microphone.Start(null, false, length, FREQUENCY);
        if (recordedClip == null)
        {
            Debug.LogError("Failed to start recording.");
            return;
        }

        isRecording = true;
        Debug.Log("Recording started.");
    }

    // Stops the recording and processes the recorded audio data
    private void StopRecording()
    {
        isRecording = false;
        Microphone.End(null);
        Debug.Log("Recording stopped.");

        EndRecording();
    }

    // Converts the recorded audio into bytes and sends it to the server
    private void EndRecording()
    {
        byte[] soundBytes = GetBytesFromAudioClip(recordedClip);
        if (soundBytes == null)
        {
            Debug.LogError("Failed to convert AudioClip to bytes.");
            return;
        }

        byte[] compressedData = CompressData(soundBytes);
        SendAudioDataChunks(compressedData, recordedClip.channels);
    }

    // Sends the audio data in chunks to the server
    private void SendAudioDataChunks(byte[] data, int channels)
    {
        int chunkSize = 2048; // Adjust for optimal performance
        for (int i = 0; i < data.Length; i += chunkSize)
        {
            int size = Math.Min(chunkSize, data.Length - i);
            byte[] chunk = new byte[size];
            System.Array.Copy(data, i, chunk, 0, size);
            SendServerRPC(chunk, channels, i + size >= data.Length);
        }
    }

    [ServerRpc(RequireOwnership = false, Delivery = RpcDelivery.Reliable)]
    private void SendServerRPC(byte[] chunk, int channels, bool isLastChunk)
    {
        SendClientRPC(chunk, channels, isLastChunk);
    }

    [ClientRpc]
    private void SendClientRPC(byte[] chunk, int channels, bool isLastChunk)
    {
        if (!IsOwner)
        {
            ReceiveDataChunk(chunk, channels, isLastChunk);
        }
    }

    // Receives the data chunk and processes it
    private void ReceiveDataChunk(byte[] chunk, int channels, bool isLastChunk)
    {
        receivedChunks.Add(chunk);

        if (isLastChunk)
        {
            byte[] fullData = CombineChunks(receivedChunks);
            receivedChunks.Clear();

            byte[] decompressedData = DecompressData(fullData);
            AudioClip audioClip = CreateAudioClipFromBytes(decompressedData, FREQUENCY, channels);

            if (audioClip != null)
            {
                PlayAudioClip(audioClip);
            }
        }
    }

    // Combines all chunks into one full audio file
    private byte[] CombineChunks(List<byte[]> chunks)
    {
        int totalSize = chunks.Sum(chunk => chunk.Length);
        byte[] result = new byte[totalSize];
        int offset = 0;
        foreach (var chunk in chunks)
        {
            System.Buffer.BlockCopy(chunk, 0, result, offset, chunk.Length);
            offset += chunk.Length;
        }
        return result;
    }

    // Plays the received audio clip
    private void PlayAudioClip(AudioClip audioClip)
    {
        float[] samples = new float[audioClip.samples * audioClip.channels];
        audioClip.GetData(samples, 0);

        // Add samples to the circular buffer
        foreach (float sample in samples)
        {
            audioBuffer.Enqueue(sample);
        }

        // Stream audio from the buffer
        if (!audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }

    void Update()
    {
        if (audioBuffer.Count > 0)
        {
            float[] samplesToPlay = new float[audioBuffer.Count];
            audioBuffer.CopyTo(samplesToPlay, 0);

            AudioClip tempClip = AudioClip.Create("TempClip", samplesToPlay.Length, 1, FREQUENCY, false);
            tempClip.SetData(samplesToPlay, 0);

            audioSource.clip = tempClip;
            audioSource.Play();

            audioBuffer.Clear();
        }
    }

    // Compresses the audio data (placeholder for actual compression logic)
    private byte[] CompressData(byte[] data)
    {
        return data; // Placeholder
    }

    // Decompresses the audio data (placeholder for actual decompression logic)
    private byte[] DecompressData(byte[] compressedData)
    {
        return compressedData; // Placeholder
    }

    // Converts AudioClip to byte array
    private byte[] GetBytesFromAudioClip(AudioClip audioClip)
    {
        float[] samples = new float[audioClip.samples * audioClip.channels];
        audioClip.GetData(samples, 0);

        byte[] bytes = new byte[samples.Length * sizeof(float)];
        System.Buffer.BlockCopy(samples, 0, bytes, 0, bytes.Length);

        return bytes;
    }

    // Creates an AudioClip from a byte array
    private AudioClip CreateAudioClipFromBytes(byte[] bytes, int frequency, int channels)
    {
        float[] samples = new float[bytes.Length / sizeof(float)];
        System.Buffer.BlockCopy(bytes, 0, samples, 0, bytes.Length);

        AudioClip audioClip = AudioClip.Create("ReceivedAudio", samples.Length, channels, frequency, false);
        audioClip.SetData(samples, 0);

        return audioClip;
    }
}
