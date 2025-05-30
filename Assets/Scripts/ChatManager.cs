using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;

public class ChatManager : NetworkBehaviour
{
    public static ChatManager Singleton;

    [SerializeField] ChatMessage chatMessagePrefab;
    [SerializeField] CanvasGroup chatContent;
    [SerializeField] TMP_InputField chatInput;

    public string playerName;

    void Awake()
{
    ChatManager.Singleton = this;

    // Auto-generate a random name like Aziz1234
    playerName = "Aziz" + Random.Range(1000, 9999);
    Debug.Log("✅ Assigned player name: " + playerName);
}


    void Update() 
    {
        if(Input.GetKeyDown(KeyCode.Return))
        {
            SendChatMessage(chatInput.text);
            chatInput.text = "";
        }
    }

    public void SendChatMessage(string _message)
    {
        if (string.IsNullOrWhiteSpace(_message)) return;

        string formattedMessage = playerName + " > " + _message;
        SendChatMessageServerRpc(formattedMessage);
    }


    void AddMessage(string msg)
    {
        ChatMessage CM = Instantiate(chatMessagePrefab, chatContent.transform);
        CM.SetText(msg);
    }

    [ServerRpc(RequireOwnership = false)]
    void SendChatMessageServerRpc(string message)
    {
        ReceiveChatMessageClientRpc(message);
    }

    [ClientRpc]
    void ReceiveChatMessageClientRpc(string message)
    {
        ChatManager.Singleton.AddMessage(message);
    }
}
