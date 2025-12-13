using UnityEngine;

public class RotateLight : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 30f;  // Degrees per second, adjustable in Inspector

    void Update()
    {
        transform.Rotate(0, rotationSpeed * Time.deltaTime, 0, Space.Self);
    }
}
