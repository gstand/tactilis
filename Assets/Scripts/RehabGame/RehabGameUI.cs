using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Handles all UI elements for the stroke rehabilitation game.
/// Modern, accessible design optimized for VR/AR visibility.
/// </summary>
public class RehabGameUI : MonoBehaviour
{
    [Header("Timer UI")]
    public GameObject timerPanel;
    public Image timerFillImage;
    public TextMeshProUGUI timerText;
    public Image timerBackground;
    public Color timerNormalColor = new Color(0.2f, 0.7f, 0.3f);
    public Color timerWarningColor = new Color(0.9f, 0.6f, 0.1f);
    public Color timerCriticalColor = new Color(0.9f, 0.2f, 0.2f);
    public float warningThreshold = 10f;
    public float criticalThreshold = 5f;

    [Header("Score UI")]
    public GameObject scorePanel;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI hitsText;
    public TextMeshProUGUI missesText;

    [Header("Countdown UI")]
    public GameObject countdownPanel;
    public TextMeshProUGUI countdownText;

    [Header("Feedback UI")]
    public GameObject feedbackPanel;
    public TextMeshProUGUI feedbackText;
    public float feedbackDuration = 0.8f;

    [Header("Results UI")]
    public GameObject resultsPanel;
    public TextMeshProUGUI resultsTitleText;
    public TextMeshProUGUI resultsScoreText;
    public TextMeshProUGUI resultsHitsText;
    public TextMeshProUGUI resultsMissesText;
    public TextMeshProUGUI resultsAccuracyText;
    public TextMeshProUGUI resultsReactionText;
    public Button playAgainButton;
    public Button mainMenuButton;

    [Header("Start UI")]
    public GameObject startPanel;
    public Button startButton;
    public TextMeshProUGUI instructionsText;

    [Header("Animation")]
    public float pulseSpeed = 2f;
    public float pulseIntensity = 0.1f;

    int _currentScore;
    int _currentHits;
    int _currentMisses;
    float _feedbackEndTime;
    bool _showingFeedback;

    void Awake()
    {
        // Wire up buttons
        if (startButton)
            startButton.onClick.AddListener(OnStartClicked);
        if (playAgainButton)
            playAgainButton.onClick.AddListener(OnPlayAgainClicked);
        if (mainMenuButton)
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);

