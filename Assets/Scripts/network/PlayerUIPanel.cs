using UnityEngine;
using UnityEngine.UI;

public class PlayerUIPanel : MonoBehaviour
{
    public Slider healthSlider;
    public Slider staminaSlider;
    public Slider thirstSlider;
    public Slider hungerSlider;
    public Slider armorSlider;

    [HideInInspector]
    public bool isAssigned = false;
}
