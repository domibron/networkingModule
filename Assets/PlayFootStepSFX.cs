using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayFootStepSFX : MonoBehaviour
{
    public AudioClip FootStepClip;

    private AudioSource _audioSource;

    // Start is called before the first frame update
    void Start()
    {
        _audioSource = GetComponentInParent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void PlayFootStep()
    {
        _audioSource.PlayOneShot(FootStepClip, 0.7f);
    }
}
