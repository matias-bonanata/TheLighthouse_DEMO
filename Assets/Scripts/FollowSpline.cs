using UnityEngine;
using UnityEngine.Splines;

public class SplineFollower : MonoBehaviour
{
    [SerializeField] private SplineContainer splineContainer; // Reference to your spline
    [SerializeField] private float speed = 5f;                // Movement speed along spline
    [SerializeField] private float progress = 0f;            // Normalized position on spline (0-1)
    [SerializeField] private Vector3 rotationOffset;

    // Floating/rocking parameters
    [SerializeField] private float floatAmplitude = 0.5f;     // Amplitude of up/down float
    [SerializeField] private float floatFrequency = 1.5f;     // Speed of up/down float
    [SerializeField] private float rockAmplitude = 10f;       // Degrees to rock side to side
    [SerializeField] private float rockFrequency = 1f;        // Speed of rocking

    void Update()
    {
        if (splineContainer == null || splineContainer.Spline == null)
            return;

        progress += speed * Time.deltaTime / splineContainer.Spline.GetLength();
        if (progress > 1f) progress = 0f;

        Vector3 position = splineContainer.EvaluatePosition(progress);

        // Add up and down floating motion using sine wave
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
    }
}