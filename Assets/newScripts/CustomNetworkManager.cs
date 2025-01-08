using System.Collections;
using System.IO;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CustomNetworkManager : NetworkManager
    {
        public string startScene;
        public string targetScene;
    
        private string[] scenesToLoad;
        private bool subscenesLoaded;
        /*private readonly List<Scene> subScenes = new List<Scene>();*/

        private bool isInTransition;
        private bool firstSceneLoaded;

        public GameObject canvas;
        public CanvasController canvasController;
        
        public override void Awake()
        {
            base.Awake();
            canvasController.InitializeData();
        }
        private void Start()
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) 
            {
                // Automatyczny start serwera w trybie headless
                StartServer();
            }
        
            int sceneCount = SceneManager.sceneCountInBuildSettings - 2; //Subtract the offline and persistent scene
            scenesToLoad = new string[sceneCount];

            for (int i = 0; i < sceneCount; i++)
            {
                scenesToLoad[i] = Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(i + 2));
            }
        }
        
        public override void OnClientConnect()
        {
            base.OnClientConnect();
            canvasController.OnClientConnect();
        }
        
        public override void OnClientDisconnect()
        {
            canvasController.OnClientDisconnect();
            base.OnClientDisconnect();
        }
        
        public override void OnStartServer()
        {
            if (mode == NetworkManagerMode.ServerOnly)
                canvas.SetActive(true);

            canvasController.OnStartServer();
        }
        
        public override void OnStartClient()
        {
            canvas.SetActive(true);
            canvasController.OnStartClient();
        }
        
        public override void OnStopServer()
        {
            canvasController.OnStopServer();
            canvas.SetActive(false);
        }
        
        public override void OnStopClient()
        {
            canvasController.OnStopClient();
        }
    
        public override void OnServerSceneChanged(string sceneName)
        {
            base.OnServerSceneChanged(sceneName);
            if (sceneName == onlineScene)
            {
                StartCoroutine(ServerLoadSubScene());
            }
        }

        public override void OnClientSceneChanged()
        {
            if (isInTransition == false)
            {
                base.OnClientSceneChanged();
                StartCoroutine(CheckScenesForTargetDelayed());
            }
        }

        IEnumerator ServerLoadSubScene()
        {
            foreach (var additiveScene in scenesToLoad)
            {
                yield return SceneManager.LoadSceneAsync(additiveScene, new LoadSceneParameters
                {
                    loadSceneMode = LoadSceneMode.Additive,
                    localPhysicsMode = LocalPhysicsMode.Physics2D
                });
            }

            subscenesLoaded = true;
        }

        public override void OnClientChangeScene(string sceneName, SceneOperation sceneOperation, bool customHandling)
        {
            if (sceneOperation == SceneOperation.UnloadAdditive)
                StartCoroutine(UnloadAdditive(sceneName));
        
            if (sceneOperation == SceneOperation.LoadAdditive)
                StartCoroutine(LoadAdditive(sceneName));
        }
    
        IEnumerator LoadAdditive(string sceneName)
        {
            isInTransition = true;

            if (mode == NetworkManagerMode.ClientOnly)
            {
                loadingSceneAsync = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

                while (loadingSceneAsync != null && !loadingSceneAsync.isDone)
                {
                    yield return null;
                }
            }

            NetworkClient.isLoadingScene = false;
            isInTransition = false;
        
            OnClientSceneChanged();

            if (firstSceneLoaded == false)
            {
                firstSceneLoaded = true;
                yield return new WaitForSeconds(0.6f);
            }
            else
            {
                firstSceneLoaded = true;
            
                yield return new WaitForSeconds(0.5f);
            }
        }

        IEnumerator UnloadAdditive(string sceneName)
        {
            isInTransition = true;

            if (mode == NetworkManagerMode.ClientOnly)
            {
                yield return SceneManager.UnloadSceneAsync(sceneName);
                yield return Resources.UnloadUnusedAssets();
            }

            NetworkClient.isLoadingScene = false;
            isInTransition = false;
        
            OnClientSceneChanged();
        }

        public override void OnServerReady(NetworkConnectionToClient conn)
        {
            base.OnServerReady(conn);
            if (conn.identity == null)
                StartCoroutine(AddPlayerDelayed(conn));
            
            canvasController.OnServerReady(conn);
        }

        IEnumerator AddPlayerDelayed(NetworkConnectionToClient conn)
        {
            while (subscenesLoaded == false)
                yield return null;

            NetworkIdentity[] allObjWithNetworkIdentity = FindObjectsOfType<NetworkIdentity>();

            foreach (var item in allObjWithNetworkIdentity)
            {
                item.enabled = true;
            }

            firstSceneLoaded = false;
        
            conn.Send(new SceneMessage{ sceneName = startScene, sceneOperation = SceneOperation.LoadAdditive, customHandling = true});

            Transform startPos = GetStartPosition();

            GameObject player = Instantiate(playerPrefab, startPos);
            player.transform.SetParent(null);
        
            yield return new WaitForEndOfFrame();
        
            SceneManager.MoveGameObjectToScene(player, SceneManager.GetSceneByName(startScene));

            NetworkServer.AddPlayerForConnection(conn, player);
        }
    
        private IEnumerator CheckScenesForTargetDelayed()
        {
            // Czekamy aż NetworkClient.localPlayer będzie dostępny
            while (NetworkClient.localPlayer == null)
            {
                yield return null; // Czekamy, aż gracz zostanie dodany
            }

            CheckScenesForTarget(); // TODO: PO CO TO??????
        }
    
        // Nowa funkcja do sprawdzania scen
        private void CheckScenesForTarget()
        {
            bool targetSceneLoaded = false;

            // Sprawdzamy wszystkie załadowane sceny
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                string sceneName = SceneManager.GetSceneAt(i).name;
                if (sceneName == targetScene)
                {
                    targetSceneLoaded = true;
                    break; // Jeśli znajdziemy scenę, zatrzymujemy dalsze sprawdzanie
                }
            }
        }
        
        public override void OnServerDisconnect(NetworkConnectionToClient conn)
        {
            StartCoroutine(DoServerDisconnect(conn));
        }

        IEnumerator DoServerDisconnect(NetworkConnectionToClient conn)
        {
            yield return canvasController.OnServerDisconnect(conn);
            base.OnServerDisconnect(conn);
        }
    }
