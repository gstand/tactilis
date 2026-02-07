using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Orchestrates all end-of-game visual and audio effects.
/// Listens to VRCircularCountdown events and drives screen fade,
/// audio stings, particle bursts, slow-motion, and optional scene reload.
/// </summary>
[AddComponentMenu("FX/End Game FX Director")]
public class EndGameFXDirector : MonoBehaviour
{
    public enum EndReason { TimerExpired, PlayerEscaped }

    [Header("References")]
    [Tooltip("The countdown timer driving the game session.")]
    public VRCircularCountdown countdown;

    [Tooltip("Screen-fade quad/overlay in front of the camera (optional if using CameraAutoFader).")]
    public CameraFullBlackFade screenFade;

    [Header("Slow Motion")]
    [Tooltip("Enable bullet-time ramp at end of game.")]
    public bool enableSlowMotion = true;
    [Range(0.01f, 1f)]
    public float slowMotionScale = 0.3f;
    public float slowMotionRampDuration = 1.5f;

    [Header("Screen Fade")]
    public float fadeDuration = 2f;
    [Tooltip("Target alpha: 1 = full black, 0 = clear.")]
    [Range(0f, 1f)]
    public float fadeTargetAlpha = 1f;
    [Tooltip("Delay before the fade begins (seconds, unscaled).")]
    public float fadeDelay = 0.5f;

    [Header("Audio")]
    [Tooltip("One-shot audio clip played on success (escape).")]
    public AudioClip successSting;
    [Tooltip("One-shot audio clip played on failure (time expired).")]
    public AudioClip failureSting;
    [Tooltip("Ambient audio sources to fade out at end.")]
    public AudioSource[] ambientSources;
    public float ambientFadeDuration = 2f;
    [Range(0f, 1f)]
    public float stingVolume = 1f;

    [Header("Particles")]
    [Tooltip("Particle systems to play on success (confetti, sparks, etc.).")]
    public ParticleSystem[] successParticles;
    [Tooltip("Particle systems to play on failure (smoke, embers, etc.).")]
    public ParticleSystem[] failureParticles;

    [Header("Post-Game")]
    [Tooltip("Seconds (unscaled) after fade before post-game events fire.")]
    public float postGameDelay = 3f;
    [Tooltip("If set, this scene is loaded after the post-game delay.")]
    public string nextSceneName = "";

    [Header("Events")]
    public UnityEvent onEndGameStart;
    public UnityEvent onSuccessStart;
    public UnityEvent onFailureStart;
    public UnityEvent onPostGame;

    AudioSource _stingSource;
    bool _ended;

    void Awake()
    {
        // Create a dedicated AudioSource for stings so we don't clash with others
        _stingSource = gameObject.AddComponent<AudioSource>();
        _stingSource.playOnAwake = false;
        _stingSource.spatialBlend = 0f; // 2D
        _stingSource.volume = stingVolume;
    }

    void OnEnable()
    {
        if (countdown)
        {
            countdown.onTimerFinished.AddListener(OnTimerExpired);
        }
    }

    void OnDisable()
    {
        if (countdown)
        {
            countdown.onTimerFinished.RemoveListener(OnTimerExpired);
        }
    }

    /// <summary>
    /// Called automatically when VRCircularCountdown reaches zero.
    /// </summary>
    void OnTimerExpired()
    {
        TriggerEndGame(EndReason.TimerExpired);
    }

    /// <summary>
    /// Call this from gameplay code when the player successfully escapes.
    /// </summary>
    public void TriggerEscape()
    {
        TriggerEndGame(EndReason.PlayerEscaped);
    }

    /// <summary>
    /// Main entry point — can only fire once per session.
    /// </summary>
    public void TriggerEndGame(EndReason reason)
    {
        if (_ended) return;
        _ended = true;

        // Stop the timer if it's still running
        if (countdown) countdown.StopTimer();

        onEndGameStart?.Invoke();

        switch (reason)
        {
            case EndReason.PlayerEscaped:
                onSuccessStart?.Invoke();
                PlayParticles(successParticles);
                PlaySting(successSting);
                break;

            case EndReason.TimerExpired:
                onFailureStart?.Invoke();
                PlayParticles(failureParticles);
                PlaySting(failureSting);
                break;
        }

        StartCoroutine(EndSequence(reason));
    }

    IEnumerator EndSequence(EndReason reason)
    {
        // 1. Slow-motion ramp
        if (enableSlowMotion)
            StartCoroutine(SlowMotionRamp());

        // 2. Fade ambient audio out
        if (ambientSources != null && ambientSources.Length > 0)
            StartCoroutine(FadeAmbient());

        // 3. Screen fade (after optional delay)
        if (screenFade)
        {
            if (fadeDelay > 0f)
                yield return new WaitForSecondsRealtime(fadeDelay);

            screenFade.FadeTo(fadeTargetAlpha, fadeDuration);
            yield return new WaitForSecondsRealtime(fadeDuration);
        }
        else
        {
            // No screen fade — just wait equivalent time
            yield return new WaitForSecondsRealtime(fadeDelay + fadeDuration);
        }

        // 4. Post-game delay
        yield return new WaitForSecondsRealtime(postGameDelay);

        // 5. Restore time scale
        Time.timeScale = 1f;

        // 6. Fire post-game event
        onPostGame?.Invoke();

        // 7. Optional scene load
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneName);
        }
    }

    IEnumerator SlowMotionRamp()
    {
        float startScale = Time.timeScale;
        float t = 0f;
        while (t < slowMotionRampDuration)
        {
            t += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(t / slowMotionRampDuration);
            Time.timeScale = Mathf.Lerp(startScale, slowMotionScale, progress);
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
            yield return null;
        }
        Time.timeScale = slowMotionScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
    }

    IEnumerator FadeAmbient()
    {
        // Capture starting volumes
        float[] startVolumes = new float[ambientSources.Length];
        for (int i = 0; i < ambientSources.Length; i++)
        {
            startVolumes[i] = ambientSources[i] ? ambientSources[i].volume : 0f;
        }

        float t = 0f;
        while (t < ambientFadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(t / ambientFadeDuration);
            for (int i = 0; i < ambientSources.Length; i++)
            {
                if (ambientSources[i])
                    ambientSources[i].volume = Mathf.Lerp(startVolumes[i], 0f, progress);
            }
            yield return null;
        }

        // Ensure fully silent
        for (int i = 0; i < ambientSources.Length; i++)
        {
            if (ambientSources[i])
            {
                ambientSources[i].volume = 0f;
                ambientSources[i].Stop();
            }
        }
    }

    void PlayParticles(ParticleSystem[] systems)
    {
        if (systems == null) return;
        foreach (var ps in systems)
        {
            if (ps) ps.Play();
        }
    }

    void PlaySting(AudioClip clip)
    {
        if (clip && _stingSource)
        {
            _stingSource.clip = clip;
            _stingSource.volume = stingVolume;
            _stingSource.Play();
        }
    }

    /// <summary>
    /// Resets the director so it can fire again (e.g., after a scene soft-reset).
    /// </summary>
    public void ResetDirector()
    {
        _ended = false;
        StopAllCoroutines();
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
    }
}
