using System.Collections;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(NetworkMatch))]
public class MatchController : NetworkBehaviour
{
    #region Variables
    // SyncDictionary to hold player data for the match
    internal readonly SyncDictionary<NetworkIdentity, MatchPlayerData> MatchPlayerData = new SyncDictionary<NetworkIdentity, MatchPlayerData>();
    // Flag to track if players want to play again
    private bool _playAgain = false;
    #endregion

    #region GUI
    [Header("GUI References")]
    // References to the GUI elements
    public CanvasGroup canvasGroup;
    public Button exitButton;
    public Button playAgainButton;
    public TMP_Text lapCounterText;
    public TMP_Text positionText;
    public TMP_Text infoText;

    [Header("Diagnostics")]
    // Reference to the CanvasController for diagnostic purposes
    [ReadOnly, SerializeField] internal CanvasController canvasController;
    #endregion

    #region Player
    [ReadOnly, SerializeField] internal NetworkIdentity player1;
    [ReadOnly, SerializeField] internal NetworkIdentity player2;

    [Header("Player Starting Positions")]
    // Array to hold starting positions for players
    public Vector3[] startingPositions = new Vector3[]
    {
        new Vector3(-4, 0, 0),  // Position for player 1
        new Vector3(4, 0, 0)    // Position for player 2
    };
    #endregion

    #region Networking
    public override void OnStartServer()
    {
        // Start adding players to the match controller on the server
        StartCoroutine(AddPlayersToMatchController());
    }

    public override void OnStartClient()
    {
        // Initialize GUI elements on the client
        lapCounterText.text = "Laps: 1";
        positionText.text = "Pos: 1";

        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        exitButton.gameObject.SetActive(false);
        playAgainButton.gameObject.SetActive(false);
    }

    [ClientRpc]
    private void RpcStartCountdown()
    {
        // Start the countdown on the clients
        StartCoroutine(StartCountdown());
    }

    [Command(requiresAuthority = false)]
    private void CmdEnablePlayerCars()
    {
        // Command to enable player cars on the server
        RpcEnablePlayerCars();
    }

