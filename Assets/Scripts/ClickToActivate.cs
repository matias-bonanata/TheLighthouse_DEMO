using UnityEngine;

public class ClickToActivate : MonoBehaviour
{
    [SerializeField] private MonoBehaviour[] scriptsToActivate;
    [SerializeField] private float returnSpeed = 5f;
    [SerializeField] private bool disablesRigidbody = false;
    [SerializeField] private GameObject activateUI;

    private Vector3 originalLocation;
    private Quaternion originalRotation;
    private bool isActive = false;
    private bool isReturning = false;
    private Camera mainCam;

    private void Awake()
    {
        mainCam = Camera.main;
    }

    private void Start()
    {
        // Ensure all scripts start deactivated
        SetScriptsActive(false);
    }

    private void Update()
    {
        // Handle mouse clicks
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                // Check if clicked on this object or any of its children
                if (IsThisObjectOrChild(hit.collider.gameObject))
                {
                    if (!isActive)
                    {
                        OnObjectClicked();
                    }
                }
                // Clicked on something else while this object is active
                else if (isActive)
                {
                    OnClickedAway();
                }
            }
            // Clicked on nothing while this object is active
            else if (isActive)
            {
                OnClickedAway();
            }
        }

        // Smooth return to original location and rotation
        if (isReturning)
        {
            transform.position = Vector3.Lerp(
                transform.position,
                originalLocation,
                Time.deltaTime * returnSpeed
            );

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                originalRotation,
                Time.deltaTime * returnSpeed
            );

            // Check if close enough to stop
            if (Vector3.Distance(transform.position, originalLocation) < 0.001f &&
                Quaternion.Angle(transform.rotation, originalRotation) < 0.1f)
            {
                transform.position = originalLocation;
                transform.rotation = originalRotation;
                isReturning = false;
            }
        }
    }

    private bool IsThisObjectOrChild(GameObject clickedObject)
    {
        // Check if the clicked object is this object
        if (clickedObject == gameObject)
        {
            return true;
        }

        // Check if the clicked object is a child of this object
        Transform current = clickedObject.transform;
        while (current != null)
        {
            if (current.gameObject == gameObject)
            {
                return true;
            }
            current = current.parent;
        }

        return false;
    }

    private void OnObjectClicked()
    {
        if (disablesRigidbody == true)
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            rb.useGravity = false;
            rb.angularVelocity = Vector3.zero;
        }

        // Save the original location and rotation
        originalLocation = transform.position;
        originalRotation = transform.rotation;

        // Activate all scripts
        SetScriptsActive(true);
        if (activateUI != null) activateUI.SetActive(true);

        isActive = true;
        isReturning = false;
    }

    private void OnClickedAway()
    {
        if (disablesRigidbody == true)
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            rb.useGravity = true;
        }

        // Deactivate all scripts
        SetScriptsActive(false);
        if (activateUI != null) activateUI.SetActive(false);

        isActive = false;
        isReturning = true;
    }

    private void SetScriptsActive(bool active)
    {
        if (scriptsToActivate == null || scriptsToActivate.Length == 0)
            return;

        foreach (var script in scriptsToActivate)
        {
            if (script != null && script != this)
            {
                script.enabled = active;
            }
        }
    }
}
