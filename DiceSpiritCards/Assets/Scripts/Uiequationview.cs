using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Owns all equation display logic: Points × Multiplier = Total.
/// Animates number count-ups and the final total reveal.
/// Attach to the Systems GameObject in the scene.
///
/// Inspector setup:
///   • pointsText      → TextMeshPro in Equation Panel
///   • multiplierText  → TextMeshPro in Equation Panel
///   • totalText       → TextMeshPro in Equation Panel
///   • equationPanel   → The RectTransform containing all text (used for panel pop)
///   • rollButton      → Assign to re-enable after animation completes
/// </summary>
public class UIEquationView : MonoBehaviour
{
        // ──────────────────────────────────────────────
        // Inspector References
        // ──────────────────────────────────────────────

        [Header("Equation Text")]
        [SerializeField] private TextMeshProUGUI pointsText;
        [SerializeField] private TextMeshProUGUI multiplierText;
        [SerializeField] private TextMeshProUGUI totalText;

        [Header("Containers")]
        [SerializeField] private RectTransform equationPanel;
        [SerializeField] private Button rollButton;

        [Header("Animation Settings")]
        [SerializeField] private float countUpDuration = 0.8f;   // Duration of the count-up
        [SerializeField] private float totalPopScale = 1.3f;   // Scale pop for the total reveal
        [SerializeField] private float totalPopDuration = 0.3f;

        [Header("Colours")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color highlightColor = Color.yellow;

        // ──────────────────────────────────────────────
        // Unity Lifecycle
        // ──────────────────────────────────────────────

        private void Start()
        {
                ResetDisplay();
        }

        // ──────────────────────────────────────────────
        // Public API
        // ──────────────────────────────────────────────

        /// <summary>
        /// Shows "? × 10 = ?" to indicate a roll is in progress.
        /// </summary>
        public void ShowRollingState()
        {
                SetPointsText("?");
                SetMultiplierText("10");
                SetTotalText("?");
        }

        /// <summary>
        /// Animate the full equation reveal.
        /// Called by GameManager after calculation is done.
        /// </summary>
        public void AnimateEquation(int points, int multiplier, int total)
        {
                StartCoroutine(EquationRevealCoroutine(points, multiplier, total));
        }

        /// <summary>
        /// Resets all text to zero/default without animation.
        /// </summary>
        public void ResetDisplay()
        {
                SetPointsText("0");
                SetMultiplierText(GameCalculator.DEFAULT_MULTIPLIER.ToString());
                SetTotalText("0");

                if (totalText != null)
                        totalText.color = normalColor;
        }

        // ──────────────────────────────────────────────
        // Coroutines
        // ──────────────────────────────────────────────

        private IEnumerator EquationRevealCoroutine(int points, int multiplier, int total)
        {
                // ── Step 1: Reveal Points ──
                yield return StartCoroutine(CountUpText(pointsText, 0, points, countUpDuration * 0.4f));

                // ── Step 2: Reveal Multiplier ──
                yield return StartCoroutine(CountUpText(multiplierText, 0, multiplier, countUpDuration * 0.3f));

                // ── Step 3: Reveal Total with pop ──
                yield return StartCoroutine(CountUpText(totalText, 0, total, countUpDuration));
                yield return StartCoroutine(PopText(totalText));

                // ── Step 4: Re-enable Roll Button ──
                EnableRollButton(true);
        }

        /// <summary>
        /// Counts a TextMeshPro label up from startVal to endVal over duration seconds.
        /// Also briefly highlights the text during the count.
        /// </summary>
        private IEnumerator CountUpText(TextMeshProUGUI label, int startVal, int endVal, float duration)
        {
                if (label == null) yield break;

                label.color = highlightColor;

                float elapsed = 0f;

                while (elapsed < duration)
                {
                        elapsed += Time.deltaTime;
                        float t = Mathf.Clamp01(elapsed / duration);
                        float eased = EaseOutQuart(t);
                        int current = Mathf.RoundToInt(Mathf.Lerp(startVal, endVal, eased));

                        label.text = current.ToString();
                        yield return null;
                }

                label.text = endVal.ToString();
                label.color = normalColor;
        }

        /// <summary>
        /// Scales a TextMeshPro label up and back down (the "pop" effect).
        /// </summary>
        private IEnumerator PopText(TextMeshProUGUI label)
        {
                if (label == null) yield break;

                // Highlight total in yellow before pop
                label.color = highlightColor;

                Vector3 originalScale = label.transform.localScale;
                float elapsed = 0f;

                // Scale up
                while (elapsed < totalPopDuration * 0.5f)
                {
                        elapsed += Time.deltaTime;
                        float t = elapsed / (totalPopDuration * 0.5f);
                        label.transform.localScale = Vector3.Lerp(originalScale, originalScale * totalPopScale, EaseOutBack(t));
                        yield return null;
                }

                // Scale back
                elapsed = 0f;
                Vector3 bigScale = originalScale * totalPopScale;

                while (elapsed < totalPopDuration * 0.5f)
                {
                        elapsed += Time.deltaTime;
                        float t = elapsed / (totalPopDuration * 0.5f);
                        label.transform.localScale = Vector3.Lerp(bigScale, originalScale, EaseInOut(t));
                        yield return null;
                }

                label.transform.localScale = originalScale;
                label.color = highlightColor;   // Stay yellow after reveal
        }

        // ──────────────────────────────────────────────
        // Helpers
        // ──────────────────────────────────────────────

        private void SetPointsText(string s) { if (pointsText != null) pointsText.text = s; }
        private void SetMultiplierText(string s) { if (multiplierText != null) multiplierText.text = s; }
        private void SetTotalText(string s) { if (totalText != null) totalText.text = s; }

        private void EnableRollButton(bool state)
        {
                if (rollButton != null)
                        rollButton.interactable = state;
        }

        // ──────────────────────────────────────────────
        // Easing
        // ──────────────────────────────────────────────

        private static float EaseOutQuart(float t) => 1f - Mathf.Pow(1f - t, 4f);
        private static float EaseOutBack(float t)
        {
                const float c = 1.70158f;
                return 1f + (c + 1f) * Mathf.Pow(t - 1f, 3f) + c * Mathf.Pow(t - 1f, 2f);
        }
        private static float EaseInOut(float t) => t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
}