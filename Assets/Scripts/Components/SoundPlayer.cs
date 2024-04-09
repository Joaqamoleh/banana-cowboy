using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

// TODO Make class to play sounds in 3D space
public class SoundPlayer : MonoBehaviour
{
    [SerializeField]
    Sound[] sfxs;

    List<Sound> _loopedSounds = new List<Sound>();

    void Start()
    {
        foreach (Sound s in sfxs)
        {
            SetupSound(s);
        }
        if (SoundManager.Instance() != null)
        {
            SoundManager.Instance().OnSFXVolumeChanged += UpdateSFXVolume;
        }
    }

    void UpdateSFXVolume(float newVolumeMult)
    {
        if (sfxs != null)
        {
            foreach (Sound s in sfxs)
            {
                if (s.src != null)
                {
                    s.src.volume = s.volume * newVolumeMult;
                }
            }
        } 
    }

    void Update()
    {
        foreach (Sound s in _loopedSounds)
        {
            if (!s.src.isPlaying)
            {
                s.src.volume = s.volume * SoundManager.Instance().SFXVolume;
                s.src.pitch = s.pitch + UnityEngine.Random.Range(0.0f, s.pitchVariance);
                s.src.Play();
            }
        }
    }

    public void PlaySFX(string name)
    {
        Sound s = System.Array.Find(sfxs, sound => sound.name == name);
        if (s == null) { return; }
        print("AHHHHHH: " + name);

        PlaySound(s);
    }

    public void StopSFX(string name)
    {
        Sound s = System.Array.Find(sfxs, sound => sound.name == name);
        if (s == null) { return; }
        StopSound(s);
    }

    public Sound GetSFX(string name)
    {
        return System.Array.Find(sfxs, sound => sound.name == name);
    }

    public void StopLoopedSounds()
    {
        _loopedSounds.Clear();
    }

    void SetupSound(Sound s)
    {
        AudioSource src = s.src;
        if (src == null)
        {
            s.src = gameObject.AddComponent<AudioSource>();
        }
        s.src.spatialBlend = s.spatialBlend;
        s.src.minDistance = s.spatialMinDist;
        s.src.maxDistance = s.spatialMaxDist;
        s.src.clip = s.audioClip;
        s.src.volume = s.volume * SoundManager.Instance().SFXVolume;
        s.src.pitch = s.pitch;
        s.type = Sound.Type.SFX;
    }

    void PlaySound(Sound s)
    {
        if (!PauseManager.pauseActive)
        {
            if (s.loop)
            {
                if (System.Array.Find(_loopedSounds.ToArray(), sound => sound.name == s.name) == null)
                {
                    _loopedSounds.Add(s);
                }
            }
            else if (s.alternatives != null && s.alternatives.Length >= 1)
            {
                int random = Random.Range(0, s.alternatives.Length + 1);
                if (random == s.alternatives.Length)
                {
                    s.src.clip = s.audioClip;
                }
                else
                {
                    s.src.clip = s.alternatives[random];
                }
                s.src.Play();
            }
            else
            {
                s.src.Play();
            }
        }
    }

    void StopSound(Sound s)
    {
        if (s.loop)
        {
            if (System.Array.Find(_loopedSounds.ToArray(), sound => sound == s) != null)
            {
                s.src.Stop();
                _loopedSounds.Remove(s);
            }
        }
        else if (s.src.isPlaying)
        {
            s.src.Stop();
        }
    }
}
