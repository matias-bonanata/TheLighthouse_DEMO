using UnityEngine;
using System.Collections;

public class DebrisPuddle_Spawner : MonoBehaviour
{
    [SerializeField] private GameObject[] childObjects;

    private void Start()
    {
        if (childObjects != null)
        {
            // Get all child game objects
            int childCount = transform.childCount;
            childObjects = new GameObject[childCount];
            for (int i = 0; i < childCount; i++)
            {
                childObjects[i] = transform.GetChild(i).gameObject;
                childObjects[i].SetActive(false); // initially set all not active
            }

            StartCoroutine(ActivateRandomChildRoutine());
        }
        
    }

    private IEnumerator ActivateRandomChildRoutine()
    {
        while (true)
        {
            if (childObjects != null)
            {
                // Pick a random debris
                int randomIndex = Random.Range(0, childObjects.Length);
                childObjects[randomIndex].SetActive(true);

                // Wait for 10 seconds before activating another
                yield return new WaitForSeconds(2f);
            }
        }
    }
}
