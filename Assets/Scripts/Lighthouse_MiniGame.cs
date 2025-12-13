using System;
using UnityEngine;
using System.Collections;

public class Lighthouse_MiniGame : MonoBehaviour
{
    [SerializeField] private GameObject completedCircle;
    [SerializeField] private GameObject[] objectsToRotate;   // The 3 rotating objects
    [SerializeField] private float rotationSpeed = 100f;     // Rotation degrees per second
    [SerializeField] private GameObject[] buttons;           // The 3 buttons to click
    [SerializeField] private Sprite[] normalButtonSprites;   // Normal sprites for buttons
    [SerializeField] private Sprite[] pressedButtonSprites;  // Pressed sprites for buttons
    [SerializeField] private SpriteRenderer[] buttonSpriteRenderers;

    [Header("if Win")]
    [SerializeField] private MonoBehaviour scriptToEnable;
    [SerializeField] private GameObject gameObjecToDisable;
    [SerializeField] private GameObject lightObjectsToEnable;

    private bool[] isLocked;               // Track if each object is locked


    void Start()
    {
        isLocked = new bool[objectsToRotate.Length];
        buttonSpriteRenderers = new SpriteRenderer[buttons.Length];

        for (int i = 0; i < buttons.Length; i++)
        {
            buttonSpriteRenderers[i] = buttons[i].GetComponent<SpriteRenderer>();
            if (buttonSpriteRenderers[i] != null && i < normalButtonSprites.Length)
            {
                buttonSpriteRenderers[i].sprite = normalButtonSprites[i]; // Set all buttons to normal sprite initially
            }
        }
        
    }

    void Update()
    {
        float rotationAmount = 0f;

        if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
        {
            rotationAmount = rotationSpeed * Time.deltaTime;
        }
        else if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
        {
            rotationAmount = -rotationSpeed * Time.deltaTime;
        }

        if (rotationAmount != 0f)
        {
            for (int i = 0; i < objectsToRotate.Length; i++)
            {
                if (!isLocked[i])
                {
                    objectsToRotate[i].transform.Rotate(0f, 0f, rotationAmount, Space.Self);
                }
            }
        }

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                for (int i = 0; i < buttons.Length; i++)
                {
                    if (hit.collider.gameObject == buttons[i])
                    {
                        // Clear all locks and reset sprites to normal
                        Array.Clear(isLocked, 0, isLocked.Length);
                        for (int j = 0; j < buttonSpriteRenderers.Length; j++)
                        {
                            if (buttonSpriteRenderers[j] != null && j < normalButtonSprites.Length)
                            {
                                buttonSpriteRenderers[j].sprite = normalButtonSprites[j];
                            }
                        }

                        // Toggle lock and update sprite for clicked button
                        isLocked[i] = !isLocked[i];
                        if (buttonSpriteRenderers[i] != null)
                        {
                            buttonSpriteRenderers[i].sprite = isLocked[i] ? pressedButtonSprites[i] : normalButtonSprites[i];
                        }

                        //Debug.Log($"Object {i} lock toggled: {isLocked[i]}");
                        break;
                    }
                }
            }
        }

        float tolerance = 5f; // degrees of tolerance

        Vector3 baseLocalEuler = objectsToRotate[0].transform.localEulerAngles;

        bool allMatch = true;

        for (int i = 1; i < objectsToRotate.Length; i++)
        {
            Vector3 currentLocalEuler = objectsToRotate[i].transform.localEulerAngles;

            if (Mathf.Abs(NormalizeAngle(currentLocalEuler.x) - NormalizeAngle(baseLocalEuler.x)) > tolerance ||
                Mathf.Abs(NormalizeAngle(currentLocalEuler.y) - NormalizeAngle(baseLocalEuler.y)) > tolerance ||
                Mathf.Abs(NormalizeAngle(currentLocalEuler.z) - NormalizeAngle(baseLocalEuler.z)) > tolerance)
            {
                allMatch = false;
                break;
            }
        }

        if (allMatch)
        {
            // All localEulerAngles match within tolerance
            Debug.Log("All objects have matching localEulerAngles!");
            completedCircle.SetActive(true);
            completedCircle.transform.localEulerAngles = baseLocalEuler;
            rotationSpeed = 0f;
            gameObjecToDisable.SetActive(false);
            lightObjectsToEnable.SetActive(true);
            StartCoroutine(ThreeSecondTimer());
        }
        else
        {
            completedCircle.SetActive(false);
        }

    }

    // Helper function to normalize angles between -180 and 180 degrees
    private float NormalizeAngle(float angle)
    {
        if (angle > 180f) return angle - 360f;
        return angle;
    }

    IEnumerator ThreeSecondTimer()
    {
        yield return new WaitForSeconds(3f);  // Waits exactly 3 seconds

        if (scriptToEnable != null) scriptToEnable.enabled = true;
        gameObject.SetActive(false);

        // 
    }
}