        // Initial state
        ShowStartUI(true);
        ShowGameUI(false);
        ShowCountdown(false);
        ShowResults(null);
    }

    void Update()
    {
        // Handle feedback fade out
        if (_showingFeedback && Time.time >= _feedbackEndTime)
        {
            _showingFeedback = false;
            if (feedbackPanel)
                feedbackPanel.SetActive(false);
        }
    }

    public void ShowStartUI(bool show)
    {
        if (startPanel)
            startPanel.SetActive(show);
    }

    public void ShowGameUI(bool show)
    {
        if (timerPanel)
            timerPanel.SetActive(show);
        if (scorePanel)
            scorePanel.SetActive(show);

        if (show)
        {
            _currentScore = 0;
            _currentHits = 0;
            _currentMisses = 0;
            UpdateScoreDisplay();
        }
    }

    public void ShowCountdown(bool show)
    {
        if (countdownPanel)
            countdownPanel.SetActive(show);
    }

    public void UpdateCountdown(int seconds)
    {
        if (countdownText)
        {
            countdownText.text = seconds.ToString();
            // Pulse animation
            float scale = 1f + Mathf.Sin(Time.time * 10f) * 0.1f;
            countdownText.transform.localScale = Vector3.one * scale;
        }
    }

    public void UpdateTimer(float remaining, float total)
    {
        if (timerText)
        {
            int seconds = Mathf.CeilToInt(remaining);
            int minutes = seconds / 60;
            seconds = seconds % 60;
            timerText.text = $"{minutes}:{seconds:00}";
        }

        if (timerFillImage)
        {
            timerFillImage.fillAmount = remaining / total;

            // Color based on time remaining
            Color targetColor;
            if (remaining <= criticalThreshold)
            {
                targetColor = timerCriticalColor;
                // Pulse when critical
                float pulse = Mathf.Sin(Time.time * pulseSpeed * 2f) * pulseIntensity;
                targetColor += new Color(pulse, pulse, pulse, 0);
            }
            else if (remaining <= warningThreshold)
            {
                targetColor = timerWarningColor;
            }
            else
            {
                targetColor = timerNormalColor;
            }

            timerFillImage.color = targetColor;
        }
    }

    public void ShowHitFeedback(float reactionTime)
    {
        _currentHits++;
        _currentScore += 100;

        // Bonus for fast reactions
        if (reactionTime < 1f)
            _currentScore += Mathf.RoundToInt((1f - reactionTime) * 50f);

        UpdateScoreDisplay();

        ShowFeedback($"HIT! +{100 + (reactionTime < 1f ? Mathf.RoundToInt((1f - reactionTime) * 50f) : 0)}", Color.green);
    }

    public void ShowMissFeedback()
    {
        _currentMisses++;
        _currentScore = Mathf.Max(0, _currentScore - 25);
        UpdateScoreDisplay();

        ShowFeedback("MISS", Color.red);
    }

    void ShowFeedback(string message, Color color)
    {
        if (feedbackPanel && feedbackText)
        {
            feedbackPanel.SetActive(true);
            feedbackText.text = message;
            feedbackText.color = color;
            _feedbackEndTime = Time.time + feedbackDuration;
            _showingFeedback = true;
        }
    }

    void UpdateScoreDisplay()
    {
        if (scoreText)
            scoreText.text = _currentScore.ToString();
        if (hitsText)
            hitsText.text = $"Hits: {_currentHits}";
        if (missesText)
            missesText.text = $"Misses: {_currentMisses}";
    }

    public void ShowResults(RehabGameManager.SessionResults results)
    {
        if (resultsPanel == null) return;

        if (results == null)
        {
            resultsPanel.SetActive(false);
            return;
        }

        resultsPanel.SetActive(true);

        if (resultsTitleText)
        {
            // Dynamic title based on performance
            if (results.accuracy >= 90f)
                resultsTitleText.text = "EXCELLENT!";
            else if (results.accuracy >= 70f)
                resultsTitleText.text = "GREAT JOB!";
            else if (results.accuracy >= 50f)
                resultsTitleText.text = "GOOD EFFORT!";
            else
                resultsTitleText.text = "KEEP PRACTICING!";
        }

        if (resultsScoreText)
            resultsScoreText.text = $"Score: {results.score}";

        if (resultsHitsText)
            resultsHitsText.text = $"Hits: {results.hits}";

        if (resultsMissesText)
            resultsMissesText.text = $"Misses: {results.misses}";

        if (resultsAccuracyText)
            resultsAccuracyText.text = $"Accuracy: {results.accuracy:F1}%";

        if (resultsReactionText)
        {
            if (results.reactionTimes.Count > 0)
            {
                resultsReactionText.text = $"Avg Reaction: {results.averageReactionTime:F2}s\n" +
                                           $"Fastest: {results.fastestReaction:F2}s";
            }
            else
            {
                resultsReactionText.text = "No hits recorded";
            }
        }
    }

    void OnStartClicked()
    {
        ShowStartUI(false);
        var gameManager = FindFirstObjectByType<RehabGameManager>();
        if (gameManager)
            gameManager.StartGame();
    }

    void OnPlayAgainClicked()
    {
        var gameManager = FindFirstObjectByType<RehabGameManager>();
        if (gameManager)
            gameManager.RestartGame();
    }

    void OnMainMenuClicked()
    {
        // Return to main menu scene or show start UI
        var gameManager = FindFirstObjectByType<RehabGameManager>();
        if (gameManager)
            gameManager.ResetGame();

        ShowStartUI(true);
    }
}
