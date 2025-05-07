using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class NetworkUI : MonoBehaviour
{
    [SerializeField] Button hostButton;
    [SerializeField] Button clientButton;
    [SerializeField] Button exitButton;

    void Start()
    {
        exitButton.gameObject.SetActive(false);

        hostButton.onClick.AddListener(() => {
            NetworkManager.Singleton.StartHost();
            SetConnectedState();
        });

        clientButton.onClick.AddListener(() => {
            NetworkManager.Singleton.StartClient();
            SetConnectedState();
        });

        exitButton.onClick.AddListener(() => {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.Shutdown();
                Destroy(NetworkManager.Singleton.gameObject);
            }
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        });
    }

    void SetConnectedState()
    {
        hostButton.gameObject.SetActive(false);
        clientButton.gameObject.SetActive(false);
        exitButton.gameObject.SetActive(true);
    }

}