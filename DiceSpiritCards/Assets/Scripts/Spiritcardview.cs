using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controls the visual state of one Spirit Card on the UI Canvas.
/// Attach one SpiritCardView per Card UI element.
/// 
/// Inspector setup:
///   • cardData        → drag the matching SpiritCard ScriptableObject
///   • cardNameText    → TextMeshPro showing the card name
///   • descriptionText → TextMeshPro showing trigger + effect
///   • cardBackground  → Image component of the card panel
///   • glowImage       → separate Image behind the card used for glow
///   • particleEffect  → optional particle system that plays on activation
/// </summary>
public class SpiritCardView : MonoBehaviour
{
        // ──────────────────────────────────────────────
        // Inspector References
        // ──────────────────────────────────────────────

        [Header("Data")]
        [SerializeField] private SpiritCard cardData;

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI cardNameText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private Image cardBackground;
        [SerializeField] private Image glowImage;

        [Header("Effects")]
        [SerializeField] private ParticleSystem particleEffect;
        [SerializeField] private AudioClip activationSound;

        [Header("Animation Settings")]
        [SerializeField] private float activationScaleAmount = 1.2f;
        [SerializeField] private float activationDuration = 0.4f;
        [SerializeField] private float glowFadeSpeed = 2f;

        // ──────────────────────────────────────────────
        // Private State
        // ──────────────────────────────────────────────

        private Color _defaultBackgroundColor;
        private Vector3 _defaultScale;
        private AudioSource _audioSource;
        private bool _isAnimating = false;

        // ──────────────────────────────────────────────
        // Unity Lifecycle
        // ──────────────────────────────────────────────

        private void Awake()
        {
                _audioSource = GetComponent<AudioSource>();
                if (_audioSource == null)
                        _audioSource = gameObject.AddComponent<AudioSource>();

                if (cardBackground != null)
                        _defaultBackgroundColor = cardBackground.color;

                _defaultScale = transform.localScale;
        }

        private void Start()
        {
                PopulateCardText();
                SetGlowVisible(false);
        }

        // ──────────────────────────────────────────────
        // Public API
        // ──────────────────────────────────────────────

        /// <summary>
        /// Returns the ScriptableObject data for this card.
        /// GameManager uses this to check trigger conditions.
        /// </summary>
        public SpiritCard CardData => cardData;

        /// <summary>
        /// Play the card activation animation.
        /// Called by GameManager when this card's trigger fires.
        /// </summary>
        public void PlayActivationEffect()
        {
                if (_isAnimating) return;
                StartCoroutine(ActivationCoroutine());
        }

        /// <summary>
        /// Reset card to its idle / inactive visual state.
        /// Called at the start of every new roll.
        /// </summary>
        public void ResetVisuals()
        {
                StopAllCoroutines();
                _isAnimating = false;
                transform.localScale = _defaultScale;
                SetGlowVisible(false);

                if (cardBackground != null)
                        cardBackground.color = _defaultBackgroundColor;
        }

        // ──────────────────────────────────────────────
        // Private Helpers
        // ──────────────────────────────────────────────

        private void PopulateCardText()
        {
                if (cardData == null) return;

                if (cardNameText != null) cardNameText.text = cardData.cardName;
                if (descriptionText != null) descriptionText.text = cardData.GetEffectDescription();
        }

        private void SetGlowVisible(bool visible)
        {
                if (glowImage == null) return;
                Color c = cardData != null ? cardData.glowColor : Color.yellow;
                c.a = visible ? 1f : 0f;
                glowImage.color = c;
        }

        // ──────────────────────────────────────────────
        // Activation Coroutine (pure Unity — no DOTween)
        // ──────────────────────────────────────────────

        private IEnumerator ActivationCoroutine()
        {
                _isAnimating = true;

                // Play particle burst
                if (particleEffect != null)
                        particleEffect.Play();

                // Play activation sound
                if (activationSound != null && _audioSource != null)
                        _audioSource.PlayOneShot(activationSound);

                // ── Phase 1: Scale up + glow in ──
                float elapsed = 0f;
                float halfDuration = activationDuration * 0.5f;

                while (elapsed < halfDuration)
                {
                        elapsed += Time.deltaTime;
                        float t = elapsed / halfDuration;

                        // Smooth scale up
                        transform.localScale = Vector3.Lerp(
                            _defaultScale,
                            _defaultScale * activationScaleAmount,
                            EaseOutBack(t)
                        );

                        // Fade glow in
                        if (glowImage != null && cardData != null)
                        {
                                Color c = cardData.glowColor;
                                c.a = Mathf.Lerp(0f, 1f, t);
                                glowImage.color = c;
                        }

                        yield return null;
                }

                // ── Phase 2: Scale back down ──
                elapsed = 0f;
                Vector3 scaledUp = _defaultScale * activationScaleAmount;

                while (elapsed < halfDuration)
                {
                        elapsed += Time.deltaTime;
                        float t = elapsed / halfDuration;

                        transform.localScale = Vector3.Lerp(scaledUp, _defaultScale, EaseInOut(t));

                        yield return null;
                }

                transform.localScale = _defaultScale;

                // ── Phase 3: Pulse glow while card stays "active" ──
                yield return StartCoroutine(PulseGlow(1.5f));

                _isAnimating = false;
        }

        private IEnumerator PulseGlow(float duration)
        {
                float elapsed = 0f;

                while (elapsed < duration)
                {
                        elapsed += Time.deltaTime;

                        // Sine-wave pulse between 0.4 and 1.0 alpha
                        float alpha = 0.4f + 0.6f * Mathf.Abs(Mathf.Sin(elapsed * glowFadeSpeed * Mathf.PI));

                        if (glowImage != null && cardData != null)
                        {
                                Color c = cardData.glowColor;
                                c.a = alpha;
                                glowImage.color = c;
                        }

                        yield return null;
                }

                // Fade glow out
                SetGlowVisible(false);
        }

        // ──────────────────────────────────────────────
        // Easing Functions
        // ──────────────────────────────────────────────

        private static float EaseOutBack(float t)
        {
                const float c = 1.70158f;
                return 1f + (c + 1f) * Mathf.Pow(t - 1f, 3f) + c * Mathf.Pow(t - 1f, 2f);
        }

        private static float EaseInOut(float t)
        {
                return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
        }
}