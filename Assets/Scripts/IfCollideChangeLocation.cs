using UnityEngine;

public class IfCollideChangeLocation : MonoBehaviour
{
    [Header("Teleport Parameters")]
    [SerializeField] private Transform player;          // drag the player here
    [SerializeField] private Transform teleportLocation;  // where to move the player

    [Header("Move Lighthouse Camera")]
    [SerializeField] private Transform lightHouseCamera;     // the object whose Y position to change
    [SerializeField] private float floorYPos;
    //[SerializeField] private float firstFloorYPos = -0.79f;    
    //[SerializeField] private float secondFloorYPos = 1.67f;    
    //[SerializeField] private float thirdFloorYPos = 4.48f;    
    //[SerializeField] private float fourthFloorYPos = 19.64f;    
    //[SerializeField] private float fifthFloorYPos = 22.62f;    

    [SerializeField] private FadeBlackScreen fadeScript;

    private void OnTriggerEnter(Collider other)
    {
        // Check if the thing that entered is the player
        if (other.transform == player)
        {
            CharacterController controller = player.GetComponent<CharacterController>();
            if (controller != null)
            {
                controller.enabled = false;
                player.transform.position = teleportLocation.position;
                controller.enabled = true;
            }
            else
            {
                player.transform.position = teleportLocation.position;
            }
            lightHouseCamera.transform.position = new Vector3(
                    127.82f, floorYPos, 41.72f);

            if (fadeScript != null) fadeScript.StartFadeSequence();
        }
    }
}
