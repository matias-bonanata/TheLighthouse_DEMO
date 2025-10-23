using UnityEngine;
using UnityEngine.Splines;

public class SplineFollower : MonoBehaviour
{
    [SerializeField] private SplineContainer splineContainer; // Reference to your spline
    [SerializeField] private float speed = 5f;                // Movement speed along spline
    [SerializeField] private float progress = 0f;            // Normalized position on spline (0-1)
    [SerializeField] private Vector3 rotationOffset;

    void Update()
    {
        if (splineContainer == null || splineContainer.Spline == null)
            return;

        progress += speed * Time.deltaTime / splineContainer.Spline.GetLength();
        if (progress > 1f) progress = 0f;

        Vector3 position = splineContainer.EvaluatePosition(progress);
        transform.position = position;

        Vector3 tangent = splineContainer.EvaluateTangent(progress);
        if (tangent != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(tangent);

            // Apply rotation offset
            Quaternion offsetRotation = Quaternion.Euler(rotationOffset);
            transform.rotation = lookRotation * offsetRotation;
        }
    }
}