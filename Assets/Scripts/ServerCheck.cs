using UnityEngine;
using Mirror;

public class ServerCheck : MonoBehaviour
{
    private void Start()
    {
        if(NetworkServer.active) Debug.LogWarning("Server active");

        if (Application.isBatchMode) Debug.LogWarning("Headless Start");
        else NetworkManager.singleton.networkAddress = "54.38.52.204";
    }
}