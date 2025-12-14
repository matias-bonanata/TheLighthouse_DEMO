using UnityEngine;

public class QtoTurnOnLight : MonoBehaviour
{
    [SerializeField] private GameObject targetObject;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            targetObject.SetActive(!targetObject.activeSelf);
        }
    }
}
