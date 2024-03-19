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
        PlayerController pc = GetComponent<PlayerController>();
        pc.OnJumpPressed += PlayerJump;
        pc.OnLeftGround += PlayerLeftGround;
        pc.OnLanded += PlayerLanded;
        pc.OnStateChanged += PlayerStateUpdate;
        pc.OnLassoStateChange += LassoStateUpdate;
        pc.OnLassoTossed += LassoToss;
        GetComponent<Health>().OnDamaged += PlayerDamaged;
    }

    private void Update()
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
        StopSFX("PlayerWalk", playerSfxs);
        StopSFX("PlayerRun", playerSfxs);
    }

    void PlayerLanded(Rigidbody hitGround)
    {
        PlaySFX("PlayerLand", playerSfxs);
    }

    void PlayerDamaged(int damage, bool hasDied)
    {
        if (hasDied)
        {
            PlaySFX("PlayerDeath", playerSfxs);
        }
        else
        {
            PlaySFX("PlayerHurt", playerSfxs);
        }
    }

    void LassoToss(LassoTossable.TossStrength strength)
    {
        if (strength == LassoTossable.TossStrength.WEAK)
        {
            PlaySFX("LassoWeakThrow", lassoSfxs);
        }
        else if (strength == LassoTossable.TossStrength.MEDIUM)
        {
            PlaySFX("LassoMediumThrow", lassoSfxs);
        }
        else
        {
            PlaySFX("LassoStrongThrow", lassoSfxs);
        }
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
        switch (state)
        {
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
