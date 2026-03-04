using UnityEngine;

public class UISoundManager : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip buttonClick;

    public void PlayClick()
    {
        audioSource.PlayOneShot(buttonClick);
    }
}