using UnityEngine;

public class MultiplayerUI : MonoBehaviour
{
    public static string playerName; // Global player name

    void Awake()
    {
        // Automatically assign a name like "Aziz1234"
        playerName = "Aziz" + Random.Range(1000, 9999);
        Debug.Log("✅ Auto-assigned player name: " + playerName);
    }
}
