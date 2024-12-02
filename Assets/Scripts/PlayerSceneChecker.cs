using UnityEngine;

public class PlayerSceneChecker : MonoBehaviour
{
    [SerializeField] private CarController carController;
    [SerializeField] private GameObject carObject;

    public void CheckScene(bool flag)
    {
        if (carController != null)
            carController.enabled = flag;

        if (carObject != null)
            carObject.SetActive(flag);
    }
}