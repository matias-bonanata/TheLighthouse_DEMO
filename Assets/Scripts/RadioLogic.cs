using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SocialPlatforms;

public class RadioLogic : MonoBehaviour
{
    [Header("Camera Preferences")]
    [SerializeField] private Camera mainCamera; // Assign your camera in inspector or find in Start
    [SerializeField] private float smoothSpeed = 5f; // Speed of the smooth movement 
    [SerializeField] private float distanceInFront = 2f; // Distance to keep in front of the camera
    [SerializeField] private float  rotationX = 0f; // Speed of the smooth movement 
    [SerializeField] private float  rotationY = -90f; // Speed of the smooth movement 
    [SerializeField] private float  rotationZ = 0.5f; // Speed of the smooth movement 


    [Header("Sounds")]
    [SerializeField] private AudioClip song1;
    [SerializeField] private AudioClip staticSound;
    [SerializeField] private float staticSoundVolume;
    [SerializeField] private float song1Volume;
    [SerializeField] public Transform radioSlider;
    [SerializeField] public Transform volumeKnob;

    private bool song1isPlaying = false;

    private void Start()
    {
        if (volumeKnob == null)
        {
            //volumeKnob = GetComponent<Transform>();
            volumeKnob = transform.Find("Volume Knob"); //force volumeknob
        }

        //Play sound at start
        SoundManager.instance.PlayWaitSoundFXClip(song1, transform, song1Volume);
        SoundManager.instance.PlayWaitSoundFXClip(staticSound, transform, staticSoundVolume);

        // If no camera assigned, use the main camera by default
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }

    private void Update()
    {
        //-----------------
        //RADIO GO TO CAMERA
        //-----------------

        //Rotate Radio
        Vector3 targetPosition = mainCamera.transform.position + mainCamera.transform.forward * distanceInFront;
        transform.position = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);

        // Calculate the target rotation so the object faces the camera but offsets the camera's 16° X rotation
        Quaternion cameraRotation = mainCamera.transform.rotation;

        //Do movement
        Quaternion editMovement = Quaternion.Euler(rotationX, rotationY, rotationZ);

        // Apply the compensation so object rotation is camera rotation minus that 16 degrees on X-axis
        Quaternion targetRotation = cameraRotation * editMovement;

        // Smooth rotate to the adjusted target rotation
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, smoothSpeed * Time.deltaTime);

        //-----------------
        //RADIO MANAGEMENT
        //-----------------

        //Channel Switch
        if (radioSlider != null &&
            radioSlider.localPosition.z > 0.23f && radioSlider.localPosition.z < 0.25f)
        {
            song1isPlaying = true;
        }
        else
        {
            song1isPlaying = false;
        }

        //Volume Knob
        if (volumeKnob != null)
        {
            float angleX = volumeKnob.transform.localEulerAngles.x;
            if (angleX > 180f)
            {
                angleX -= 360f;  // Convert to -180 to 180 range
            }

            if (song1isPlaying)
            {
                song1Volume = (80f - angleX) / 80f; //if playing song, change sound
                staticSoundVolume = 0f;
            }
            else 
            { 
                song1Volume = 0f;
                staticSoundVolume = (80f - angleX) / 80f;
            }
        }

        SoundManager.instance.SetVolume(song1, song1Volume);
        SoundManager.instance.SetVolume(staticSound, staticSoundVolume);

        //Play static if not playing
        if (!SoundManager.instance.IsSoundPlaying(staticSound))
        {
            SoundManager.instance.PlayWaitSoundFXClip(staticSound, transform, staticSoundVolume);
        }

        if (!SoundManager.instance.IsSoundPlaying(song1))
        {
            SoundManager.instance.PlayWaitSoundFXClip(song1, transform, staticSoundVolume);
        }

    }
}
