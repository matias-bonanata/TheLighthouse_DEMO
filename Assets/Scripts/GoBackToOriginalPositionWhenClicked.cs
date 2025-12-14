using UnityEngine;
using System.Collections;

public class GoBackToOriginalPositionWhenClicked : MonoBehaviour
{
    [SerializeField] private Transform targetPosition;
    [SerializeField] private float moveSpeed = 2f;

    [SerializeField] private float squashAmount = 0.8f;  // How much to squash (0.8 = 80% size)
    [SerializeField] private float stretchAmount = 1.2f; // How much to stretch (1.2 = 120% size)
    [SerializeField] private float stretchSpeed = 0.2f;

    [SerializeField] private MentalMeter mentalMeter;

    private void Start()
    {
    
    }
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit) && hit.collider.gameObject == gameObject)
            {
                StartCoroutine(MoveTowardsTarget());
            }
        }
    }

    private IEnumerator MoveTowardsTarget()
    {
        while (Vector3.Distance(transform.position, targetPosition.position) > 0.01f)
        {
            //Debug.Log("Target: " + targetPosition + " | Current: " + transform.position);
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPosition.position,
                moveSpeed * Time.deltaTime
            );
            yield return null;
        }

        mentalMeter.UpdateHealth("Recover", 2);
        mentalMeter.ChangeMentalBarColor(Color.green);

        StartSquashStretch();
    }

    public void StartSquashStretch()
    {
        StartCoroutine(AnimateSquashStretch());
    }

    private IEnumerator AnimateSquashStretch()
    {
        Vector3 originalScale = transform.localScale;

        // Phase 1: Squash (0.5 seconds)
        float time = 0f;
        while (time < stretchSpeed)
        {
            time += Time.deltaTime;
            float progress = time / stretchSpeed;

            // Squash Y, stretch X/Z
            transform.localScale = new Vector3(
                originalScale.x * Mathf.Lerp(1f, stretchAmount, progress),
                originalScale.y * Mathf.Lerp(1f, squashAmount, progress),
                originalScale.z * Mathf.Lerp(1f, stretchAmount, progress)
            );
            yield return null;
        }

        // Phase 2: Stretch back (0.5 seconds)
        time = 0f;
        while (time < stretchSpeed)
        {
            time += Time.deltaTime;
            float progress = time / stretchSpeed;

            // Return to original
            transform.localScale = Vector3.Lerp(
                transform.localScale,
                originalScale,
                progress
            );
            yield return null;
        }

        transform.localScale = originalScale; // Exact reset
        GetComponent<GoBackToOriginalPositionWhenClicked>().enabled = false;
    }


}
