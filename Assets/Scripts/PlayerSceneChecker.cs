using Mirror;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerSceneChecker : NetworkBehaviour
{
    [SyncVar(hook = nameof(HandleDisplayColorChange))] [SerializeField] private bool isCarVisible;
    
    [SerializeField] private GameObject carSpriteObject;

    private void OnEnable()
    {
        Debug.Log("Subscribing to sceneLoaded");
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        Debug.Log("Unsubscribing from sceneLoaded");
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"Scene loaded: {scene.name}, Mode: {mode}");

        // Jeśli używasz addytywnego ładowania, sprawdź wszystkie załadowane sceny
        if (mode == LoadSceneMode.Additive)
        {
            Debug.Log("Checking all loaded scenes...");
            CheckAllScenes();
        }
        else
        {
            // Jeśli scena nie jest ładowana addytywnie, wystarczy sprawdzić tę jedną
            CheckScene(scene);
        }
    }

    private void CheckAllScenes()
    {
        // Przejrzyj wszystkie załadowane sceny
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            Debug.Log($"Checking scene: {scene.name}");

            // Dostosuj logikę dla każdej sceny
            CheckScene(scene);
        }
    }
    
    private void CheckScene(Scene scene)
    {
        Debug.Log($"Handling scene: {scene.name}");

        if (scene.name == "Game")
        {
            Debug.Log("Game Scene detected, enabling carSpriteObject");
            SetCarVisibility(true);
            foreach (var obj in scene.GetRootGameObjects())
            {
                RaceController raceController = obj.GetComponent<RaceController>();
                if (raceController != null)
                {
                    raceController.RegisterPlayer(gameObject);
                    Debug.Log(raceController + " registered: " + gameObject);
                }
            }
        }
        else if (scene.name == "Lobby")
        {
            Debug.Log("Lobby Scene detected, disabling carSpriteObject");
            SetCarVisibility(false);
        }
    } 

    [Command(requiresAuthority = false)]
    private void SetCarVisibility(bool isVisible)
    {
        isCarVisible = isVisible;
    }
    
    private void HandleDisplayColorChange(bool oldVisibility, bool newVisibility)
    {
        carSpriteObject.SetActive(newVisibility);
    }
}