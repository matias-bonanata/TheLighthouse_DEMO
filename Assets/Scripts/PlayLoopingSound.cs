using UnityEngine;

public class PlayLoopingSound : MonoBehaviour
{
    [SerializeField] private AudioClip soundToPlay;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!SoundManager.instance.IsSoundPlaying(soundToPlay))
        {
            SoundManager.instance.PlayWaitSoundFXClip(soundToPlay, transform, 1f);
        }
    }
}
