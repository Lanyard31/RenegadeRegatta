using EasyTransition;
using UnityEngine;

public class PlayButton : MonoBehaviour
{
    public TransitionManager transitionManager;
    public TransitionSettings transitionSettings;
    public float transitionDuration = 1f;
    
    public void TransitionIt()
    {
        transitionManager.Transition("SampleScene", transitionSettings, transitionDuration);
    }
}
