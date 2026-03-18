using UnityEngine;

// ──────────────────────────────────────────────────────────────────────────────
// Enums — defined outside the class so they're accessible project-wide
// ──────────────────────────────────────────────────────────────────────────────

/// <summary>
/// The condition that must be true for the card to activate.
/// </summary>
public enum CardTriggerType
{
        DiceEquals,          // Dice result == TriggerValue
        DiceGreaterThan,     // Dice result >  TriggerValue
        DiceLessThan,        // Dice result <  TriggerValue
        Always               // Always fires (useful for testing)
}

/// <summary>
/// What the card does when triggered.
/// </summary>
public enum CardEffectType
{
        OverrideMultiplier,  // Set Multiplier = EffectValue         (Card A behaviour)
        AddToPoints,         // Add EffectValue to Points            (Card B behaviour)
        MultiplyPoints,      // Multiply Points × EffectValue
        AddToMultiplier      // Add EffectValue to current Multiplier
}

// ──────────────────────────────────────────────────────────────────────────────
// ScriptableObject
// ──────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Data-only ScriptableObject for a Spirit Card.
/// Create assets via: Assets → Create → DiceGame → Spirit Card
/// Each card asset lives in Assets/ScriptableObjects/.
/// </summary>
[CreateAssetMenu(fileName = "SpiritCard", menuName = "DiceGame/Spirit Card", order = 0)]
public class SpiritCard : ScriptableObject
{
        [Header("Identity")]
        [Tooltip("Display name shown on the card face.")]
        public string cardName = "Spirit Card";

        [TextArea(2, 4)]
        [Tooltip("Flavour text shown below the card name.")]
        public string description = "A mysterious spirit card.";

        [Header("Trigger")]
        [Tooltip("When should this card activate?")]
        public CardTriggerType triggerType = CardTriggerType.DiceEquals;

        [Tooltip("The dice value used in the trigger comparison.")]
        public int triggerValue = 6;

        [Header("Effect")]
        [Tooltip("What does the card do when triggered?")]
        public CardEffectType effectType = CardEffectType.OverrideMultiplier;

        [Tooltip("The value applied by the effect.")]
        public int effectValue = 2;

        [Header("Visuals")]
        [Tooltip("Colour used for the card glow / highlight effect.")]
        public Color glowColor = Color.yellow;

        // ──────────────────────────────────────────────
        // Logic
        // ──────────────────────────────────────────────

        /// <summary>
        /// Returns true if this card should activate for the given dice result.
        /// </summary>
        public bool IsTriggered(int diceResult)
        {
                return triggerType switch
                {
                        CardTriggerType.DiceEquals => diceResult == triggerValue,
                        CardTriggerType.DiceGreaterThan => diceResult > triggerValue,
                        CardTriggerType.DiceLessThan => diceResult < triggerValue,
                        CardTriggerType.Always => true,
                        _ => false
                };
        }

        /// <summary>
        /// Applies this card's effect to the calculator.
        /// </summary>
        public void ApplyEffect(GameCalculator calculator)
        {
                switch (effectType)
                {
                        case CardEffectType.OverrideMultiplier:
                                calculator.SetMultiplier(effectValue);
                                break;

                        case CardEffectType.AddToPoints:
                                calculator.AddToPoints(effectValue);
                                break;

                        case CardEffectType.MultiplyPoints:
                                // Multiply current points by effectValue
                                calculator.AddToPoints(calculator.Points * (effectValue - 1));
                                break;

                        case CardEffectType.AddToMultiplier:
                                calculator.SetMultiplier(calculator.Multiplier + effectValue);
                                break;
                }

                Debug.Log($"[SpiritCard] '{cardName}' applied effect: {effectType} ({effectValue})");
        }

        /// <summary>
        /// Human-readable string describing what the card does.
        /// Used for tooltips / debugging.
        /// </summary>
        public string GetEffectDescription()
        {
                return effectType switch
                {
                        CardEffectType.OverrideMultiplier => $"Multiplier becomes {effectValue}",
                        CardEffectType.AddToPoints => $"Points + {effectValue}",
                        CardEffectType.MultiplyPoints => $"Points × {effectValue}",
                        CardEffectType.AddToMultiplier => $"Multiplier + {effectValue}",
                        _ => "Unknown effect"
                };
        }
}