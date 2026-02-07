using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Main game manager for the stroke rehabilitation game.
/// Handles game flow, scoring, timing, and session management.
/// </summary>
public class RehabGameManager : MonoBehaviour
{
    public enum GameState { Idle, Countdown, Playing, Finished }

    [Header("References")]
    public RehabGrid grid;
    public HandTrackingPressDetector pressDetector;
    public RehabGameUI gameUI;

    [Header("Game Settings")]
    [Tooltip("Total game duration in seconds")]
    public float gameDuration = 30f;
    [Tooltip("Seconds before game starts (countdown)")]
    public float countdownDuration = 3f;
    [Tooltip("Time before a target despawns as a miss (seconds)")]
    public float targetLifetime = 3f;
    [Tooltip("Minimum time between target spawns")]
    public float minSpawnInterval = 0.8f;
    [Tooltip("Maximum time between target spawns")]
    public float maxSpawnInterval = 2f;
    [Tooltip("Maximum simultaneous active targets")]
    public int maxActiveTargets = 2;

    [Header("Difficulty Scaling")]
    [Tooltip("Reduce spawn interval over time")]
    public bool enableDifficultyScaling = true;
    [Tooltip("Minimum spawn interval at end of game")]
    public float minSpawnIntervalAtEnd = 0.5f;

    [Header("Audio")]
    public AudioClip countdownBeep;
    public AudioClip gameStartSound;
    public AudioClip gameEndSound;
    public AudioSource audioSource;

    [Header("Events")]
    public UnityEvent onGameStart;
    public UnityEvent onGameEnd;
    public UnityEvent<SessionResults> onResultsReady;

    public GameState CurrentState => _state;
    public float TimeRemaining => _timeRemaining;
    public float TimeElapsed => gameDuration - _timeRemaining;
    public SessionResults LastResults => _lastResults;

    GameState _state = GameState.Idle;
    float _timeRemaining;
    float _nextSpawnTime;
    SessionResults _lastResults;
    List<ActiveTarget> _activeTargets = new List<ActiveTarget>();

    struct ActiveTarget
    {
        public GridCell cell;
        public float spawnTime;
        public float expireTime;
    }

    [System.Serializable]
    public class SessionResults
    {
        public int totalTargets;
        public int hits;
        public int misses;
        public float accuracy;
        public float averageReactionTime;
        public float fastestReaction;
        public float slowestReaction;
        public int score;
        public float gameDuration;
        public List<float> reactionTimes = new List<float>();

        public void Calculate()
        {
            accuracy = totalTargets > 0 ? (float)hits / totalTargets * 100f : 0f;

            if (reactionTimes.Count > 0)
            {
                float sum = 0f;
                fastestReaction = float.MaxValue;
                slowestReaction = 0f;

                foreach (float rt in reactionTimes)
                {
                    sum += rt;
                    if (rt < fastestReaction) fastestReaction = rt;
                    if (rt > slowestReaction) slowestReaction = rt;
                }
                averageReactionTime = sum / reactionTimes.Count;
            }
            else
            {
                averageReactionTime = 0f;
                fastestReaction = 0f;
                slowestReaction = 0f;
            }

            // Score calculation: base points for hits, bonus for speed
            score = hits * 100;
            foreach (float rt in reactionTimes)
            {
                // Bonus points for fast reactions (under 1 second)
                if (rt < 1f)
                    score += Mathf.RoundToInt((1f - rt) * 50f);
            }
            // Penalty for misses
            score -= misses * 25;
            score = Mathf.Max(0, score);
        }
    }

    SessionResults _currentSession;