    [ClientRpc]
    private void RpcEnablePlayerCars()
    {
        // Enable car controllers for all players on the clients
        foreach (var player in MatchPlayerData)
        {
            player.Value.carController.enabled = true;
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdDisablePlayerCars()
    {
        // Command to disable player cars on the server
        RpcDisablePlayerCars();
    }

    [ClientRpc]
    private void RpcDisablePlayerCars()
    {
        // Disable car controllers for all players on the clients and stop the cars
        foreach (var player in MatchPlayerData)
        {
            player.Value.carController.enabled = false;
            player.Value.carController.StopCar();
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdShowWinner(NetworkIdentity winner)
    {
        // Command to show the winner on the server
        RpcShowWinner(winner);
    }

    [ClientRpc]
    private void RpcShowWinner(NetworkIdentity winner)
    {
        // Display winner or loser text on the clients
        if (winner.gameObject.GetComponent<NetworkIdentity>().isLocalPlayer)
        {
            infoText.text = "Winner!";
            infoText.color = Color.blue;
        }
        else
        {
            infoText.text = "Loser!";
            infoText.color = Color.red;
        }

        exitButton.gameObject.SetActive(true);
        playAgainButton.gameObject.SetActive(true);
    }

    // Assigned in inspector to ReplayButton::OnClick
    [ClientCallback]
    public void RequestPlayAgain()
    {
        // Handle play again request from the client
        playAgainButton.gameObject.SetActive(false);
        CmdPlayAgain();
    }

    [Command(requiresAuthority = false)]
    private void CmdPlayAgain()
    {
        // Command to handle play again logic on the server
        if (!_playAgain)
            _playAgain = true;
        else
        {
            _playAgain = false;
            RestartGame();
        }
    }

    [ServerCallback]
    private void RestartGame()
    {
        // Restart the game on the server
        NetworkIdentity[] keys = new NetworkIdentity[MatchPlayerData.Keys.Count];
        MatchPlayerData.Keys.CopyTo(keys, 0);

        foreach (NetworkIdentity identity in keys)
        {
            MatchPlayerData mpd = MatchPlayerData[identity];
            MatchPlayerData[identity] = mpd;
        }

        RpcResetPlayerPositions();
        RpcRestartGame();
        RpcStartCountdown();
    }

    [ClientRpc]
    private void RpcRestartGame()
    {
        // Restart the game on the clients
        exitButton.gameObject.SetActive(false);
        playAgainButton.gameObject.SetActive(false);
    }

    [ClientRpc]
    private void RpcResetPlayerPositions()
    {
        // Reset player positions on the clients
        int index = 0;
        foreach (var player in MatchPlayerData.Keys)
        {
            if (index < startingPositions.Length)
            {
                player.transform.position = startingPositions[index];
                player.transform.rotation = Quaternion.Euler(0, 0, 0);
                index++;
            }
        }
    }

    // Assigned in inspector to BackButton::OnClick
    [Client]
    public void RequestExitGame()
    {
        // Handle exit game request from the client
        exitButton.gameObject.SetActive(false);
        playAgainButton.gameObject.SetActive(false);
        CmdRequestExitGame();
    }

    [Command(requiresAuthority = false)]
    private void CmdRequestExitGame(NetworkConnectionToClient sender = null)
    {
        // Command to handle exit game logic on the server
        StartCoroutine(ServerEndMatch(sender, false));
    }

    [ServerCallback]
    public void OnPlayerDisconnected(NetworkConnectionToClient conn)
    {
        // Handle player disconnection on the server
        if (player1 == conn.identity || player2 == conn.identity)
            StartCoroutine(ServerEndMatch(conn, true));
    }

    [ServerCallback]
    private IEnumerator ServerEndMatch(NetworkConnectionToClient conn, bool disconnected)
    {
        // End the match on the server
        RpcExitGame();
        canvasController.OnPlayerDisconnected -= OnPlayerDisconnected;

        yield return new WaitForSeconds(0.1f);

        if (!disconnected)
        {
            NetworkServer.RemovePlayerForConnection(player1.connectionToClient, RemovePlayerOptions.Destroy);
            CanvasController.waitingConnections.Add(player1.connectionToClient);

            NetworkServer.RemovePlayerForConnection(player2.connectionToClient, RemovePlayerOptions.Destroy);
            CanvasController.waitingConnections.Add(player2.connectionToClient);
        }
        else if (conn == player1.connectionToClient)
        {
            // player1 has disconnected - send player2 back to Lobby
            NetworkServer.RemovePlayerForConnection(player2.connectionToClient, RemovePlayerOptions.Destroy);
            CanvasController.waitingConnections.Add(player2.connectionToClient);
        }
        else if (conn == player2.connectionToClient)
        {
            // player2 has disconnected - send player1 back to Lobby
            NetworkServer.RemovePlayerForConnection(player1.connectionToClient, RemovePlayerOptions.Destroy);
            CanvasController.waitingConnections.Add(player1.connectionToClient);
        }

        yield return null;

        canvasController.SendMatchList();
        NetworkServer.Destroy(gameObject);
    }

    [ClientRpc]
    private void RpcExitGame()
    {
        // Handle exit game logic on the clients
        canvasController.OnMatchEnded();
        canvasController.minimap.SetActive(false);
    }
    #endregion

    #region Unity Callbacks
    void Awake()
    {
        // Initialize the canvas controller
        canvasController = GameObject.FindObjectOfType<CanvasController>();
    }
    #endregion

    #region Methods
    // For the SyncDictionary to properly fire the update callback, we must
    // wait a frame before adding the players to the already spawned MatchController
    IEnumerator AddPlayersToMatchController()
    {
        yield return null;

        MatchPlayerData.Add(player1, new MatchPlayerData
        {
            playerIndex = CanvasController.playerInfos[player1.connectionToClient].playerIndex,
            carController = player1.GetComponent<CarController>()
        });
        MatchPlayerData.Add(player2, new MatchPlayerData
        {
            playerIndex = CanvasController.playerInfos[player2.connectionToClient].playerIndex,
            carController = player2.GetComponent<CarController>()
        });

        RpcStartCountdown();
    }

    private IEnumerator StartCountdown()
    {
        // Display countdown on the clients
        infoText.text = "3";
        infoText.color = Color.white;
        yield return new WaitForSeconds(1f);

        infoText.text = "2";
        yield return new WaitForSeconds(1f);

        infoText.text = "1";
        yield return new WaitForSeconds(1f);

        infoText.text = "Start!";
        infoText.color = Color.green;

        CmdEnablePlayerCars();

        yield return new WaitForSeconds(1f);

        infoText.text = ""; // Hide text after the countdown
    }
    
    public void ResetCarLapCounters()
    {
        // Reset the lap counters for all cars
        CarLapCounter[] carLapCounters = FindObjectsOfType<CarLapCounter>();
        foreach (CarLapCounter carLapCounter in carLapCounters) carLapCounter.Reset();
    }
    #endregion
}