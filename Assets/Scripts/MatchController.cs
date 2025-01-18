using System.Collections;
using Mirror;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(NetworkMatch))]
    public class MatchController : NetworkBehaviour
    {
        internal readonly SyncDictionary<NetworkIdentity, MatchPlayerData> matchPlayerData = new SyncDictionary<NetworkIdentity, MatchPlayerData>();

        bool playAgain = false;

        [Header("GUI References")]
        public CanvasGroup canvasGroup;
        public Text gameText;
        public Button exitButton;
        public Button playAgainButton;

        [Header("Diagnostics")]
        [ReadOnly, SerializeField] internal CanvasController canvasController;
        [ReadOnly, SerializeField] internal NetworkIdentity player1;
        [ReadOnly, SerializeField] internal NetworkIdentity player2;
        
        [Header("Player Starting Positions")]
        public Vector3[] startingPositions = new Vector3[]
        {
            new Vector3(-4, 0, 0),  // Position for player 1
            new Vector3(4, 0, 0)   // Position for player 2
        };
        void Awake()
        {
            canvasController = GameObject.FindObjectOfType<CanvasController>();
        }

        public override void OnStartServer()
        {
            StartCoroutine(AddPlayersToMatchController());
        }

        // For the SyncDictionary to properly fire the update callback, we must
        // wait a frame before adding the players to the already spawned MatchController
        IEnumerator AddPlayersToMatchController()
        {
            yield return null;

            matchPlayerData.Add(player1, new MatchPlayerData
            {
                playerIndex = CanvasController.playerInfos[player1.connectionToClient].playerIndex,
                carController = player1.GetComponent<CarController>()
            });
            matchPlayerData.Add(player2, new MatchPlayerData
            {
                playerIndex = CanvasController.playerInfos[player2.connectionToClient].playerIndex,
                carController = player2.GetComponent<CarController>()
            });

            RpcStartCountdown();
        }

        public override void OnStartClient()
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            exitButton.gameObject.SetActive(false);
            playAgainButton.gameObject.SetActive(false);
        }
        
        [ClientRpc]
        private void RpcStartCountdown()
        {
            StartCoroutine(StartCountdown());
        }
        
        private IEnumerator StartCountdown()
        {
            gameText.text = "3";
            gameText.color = Color.white;
            yield return new WaitForSeconds(1f);

            gameText.text = "2";
            yield return new WaitForSeconds(1f);

            gameText.text = "1";
            yield return new WaitForSeconds(1f);

            gameText.text = "Start!";
            gameText.color = Color.green;
            
            CmdEnablePlayerCars();

            yield return new WaitForSeconds(1f);

            gameText.text = ""; // Ukryj tekst po odliczaniu
        }
        
        [Command(requiresAuthority = false)]
        private void CmdEnablePlayerCars()
        {
            RpcEnablePlayerCars();
        }
        
        [ClientRpc]
        private void RpcEnablePlayerCars()
        {
            foreach (var player in matchPlayerData)
            {
                player.Value.carController.enabled = true;
            }
        }
        
        [Command(requiresAuthority = false)]
        public void CmdDisablePlayerCars()
        {
            RpcDisablePlayerCars();
        }
        
        [ClientRpc]
        private void RpcDisablePlayerCars()
        {
            foreach (var player in matchPlayerData)
            {
                player.Value.carController.enabled = false;
                player.Value.carController.StopCar();
            }
        }

        [Command(requiresAuthority = false)]
        public void CmdShowWinner(NetworkIdentity winner)
        {
            RpcShowWinner(winner);
        }
        [ClientRpc]
        public void RpcShowWinner(NetworkIdentity winner)
        {
            if (winner.gameObject.GetComponent<NetworkIdentity>().isLocalPlayer)
            {
                gameText.text = "Winner!";
                gameText.color = Color.blue;
            }
            else
            {
                gameText.text = "Loser!";
                gameText.color = Color.red;
            }

            exitButton.gameObject.SetActive(true);
            playAgainButton.gameObject.SetActive(true);
        }

        public void ResetCarLapCounters()
        {
            CarLapCounter[] carLapCounters = FindObjectsOfType<CarLapCounter>();
            foreach (CarLapCounter carLapCounter in carLapCounters) carLapCounter.Reset();
        }

        // Assigned in inspector to ReplayButton::OnClick
        [ClientCallback]
        public void RequestPlayAgain()
        {
            playAgainButton.gameObject.SetActive(false);
            CmdPlayAgain();
        }

        [Command(requiresAuthority = false)]
        private void CmdPlayAgain()
        {
            if (!playAgain)
                playAgain = true;
            else
            {
                playAgain = false;
                RestartGame();
            }
        }

        [ServerCallback]
        public void RestartGame()
        {
            NetworkIdentity[] keys = new NetworkIdentity[matchPlayerData.Keys.Count];
            matchPlayerData.Keys.CopyTo(keys, 0);

            foreach (NetworkIdentity identity in keys)
            {
                MatchPlayerData mpd = matchPlayerData[identity];
                matchPlayerData[identity] = mpd;
            }

            RpcResetPlayerPositions();
            
            RpcRestartGame();

            RpcStartCountdown();
        }

        [ClientRpc]
        private void RpcRestartGame()
        {
            exitButton.gameObject.SetActive(false);
            playAgainButton.gameObject.SetActive(false);
        }
        
        [ClientRpc]
        private void RpcResetPlayerPositions()
        {
            int index = 0;
            foreach (var player in matchPlayerData.Keys)
            {
                if (index < startingPositions.Length)
                {
                    player.transform.position = startingPositions[index];
                    player.transform.rotation = Quaternion.Euler(0,0,0);
                    index++;
                }
            }
        }

        // Assigned in inspector to BackButton::OnClick
        [Client]
        public void RequestExitGame()
        {
            exitButton.gameObject.SetActive(false);
            playAgainButton.gameObject.SetActive(false);
            CmdRequestExitGame();
        }

        [Command(requiresAuthority = false)]
        public void CmdRequestExitGame(NetworkConnectionToClient sender = null)
        {
            StartCoroutine(ServerEndMatch(sender, false));
        }

        [ServerCallback]
        public void OnPlayerDisconnected(NetworkConnectionToClient conn)
        {
            // Check that the disconnecting client is a player in this match
            if (player1 == conn.identity || player2 == conn.identity)
                StartCoroutine(ServerEndMatch(conn, true));
        }

        [ServerCallback]
        public IEnumerator ServerEndMatch(NetworkConnectionToClient conn, bool disconnected)
        {
            RpcExitGame();

            canvasController.OnPlayerDisconnected -= OnPlayerDisconnected;

            // Wait for the ClientRpc to get out ahead of object destruction
            yield return new WaitForSeconds(0.1f);

            // Mirror will clean up the disconnecting client so we only need to clean up the other remaining client.
            // If both players are just returning to the Lobby, we need to remove both connection Players

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

            // Skip a frame to allow the Removal(s) to complete
            yield return null;

            // Send latest match list
            canvasController.SendMatchList();

            NetworkServer.Destroy(gameObject);
        }

        [ClientRpc]
        public void RpcExitGame()
        {
            canvasController.OnMatchEnded();
        }
    }
