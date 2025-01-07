using System.Collections.Generic;
using UnityEngine;

public class RaceController : MonoBehaviour
{
    public List<GameObject> registeredPlayers = new List<GameObject>();
    
    public void RegisterPlayer(GameObject player)
    {
        Debug.Log($"Wywołanie RegisterPlayer przez {gameObject.name}.");

        if (!registeredPlayers.Contains(player))
        {
            registeredPlayers.Add(player);
            Debug.Log($"Gracz {player.name} zarejestrowany w wyścigu.");
        }
    }

    
    public void UnregisterPlayer(GameObject player)
    {
        if (registeredPlayers.Contains(player))
        {
            registeredPlayers.Remove(player);
            Debug.Log($"Gracz {player.name} opuścił wyścig.");
        }
    }
}
