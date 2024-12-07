using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class RaceController : NetworkBehaviour
{
    public List<GameObject> RegisteredPlayers = new List<GameObject>();
    
    public void RegisterPlayer(GameObject player)
    {
        if (!isServer) return;

        if (!RegisteredPlayers.Contains(player))
        {
            RegisteredPlayers.Add(player);
            Debug.Log($"Gracz {player.name} zarejestrowany w wyścigu.");
        }
    }
    
    public void UnregisterPlayer(GameObject player)
    {
        if (!isServer) return;

        if (RegisteredPlayers.Contains(player))
        {
            RegisteredPlayers.Remove(player);
            Debug.Log($"Gracz {player.name} opuścił wyścig.");
        }
    }
}
