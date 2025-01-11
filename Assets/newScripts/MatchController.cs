using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(NetworkMatch))]
    public class MatchController : NetworkBehaviour
    {
        internal readonly SyncDictionary<NetworkIdentity, MatchPlayerData> matchPlayerData = new SyncDictionary<NetworkIdentity, MatchPlayerData>();
        //internal readonly Dictionary<CellValue, CellGUI> MatchCells = new Dictionary<CellValue, CellGUI>();

        //CellValue boardScore = CellValue.None;
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
        [ReadOnly, SerializeField] internal NetworkIdentity startingPlayer;
        
        [SyncVar(hook = nameof(UpdateGameUI))]
        [ReadOnly, SerializeField] internal NetworkIdentity currentPlayer;

        [Header("Player Starting Positions")]
        public Vector3[] startingPositions = new Vector3[]
        {
            new Vector3(-4, 0, 0),  // Position for player 1
            new Vector3(4, 0, 0)   // Position for player 2
        };
        void Awake()
        {
#if UNITY_2022_2_OR_NEWER
            canvasController = GameObject.FindAnyObjectByType<CanvasController>();
#else
            // Deprecated in Unity 2023.1
            canvasController = GameObject.FindObjectOfType<CanvasController>();
#endif
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
            yield return new WaitForSeconds(1f);

            CmdEnablePlayerCars();
            
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

        [ClientCallback]
        public void UpdateGameUI(NetworkIdentity _, NetworkIdentity newPlayerTurn)
        {
            /*if (!newPlayerTurn) return;

            if (newPlayerTurn.gameObject.GetComponent<NetworkIdentity>().isLocalPlayer)
            {
                gameText.text = "Your Turn";
                gameText.color = Color.blue;
            }
            else
            {
                gameText.text = "Their Turn";
                gameText.color = Color.red;
            }*/
        }

        /*[Command(requiresAuthority = false)]
        public void CmdMakePlay(CellValue cellValue, NetworkConnectionToClient sender = null)
        {
            // If wrong player or cell already taken, ignore
            if (sender.identity != currentPlayer || MatchCells[cellValue].playerIdentity != null)
                return;

            MatchCells[cellValue].playerIdentity = currentPlayer;
            RpcUpdateCell(cellValue, currentPlayer);

            MatchPlayerData mpd = matchPlayerData[currentPlayer];
            mpd.currentScore = mpd.currentScore | cellValue;
            matchPlayerData[currentPlayer] = mpd;

            boardScore |= cellValue;

            if (CheckWinner(mpd.currentScore))
            {
                mpd.wins += 1;
                matchPlayerData[currentPlayer] = mpd;
                RpcShowWinner(currentPlayer);
                currentPlayer = null;
            }
            else if (boardScore == CellValue.Full)
            {
                RpcShowWinner(null);
                currentPlayer = null;
            }
            else
            {
                // Set currentPlayer SyncVar so clients know whose turn it is
                currentPlayer = currentPlayer == player1 ? player2 : player1;
            }

        }*/

        /*[ServerCallback]
        bool CheckWinner(CellValue currentScore)
        {
            if ((currentScore & CellValue.TopRow) == CellValue.TopRow)
                return true;
            if ((currentScore & CellValue.MidRow) == CellValue.MidRow)
                return true;
            if ((currentScore & CellValue.BotRow) == CellValue.BotRow)
                return true;
            if ((currentScore & CellValue.LeftCol) == CellValue.LeftCol)
                return true;
            if ((currentScore & CellValue.MidCol) == CellValue.MidCol)
                return true;
            if ((currentScore & CellValue.RightCol) == CellValue.RightCol)
                return true;
            if ((currentScore & CellValue.Diag1) == CellValue.Diag1)
                return true;
            if ((currentScore & CellValue.Diag2) == CellValue.Diag2)
                return true;

            return false;
        }*/

        /*[ClientRpc]
        public void RpcUpdateCell(CellValue cellValue, NetworkIdentity player)
        {
            MatchCells[cellValue].SetPlayer(player);
        }*/

        [Command(requiresAuthority = false)]
        public void CmdShowWinner(NetworkIdentity winner)
        {
            RpcShowWinner(winner);
        }
        [ClientRpc]
        public void RpcShowWinner(NetworkIdentity winner)
        {
            /*foreach (CellGUI cellGUI in MatchCells.Values)
                cellGUI.GetComponent<Button>().interactable = false;*/

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

        // Assigned in inspector to ReplayButton::OnClick
        [ClientCallback]
        public void RequestPlayAgain()
        {
            playAgainButton.gameObject.SetActive(false);
            CmdPlayAgain();
        }

        [Command(requiresAuthority = false)]
        public void CmdPlayAgain(NetworkConnectionToClient sender = null)
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
            /*foreach (CellGUI cellGUI in MatchCells.Values)
                cellGUI.SetPlayer(null);*/

            //boardScore = CellValue.None;

            NetworkIdentity[] keys = new NetworkIdentity[matchPlayerData.Keys.Count];
            matchPlayerData.Keys.CopyTo(keys, 0);

            foreach (NetworkIdentity identity in keys)
            {
                MatchPlayerData mpd = matchPlayerData[identity];
                mpd.currentScore = CellValue.None;
                matchPlayerData[identity] = mpd;
            }

            RpcResetPlayerPositions();
            
            RpcRestartGame();

            startingPlayer = startingPlayer == player1 ? player2 : player1;
            currentPlayer = startingPlayer;

            RpcStartCountdown();
        }

        [ClientRpc]
        public void RpcRestartGame()
        {
            /*foreach (CellGUI cellGUI in MatchCells.Values)
                cellGUI.SetPlayer(null);*/

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
