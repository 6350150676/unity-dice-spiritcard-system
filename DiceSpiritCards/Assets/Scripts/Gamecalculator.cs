using System;
using UnityEngine;

/// <summary>
/// Pure data and calculation class. Stores Points, Multiplier, and Total.
/// Has no Unity dependencies — easy to unit test.
/// Attach to the Systems GameObject in the scene.
/// </summary>
public class GameCalculator : MonoBehaviour
{
        // ──────────────────────────────────────────────
        // Constants
        // ──────────────────────────────────────────────

        public const int DEFAULT_MULTIPLIER = 10;

        // ──────────────────────────────────────────────
        // Events
        // ──────────────────────────────────────────────

        /// <summary>
        /// Fired whenever the equation values change.
        /// UIEquationView subscribes to re-draw the display.
        /// </summary>
        public event Action<int, int, int> OnEquationUpdated; // (points, multiplier, total)

        // ──────────────────────────────────────────────
        // Read-Only Properties
        // ──────────────────────────────────────────────

        public int Points { get; private set; }
        public int Multiplier { get; private set; }
        public int Total { get; private set; }

        // ──────────────────────────────────────────────
        // Public API
        // ──────────────────────────────────────────────

        /// <summary>
        /// Initialise the equation from a fresh dice roll.
        /// Points = diceResult, Multiplier = 10.
        /// </summary>
        public void SetupFromDiceRoll(int diceResult)
        {
                Points = diceResult;
                Multiplier = DEFAULT_MULTIPLIER;

                Debug.Log($"[GameCalculator] Setup: {Points} × {Multiplier}");
        }

        /// <summary>
        /// Override the multiplier (used by Spirit Card A).
        /// </summary>
        public void SetMultiplier(int value)
        {
                Multiplier = value;
                Debug.Log($"[GameCalculator] Multiplier overridden to: {Multiplier}");
        }

        /// <summary>
        /// Add to points (used by Spirit Card B).
        /// </summary>
        public void AddToPoints(int amount)
        {
                Points += amount;
                Debug.Log($"[GameCalculator] Points increased by {amount} → now {Points}");
        }

        /// <summary>
        /// Perform the final multiplication and fire the update event.
        /// Call AFTER all Spirit Cards have applied their effects.
        /// </summary>
        public void Calculate()
        {
                Total = Points * Multiplier;
                Debug.Log($"[GameCalculator] Final equation: {Points} × {Multiplier} = {Total}");
                OnEquationUpdated?.Invoke(Points, Multiplier, Total);
        }

        /// <summary>
        /// Resets all values back to zero.
        /// Called at the start of a new roll.
        /// </summary>
        public void Reset()
        {
                Points = 0;
                Multiplier = DEFAULT_MULTIPLIER;
                Total = 0;
        }
}