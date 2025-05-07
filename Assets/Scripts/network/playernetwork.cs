using System.Globalization;
using Unity.Netcode;
using UnityEngine;

public class playernetwork : NetworkBehaviour
{
    public GameObject cinmachins;
    public GameObject camera;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            cinmachins.gameObject.SetActive(true);
            camera.gameObject.SetActive(true);
            GetComponent<playerController>().enabled = true;
            DisableCollider<CapsuleCollider>();

        }

    }
    private void DisableCollider<T>() where T : Collider
    {
        T collider = GetComponent<T>();
        if (collider != null)
        {
            collider.enabled = false;
        }
    }
}
