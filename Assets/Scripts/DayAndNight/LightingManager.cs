using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class LightingManager : MonoBehaviour
{
    // Scene References
    [SerializeField] private Light DirectionalLight;
    [SerializeField] private LightingPreset Preset;

    // Variables
    [SerializeField, Range(0, 24)] private float TimeOfDay;

    // Objects to disappear between 18.46 and 5.2 time
    [SerializeField] private GameObject[] objectsToDisappear;
    [SerializeField] private GameObject[] otherObjectsToDisappear;

    // Object to spawn between 18.46 and 5.2 time
    [SerializeField] private GameObject objectToSpawn;

    private void Update()
    {
        if (Preset == null)
            return;

        if (Application.isPlaying)
        {
            // (Replace with a reference to the game time)
            TimeOfDay += Time.deltaTime;
            TimeOfDay %= 24; // Modulus to ensure always between 0-24
            UpdateLighting(TimeOfDay / 24f);
        }
        else
        {
            UpdateLighting(TimeOfDay / 24f);
        }

        HandleObjectVisibility();
    }

    private void UpdateLighting(float timePercent)
    {
        // Set ambient and fog
        RenderSettings.ambientLight = Preset.AmbientColor.Evaluate(timePercent);
        RenderSettings.fogColor = Preset.FogColor.Evaluate(timePercent);

        // If the directional light is set then rotate and set its color
        if (DirectionalLight != null)
        {
            DirectionalLight.color = Preset.DirectionalColor.Evaluate(timePercent);
            DirectionalLight.transform.localRotation = Quaternion.Euler(new Vector3((timePercent * 360f) - 90f, 170f, 0));
        }
    }

    private void HandleObjectVisibility()
    {
        bool shouldBeActive = !(TimeOfDay >= 18.46f || TimeOfDay <= 5.2f);

        foreach (var obj in objectsToDisappear)
        {
            if (obj != null)
                obj.SetActive(shouldBeActive);
        }

        foreach (var obj in otherObjectsToDisappear)
        {
            if (obj != null)
                obj.SetActive(shouldBeActive);
        }

        if (objectToSpawn != null)
        {
            objectToSpawn.SetActive(!shouldBeActive);
        }
    }

    // Try to find a directional light to use if we haven't set one
    private void OnValidate()
    {
        if (DirectionalLight != null)
            return;

        // Search for lighting tab sun
        if (RenderSettings.sun != null)
        {
            DirectionalLight = RenderSettings.sun;
        }
        // Search scene for light that fits criteria (directional)
        else
        {
            Light[] lights = GameObject.FindObjectsOfType<Light>();
            foreach (Light light in lights)
            {
                if (light.type == LightType.Directional)
                {
                    DirectionalLight = light;
                    return;
                }
            }
        }
    }
}
