using UnityEngine;
using UnityEngine.Events;
using System.Collections;

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
    public CalibrationManager calibrationManager;
    public WhackAMoleGameManager gameManager;
    public ARGameUI arUI;

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
        if (calibrationManager != null)
        {
            calibrationManager.OnCalibrationConfirmed.AddListener(OnCalibrationConfirmed);
            calibrationManager.OnCalibrationCancelled.AddListener(OnCalibrationCancelled);
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (calibrationManager != null)
        {
            calibrationManager.OnCalibrationConfirmed.RemoveListener(OnCalibrationConfirmed);
            calibrationManager.OnCalibrationCancelled.RemoveListener(OnCalibrationCancelled);
        }
    }

    private void InitializeGame()
    {
        currentState = GameState.WaitingForCalibration;

        if (gameManager != null)
        {
            gameManager.enabled = false;
        }

        if (arUI != null)
        {
            arUI.HideGameOver();
        }

        Debug.Log("[GameController] Game initialized - waiting for calibration");
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
            gameManager.enabled = true;
            gameManager.StartGameDirectly();
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

        if (gameManager != null)
        {
            gameManager.enabled = false;
        }

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
            gameManager.enabled = false;
            gameManager.ResetGame();
        }

        if (calibrationManager != null)
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
            gameManager.enabled = false;
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