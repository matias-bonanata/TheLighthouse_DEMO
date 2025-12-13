using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class NPCFloatingUI : MonoBehaviour
{
    [SerializeField] private GameObject player;
    [SerializeField] private GameObject hoverObject;      // The floating visual above the NPC
    [SerializeField] private SpriteRenderer hoverImage;            // Reference to the Image component (optional, for color manipulation)

    //ui activation and scale
    [SerializeField] public float activationDistance = 3f;
    [SerializeField] private float awayScale = 0.5f;
    private bool isPlayerColliding = false;

    //rotation when press e
    [SerializeField] private float rotationSpeed = 200f; // degrees per second
    private bool isSpinning = false;
    [SerializeField] private bool canSpin = false;
    private Coroutine spinCoroutine;

    private void Start()
    {
    }

    private void Update()
    {
        // Distance check
        float dist = Vector3.Distance(player.transform.position, transform.position);
        bool shouldShow = dist < activationDistance;

        // Activate/deactivate hovering object based on distance
        hoverObject.SetActive(shouldShow);

        if (shouldShow)
        {
            // Set visual state based on collision
            if (isPlayerColliding)
            {
                if (!isSpinning) //turn normal when close
                {
                    SetImageColor(Color.white, 1f);
                    hoverObject.transform.localScale = new Vector3(1f, 1f, 1f);
                }

                //SPINNING
                if (Input.GetKeyDown(KeyCode.E) && !isSpinning && canSpin == true)
                {
                    isSpinning = true;
                    spinCoroutine = StartCoroutine(Spin());
                }

                // Stop spinning when releasing E
                if (Input.GetKeyUp(KeyCode.E))
                {
                    hoverObject.transform.rotation = Quaternion.identity;
                    isSpinning = false;
                    if (spinCoroutine != null)
                        StopCoroutine(spinCoroutine);
                }
            }
            else
            {
                //if not show
                Color grayHalfTransparent = new Color(0.5f, 0.5f, 0.5f, 0.5f); // Grayscale, alpha 50%
                SetImageColor(grayHalfTransparent, 0.5f);
                hoverObject.transform.localScale = new Vector3((awayScale / dist), 
                    awayScale / dist, awayScale / dist);

                //SPINNING
                isSpinning = false;
                if (spinCoroutine != null)
                    StopCoroutine(spinCoroutine);
                //go back to normal
                hoverObject.transform.rotation = Quaternion.identity;
            }
        }


    }

    // Collider methods
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == player)
        {
            isPlayerColliding = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject == player)
        {
            isPlayerColliding = false;
            hoverObject.transform.rotation = Quaternion.identity; //go back to normal
        }
    }

    // Helper to set image color and alpha
    private void SetImageColor(Color color, float alpha)
    {
        if (hoverImage != null)
        {
            color.a = alpha;
            hoverImage.color = color;
        }
    }

    //SPINNING
    private IEnumerator Spin()
    {
        float fadeDuration = 1.8f; // match with hold time in other script
        float elapsed = 0f;
        Color originalColor = hoverImage.color;

        while (isSpinning)
        {
            // Rotate the hoverObject
            hoverObject.transform.Rotate(0, 0, -rotationSpeed * Time.deltaTime);

            // Gradually reduce alpha while spinning (clamp to minimum 0)
            if (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
                Color c = hoverImage.color;
                c.a = alpha;
                hoverImage.color = c;
            }

            yield return null; // wait one frame
        }

        // Reset alpha back when stop spinning
        hoverImage.color = originalColor;
        isSpinning = false;
    }
}
