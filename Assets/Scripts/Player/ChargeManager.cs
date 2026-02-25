using UnityEngine;
using UnityEngine.Events;

namespace LeaderboardGame
{
    /// <summary>
    /// Manages click charges: pool, recharge, and item-driven modifiers.
    /// Singleton — accessed by PlayerController, LeaderboardUIRuntime, ItemSystem.
    /// </summary>
    public class ChargeManager : MonoBehaviour
    {
        public static ChargeManager Instance { get; private set; }

        [Header("Config")]
        [SerializeField] private int defaultMaxCharges = 10;
        [SerializeField] private float defaultRechargeRate = 1f; // charges per second

        [Header("Events")]
        public UnityEvent<int, int> OnChargesChanged; // current, max
        public UnityEvent OnChargesDepleted;
        public UnityEvent OnChargesRefilled;

        private float currentCharges;
        private int maxCharges;
        private float rechargeRate;

        // Buff overrides (set by ItemSystem)
        private int buffMaxCharges = 0;       // 0 = no override
        private float buffRechargeMultiplier = 1f;
        private bool freeTaps = false;        // Rapid Fire: taps cost 0

        public int CurrentCharges => Mathf.FloorToInt(currentCharges);
        public int MaxCharges => buffMaxCharges > 0 ? buffMaxCharges : maxCharges;
        public float RechargeRate => defaultRechargeRate * buffRechargeMultiplier;
        public bool HasCharge => CurrentCharges > 0;
        public bool FreeTaps => freeTaps;
        public float FillPercent => MaxCharges > 0 ? currentCharges / MaxCharges : 0f;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (OnChargesChanged == null) OnChargesChanged = new UnityEvent<int, int>();
            if (OnChargesDepleted == null) OnChargesDepleted = new UnityEvent();
            if (OnChargesRefilled == null) OnChargesRefilled = new UnityEvent();

            maxCharges = defaultMaxCharges;
            rechargeRate = defaultRechargeRate;
            currentCharges = maxCharges;
        }

        private void Update()
        {
            if (currentCharges < MaxCharges)
            {
                float oldCharges = currentCharges;
                currentCharges += RechargeRate * Time.deltaTime;
                currentCharges = Mathf.Min(currentCharges, MaxCharges);

                // Fire event when we cross an integer boundary
                if (Mathf.FloorToInt(currentCharges) != Mathf.FloorToInt(oldCharges))
                {
                    OnChargesChanged?.Invoke(CurrentCharges, MaxCharges);

                    if (Mathf.FloorToInt(oldCharges) == 0 && CurrentCharges > 0)
                        OnChargesRefilled?.Invoke();
                }
            }
        }

        /// <summary>Try to consume 1 charge. Returns false if no charges available.</summary>
        public bool TryConsume()
        {
            if (freeTaps) return true; // Rapid Fire: free

            if (CurrentCharges <= 0) return false;

            currentCharges -= 1f;
            currentCharges = Mathf.Max(0f, currentCharges);
            OnChargesChanged?.Invoke(CurrentCharges, MaxCharges);

            if (CurrentCharges <= 0)
                OnChargesDepleted?.Invoke();

            return true;
        }

        /// <summary>Instantly refill charges to max.</summary>
        public void Refill()
        {
            currentCharges = MaxCharges;
            OnChargesChanged?.Invoke(CurrentCharges, MaxCharges);
            OnChargesRefilled?.Invoke();
        }

        /// <summary>Set charges to a specific value (for Overcharge).</summary>
        public void SetCharges(int amount)
        {
            currentCharges = Mathf.Min(amount, MaxCharges);
            OnChargesChanged?.Invoke(CurrentCharges, MaxCharges);
        }

        // === Buff API (called by ItemSystem) ===

        public void SetBuffMaxCharges(int max)
        {
            buffMaxCharges = max;
            OnChargesChanged?.Invoke(CurrentCharges, MaxCharges);
        }

        public void ClearBuffMaxCharges()
        {
            buffMaxCharges = 0;
            // Clamp charges down if over new max
            if (currentCharges > MaxCharges)
                currentCharges = MaxCharges;
            OnChargesChanged?.Invoke(CurrentCharges, MaxCharges);
        }

        public void SetRechargeMultiplier(float mult)
        {
            buffRechargeMultiplier = mult;
        }

        public void ClearRechargeMultiplier()
        {
            buffRechargeMultiplier = 1f;
        }

        public void SetFreeTaps(bool free)
        {
            freeTaps = free;
        }
    }
}
