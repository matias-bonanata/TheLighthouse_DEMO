using UnityEngine;

public class CrabSpawner : MonoBehaviour
{
    [SerializeField] private GameObject[] crabPrefabs;  // Your array of crab objects

    public void SpawnCrabs(int amount)
    {
        // Find all inactive crabs first
        System.Collections.Generic.List<GameObject> inactiveCrabs = new System.Collections.Generic.List<GameObject>();

        foreach (GameObject crab in crabPrefabs)
        {
            if (!crab.activeInHierarchy)
            {
                inactiveCrabs.Add(crab);
            }
        }

        // Activate the requested amount (or fewer if not enough inactive crabs)
        for (int i = 0; i < amount && i < inactiveCrabs.Count; i++)
        {
            inactiveCrabs[i].SetActive(true);
        }
    }
}
