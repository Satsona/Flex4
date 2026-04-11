using UnityEngine;
using Unity.Netcode;

public class Bootstrap : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            NetworkManager.Singleton.StartHost();
            Debug.Log("HOST START");
        }

        if (Input.GetKeyDown(KeyCode.J))
        {
            NetworkManager.Singleton.StartClient();
            Debug.Log("CLIENT START");
        }
    }
}