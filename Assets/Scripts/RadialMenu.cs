using UnityEngine;

public class RadialMenu : MonoBehaviour
{
    [SerializeField] private MonoBehaviour[] scriptsToToggle;
    [SerializeField] private float returnSpeed = 5f;

    private Vector3 originalLocation;
    private bool menuIsActive = false;
    private bool isReturning = false;
    private Camera mainCam;

    private void Start()
    {
        mainCam = Camera.main;

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
                // Check if clicked object has ClickableObject script
                RadialMenu clickedObject = hit.collider.GetComponent<RadialMenu>();

                if (clickedObject != null)
                {
                    // Clicked on this specific object
                    if (clickedObject == this && !menuIsActive)
                    {
                        OnObjectClicked();
                    }
                    // Clicked on a different ClickableObject while this one is active
                    else if (clickedObject != this && menuIsActive)
                    {
                        OnClickedAway();
                    }
                }
                // Clicked on something without the script while this object is active
                else if (menuIsActive)
                {
                    OnClickedAway();
                }
            }
            // Clicked on nothing while this object is active
            else if (menuIsActive)
            {
                OnClickedAway();
            }
        }

        // Smooth return to original location
        if (isReturning)
        {
            transform.position = Vector3.Lerp(
                transform.position,
                originalLocation,
                Time.deltaTime * returnSpeed
            );

            // Check if close enough to stop
            if (Vector3.Distance(transform.position, originalLocation) < 0.001f)
            {
                transform.position = originalLocation;
                isReturning = false;
            }
        }
    }

    private void OnObjectClicked()
    {
        // Save the original location
        originalLocation = transform.position;

        // Activate all scripts in the array
        SetScriptsActive(true);

        menuIsActive = true;
        isReturning = false;
    }

    private void OnClickedAway()
    {
        // Deactivate all scripts in the array
        SetScriptsActive(false);

        menuIsActive = false;
        isReturning = true;
    }

    private void SetScriptsActive(bool active)
    {
        if (scriptsToToggle == null) return;

        foreach (var script in scriptsToToggle)
        {
            if (script != null)
            {
                script.enabled = active;
            }
        }
    }
}
