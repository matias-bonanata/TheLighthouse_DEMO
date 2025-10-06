using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager instance;
    [SerializeField] private AudioSource soundFXObject;

    // Track currently playing sounds
    private Dictionary<AudioClip, AudioSource> playingSounds = new Dictionary<AudioClip, AudioSource>();

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    public void PlaySoundFXClip(AudioClip audioClip, Transform spawnTransform, float volume)
    {
        // If this sound is already playing, stop and remove it
        if (playingSounds.TryGetValue(audioClip, out AudioSource existingSource))
        {
            existingSource.Stop();
            Destroy(existingSource.gameObject);
            playingSounds.Remove(audioClip);
        }

        // Spawn new AudioSource
        AudioSource audioSource = Instantiate(soundFXObject, spawnTransform.position, Quaternion.identity);
        audioSource.clip = audioClip;
        audioSource.volume = volume;
        audioSource.Play();

        // Add to dictionary
        playingSounds[audioClip] = audioSource;

        // Clean up after playing
        StartCoroutine(DestroyAfterPlay(audioClip, audioSource));
    }

    private System.Collections.IEnumerator DestroyAfterPlay(AudioClip clip, AudioSource source)
    {
        yield return new WaitForSeconds(source.clip.length);
        if (playingSounds.TryGetValue(clip, out AudioSource trackedSource) && trackedSource == source)
        {
            playingSounds.Remove(clip);
        }
        Destroy(source.gameObject);
    }

    public void PlayWaitSoundFXClip(AudioClip audioClip, Transform spawnTransform, float volume)
    {
        // If this sound is already playing, do nothing
        if (playingSounds.TryGetValue(audioClip, out AudioSource existingSource))
        {
            if (existingSource != null && existingSource.isPlaying)
            {
                // Sound is still playing, so don't play it again
                return;
            }
            else
            {
                // Clean up if the AudioSource is no longer valid
                playingSounds.Remove(audioClip);
            }
        }

        // Spawn new AudioSource
        AudioSource audioSource = Instantiate(soundFXObject, spawnTransform.position, Quaternion.identity);
        audioSource.clip = audioClip;
        audioSource.volume = volume;
        audioSource.Play();

        // Add to dictionary
        playingSounds[audioClip] = audioSource;

        // Clean up after playing
        StartCoroutine(DestroyAfterWaitPlay(audioClip, audioSource));
    }

    private System.Collections.IEnumerator DestroyAfterWaitPlay(AudioClip clip, AudioSource source)
    {
        yield return new WaitForSeconds(source.clip.length);
        if (playingSounds.TryGetValue(clip, out AudioSource trackedSource) && trackedSource == source)
        {
            playingSounds.Remove(clip);
        }
        Destroy(source.gameObject);
    }

    public void StopSoundFXClip(AudioClip clip)
    {
        if (playingSounds.TryGetValue(clip, out AudioSource source))
        {
            source.Stop();
            Destroy(source.gameObject);
            playingSounds.Remove(clip);
        }
    }



    //IGNORE IT


    public void PlayRandomSoundFXClip(AudioClip[] audioClips, Transform spawnTransform, float volume)
    {
        //assign random index
        int rand = Random.Range(0, audioClips.Length);

        // If this sound is already playing, do nothing
        if (playingSounds.TryGetValue(audioClips[rand], out AudioSource existingSource))
        {
            if (existingSource != null && existingSource.isPlaying)
            {
                // Sound is still playing, so don't play it again
                return;
            }
            else
            {
                // Clean up if the AudioSource is no longer valid
                playingSounds.Remove(audioClips[rand]);
            }
        }

        // Spawn new AudioSource
        AudioSource audioSource = Instantiate(soundFXObject, spawnTransform.position, Quaternion.identity);
        audioSource.clip = audioClips[rand];
        audioSource.volume = volume;
        audioSource.Play();

        // Add to dictionary
        playingSounds[audioClips[rand]] = audioSource;

        // Clean up after playing
        StartCoroutine(DestroyAfterRandomPlay(audioClips[rand], audioSource));
    }

    private System.Collections.IEnumerator DestroyAfterRandomPlay(AudioClip clip, AudioSource source)
    {
        yield return new WaitForSeconds(source.clip.length);
        if (playingSounds.TryGetValue(clip, out AudioSource trackedSource) && trackedSource == source)
        {
            playingSounds.Remove(clip);
        }
        Destroy(source.gameObject);
    }
}