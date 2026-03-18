using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Handles all dice rolling logic: animation, random result, and result event emission.
/// Attach to the Dice GameObject in the scene.
/// </summary>
public class DiceRoller : MonoBehaviour
{
        // ──────────────────────────────────────────────
        // Inspector Settings
        // ──────────────────────────────────────────────

        [Header("Roll Animation Settings")]
        [SerializeField] private float rollDuration = 1.5f;   // Total spin duration in seconds
        [SerializeField] private float bounceHeight = 0.4f;   // How high the dice bounces
        [SerializeField] private float rotationSpeed = 720f;   // Degrees per second during spin
        [SerializeField] private AudioClip rollSound;             // Assign in Inspector
        [SerializeField] private AudioClip settleSound;           // Assign in Inspector

        [Header("Screen Shake")]
        [SerializeField] private bool useScreenShake = true;
        [SerializeField] private float shakeIntensity = 0.1f;
        [SerializeField] private float shakeDuration = 0.3f;

        // ──────────────────────────────────────────────
        // Events
        // ──────────────────────────────────────────────

        /// <summary>
        /// Fired when the dice has finished rolling. Passes the result (1–6).
        /// GameManager should subscribe to this.
        /// </summary>
        public event Action<int> OnRollFinished;

        // ──────────────────────────────────────────────
        // Private State
        // ──────────────────────────────────────────────

        private Vector3 _startPosition;
        private Quaternion _startRotation;
        private AudioSource _audioSource;
        private bool _isRolling = false;
        private int _forcedResult = -1;   // -1 = use Random; set via ForceResult()

        // ──────────────────────────────────────────────
        // Unity Lifecycle
        // ──────────────────────────────────────────────

        private void Awake()
        {
                _startPosition = transform.localPosition;
                _startRotation = transform.localRotation;
                _audioSource = GetComponent<AudioSource>();

                // Auto-add AudioSource if missing
                if (_audioSource == null)
                        _audioSource = gameObject.AddComponent<AudioSource>();
        }

        // ──────────────────────────────────────────────
        // Public API
        // ──────────────────────────────────────────────

        /// <summary>
        /// Begin the roll sequence. Called by GameManager.
        /// </summary>
        public void RollDice()
        {
                if (_isRolling) return;
                StartCoroutine(RollCoroutine());
        }

        /// <summary>
        /// Forces the next roll to return a specific value (1–6).
        /// Useful for debugging and QA.
        /// </summary>
        /// <param name="value">Desired result (1–6)</param>
        public void ForceResult(int value)
        {
                _forcedResult = Mathf.Clamp(value, 1, 6);
                Debug.Log($"[DiceRoller] Next roll forced to: {_forcedResult}");
        }

        // ──────────────────────────────────────────────
        // Roll Coroutine
        // ──────────────────────────────────────────────

        private IEnumerator RollCoroutine()
        {
                _isRolling = true;

                // ── 1. Play roll sound ──
                PlaySound(rollSound);

                // ── 2. Animate dice (shake + spin + bounce) ──
                yield return StartCoroutine(AnimateRoll());

                // ── 3. Generate result ──
                int result = (_forcedResult > 0) ? _forcedResult : UnityEngine.Random.Range(1, 7);
                _forcedResult = -1;   // Reset forced result after use

                // ── 4. Play settle sound ──
                PlaySound(settleSound);

                // ── 5. Screen shake ──
                if (useScreenShake)
                        yield return StartCoroutine(ShakeCamera());

                // ── 6. Reset dice to start pose ──
                transform.localPosition = _startPosition;
                transform.localRotation = _startRotation;

                _isRolling = false;

                // ── 7. Fire event to GameManager ──
                Debug.Log($"[DiceRoller] Roll finished. Result: {result}");
                OnRollFinished?.Invoke(result);
        }

        // ──────────────────────────────────────────────
        // Animation Helpers
        // ──────────────────────────────────────────────

        private IEnumerator AnimateRoll()
        {
                float elapsed = 0f;

                while (elapsed < rollDuration)
                {
                        elapsed += Time.deltaTime;
                        float t = elapsed / rollDuration;

                        // Spin the dice on all axes
                        transform.Rotate(
                            rotationSpeed * Time.deltaTime,
                            rotationSpeed * 0.7f * Time.deltaTime,
                            rotationSpeed * 0.5f * Time.deltaTime
                        );

                        // Bounce up and down using a sine wave that dampens over time
                        float bounce = Mathf.Sin(t * Mathf.PI * 6f) * bounceHeight * (1f - t);
                        transform.localPosition = _startPosition + Vector3.up * bounce;

                        yield return null;
                }
        }

        private IEnumerator ShakeCamera()
        {
                Camera cam = Camera.main;
                if (cam == null) yield break;

                Vector3 originalPos = cam.transform.localPosition;
                float elapsed = 0f;

                while (elapsed < shakeDuration)
                {
                        elapsed += Time.deltaTime;
                        float magnitude = shakeIntensity * (1f - elapsed / shakeDuration);

                        cam.transform.localPosition = originalPos + new Vector3(
                            UnityEngine.Random.Range(-magnitude, magnitude),
                            UnityEngine.Random.Range(-magnitude, magnitude),
                            0f
                        );

                        yield return null;
                }

                cam.transform.localPosition = originalPos;
        }

        // ──────────────────────────────────────────────
        // Audio Helper
        // ──────────────────────────────────────────────

        private void PlaySound(AudioClip clip)
        {
                if (clip != null && _audioSource != null)
                        _audioSource.PlayOneShot(clip);
        }
}