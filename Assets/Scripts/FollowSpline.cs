using UnityEngine;
using UnityEngine.Splines;

public class SplineFollower : MonoBehaviour
{
    [SerializeField] private SplineContainer splineContainer; // Reference
    [SerializeField] private float speed = 5f;                // Speed along spline
    [SerializeField] private float progress = 0f;            // Normalised (1 = 100%)
    [SerializeField] private Vector3 rotationOffset;

    //Floating and Rocking
    [SerializeField] private float floatAmplitude = 0.5f;     // How much up/down
    [SerializeField] private float floatFrequency = 1.5f;     // how fast up/down
    [SerializeField] private float rockAmplitude = 10f;       // How much rocking
    [SerializeField] private float rockFrequency = 1f;        // Rocking speed

    void Update()
    {
        if (splineContainer == null || splineContainer.Spline == null)
            return;

        progress += speed * Time.deltaTime / splineContainer.Spline.GetLength();
        Vector3 position = splineContainer.EvaluatePosition(progress);

        //Use Sine wave
        position.y += Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;

        transform.position = position;

        Vector3 tangent = splineContainer.EvaluateTangent(progress);
        if (tangent != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(tangent);

            // Calculate rocking rotation around forward axis (roll)
            float rockAngle = Mathf.Sin(Time.time * rockFrequency) * rockAmplitude;
            Quaternion rockRotation = Quaternion.AngleAxis(rockAngle, tangent);

            // Apply rotation offset plus rocking
            Quaternion offsetRotation = Quaternion.Euler(rotationOffset);
            transform.rotation = lookRotation * rockRotation * offsetRotation;

        }
        //if (progress > 0.95f) speed = 0f;
        if (progress > 1f) progress = 1f;
    }
}