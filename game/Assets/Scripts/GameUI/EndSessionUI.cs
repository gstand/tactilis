using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.Events;

namespace GameUI
{
    /// <summary>
    /// </summary>
    public class EndSessionUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform panelTransform;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI scoreValueText;
        [SerializeField] private TextMeshProUGUI scoreLabelText;
        [SerializeField] private Image panelBackground;
        [SerializeField] private Image panelBorder;
        [SerializeField] private Button restartButton;
        [SerializeField] private TextMeshProUGUI restartButtonText;

        [Header("Animation Settings")]
        [SerializeField] private float fadeInDuration = 0.5f;
        [SerializeField] private float scaleInDuration = 0.3f;
        [SerializeField] private AnimationCurve appearCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Colors - Calming Blue Scheme")]
        [SerializeField] private Color panelBackgroundColor = new Color(0.12f, 0.18f, 0.28f, 0.95f);
        [SerializeField] private Color panelBorderColor = new Color(0.3f, 0.5f, 0.7f, 0.8f);
        [SerializeField] private Color titleColor = new Color(0.6f, 0.8f, 0.95f, 1f);
        [SerializeField] private Color scoreColor = new Color(0.4f, 0.75f, 0.95f, 1f);
        [SerializeField] private Color labelColor = new Color(0.5f, 0.65f, 0.8f, 0.8f);
        [SerializeField] private Color buttonColor = new Color(0.25f, 0.55f, 0.8f, 1f);
        [SerializeField] private Color buttonTextColor = Color.white;

        [Header("Events")]
        public UnityEvent OnRestartClicked;

        private Coroutine showCoroutine;

        private void Awake()
        {
            ApplyColorScheme();
            
            if (restartButton != null)
            {
                restartButton.onClick.AddListener(HandleRestartClick);
            }

            Hide();
        }

        private void ApplyColorScheme()
        {
            if (panelBackground != null)
            {
                panelBackground.color = panelBackgroundColor;
            }

            if (panelBorder != null)
            {
                panelBorder.color = panelBorderColor;
            }

            if (titleText != null)
            {
                titleText.color = titleColor;
                titleText.text = "Session Ended";
            }

            if (scoreValueText != null)
            {
                scoreValueText.color = scoreColor;
            }

            if (scoreLabelText != null)
            {
                scoreLabelText.color = labelColor;
                scoreLabelText.text = "Your Score";
            }

            if (restartButton != null)
            {
                var buttonImage = restartButton.GetComponent<Image>();
                if (buttonImage != null)
                {
                    buttonImage.color = buttonColor;
                }

                var colors = restartButton.colors;
                colors.normalColor = buttonColor;
                colors.highlightedColor = new Color(buttonColor.r + 0.1f, buttonColor.g + 0.1f, buttonColor.b + 0.1f, 1f);
                colors.pressedColor = new Color(buttonColor.r - 0.1f, buttonColor.g - 0.1f, buttonColor.b - 0.1f, 1f);
                restartButton.colors = colors;
            }

            if (restartButtonText != null)
            {
                restartButtonText.color = buttonTextColor;
                restartButtonText.text = "Try Again";
            }
        }

        /// <summary>
        /// Shows the end session panel with the final score.
        /// </summary>
        /// <param name="finalScore">The final score to display</param>
        public void Show(int finalScore)
        {
            gameObject.SetActive(true);

            if (scoreValueText != null)
            {
                scoreValueText.text = finalScore.ToString();
            }

            if (showCoroutine != null)
            {
                StopCoroutine(showCoroutine);
            }
            showCoroutine = StartCoroutine(AnimateShow());
        }

        /// <summary>
        /// Hides the end session panel.
        /// </summary>
        public void Hide()
        {
            if (showCoroutine != null)
            {
                StopCoroutine(showCoroutine);
                showCoroutine = null;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }

            if (panelTransform != null)
            {
                panelTransform.localScale = Vector3.zero;
            }

            gameObject.SetActive(false);
        }

        private IEnumerator AnimateShow()
        {
            float elapsed = 0f;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }

            if (panelTransform != null)
            {
                panelTransform.localScale = Vector3.one * 0.5f;
            }

            while (elapsed < Mathf.Max(fadeInDuration, scaleInDuration))
            {
                elapsed += Time.deltaTime;

                if (canvasGroup != null)
                {
                    float fadeT = Mathf.Clamp01(elapsed / fadeInDuration);
                    canvasGroup.alpha = appearCurve.Evaluate(fadeT);
                }

                if (panelTransform != null)
                {
                    float scaleT = Mathf.Clamp01(elapsed / scaleInDuration);
                    float scale = Mathf.LerpUnclamped(0.5f, 1f, appearCurve.Evaluate(scaleT));
                    panelTransform.localScale = Vector3.one * scale;
                }

                yield return null;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }

            if (panelTransform != null)
            {
                panelTransform.localScale = Vector3.one;
            }

            showCoroutine = null;
        }

        private void HandleRestartClick()
        {
            OnRestartClicked?.Invoke();
        }

        private void OnDestroy()
        {
            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(HandleRestartClick);
            }
        }
    }
}
