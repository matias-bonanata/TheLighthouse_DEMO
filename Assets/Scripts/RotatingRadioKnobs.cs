using UnityEngine;

public class RotatingRadioKnobs : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 5f;
    private bool isDragging = false;
    private Vector3 lastMousePosition;
    public float currentRotationX;

    public Transform radioSlider;

    private void Start()
    {
        // Store initial X rotation on start
        currentRotationX = transform.localEulerAngles.x;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.collider == GetComponent<Collider>())
                {
                    isDragging = true;
                    lastMousePosition = Input.mousePosition;
                }
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }

        if (isDragging)
        {
            Vector3 currentMousePosition = Input.mousePosition;
            float deltaX = currentMousePosition.x - lastMousePosition.x;

            // Update the rotation only on the X axis
            currentRotationX += -deltaX * rotationSpeed * Time.deltaTime;
            currentRotationX = Mathf.Clamp(currentRotationX, -80f, 80f);

            // Apply the rotation locking Y and Z angles
            transform.localEulerAngles = new Vector3(currentRotationX, transform.localEulerAngles.y, transform.localEulerAngles.z);

            float normalized = (currentRotationX + 80f) / 160f; // 0 to 1
            float otherZ = Mathf.Lerp(0.33f, -0.33f, normalized);

            if (radioSlider != null)
            {
                //Affect other rotation
                Vector3 otherPos = radioSlider.localPosition;
                otherPos.z = -otherZ;
                radioSlider.localPosition = otherPos;
            }

            lastMousePosition = currentMousePosition;
        }
    }
}
