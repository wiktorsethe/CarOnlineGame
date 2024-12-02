using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using Mirror;
using UnityEngine;
using UnityEngine.Networking.Types;
using UnityEngine.SceneManagement;

[RequireComponent (typeof (NetworkMatch))]
    public class Player : NetworkBehaviour {

        public static Player localPlayer;
        [SyncVar] public string matchID;
        [SyncVar] public int playerIndex;

        NetworkMatch networkMatch;

        [SyncVar] public Match currentMatch;

        [SerializeField] GameObject playerLobbyUI;

        Guid netIDGuid;
        
        public string destinationScene;
        
        public CinemachineVirtualCamera virtualCamera;
        void Awake () {
            networkMatch = GetComponent<NetworkMatch> ();
            virtualCamera = FindObjectOfType<CinemachineVirtualCamera>();
        }

        public override void OnStartServer () {
            netIDGuid = netId.ToString ().ToGuid ();
            networkMatch.matchId = netIDGuid;
        }

        public override void OnStartClient () {
            if (isLocalPlayer) {
                localPlayer = this;
            } else {
                Debug.Log ($"Spawning other player UI Prefab");
                playerLobbyUI = UILobby.instance.SpawnPlayerUIPrefab (this);
            }
        }
        
        public override void OnStopClient () {
            Debug.Log ($"Client Stopped");
            ClientDisconnect ();
        }

        public override void OnStopServer () {
            Debug.Log ($"Client Stopped on Server");
            ServerDisconnect ();
        }

        public override void OnStartLocalPlayer()
        {
            if (virtualCamera != null)
            {
                virtualCamera.Follow = transform;
            }
        }

        /* 
            HOST MATCH
        */

        public void HostGame (bool publicMatch) {
            string matchID = MatchMaker.GetRandomMatchID ();
            CmdHostGame (matchID, publicMatch);
        }

        [Command]
        void CmdHostGame (string _matchID, bool publicMatch) {
            matchID = _matchID;
            if (MatchMaker.instance.HostGame (_matchID, this, publicMatch, out playerIndex)) {
                Debug.Log ($"<color=green>Game hosted successfully</color>");
                networkMatch.matchId = _matchID.ToGuid ();
                TargetHostGame (true, _matchID, playerIndex);
            } else {
                Debug.Log ($"<color=red>Game hosted failed</color>");
                TargetHostGame (false, _matchID, playerIndex);
            }
        }

        [TargetRpc]
        void TargetHostGame (bool success, string _matchID, int _playerIndex) {
            playerIndex = _playerIndex;
            matchID = _matchID;
            Debug.Log ($"MatchID: {matchID} == {_matchID}");
            UILobby.instance.HostSuccess (success, _matchID);
        }

        /* 
            JOIN MATCH
        */

        public void JoinGame (string _inputID) {
            CmdJoinGame (_inputID);
        }

        [Command]
        void CmdJoinGame (string _matchID) {
            matchID = _matchID;
            if (MatchMaker.instance.JoinGame (_matchID, this, out playerIndex)) {
                Debug.Log ($"<color=green>Game Joined successfully</color>");
                networkMatch.matchId = _matchID.ToGuid ();
                TargetJoinGame (true, _matchID, playerIndex);

                //Host
                if (isServer && playerLobbyUI != null) {
                    playerLobbyUI.SetActive (true);
                }
            } else {
                Debug.Log ($"<color=red>Game Joined failed</color>");
                TargetJoinGame (false, _matchID, playerIndex);
            }
        }

        [TargetRpc]
        void TargetJoinGame (bool success, string _matchID, int _playerIndex) {
            playerIndex = _playerIndex;
            matchID = _matchID;
            Debug.Log ($"MatchID: {matchID} == {_matchID}");
            UILobby.instance.JoinSuccess (success, _matchID);
        }

        /* 
            DISCONNECT
        */

        public void DisconnectGame () {
            CmdDisconnectGame ();
        }

        [Command]
        void CmdDisconnectGame () {
            ServerDisconnect ();
        }

        void ServerDisconnect () {
            MatchMaker.instance.PlayerDisconnected (this, matchID);
            RpcDisconnectGame ();
            networkMatch.matchId = netIDGuid;
        }

        [ClientRpc]
        void RpcDisconnectGame () {
            ClientDisconnect ();
        }

        void ClientDisconnect () {
            if (playerLobbyUI != null) {
                if (!isServer) {
                    Destroy (playerLobbyUI);
                } else {
                    playerLobbyUI.SetActive (false);
                }
            }
        }

        /* 
            SEARCH MATCH
        */

        public void SearchGame () {
            CmdSearchGame ();
        }

        [Command]
        void CmdSearchGame () {
            if (MatchMaker.instance.SearchGame (this, out playerIndex, out matchID)) {
                Debug.Log ($"<color=green>Game Found Successfully</color>");
                networkMatch.matchId = matchID.ToGuid ();
                TargetSearchGame (true, matchID, playerIndex);

                //Host
                if (isServer && playerLobbyUI != null) {
                    playerLobbyUI.SetActive (true);
                }
            } else {
                Debug.Log ($"<color=red>Game Search Failed</color>");
                TargetSearchGame (false, matchID, playerIndex);
            }
        }

        [TargetRpc]
        void TargetSearchGame (bool success, string _matchID, int _playerIndex) {
            playerIndex = _playerIndex;
            matchID = _matchID;
            Debug.Log ($"MatchID: {matchID} == {_matchID} | {success}");
            UILobby.instance.SearchGameSuccess (success, _matchID);
        }

        /* 
            MATCH PLAYERS
        */

        [Server]
        public void PlayerCountUpdated (int playerCount) {
            TargetPlayerCountUpdated (playerCount);
        }

        [TargetRpc]
        void TargetPlayerCountUpdated (int playerCount) {
            if (playerCount > 1) {
                UILobby.instance.SetStartButtonActive(true);
            } else {
                //UILobby.instance.SetStartButtonActive(false);
            }
        }

        /* 
            BEGIN MATCH
        */

        public void BeginGame () {
            //sceneChanger.ChangeScene("Game");
            CmdBeginGame ();
        }

        [Command]
        void CmdBeginGame () {
            MatchMaker.instance.BeginGame (matchID);
            Debug.Log ($"<color=red>Game Beginning</color>");
        }

        public void StartGame()
        {
            if (!isServer)
            {
                Debug.LogError("StartGame może być wywołane tylko na serwerze!");
                return;
            }

            if (!TryGetComponent<NetworkIdentity>(out var identity))
            {
                Debug.LogError("Brak NetworkIdentity w obiekcie Player!");
                return;
            }

            var conn = identity.connectionToClient;
            if (conn != null)
            {
                TargetBeginGame(); // Wywołanie TargetRpc bez parametru
            }
            else
            {
                Debug.LogError("Nie znaleziono połączenia klienta dla tego gracza.");
            }
        }

        void TargetBeginGame()
        {
            Debug.Log($"MatchID: {matchID} | Rozpoczęcie gry");
            StartCoroutine(SendPlayerToNewScene(gameObject));
        }
        
        [ServerCallback]
        IEnumerator SendPlayerToNewScene(GameObject player)
        {
            Debug.Log("STARTED TO CHANGE SCENE!");
            
            if (!player.TryGetComponent(out NetworkIdentity identity))
            {
                Debug.LogError("Player does not have a NetworkIdentity!");
                yield break;
            }

            NetworkConnectionToClient conn = identity.connectionToClient;
            if (conn == null)
            {
                Debug.LogError("NO CONN!");
                yield break;
            }

            string currentScene = SceneManager.GetActiveScene().path;

            Debug.Log($"Przenoszenie gracza z {currentScene} do {destinationScene}");

            // Wyślij polecenie rozładowania aktualnej sceny do klienta
            conn.Send(new SceneMessage { sceneName = player.scene.name, sceneOperation = SceneOperation.UnloadAdditive, customHandling = true });

            yield return new WaitForSeconds(1f);
            
            //NetworkServer.RemovePlayerForConnection(conn, RemovePlayerOptions.Unspawn);

            // Sprawdź, czy docelowa scena jest już załadowana na serwerze
            Scene newScene = SceneManager.GetSceneByPath(destinationScene);
            if (!newScene.isLoaded)
            {
                Debug.Log($"Ładowanie sceny {destinationScene} na serwerze...");
                yield return SceneManager.LoadSceneAsync(destinationScene, LoadSceneMode.Additive);
                newScene = SceneManager.GetSceneByPath(destinationScene);
            }

            // Przenieś obiekt gracza do nowej sceny na serwerze
            if (newScene.isLoaded)
            {
                SceneManager.MoveGameObjectToScene(player, newScene);
                Debug.Log($"Gracz przeniesiony do sceny {destinationScene} na serwerze.");
            }
            else
            {
                Debug.LogError($"Nie udało się załadować sceny {destinationScene} na serwerze.");
                yield break;
            }

            // Wyślij polecenie załadowania nowej sceny do klienta
            conn.Send(new SceneMessage { sceneName = destinationScene, sceneOperation = SceneOperation.LoadAdditive, customHandling = true });

            // Dodaj gracza z powrotem do serwera w nowej scenie
            //NetworkServer.AddPlayerForConnection(conn, player);
        }
    }
