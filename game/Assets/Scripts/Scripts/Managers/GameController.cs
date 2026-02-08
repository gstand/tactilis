using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using Tactilis;

/// <summary>
/// Master controller that orchestrates the entire game flow:
/// Calibration → Countdown → Playing → GameOver → Restart
/// </summary>
public class GameController : MonoBehaviour
{
    public enum GameState
    {
        WaitingForCalibration,
        Calibrating,
        Countdown,
        Playing,
        GameOver
    }

    [Header("References")]
    [Tooltip("New hybrid calibration system (preferred)")]
    public SollertiaCalibrationSystem sollertiaCalibration;
    [Tooltip("Legacy manual calibration (fallback)")]
    public CalibrationManager calibrationManager;
    public WhackAMoleGameManager gameManager;
    public ARGameUI arUI;
    
    [Header("Calibration Mode")]
    [Tooltip("Use the new hybrid calibration system if available")]
    public bool useHybridCalibration = true;

    [Header("Countdown Settings")]
    public float getReadyDuration = 1.5f;
    public int countdownFrom = 3;
    public float countdownInterval = 1f;

    [Header("Audio (Optional)")]
    public AudioSource audioSource;
    public AudioClip countdownBeep;
    public AudioClip goSound;

    [Header("Events")]
    public UnityEvent OnGameStarted;
    public UnityEvent OnGameEnded;
    public UnityEvent OnGameRestarted;

    [Header("Debug")]
    [SerializeField] private GameState currentState = GameState.WaitingForCalibration;

    public GameState CurrentState => currentState;

    private Coroutine countdownCoroutine;

    private void Start()
    {
        // Find hybrid calibration system first
        if (sollertiaCalibration == null)
        {
            sollertiaCalibration = FindFirstObjectByType<SollertiaCalibrationSystem>();
        }
        if (calibrationManager == null)
        {
            calibrationManager = FindFirstObjectByType<CalibrationManager>();
        }
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<WhackAMoleGameManager>();
        }
        if (arUI == null)
        {
            arUI = FindFirstObjectByType<ARGameUI>();
        }

        SubscribeToEvents();
        InitializeGame();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    private void SubscribeToEvents()
    {
        // Subscribe to hybrid calibration system
        if (sollertiaCalibration != null && useHybridCalibration)
        {
            sollertiaCalibration.OnCalibrationCompleted.AddListener(OnHybridCalibrationCompleted);
        }
        
        // Subscribe to legacy calibration manager
        if (calibrationManager != null)
        {
            calibrationManager.OnCalibrationConfirmed.AddListener(OnCalibrationConfirmed);
            calibrationManager.OnCalibrationCancelled.AddListener(OnCalibrationCancelled);
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (sollertiaCalibration != null)
        {
            sollertiaCalibration.OnCalibrationCompleted.RemoveListener(OnHybridCalibrationCompleted);
        }
        
        if (calibrationManager != null)
        {
            calibrationManager.OnCalibrationConfirmed.RemoveListener(OnCalibrationConfirmed);
            calibrationManager.OnCalibrationCancelled.RemoveListener(OnCalibrationCancelled);
        }
    }
    
    private void OnHybridCalibrationCompleted(Vector3 position, Quaternion rotation)
    {
        Debug.Log($"[GameController] Hybrid calibration completed at {position}");
        StartCountdown();
    }

    private void InitializeGame()
    {
        currentState = GameState.WaitingForCalibration;

        // Don't disable gameManager — it stays in Idle state and its Update() is a no-op.
        // Disabling it causes issues when re-enabling mid-frame.

        if (arUI != null)
        {
            arUI.HideGameOver();
        }

        // Auto-start hybrid calibration if enabled
        if (useHybridCalibration && sollertiaCalibration != null)
        {
            currentState = GameState.Calibrating;
            sollertiaCalibration.StartCalibration();
            Debug.Log("[GameController] Game initialized - starting hybrid calibration");
        }
        else
        {
            Debug.Log("[GameController] Game initialized - waiting for manual calibration");
        }
    }

    private void OnCalibrationConfirmed()
    {
        Debug.Log("[GameController] Calibration confirmed - starting countdown");
        StartCountdown();
    }

    private void OnCalibrationCancelled()
    {
        currentState = GameState.WaitingForCalibration;
        Debug.Log("[GameController] Calibration cancelled - waiting for retry");
    }

    private void StartCountdown()
    {
        currentState = GameState.Countdown;

        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
        }
        countdownCoroutine = StartCoroutine(CountdownSequence());
    }

    private IEnumerator CountdownSequence()
    {
        // Show "Get Ready"
        if (arUI != null)
        {
            arUI.SetRulesText("<size=150%><b>GET READY!</b></size>");
            arUI.ShowRules(true);
        }

        yield return new WaitForSeconds(getReadyDuration);

        // Countdown 3... 2... 1...
        for (int i = countdownFrom; i > 0; i--)
        {
            if (arUI != null)
            {
                arUI.SetRulesText($"<size=200%><b>{i}</b></size>");
            }

            PlaySound(countdownBeep);
            yield return new WaitForSeconds(countdownInterval);
        }

        // GO!
        if (arUI != null)
        {
            arUI.SetRulesText("<size=200%><b>GO!</b></size>");
        }

        PlaySound(goSound);
        yield return new WaitForSeconds(0.5f);

        // Hide countdown UI and start game
        if (arUI != null)
        {
            arUI.ShowRules(false);
        }

        StartGame();
        countdownCoroutine = null;
    }

    private void StartGame()
    {
        currentState = GameState.Playing;

        if (gameManager != null)
        {
            gameManager.StartGameDirectly();
        }
        else
        {
            Debug.LogError("[GameController] gameManager is NULL — cannot start game!");
        }

        OnGameStarted?.Invoke();
        Debug.Log("[GameController] Game started!");
    }

    /// <summary>
    /// Called when the game ends (from WhackAMoleGameManager or timeout).
    /// </summary>
    public void EndGame()
    {
        currentState = GameState.GameOver;

        OnGameEnded?.Invoke();
        Debug.Log("[GameController] Game ended");
    }

    /// <summary>
    /// Restarts the entire flow from calibration.
    /// </summary>
    public void RestartGame()
    {
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
        }

        currentState = GameState.WaitingForCalibration;

        if (gameManager != null)
        {
            gameManager.ResetGame();
        }

        // Reset appropriate calibration system
        if (useHybridCalibration && sollertiaCalibration != null)
        {
            sollertiaCalibration.CancelCalibration();
            currentState = GameState.Calibrating;
            sollertiaCalibration.StartCalibration();
        }
        else if (calibrationManager != null)
        {
            calibrationManager.ResetCalibration();
        }

        if (arUI != null)
        {
            arUI.HideGameOver();
        }

        OnGameRestarted?.Invoke();
        Debug.Log("[GameController] Game restarted - back to calibration");
    }

    /// <summary>
    /// Restarts the game without recalibrating (keeps grid position).
    /// </summary>
    public void RestartWithoutCalibration()
    {
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
        }

        if (gameManager != null)
        {
            gameManager.ResetGame();
        }

        if (arUI != null)
        {
            arUI.HideGameOver();
        }

        StartCountdown();
        Debug.Log("[GameController] Game restarted - keeping calibration");
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    private void Update()
    {
        // Monitor game state for game over
        if (currentState == GameState.Playing && gameManager != null)
        {
            if (gameManager.IsGameOver)
            {
                EndGame();
            }
        }
    }
}
