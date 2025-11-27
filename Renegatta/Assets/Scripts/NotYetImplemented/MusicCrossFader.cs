using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

/// <summary>
/// MusicCrossFader
/// - Singleton music manager that crossfades between tracks using two AudioSources.
/// - Auto-adds/uses two AudioSources so you can drop this into any scene and be done.
/// - Supports initial track with optional fade-in, silence gap between tracks, FadeOut to silence,
///   optional DontDestroyOnLoad behavior, and optional automatic crossfade to a "scene default" track on scene load.
/// 
/// Usage examples:
///   MusicCrossFader.Instance.PlayTrack(myClip); // crossfade with default duration
///   MusicCrossFader.Instance.PlayTrack(myClip, 3f, 0.5f); // 3s crossfade with 0.5s silence gap
///   MusicCrossFader.Instance.FadeOut(2f); // fade to silence over 2s
///   MusicCrossFader.Instance.PlayTrackByIndex(0); // play track from the serialized list
/// 
/// Drop in one component, configure inspector fields, call the static Instance methods from anything.
/// </summary>
[DisallowMultipleComponent]
public class MusicCrossFader : MonoBehaviour
{
    #region Inspector Fields

    [Header("Tracks")]
    [Tooltip("A list of music clips you commonly use. Optional -- PlayTrack accepts any AudioClip at runtime.")]
    [SerializeField] private List<AudioClip> musicTracks = new List<AudioClip>();

    [Header("Defaults")]
    [Tooltip("If set, this clip will play on Start (optionally with a fade-in). Leave blank for silence on scene load.")]
    [SerializeField] private AudioClip initialTrack = null;

    [Tooltip("If true, the initialTrack will fade in on Start using DefaultCrossfadeDuration. Otherwise it will start immediately.")]
    [SerializeField] private bool initialFadeIn = true;

    [Header("Crossfade")]
    [Tooltip("Default crossfade duration in seconds used when PlayTrack is called without a duration.")]
    [SerializeField] private float defaultCrossfadeDuration = 2f;

    [Tooltip("Master volume scalar for music (0..1). Crossfades obey this value.")]
    [Range(0f, 1f)]
    [SerializeField] private float masterVolume = 1f;

    [Header("Scene & Persistence")]
    [Tooltip("If true this object will persist between scene loads (DontDestroyOnLoad).")]
    [SerializeField] private bool dontDestroyOnLoad = true;

    [Tooltip("If true and SceneDefaultTrack is set, the manager will automatically crossfade to that track on every scene load.")]
    [SerializeField] private bool resetToSceneDefaultOnLoad = true;

    [Tooltip("Optional track to be used as the 'scene default' on scene load when resetToSceneDefaultOnLoad is true.")]
    [SerializeField] private AudioClip sceneDefaultOnLoad = null;

    [Header("AudioSources (read-only)")]
    [Tooltip("Automatically created AudioSource used for playback. Two sources will exist; these fields are exposed for debugging.")]
    [SerializeField] private AudioSource audioSourceA = null;
    [SerializeField] private AudioSource audioSourceB = null;

    [Header("Events")]
    [Tooltip("Inspector-only event invoked after a crossfade completes.")]
    [SerializeField] private UnityEvent onCrossfadeComplete = new UnityEvent();

    #endregion

    #region Public API

    /// <summary>Singleton instance (lazy-initialized if necessary).</summary>
    public static MusicCrossFader Instance { get; private set; }

    /// <summary>Event invoked when a crossfade completes. Passes the clip that ended up playing (may be null if silence).</summary>
    public event Action<AudioClip> OnCrossfadeComplete;

    /// <summary>Read-only list of serialized tracks. Can be modified at runtime if you want.</summary>
    public IList<AudioClip> MusicTracks => musicTracks;

    #endregion

    #region Private fields

    private AudioSource[] sources = new AudioSource[2];
    private int activeIndex = 0; // index of currently-active source (0 or 1)

    private Coroutine currentFadeRoutine = null;

    private enum FadeState { Idle, Crossfading, FadingOut, FadingIn }
    private FadeState state = FadeState.Idle;

    #endregion

