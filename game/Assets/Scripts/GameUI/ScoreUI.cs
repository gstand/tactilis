using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace GameUI
{
    /// <summary>
    /// </summary>
    public class ScoreUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI labelText;
        [SerializeField] private Image backgroundPanel;

        [Header("Animation Settings")]
        [SerializeField] private bool animateOnChange = true;
        [SerializeField] private float animationDuration = 0.2f;
        [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 1, 1, 1);

        [Header("Colors")]
        [SerializeField] private Color primaryBlue = new Color(0.2f, 0.5f, 0.8f, 1f);
        [SerializeField] private Color lightBlue = new Color(0.4f, 0.7f, 0.95f, 1f);
        [SerializeField] private Color panelColor = new Color(0.1f, 0.2f, 0.35f, 0.85f);

        private int displayedScore;
        private Coroutine animationCoroutine;
        private Vector3 originalScale;

        private void Awake()
        {
            originalScale = transform.localScale;
            ApplyColorScheme();
        }

        private void ApplyColorScheme()
        {
            if (scoreText != null)
            {
                scoreText.color = lightBlue;
            }

            if (labelText != null)
            {
                labelText.color = new Color(lightBlue.r, lightBlue.g, lightBlue.b, 0.7f);
                labelText.text = "SCORE";
            }

            if (backgroundPanel != null)
            {
                backgroundPanel.color = panelColor;
            }
        }

        /// <summary>
        /// Updates the score display. Called by GameSessionManager.
        /// </summary>
        /// <param name="newScore">The new score value</param>
        public void UpdateScore(int newScore)
        {
            if (animateOnChange && newScore != displayedScore && gameObject.activeInHierarchy)
            {
                if (animationCoroutine != null)
                {
                    StopCoroutine(animationCoroutine);
                }
                animationCoroutine = StartCoroutine(AnimateScoreChange(displayedScore, newScore));
            }
            else
            {
                SetScoreImmediate(newScore);
            }

            displayedScore = newScore;
        }

        private void SetScoreImmediate(int score)
        {
            if (scoreText != null)
            {
                scoreText.text = score.ToString();
            }
        }

        private IEnumerator AnimateScoreChange(int fromScore, int toScore)
        {
            float elapsed = 0f;
            Vector3 startScale = originalScale;
            Vector3 punchScale = originalScale * 1.15f;

            while (elapsed < animationDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / animationDuration;

                float curveValue = scaleCurve.Evaluate(t);
                transform.localScale = Vector3.Lerp(startScale, punchScale, curveValue);

                int currentDisplayScore = Mathf.RoundToInt(Mathf.Lerp(fromScore, toScore, t));
                if (scoreText != null)
                {
                    scoreText.text = currentDisplayScore.ToString();
                }

                yield return null;
            }

            transform.localScale = originalScale;
            SetScoreImmediate(toScore);
            animationCoroutine = null;
        }

        /// <summary>
        /// Sets the label text (default is "SCORE").
        /// </summary>
        public void SetLabel(string label)
        {
            if (labelText != null)
            {
                labelText.text = label;
            }
        }
    }
}
