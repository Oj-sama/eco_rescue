using Unity.Netcode;
using UnityEngine;

public class GameNetworkManager : NetworkBehaviour
{
    public static GameNetworkManager Instance;

    [SerializeField] private GameObject playerPrefab;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

   
}