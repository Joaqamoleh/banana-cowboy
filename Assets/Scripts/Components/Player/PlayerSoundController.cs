using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSoundController : MonoBehaviour
{
    [SerializeField]
    Sound[] lassoSfxs, playerSfxs;

    List<Sound> _loopedSounds = new List<Sound>();

    private void Start()
    {
        foreach (Sound s in lassoSfxs)
        {
            SetupSFX(s);
        }
        foreach (Sound s in playerSfxs)
        { 
            SetupSFX(s); 
        }
        print("Added sound components: " + GetComponent<PlayerController>());
        GetComponent<PlayerController>().OnJumpPressed += PlayerJump;
        GetComponent<PlayerController>().OnLeftGround += PlayerLeftGround;
        GetComponent<PlayerController>().OnLanded += PlayerLanded;
        GetComponent<PlayerController>().OnStateChanged += PlayerStateUpdate;
        GetComponent<PlayerController>().OnLassoStateChange += LassoStateUpdate;
        GetComponent<Health>().OnDamaged += PlayerDamaged;
    }

    private void Update()
    {
        foreach (Sound s in _loopedSounds.ToArray())
        {
            if (!s.src.isPlaying)
            {
                s.src.volume = s.volume * SoundManager.Instance().SFXVolume;
                s.src.pitch = s.pitch + UnityEngine.Random.Range(0.0f, s.pitchVariance);
                s.src.Play();
            }
        }
    }

    void SetupSFX(Sound s)
    {
        s.src = gameObject.AddComponent<AudioSource>();
        s.src.spatialBlend = 0.5f;
        s.src.clip = s.audioClip;
        s.src.volume = s.volume * SoundManager.Instance().SFXVolume;
        s.src.pitch = s.pitch;
        s.type = Sound.Type.SFX;
    }

    void PlayerJump()
    {
        PlaySFX("PlayerJump", playerSfxs);
    }

    void PlayerLeftGround()
    {
        print("callback left gorund");

        StopSFX("PlayerWalk", playerSfxs);
        StopSFX("PlayerRun", playerSfxs);
    }

    void PlayerLanded(Rigidbody hitGround)
    {
        print("callback landed");
        PlaySFX("PlayerLand", playerSfxs);
    }

    void PlayerDamaged(int damage)
    {
        PlaySFX("PlayerHurt", playerSfxs);
    }

    void PlayerStateUpdate(PlayerController.State updated)
    {
        switch (updated)
        {
            default:
            case PlayerController.State.IDLE:
                StopSFX("PlayerRun", playerSfxs);
                StopSFX("PlayerWalk", playerSfxs);
                break;
            case PlayerController.State.WALK:
                PlaySFX("PlayerWalk", playerSfxs);
                StopSFX("PlayerRun", playerSfxs);
                break;
            case PlayerController.State.RUN:
                PlaySFX("PlayerRun", playerSfxs);
                StopSFX("PlayerWalk", playerSfxs);
                break;

        }
    }
    void LassoStateUpdate(PlayerController.LassoState state)
    {
        switch(state) {
            default:
            case PlayerController.LassoState.NONE:
                StopSFX("LassoThrow", lassoSfxs);
                StopSFX("LassoSpin", lassoSfxs);
                break;
            case PlayerController.LassoState.PULL:
                break;
            case PlayerController.LassoState.THROWN:
                PlaySFX("LassoThrow", lassoSfxs);
                break;
            case PlayerController.LassoState.HOLD:
                PlaySFX("LassoSpin", lassoSfxs);
                break;
        }
    }

    void PlaySFX(string name, Sound[] sfxs)
    {
        Sound s = Array.Find(sfxs, sound => sound.name == name);
        if (s == null) { return; }
        if (!s.src.isPlaying && !PauseManager.pauseActive)
        {
            print("Playing: " + s.name);
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

    void StopSFX(string name, Sound[] sfxs)
    {
        Sound s = Array.Find(sfxs, sound => sound.name == name);
        if (s == null) { return; }
        if (s.loop)
        {
            if (Array.Find(_loopedSounds.ToArray(), sound => sound.name == name) != null)
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
