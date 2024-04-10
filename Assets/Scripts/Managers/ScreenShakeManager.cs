using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScreenShakeManager : MonoBehaviour
{
    private CinemachineBasicMultiChannelPerlin cinCamera;
    private float _shakeTimer;
    private float _shakeTimerTotal;
    private float _startingIntensity;

    public static ScreenShakeManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        cinCamera = GameObject.Find("Third Person Camera").GetComponentInChildren<CinemachineBasicMultiChannelPerlin>();
    }

    public void ShakeCamera(float intensity, float frequency, float time)
    {
        print("called");
        _shakeTimerTotal = time;
        cinCamera.m_AmplitudeGain = intensity;
        cinCamera.m_FrequencyGain = frequency;
        _startingIntensity = intensity;
        _shakeTimer = time;
    }

    private void Update()
    {
        if (_shakeTimer > 0)
        {
            _shakeTimer -= Time.deltaTime;
            cinCamera.m_AmplitudeGain = Mathf.Lerp(_startingIntensity, 0f, 1 - (_shakeTimer / _shakeTimerTotal));
            
        }
    }
}
