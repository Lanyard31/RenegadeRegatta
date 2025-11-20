using UnityEngine;

public class Horn : MonoBehaviour
{
    AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }


    void Update()
    {
        //if H is pressed, play the horn
        if(Input.GetKey("h"))
        {
           audioSource.Play();
        }
        
    }
}
