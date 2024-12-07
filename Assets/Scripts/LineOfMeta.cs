using Mirror;
using UnityEngine;

public class LineOfMeta : NetworkBehaviour
{
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
