using EasyTransition;
using UnityEngine;

public class PlayButton : MonoBehaviour
{
    public TransitionManager transitionManager;
    public TransitionSettings transitionSettings;
    public float transitionDuration = 1f;
    
    //if the player presses any common key like enter or space, the game starts
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
        {
            TransitionIt();
        }
    }

    public void TransitionIt()
    {
        transitionManager.Transition("SampleScene", transitionSettings, transitionDuration);
    }
}
