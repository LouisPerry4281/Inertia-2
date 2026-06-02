using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer audioMixer;

    [Header("Sound Library")]
    [SerializeField] private Sound[] sounds;

    private Dictionary<string, Sound> soundDictionary;

    private void Awake()
    {
        // Singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        soundDictionary = new Dictionary<string, Sound>();

        foreach (Sound s in sounds)
        {
            s.source = gameObject.AddComponent<AudioSource>();

            s.source.clip = s.clip;
            s.source.volume = s.volume;
            s.source.pitch = s.pitch;
            s.source.loop = s.loop;
            s.source.outputAudioMixerGroup = s.mixerGroup;

            soundDictionary.Add(s.soundName, s);
        }
    }

    public void Play(string soundName)
    {
        if (soundDictionary.TryGetValue(soundName, out Sound s))
        {
            s.source.Play();
        }
        else
        {
            Debug.LogWarning($"Sound '{soundName}' not found.");
        }
    }

    public void PlayRandom(string soundPrefix, int minIndex, int maxIndex)
    {
        if (maxIndex < minIndex)
        {
            Debug.LogWarning($"Invalid sound range for '{soundPrefix}': {minIndex} to {maxIndex}.");
            return;
        }

        int index = Random.Range(minIndex, maxIndex + 1);
        Play($"{soundPrefix} {index}");
    }

    public void Stop(string soundName)
    {
        if (soundDictionary.TryGetValue(soundName, out Sound s))
        {
            s.source.Stop();
        }
    }

    public void Pause(string soundName)
    {
        if (soundDictionary.TryGetValue(soundName, out Sound s))
        {
            s.source.Pause();
        }
    }

    public void SetMasterVolume(float volume)
    {
        audioMixer.SetFloat("MasterVolume", Mathf.Log10(volume) * 20);
    }

    public void SetMusicVolume(float volume)
    {
        audioMixer.SetFloat("MusicVolume", Mathf.Log10(volume) * 20);
    }

    public void SetSFXVolume(float volume)
    {
        audioMixer.SetFloat("SFXVolume", Mathf.Log10(volume) * 20);
    }
}
