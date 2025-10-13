using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SocialPlatforms;

public class RadioLogic : MonoBehaviour
{
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
    }

    private void Update()
    {
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
