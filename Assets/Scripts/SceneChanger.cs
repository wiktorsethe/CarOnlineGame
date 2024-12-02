using System.Collections;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChanger : NetworkBehaviour
{
    [Command(requiresAuthority = false)]
    public void ChangeScene(string scene, NetworkConnectionToClient conn = null)
    {
        if (conn != null)
        { 
            GameObject player = conn.identity.gameObject; // Pobieramy obiekt gracza
       
            // Unload current scene for the client
            conn.Send(new SceneMessage { sceneName = player.scene.name, sceneOperation = SceneOperation.UnloadAdditive, customHandling = true });
       
            // Ensure the scene is loaded on the server
            StartCoroutine(EnsureSceneLoadedAndMovePlayer(scene, player, conn));
        }
    }
       
    IEnumerator EnsureSceneLoadedAndMovePlayer(string scene, GameObject player, NetworkConnectionToClient conn)
    {
        Scene newScene = SceneManager.GetSceneByName(scene);
       
           // Sprawdź, czy scena jest załadowana, jeśli nie - załaduj ją
        if (!newScene.isLoaded) 
        {
            yield return SceneManager.LoadSceneAsync(scene, LoadSceneMode.Additive);
            newScene = SceneManager.GetSceneByName(scene);
        }
       
           // Przenieś gracza do nowej sceny
        if (newScene.isLoaded)
        {
            SceneManager.MoveGameObjectToScene(player, newScene);
            conn.Send(new SceneMessage { sceneName = scene, sceneOperation = SceneOperation.LoadAdditive });
       
            // Dodaj gracza do serwera w nowej scenie
            NetworkServer.AddPlayerForConnection(conn, player);
        }
    }

    /*[Command(requiresAuthority = false)]
    public void ChangeScene(string scene, NetworkConnectionToClient conn = null)
    {
        if (conn != null)
        {
            conn.Send(new SceneMessage { sceneName = this.gameObject.scene.path, sceneOperation = SceneOperation.UnloadAdditive, customHandling = true });
        
            conn.Send(new SceneMessage { sceneName = scene, sceneOperation = SceneOperation.LoadAdditive });

        }
    }*/
    
}
