using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        Sound s = Array.Find(sfxs, sound => sound.name == name);
        if (s == null) { return; }
        PlaySound(s);
    }

    public void StopSFX(string name)
    {
        Sound s = Array.Find(sfxs, sound => sound.name == name);
        if (s == null) { return; }
        StopSound(s);
    }

    void SetupSound(Sound s)
    {
        s.src = gameObject.AddComponent<AudioSource>();
        s.src.spatialBlend = s.isSpatial ? 1.0f : 0.0f;
        if (s.isSpatial)
        {
            s.src.maxDistance = s.spatialSoundTravelDistance;
        }
        s.src.clip = s.audioClip;
        s.src.volume = s.volume * SoundManager.Instance().SFXVolume;
        s.src.pitch = s.pitch;
        s.type = Sound.Type.SFX;
    }

    void PlaySound(Sound s)
    {
        if (!s.src.isPlaying && !PauseManager.pauseActive)
        {
            if (s.loop)
            {
                if (Array.Find(_loopedSounds.ToArray(), sound => sound.name == name) == null)
                {
                    _loopedSounds.Add(s);
                }
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
            if (Array.Find(_loopedSounds.ToArray(), sound => sound == s) != null)
            {
                _loopedSounds.Remove(s);
            }
        }
        else if (s.src.isPlaying)
        {
            s.src.Stop();
        }
    }
}
