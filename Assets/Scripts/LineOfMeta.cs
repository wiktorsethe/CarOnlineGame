using System;
using Mirror;
using UnityEngine;
using UnityEngine.Serialization;

public class LineOfMeta : NetworkBehaviour
{
    [SerializeField] private RaceController raceController;

    private void Start()
    {
        raceController = FindObjectOfType(typeof(RaceController)) as RaceController;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Sprawdź, czy obiekt, który wjechał w trigger, to gracz
        if (collision.CompareTag("Player"))
        {
            // Wywołaj komendę na serwerze, aby wyłączyć skrypt wszystkim graczom
            CmdDisableCarControllers();
            
        }
    }

    [Command(requiresAuthority = false)]
    private void CmdDisableCarControllers()
    {
        // Wywołaj RPC, aby wyłączyć skrypt na wszystkich klientach
        RpcDisableCarControllers();
    }

    [ClientRpc]
    private void RpcDisableCarControllers()
    {
        // Znajdź wszystkie obiekty graczy i wyłącz CarController
        foreach (var player in FindObjectsOfType<CarController>())
        {
            if (player!= null)
            {
                player.enabled = false;
            }
        }
    }
}
