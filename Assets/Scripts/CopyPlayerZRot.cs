using UnityEngine;

public class CopyPlayerZRot : MonoBehaviour
{
    public Transform player;  // Drag your player here
    [SerializeField] private float offset;

    private void Update()
    {
        if (player != null)
        {
            // Copy player's Y euler angle to UI's Z rotation
            Vector3 uiRotation = transform.eulerAngles;
            uiRotation.z = player.eulerAngles.y + offset;
            transform.eulerAngles = uiRotation;
        }
    }
}
