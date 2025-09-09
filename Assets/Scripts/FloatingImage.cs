using UnityEngine;

public class FloatingImage : MonoBehaviour
{
    [SerializeField] private Transform player;            // Reference to the player transform
    [SerializeField] private Transform npc;
    [SerializeField] private float floatHeight = 2f; 
    [SerializeField] private float floatAmplitude = 0.5f; // How high the object floats up and down
    [SerializeField] private float floatFrequency = 1f;   // Speed of floating up/down
    [SerializeField] private float pullStrength = 0.1f;   // How strongly the object is pulled toward the player on XZ plane

    private Vector3 basePosition;

    void Update()
    {
        if (npc == null)
            return;

        // Base position is NPC position plus fixed height offset
        basePosition = npc.position + Vector3.up * floatHeight;

        // Floating up and down on y-axis with sine wave
        float newY = basePosition.y + Mathf.Sin(Time.time * Mathf.PI * floatFrequency) * floatAmplitude;

        Vector3 horizontalPos = new Vector3(basePosition.x, 0, basePosition.z);

        if (pullStrength > 0 && player != null)
        {
            // Calculate direction from NPC to player but keep only x,z
            Vector3 directionToPlayer = player.position - npc.position;
            Vector3 horizontalPull = new Vector3(directionToPlayer.x, 0, directionToPlayer.z).normalized * pullStrength;

            horizontalPos += horizontalPull;
        }

        // Set position: horizontally above NPC (possibly pulled), y floats
        transform.position = new Vector3(horizontalPos.x, newY, horizontalPos.z);
    }

}
