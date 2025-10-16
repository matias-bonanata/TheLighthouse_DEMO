using UnityEngine;

public class TwoD_GoToCamera : MonoBehaviour
{
    public Camera mainCam;
    public float distanceFromCamera = 2f; // how far in front of the camera

    void Start()
    {
        if (mainCam == null)
            mainCam = Camera.main;
    }

    void LateUpdate()
    {
        // Always face the camera
        transform.LookAt(transform.position + mainCam.transform.rotation * Vector3.forward,
                         mainCam.transform.rotation * Vector3.up);

        // Always stay a fixed distance in front of the camera
        transform.position = mainCam.transform.position + mainCam.transform.forward * distanceFromCamera;
    }
}
