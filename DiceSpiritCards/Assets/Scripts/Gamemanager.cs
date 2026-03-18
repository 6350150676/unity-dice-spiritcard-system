using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Master game controller. Wires all systems together and drives the game loop.
///
/// Game Flow:
///   Roll Button pressed
///     → DiceRoller.RollDice()
///     → DiceRoller.OnRollFinished fires
///     → GameCalculator.SetupFromDiceRoll()
///     → Spirit Cards checked and applied
///     → GameCalculator.Calculate()
///     → UIEquationView.AnimateEquation()
///     → Roll History updated
///
/// Inspector setup:
///   Drag each component reference into the matching slot.
/// </summary>
public class GameManager : MonoBehaviour
{
    // ──────────────────────────────────────────────
    // Inspector References
    // ──────────────────────────────────────────────

    [Header("Core Systems")]
    [SerializeField] private DiceRoller      diceRoller;
    [SerializeField] private GameCalculator  calculator;
    [SerializeField] private UIEquationView  equationView;

    [Header("Spirit Cards")]
    [Tooltip("Drag all SpiritCardView components in the Spirit Cards Panel here.")]
    [SerializeField] private List<SpiritCardView> spiritCardViews = new();

    [Header("UI Controls")]
    [SerializeField] private Button rollButton;

    [Header("Roll History (Optional)")]
    [SerializeField] private Transform         rollHistoryContainer;   // Parent of history entries
    [SerializeField] private TextMeshProUGUI   rollHistoryItemPrefab;  // Single-number label prefab
    [SerializeField] private int               maxHistoryItems = 5;

    [Header("Debug Controls (Optional)")]
    [SerializeField] private Button debugForce3Button;
    [SerializeField] private Button debugForce6Button;

    // ──────────────────────────────────────────────
    // Private State
    // ──────────────────────────────────────────────

    private readonly Queue<int> _rollHistory = new();
    private bool                _isProcessing = false;

    // ──────────────────────────────────────────────
    // Unity Lifecycle
    // ──────────────────────────────────────────────

    private void Awake()
    {
        ValidateReferences();
    }

    private void Start()
    {
        // Subscribe to dice result event
        if (diceRoller != null)
            diceRoller.OnRollFinished += HandleRollResult;

        // Wire up roll button
        if (rollButton != null)
            rollButton.onClick.AddListener(OnRollButtonPressed);

        // Wire up debug buttons
        if (debugForce3Button != null)
            debugForce3Button.onClick.AddListener(() => ForceRoll(3));

        if (debugForce6Button != null)
            debugForce6Button.onClick.AddListener(() => ForceRoll(6));

        equationView?.ResetDisplay();
    }

    private void OnDestroy()
    {
        // Always unsubscribe to prevent memory leaks
        if (diceRoller != null)
            diceRoller.OnRollFinished -= HandleRollResult;
    }

    // ──────────────────────────────────────────────
    // Button Handlers
    // ──────────────────────────────────────────────

    private void OnRollButtonPressed()
    {
        if (_isProcessing) return;
        StartRoll();
    }

    // ──────────────────────────────────────────────
    // Roll Flow
    // ──────────────────────────────────────────────

    private void StartRoll()
    {
        _isProcessing = true;

        // Disable roll button during animation
        SetRollButtonInteractable(false);

        // Reset calculator and cards to clean state
        calculator?.Reset();
        ResetAllCardVisuals();

        // Show rolling state in UI
        equationView?.ShowRollingState();

        // Begin dice animation
        diceRoller?.RollDice();
    }

    /// <summary>
    /// Called by DiceRoller.OnRollFinished event.
    /// This is the heart of the game loop.
    /// </summary>
    private void HandleRollResult(int diceResult)
    {
        StartCoroutine(ProcessRollResult(diceResult));
    }

    private IEnumerator ProcessRollResult(int diceResult)
    {
        Debug.Log($"[GameManager] Processing dice result: {diceResult}");

        // ── Step 1: Set base values ──
        calculator.SetupFromDiceRoll(diceResult);

        // ── Step 2: Apply Spirit Cards (small delay between each for visual clarity) ──
        foreach (var cardView in spiritCardViews)
        {
            if (cardView == null || cardView.CardData == null) continue;

            if (cardView.CardData.IsTriggered(diceResult))
            {
                Debug.Log($"[GameManager] '{cardView.CardData.cardName}' triggered!");
                cardView.CardData.ApplyEffect(calculator);
                cardView.PlayActivationEffect();

                // Brief pause so the player can see each card activating
                yield return new WaitForSeconds(0.4f);
            }
        }

        // ── Step 3: Final calculation ──
        calculator.Calculate();

        // ── Step 4: Add to roll history ──
        AddToRollHistory(diceResult);

        // ── Step 5: Animate equation UI ──
        equationView?.AnimateEquation(calculator.Points, calculator.Multiplier, calculator.Total);

        // Note: Roll Button is re-enabled by UIEquationView after animation completes.
        _isProcessing = false;
    }

    // ──────────────────────────────────────────────
    // Debug API
    // ──────────────────────────────────────────────

    /// <summary>
    /// Forces the next roll to a specific value.
    /// Wired to debug buttons in the Inspector.
    /// </summary>
    public void ForceRoll(int value)
    {
        if (_isProcessing) return;
        diceRoller?.ForceResult(value);
        StartRoll();
    }

    // ──────────────────────────────────────────────
    // Roll History
    // ──────────────────────────────────────────────

    private void AddToRollHistory(int result)
    {
        if (rollHistoryContainer == null || rollHistoryItemPrefab == null) return;

        _rollHistory.Enqueue(result);

        // Keep history capped at maxHistoryItems
        while (_rollHistory.Count > maxHistoryItems)
            _rollHistory.Dequeue();

        // Rebuild history UI
        RefreshHistoryUI();
    }

    private void RefreshHistoryUI()
    {
        // Clear existing children
        foreach (Transform child in rollHistoryContainer)
            Destroy(child.gameObject);

        // Spawn new labels (most recent last)
        foreach (int roll in _rollHistory)
        {
            TextMeshProUGUI item = Instantiate(rollHistoryItemPrefab, rollHistoryContainer);
            item.text = roll.ToString();
        }
    }

    // ──────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────

    private void SetRollButtonInteractable(bool state)
    {
        if (rollButton != null)
            rollButton.interactable = state;
    }

    private void ResetAllCardVisuals()
    {
        foreach (var card in spiritCardViews)
            card?.ResetVisuals();
    }

    private void ValidateReferences()
    {
        if (diceRoller   == null) Debug.LogError("[GameManager] DiceRoller reference is missing!");
        if (calculator   == null) Debug.LogError("[GameManager] GameCalculator reference is missing!");
        if (equationView == null) Debug.LogError("[GameManager] UIEquationView reference is missing!");
        if (rollButton   == null) Debug.LogWarning("[GameManager] Roll Button not assigned.");
    }
}