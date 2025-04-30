using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GasGrenadeSFXPlayer : MonoBehaviour
{
    public AudioClip MetalClip;

    private AudioSource _audioSource;

    void Start()
    {
        _audioSource = GetComponent<AudioSource>();
    }

    void OnCollisionEnter(Collision collision)
    {
        _audioSource.PlayOneShot(MetalClip);
    }
}