    void Awake()
    {
        if (!grid)
            grid = FindFirstObjectByType<RehabGrid>();

        if (!pressDetector)
            pressDetector = FindFirstObjectByType<HandTrackingPressDetector>();

        if (!audioSource)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    void Start()
    {
        if (pressDetector)
        {
            pressDetector.onCellPressed.AddListener(OnCellPressed);
        }

        if (grid)
        {
            grid.onCellMissed.AddListener(OnCellMissed);
        }
    }

    void Update()
    {
        switch (_state)
        {
            case GameState.Playing:
                UpdatePlaying();
                break;
        }
    }

    void UpdatePlaying()
    {
        _timeRemaining -= Time.deltaTime;

        // Check for expired targets
        CheckExpiredTargets();

        // Spawn new targets
        if (Time.time >= _nextSpawnTime && _activeTargets.Count < maxActiveTargets)
        {
            SpawnTarget();
            ScheduleNextSpawn();
        }

        // Update UI
        if (gameUI)
            gameUI.UpdateTimer(_timeRemaining, gameDuration);

        // Check for game end
        if (_timeRemaining <= 0f)
        {
            EndGame();
        }
    }

    void CheckExpiredTargets()
    {
        for (int i = _activeTargets.Count - 1; i >= 0; i--)
        {
            if (Time.time >= _activeTargets[i].expireTime)
            {
                _activeTargets[i].cell.RegisterMiss();
                _activeTargets.RemoveAt(i);
            }
        }
    }

    void SpawnTarget()
    {
        GridCell cell = grid.ActivateRandomCell();
        if (cell != null)
        {
            _activeTargets.Add(new ActiveTarget
            {
                cell = cell,
                spawnTime = Time.time,
                expireTime = Time.time + targetLifetime
            });
            _currentSession.totalTargets++;
        }
    }

    void ScheduleNextSpawn()
    {
        float interval;
        if (enableDifficultyScaling)
        {
            // Lerp spawn interval based on time elapsed
            float progress = TimeElapsed / gameDuration;
            interval = Mathf.Lerp(maxSpawnInterval, minSpawnIntervalAtEnd, progress);
            interval = Mathf.Max(interval, minSpawnInterval);
        }
        else
        {
            interval = Random.Range(minSpawnInterval, maxSpawnInterval);
        }

        _nextSpawnTime = Time.time + interval;
    }

    void OnCellPressed(GridCell cell, FRSSerialManager.FingerType finger)
    {
        if (_state != GameState.Playing) return;

        // Find and remove from active targets
        for (int i = _activeTargets.Count - 1; i >= 0; i--)
        {
            if (_activeTargets[i].cell == cell)
            {
                float reactionTime = Time.time - _activeTargets[i].spawnTime;
                _currentSession.hits++;
                _currentSession.reactionTimes.Add(reactionTime);
                _activeTargets.RemoveAt(i);

                if (gameUI)
                    gameUI.ShowHitFeedback(reactionTime);

                break;
            }
        }
    }

    void OnCellMissed(GridCell cell)
    {
        if (_state != GameState.Playing) return;

        _currentSession.misses++;

        if (gameUI)
            gameUI.ShowMissFeedback();
    }

    /// <summary>
    /// Start a new game session
    /// </summary>
    public void StartGame()
    {
        if (_state != GameState.Idle) return;

        StartCoroutine(StartGameSequence());
    }

    IEnumerator StartGameSequence()
    {
        _state = GameState.Countdown;

        // Initialize session
        _currentSession = new SessionResults { gameDuration = gameDuration };
        _activeTargets.Clear();
        grid.DeactivateAll();

        // Countdown
        if (gameUI)
            gameUI.ShowCountdown(true);

        for (int i = (int)countdownDuration; i > 0; i--)
        {
            if (gameUI)
                gameUI.UpdateCountdown(i);

            PlaySound(countdownBeep);
            yield return new WaitForSeconds(1f);
        }

        if (gameUI)
        {
            gameUI.ShowCountdown(false);
            gameUI.ShowGameUI(true);
        }

        // Start game
        _state = GameState.Playing;
        _timeRemaining = gameDuration;
        _nextSpawnTime = Time.time + 0.5f; // Small delay before first target

        PlaySound(gameStartSound);
        onGameStart?.Invoke();
    }

    void EndGame()
    {
        _state = GameState.Finished;

        // Mark remaining targets as missed
        foreach (var target in _activeTargets)
        {
            target.cell.RegisterMiss();
            _currentSession.misses++;
        }
        _activeTargets.Clear();

        grid.DeactivateAll();

        // Calculate final results
        _currentSession.Calculate();
        _lastResults = _currentSession;

        PlaySound(gameEndSound);
        onGameEnd?.Invoke();

        if (gameUI)
        {
            gameUI.ShowGameUI(false);
            gameUI.ShowResults(_lastResults);
        }

        onResultsReady?.Invoke(_lastResults);

        Debug.Log($"[RehabGameManager] Game Over! Hits: {_lastResults.hits}, Misses: {_lastResults.misses}, " +
                  $"Accuracy: {_lastResults.accuracy:F1}%, Score: {_lastResults.score}");
    }

    /// <summary>
    /// Reset to idle state, ready for new game
    /// </summary>
    public void ResetGame()
    {
        StopAllCoroutines();
        _state = GameState.Idle;
        _activeTargets.Clear();
        grid.DeactivateAll();

        if (gameUI)
        {
            gameUI.ShowGameUI(false);
            gameUI.ShowResults(null);
            gameUI.ShowCountdown(false);
        }
    }

    /// <summary>
    /// Restart immediately (for replay button)
    /// </summary>
    public void RestartGame()
    {
        ResetGame();
        StartGame();
    }

    void PlaySound(AudioClip clip)
    {
        if (clip && audioSource)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}
