using UnityEngine;

/// <summary>
/// SceneMusic
/// Place this in any scene to declare which music clip that scene prefers.
/// On scene load, this component will instruct the MusicCrossFader to fade to the chosen track.
/// 
/// Rules:
/// - If multiple SceneMusic components exist, the last one executing OnEnable wins.
/// - If MusicCrossFader.Instance does not exist, nothing breaks.
/// - By design, this overrides the crossfader's sceneDefaultOnLoad setting.
/// </summary>
public class SceneMusic : MonoBehaviour
{
    [Header("Scene Music")]
    [Tooltip("Music clip this scene should fade to on load.")]
    [SerializeField] private AudioClip sceneTrack;

    [Tooltip("Duration of the fade. If <= 0, MusicCrossFader's default duration is used.")]
    [SerializeField] private float fadeDuration = -1f;

    [Tooltip("Optional silence gap before fading in the new track.")]
    [SerializeField] private float silenceGap = 0f;

    [Tooltip("If true, will always fade even if this clip is already playing.")]
    [SerializeField] private bool force = false;

    private void OnEnable()
    {
        var fader = MusicCrossFader.Instance;
        if (fader == null)
            return;

        if (sceneTrack == null)
            return;

        fader.PlayTrack(sceneTrack, fadeDuration, silenceGap, force);
    }
}
