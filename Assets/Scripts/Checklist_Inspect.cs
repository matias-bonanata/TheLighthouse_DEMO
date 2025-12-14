using UnityEngine;
using System.Collections;

public class Checklist_Inspect : MonoBehaviour
{
    // Arrays of objects and their renderers to toggle
    [SerializeField] private GameObject[] targetObjects;
    [SerializeField] private Renderer[] targetRenderers;

    [Header("Should Fade")]
    [SerializeField] private FadeBlackScreen fadeScript;

    [SerializeField] private AudioClip ambientSound;

    void Start()
    {
        // Initialize the renderer array and disable all renderers to start invisible
        targetRenderers = new Renderer[targetObjects.Length];
        for (int i = 0; i < targetObjects.Length; i++)
        {
            if (targetObjects[i] != null)
            {
                targetRenderers[i] = targetObjects[i].GetComponent<Renderer>();
                if (targetRenderers[i] != null)
                {
                    targetRenderers[i].enabled = false;
                }
            }
        }

        StartCoroutine(LoadNextSceneDelayed());
    }

    void Update()
    {
        if (!SoundManager.instance.IsSoundPlaying(ambientSound))
        {
            SoundManager.instance.PlayWaitSoundFXClip(ambientSound, transform, 1f);
        }

        if (Input.GetMouseButtonDown(0)) // Left mouse click
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                // Check if clicked object's collider matches any target object
                for (int i = 0; i < targetObjects.Length; i++)
                {
                    if (hit.collider.gameObject == targetObjects[i])
                    {
                        if (targetRenderers[i] != null)
                        {
                            targetRenderers[i].enabled = !targetRenderers[i].enabled; // Toggle visibility
                        }
                        break; // Exit loop after toggling
                    }
                }
            }
        }
    }

    private IEnumerator LoadNextSceneDelayed()
    {
        yield return new WaitForSeconds(60f);
        //Debug.Log("fade");
        if (fadeScript != null) fadeScript.StartFadeSequence();
        yield return new WaitForSeconds(0.5f);
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex + 1);
    }
}
