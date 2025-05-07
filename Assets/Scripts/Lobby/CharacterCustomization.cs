using System.Collections.Generic;
using UnityEngine;

public class CharacterCustomization : MonoBehaviour
{
    [Header("Customization Categories")]
    public List<GameObject> caps;
    public List<GameObject> pants;
    public List<GameObject> hands;
    public List<GameObject> boots;
    public List<GameObject> vests;

    private int capIndex = 0;
    private int pantIndex = 0;
    private int handIndex = 0;
    private int bootIndex = 0;
    private int vestIndex = 0;

    void Start()
    {
        // Initialize: Activate the first item in each category, deactivate others
        ActivateItem(caps, capIndex);
        ActivateItem(pants, pantIndex);
        ActivateItem(hands, handIndex);
        ActivateItem(boots, bootIndex);
        ActivateItem(vests, vestIndex);
    }

    // Function to activate a specific item in the list and deactivate others
    private void ActivateItem(List<GameObject> items, int activeIndex)
    {
        for (int i = 0; i < items.Count; i++)
        {
            items[i].SetActive(i == activeIndex);
        }
    }

    // Navigation Functions for Each Category
    public void NextCap() { capIndex = (capIndex + 1) % caps.Count; ActivateItem(caps, capIndex); }
    public void PreviousCap() { capIndex = (capIndex - 1 + caps.Count) % caps.Count; ActivateItem(caps, capIndex); }

    public void NextPant() { pantIndex = (pantIndex + 1) % pants.Count; ActivateItem(pants, pantIndex); }
    public void PreviousPant() { pantIndex = (pantIndex - 1 + pants.Count) % pants.Count; ActivateItem(pants, pantIndex); }

    public void NextHand() { handIndex = (handIndex + 1) % hands.Count; ActivateItem(hands, handIndex); }
    public void PreviousHand() { handIndex = (handIndex - 1 + hands.Count) % hands.Count; ActivateItem(hands, handIndex); }

    public void NextBoot() { bootIndex = (bootIndex + 1) % boots.Count; ActivateItem(boots, bootIndex); }
    public void PreviousBoot() { bootIndex = (bootIndex - 1 + boots.Count) % boots.Count; ActivateItem(boots, bootIndex); }

    public void NextVest() { vestIndex = (vestIndex + 1) % vests.Count; ActivateItem(vests, vestIndex); }
    public void PreviousVest() { vestIndex = (vestIndex - 1 + vests.Count) % vests.Count; ActivateItem(vests, vestIndex); }
}
