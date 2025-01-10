using Mirror;
using UnityEngine;

public class LineOfMeta : NetworkBehaviour
{
    [SerializeField] private MatchController matchController;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Sprawdź, czy obiekt, który wjechał w trigger, to gracz
        if (collision.CompareTag("Player"))
        {
            // Wywołaj komendę na serwerze, aby wyłączyć skrypt wszystkim graczom
            matchController.CmdDisablePlayerCars();
            
            //TODO: DODAC ODWOLANIE DO MATCHCONTROLLER ZEBY WYWOLYWAL GUZIKI POWROTU I RESTARTU 
            NetworkIdentity playerIdentity = collision.GetComponent<NetworkIdentity>();
            matchController.CmdShowWinner(playerIdentity);
        }
    }
}
