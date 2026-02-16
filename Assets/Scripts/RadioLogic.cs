using PixelCrushers.DialogueSystem;
using UnityEngine;

public class RadioLogic : MonoBehaviour
{
    [Header("Sounds")]
    [SerializeField] private AudioClip staticSound;
    [SerializeField] private float staticSoundVolume;
    [SerializeField] public Transform radioSlider;
    [SerializeField] private RotatingRadioKnobs radioKnob;
    [SerializeField] public Transform volumeKnob;

    // Boat conversations and states
    [Header("Boat Conversations")]
    [SerializeField] private string conversation1; // Channel 1: Boat 1
    [SerializeField] private string conversation2; // Channel 2: Boat 2  
    [SerializeField] private string conversation3; // Channel 3: Boat 3
    [SerializeField] private string conversation4; // Channel 4: Boat 4

    [Header("Boat States")]
    public bool boat1active = false;
    public bool boat2active = false;
    public bool boat3active = false;
    public bool boat4active = false;

    // Channel states
    [SerializeField] private bool channel1Active = false;
    private bool channel2Active = false;
    private bool channel3Active = false;
    private bool channel4Active = false;

    // **NEW** - Track previous state to stop conversations
    private bool wasAnyChannelActive = false;

    private void Start()
    {
        if (volumeKnob == null)
        {
            volumeKnob = transform.Find("Volume Knob");
        }
        SoundManager.instance.PlayWaitSoundFXClip(staticSound, transform, staticSoundVolume);
    }

    private void Update()
    {
        float sliderValue = radioKnob.currentRotationX;

        // Calculate NEW channel states
        bool newChannel1Active = sliderValue >= 64f && sliderValue <= 72f && boat1active;
        bool newChannel2Active = sliderValue >= 35f && sliderValue <= 42f && boat2active;
        bool newChannel3Active = sliderValue >= -4f && sliderValue <= 2f && boat3active;
        bool newChannel4Active = sliderValue >= -52f && sliderValue <= -45f && boat4active;

        bool anyNewChannelActive = newChannel1Active || newChannel2Active || newChannel3Active || newChannel4Active;

        // **STOP CONVERSATION** when leaving channel range
        if (wasAnyChannelActive && !anyNewChannelActive)
        {
            DialogueManager.StopConversation();
            Debug.Log("Radio: Stopped conversation - switched channels");
        }

        // **START CONVERSATION** only when entering valid channel
        if (anyNewChannelActive && !wasAnyChannelActive)
        {
            if (newChannel1Active && !string.IsNullOrEmpty(conversation1))
                DialogueManager.StartConversation(conversation1);
            else if (newChannel2Active && !string.IsNullOrEmpty(conversation2))
                DialogueManager.StartConversation(conversation2);
            else if (newChannel3Active && !string.IsNullOrEmpty(conversation3))
                DialogueManager.StartConversation(conversation3);
            else if (newChannel4Active && !string.IsNullOrEmpty(conversation4))
                DialogueManager.StartConversation(conversation4);
        }

        // Update states
        channel1Active = newChannel1Active;
        channel2Active = newChannel2Active;
        channel3Active = newChannel3Active;
        channel4Active = newChannel4Active;
        wasAnyChannelActive = anyNewChannelActive;

        // Volume control
        if (volumeKnob != null)
        {
            float angleX = volumeKnob.localEulerAngles.x;
            if (angleX > 180f) angleX -= 360f;
            staticSoundVolume = anyNewChannelActive ? 0f : (80f - angleX) / 80f;
        }

        SoundManager.instance.SetVolume(staticSound, staticSoundVolume);

        // Play static if no conversations active
        if (!anyNewChannelActive)
        {
            SoundManager.instance.PlayWaitSoundFXClip(staticSound, transform, staticSoundVolume);
        }
    }
}
