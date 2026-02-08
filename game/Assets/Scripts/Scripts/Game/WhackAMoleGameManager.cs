using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class WhackAMoleGameManager : MonoBehaviour {
    [Header("Game Settings")]
    public float gameDuration = 30f;           // Total game time
    public float buttonActiveTime = 1.5f;      // How long each button stays lit
    public float minSpawnDelay = 0.3f;         // Min time between spawns
    public float maxSpawnDelay = 0.8f;         // Max time between spawns
    public float rulesDisplayTime = 5f;        // How long to show rules before game starts
    
    [Header("Scoring")]
    public int pointsForCorrect = 10;
    public int pointsLostForWrong = 5;
    public int pointsLostForTimeout = 3;
    
    [Header("References")]
    public WhackAMoleButton[] buttons;         // Assign all buttons in inspector
    public Transform blueFingerTip;            // Blue finger (e.g., left index)
    public Transform redFingerTip;             // Red finger (e.g., right index)
    public float touchRadius = 0.03f;          // How close finger needs to be
    
    [Header("AR UI")]
    public ARGameUI arUI;                      // Reference to AR UI system
    
    [Header("Audio (Optional)")]
    public AudioSource audioSource;
    public AudioClip correctSound;
    public AudioClip wrongSound;
    public AudioClip timeoutSound;
    public AudioClip countdownSound;
    public AudioClip gameStartSound;
    
    // Game state
    private enum GameState { ShowingRules, Countdown, Playing, GameOver }
    private GameState currentState = GameState.ShowingRules;
    
    private int score = 0;
    private float gameTimer;
    private float stateTimer;
    private float nextSpawnTime;
    private int countdownNumber = 3;
    
    // Track active buttons
    private List<WhackAMoleButton> activeButtons = new List<WhackAMoleButton>();
    
    // Rules text
    private string rulesText = 
        "<size=120%><b>WHACK-A-MOLE</b></size>\n\n" +
        "<color=#4488FF>BLUE finger</color> = Press <color=#4488FF>BLUE</color> buttons\n" +
        "<color=#FF4444>RED finger</color> = Press <color=#FF4444>RED</color> buttons\n\n" +
        "<color=#44FF44>+10 pts</color> for correct match\n" +
        "<color=#FF4444>-5 pts</color> for wrong color\n" +
        "<color=#FFAA44>-3 pts</color> if button times out\n\n" +
        "Game lasts <b>30 seconds</b>. Good luck!";

    void Start() {
        // Deactivate all buttons
        foreach (var button in buttons) {
            if (button != null) button.Deactivate();
        }
        activeButtons.Clear();
        
        // Show rules
        StartShowingRules();
    }
    
    void StartShowingRules() {
        currentState = GameState.ShowingRules;
        stateTimer = rulesDisplayTime;
        
        if (arUI != null) {
            arUI.SetRulesText(rulesText);
            arUI.ShowRules(true);
            arUI.SetScore(0);
            arUI.HideGameOver();
        }
    }

    void Update() {
        switch (currentState) {
            case GameState.ShowingRules:
                UpdateShowingRules();
                break;
            case GameState.Countdown:
                UpdateCountdown();
                break;
            case GameState.Playing:
                UpdatePlaying();
                break;
            case GameState.GameOver:
                // Wait for restart
                break;
        }
    }
    
    void UpdateShowingRules() {
        stateTimer -= Time.deltaTime;
        
        if (arUI != null) {
            arUI.SetTimer(stateTimer);
        }
        
        if (stateTimer <= 0) {
            StartCountdown();
        }
    }
    
    void StartCountdown() {
        currentState = GameState.Countdown;
        countdownNumber = 3;
        stateTimer = 1f;
        
        if (arUI != null) {
            arUI.SetRulesText($"<size=200%><b>{countdownNumber}</b></size>");
        }
        
        PlaySound(countdownSound);
    }
    
    void UpdateCountdown() {
        stateTimer -= Time.deltaTime;
        
        if (stateTimer <= 0) {
            countdownNumber--;
            
            if (countdownNumber > 0) {
                stateTimer = 1f;
                if (arUI != null) {
                    arUI.SetRulesText($"<size=200%><b>{countdownNumber}</b></size>");
                }
                PlaySound(countdownSound);
            } else {
                StartGame();
            }
        }
    }

    void StartGame() {
        currentState = GameState.Playing;
        score = 0;
        gameTimer = gameDuration;
        nextSpawnTime = Time.time + 0.5f;
        
        if (arUI != null) {
            arUI.SetRulesText("<size=150%><b>GO!</b></size>");
            arUI.SetScore(score);
        }
        
        PlaySound(gameStartSound);
        
        // Hide the "GO!" after a moment
        Invoke(nameof(HideRulesPanel), 0.5f);
    }
    
    void HideRulesPanel() {
        if (arUI != null) {
            arUI.ShowRules(false);
        }
    }
    
    void UpdatePlaying() {
        // Update game timer
        gameTimer -= Time.deltaTime;
        
        if (arUI != null) {
            arUI.SetTimer(gameTimer);
        }
        
        if (gameTimer <= 0) {
            EndGame();
            return;
        }
        
        // Check for button spawning
        if (Time.time >= nextSpawnTime) {
            SpawnRandomButton();
            nextSpawnTime = Time.time + Random.Range(minSpawnDelay, maxSpawnDelay);
        }
        
        // Check for finger touches
        CheckFingerTouches();
        
        // Check for timeouts
        CheckTimeouts();
    }
    
    void SpawnRandomButton() {
        // Get list of inactive buttons
        List<WhackAMoleButton> inactiveButtons = new List<WhackAMoleButton>();
        foreach (var button in buttons) {
            if (button != null && !button.IsActive) {
                inactiveButtons.Add(button);
            }
        }
        
        if (inactiveButtons.Count == 0) return;
        
        // Pick a random inactive button
        WhackAMoleButton chosenButton = inactiveButtons[Random.Range(0, inactiveButtons.Count)];
        
        // Pick a random color (Blue or Red)
        WhackAMoleButton.ButtonColor color = Random.value > 0.5f ? 
            WhackAMoleButton.ButtonColor.Blue : 
            WhackAMoleButton.ButtonColor.Red;
        
        // Activate the button
        chosenButton.Activate(color, buttonActiveTime);
        activeButtons.Add(chosenButton);
    }
    
    void CheckFingerTouches() {
        // Check blue finger
        if (blueFingerTip != null) {
            CheckFingerAgainstButtons(blueFingerTip.position, WhackAMoleButton.ButtonColor.Blue);
        }
        
        // Check red finger
        if (redFingerTip != null) {
            CheckFingerAgainstButtons(redFingerTip.position, WhackAMoleButton.ButtonColor.Red);
        }
    }
    
    void CheckFingerAgainstButtons(Vector3 fingerPos, WhackAMoleButton.ButtonColor fingerColor) {
        for (int i = activeButtons.Count - 1; i >= 0; i--) {
            WhackAMoleButton button = activeButtons[i];
            if (button == null) {
                activeButtons.RemoveAt(i);
                continue;
            }
            
            float distance = Vector3.Distance(fingerPos, button.transform.position);
            if (distance < touchRadius) {
                // Button was touched!
                if (button.CurrentColor == fingerColor) {
                    // Correct! Matching color
                    score += pointsForCorrect;
                    PlaySound(correctSound);
                    Debug.Log($"[WhackAMole] Correct! +{pointsForCorrect} points. Score: {score}");
                } else {
                    // Wrong finger color!
                    score -= pointsLostForWrong;
                    PlaySound(wrongSound);
                    Debug.Log($"[WhackAMole] Wrong color! -{pointsLostForWrong} points. Score: {score}");
                }
                
                // Update UI
                if (arUI != null) {
                    arUI.SetScore(score);
                }
                
                button.Deactivate();
                activeButtons.RemoveAt(i);
            }
        }
    }
    
    void CheckTimeouts() {
        for (int i = activeButtons.Count - 1; i >= 0; i--) {
            WhackAMoleButton button = activeButtons[i];
            if (button == null) {
                activeButtons.RemoveAt(i);
                continue;
            }
            
            if (button.HasTimedOut()) {
                // Player didn't press in time
                score -= pointsLostForTimeout;
                PlaySound(timeoutSound);
                Debug.Log($"[WhackAMole] Timeout! -{pointsLostForTimeout} points. Score: {score}");
                
                // Update UI
                if (arUI != null) {
                    arUI.SetScore(score);
                }
                
                button.Deactivate();
                activeButtons.RemoveAt(i);
            }
        }
    }
    
    void EndGame() {
        currentState = GameState.GameOver;
        
        // Deactivate all buttons
        foreach (var button in buttons) {
            if (button != null) button.Deactivate();
        }
        activeButtons.Clear();
        
        if (arUI != null) {
            arUI.ShowGameOver(score);
        }
        
        Debug.Log($"[WhackAMole] Game Over! Final Score: {score}");
    }
    
    void PlaySound(AudioClip clip) {
        if (audioSource != null && clip != null) {
            audioSource.PlayOneShot(clip);
        }
    }
    
    // Call this to restart the game
    public void RestartGame() {
        StartShowingRules();
    }
}