    #region Unity lifecycle

    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            // If there's already an instance and this one shouldn't replace it, destroy ourselves.
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Optionally persist
        if (dontDestroyOnLoad)
        {
            DontDestroyOnLoad(gameObject);
        }

        EnsureTwoAudioSources();
        // wire local array for convenience
        sources[0] = audioSourceA;
        sources[1] = audioSourceB;

        // Set sensible defaults for the sources (2D music)
        for (int i = 0; i < 2; i++)
        {
            var s = sources[i];
            s.playOnAwake = false;
            s.loop = true;
            s.spatialBlend = 0f; // 2D
            s.volume = 0f;
        }

        // Listen for scene loads to optionally restore scene default music
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        // If initial track is set, play it on start (optionally fade in)
        if (initialTrack != null)
        {
            if (initialFadeIn)
                PlayTrack(initialTrack, defaultCrossfadeDuration);
            else
            {
                // immediate start
                var src = sources[activeIndex];
                src.clip = initialTrack;
                src.volume = masterVolume;
                src.Play();

                // ensure the other source is silent
                sources[1 - activeIndex].Stop();
                sources[1 - activeIndex].volume = 0f;
            }
        }
    }

    private void OnDestroy()
    {
        // cleanup
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (Instance == this)
            Instance = null;
    }

    #endregion

    #region Public control methods

    /// <summary>
    /// Crossfade to a clip. If clip is null the system will fade to silence (stop playback).
    /// </summary>
    /// <param name="clip">AudioClip to play. If null, crossfades to silence (FadeOut behavior).</param>
    /// <param name="duration">Crossfade duration. If negative, defaultCrossfadeDuration is used.</param>
    /// <param name="silenceGap">
    /// If > 0, a silence gap (seconds) will be inserted between fade-out and fade-in.
    /// If 0, the new track will overlap the outgoing one and fade concurrently.
    /// </param>
    /// <param name="force">If true will force a crossfade even if the requested clip is already playing.</param>
    public void PlayTrack(AudioClip clip, float duration = -1f, float silenceGap = 0f, bool force = false)
    {
        if (duration <= 0f) duration = defaultCrossfadeDuration;
        duration = Mathf.Max(0.01f, duration);
        silenceGap = Mathf.Max(0f, silenceGap);

        AudioClip current = GetCurrentClip();

        if (!force && clip != null && current == clip && IsPlaying())
        {
            // Already playing requested clip; nothing to do
            return;
        }

        // Stop any running fade and start new crossfade
        if (currentFadeRoutine != null)
            StopCoroutine(currentFadeRoutine);

        currentFadeRoutine = StartCoroutine(CrossfadeCoroutine(clip, duration, silenceGap));
    }

    /// <summary>Convenience overload: play a clip from the serialized list by index.</summary>
    public void PlayTrackByIndex(int index, float duration = -1f, float silenceGap = 0f, bool force = false)
    {
        if (index < 0 || index >= musicTracks.Count) throw new ArgumentOutOfRangeException(nameof(index));
        PlayTrack(musicTracks[index], duration, silenceGap, force);
    }

    /// <summary>Fade the music out to silence over duration seconds. Equivalent to PlayTrack(null, duration).</summary>
    public void FadeOut(float duration = -1f)
    {
        if (duration <= 0f) duration = defaultCrossfadeDuration;
        PlayTrack(null, duration, 0f, true);
    }

    /// <summary>Return currently playing clip (may be null if silent).</summary>
    public AudioClip GetCurrentClip()
    {
        var s = sources[activeIndex];
        return s != null ? s.clip : null;
    }

    /// <summary>True if currently a clip is playing and volume > 0</summary>
    public bool IsPlaying()
    {
        var s = sources[activeIndex];
        return s != null && s.isPlaying && s.volume > 0.0001f;
    }

    #endregion

    #region Internal behavior

    private IEnumerator CrossfadeCoroutine(AudioClip newClip, float duration, float silenceGap)
    {
        state = FadeState.Crossfading;

        AudioSource outgoing = sources[activeIndex];
        AudioSource incoming = sources[1 - activeIndex];

        // If newClip is null -> fade out outgoing and stop
        if (newClip == null)
        {
            float startVol = outgoing.volume;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / duration);
                float v = Mathf.Lerp(startVol, 0f, p) * masterVolume;
                outgoing.volume = v;
                yield return null;
            }

            outgoing.volume = 0f;
            outgoing.Stop();

            // keep activeIndex the same (no incoming)
            state = FadeState.Idle;
            currentFadeRoutine = null;
            OnCrossfadeComplete?.Invoke(null);
            onCrossfadeComplete?.Invoke();
            yield break;
        }

        // Normal crossfade path
        // If silence gap, fade out old over half duration, wait gap, then fade in new over half duration.
        if (silenceGap > 0f)
        {
            float half = duration * 0.5f;
            // Fade out outgoing
            float startOut = outgoing.volume;
            float tOut = 0f;
            while (tOut < half)
            {
                tOut += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(tOut / half);
                outgoing.volume = Mathf.Lerp(startOut, 0f, p) * masterVolume;
                yield return null;
            }
            outgoing.volume = 0f;
            outgoing.Stop();

            // silence gap
            if (silenceGap > 0f) yield return new WaitForSecondsRealtime(silenceGap);

            // Start incoming
            incoming.clip = newClip;
            incoming.volume = 0f;
            incoming.Play();

            // Fade in incoming
            float tIn = 0f;
            while (tIn < half)
            {
                tIn += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(tIn / half);
                incoming.volume = Mathf.Lerp(0f, 1f, p) * masterVolume;
                yield return null;
            }

            incoming.volume = 1f * masterVolume;
            // swap active
            activeIndex = 1 - activeIndex;
        }
        else
        {
            // Overlap crossfade: incoming starts immediately and both fade
            incoming.clip = newClip;
            incoming.volume = 0f;
            incoming.Play();

            float t = 0f;
            float outStart = outgoing.volume;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / duration);
                incoming.volume = Mathf.Lerp(0f, 1f, p) * masterVolume;
                outgoing.volume = Mathf.Lerp(outStart, 0f, p) * masterVolume;
                yield return null;
            }

            incoming.volume = 1f * masterVolume;
            outgoing.volume = 0f;
            outgoing.Stop();

            // swap active
            activeIndex = 1 - activeIndex;
        }

        state = FadeState.Idle;
        currentFadeRoutine = null;

        // callbacks
        OnCrossfadeComplete?.Invoke(newClip);
        onCrossfadeComplete?.Invoke();
    }

    private void EnsureTwoAudioSources()
    {
        // If user assigned them in inspector, use them; otherwise find two or add two.
        if (audioSourceA == null || audioSourceB == null)
        {
            var found = GetComponents<AudioSource>();
            if (found.Length >= 2)
            {
                audioSourceA = found[0];
                audioSourceB = found[1];
            }
            else if (found.Length == 1)
            {
                audioSourceA = found[0];
                audioSourceB = gameObject.AddComponent<AudioSource>();
            }
            else
            {
                audioSourceA = gameObject.AddComponent<AudioSource>();
                audioSourceB = gameObject.AddComponent<AudioSource>();
            }
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // If persistence is enabled (DontDestroyOnLoad) and we still want to respect scene defaults,
        // optionally crossfade to the provided sceneDefaultOnLoad clip.
        // This is a simple, inspector-driven behavior. If you want per-scene defaults, create a small SceneMusic
        // component that tells MusicCrossFader.Instance what the scene default should be (example in comments).
        if (!resetToSceneDefaultOnLoad) return;

        if (sceneDefaultOnLoad != null)
        {
            // Crossfade to the scene default when a new scene loads
            PlayTrack(sceneDefaultOnLoad, defaultCrossfadeDuration, 0f, false);
        }
    }

    #endregion

    #region Editor helpers & small utilities

#if UNITY_EDITOR
    // Utility to expose a quick inspector control (context menu) to play/pause from editor
    [ContextMenu("Stop Music (Editor)")]
    private void EditorStop()
    {
        if (Application.isPlaying)
        {
            for (int i = 0; i < 2; i++)
            {
                if (sources[i] != null)
                {
                    sources[i].Stop();
                    sources[i].clip = null;
                    sources[i].volume = 0f;
                }
            }
        }
    }
#endif

    #endregion
}
