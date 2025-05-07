using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    public List<PlayerUIPanel> playerUIPanels;

    private void Awake()
    {
        Instance = this;
    }

    public PlayerUIPanel AssignPanelToPlayer(ulong clientId)
    {
        foreach (var panel in playerUIPanels)
        {
            if (!panel.isAssigned)
            {
                panel.isAssigned = true;
                panel.gameObject.SetActive(true);
                return panel;
            }
        }

        Debug.LogWarning("No free UI panel for player " + clientId);
        return null;
    }
}
