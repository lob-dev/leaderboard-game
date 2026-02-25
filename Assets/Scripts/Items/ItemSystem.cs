using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace LeaderboardGame
{
    /// <summary>
    /// Manages item spawning, active effects, and expiry.
    /// Singleton — accessed by PlayerController and LeaderboardManager.
    /// </summary>
    public class ItemSystem : MonoBehaviour
    {
        public static ItemSystem Instance { get; private set; }

        [Header("Spawn Config")]
        [SerializeField] private float minSpawnInterval = 8f;
        [SerializeField] private float maxSpawnInterval = 15f;
        [SerializeField] private float itemLifetime = 5f; // how long an uncollected item stays
        [SerializeField] private int autoTapRate = 6; // taps per second during AutoTap

        [Header("Events")]
        public UnityEvent<ItemType> OnItemCollected;
        public UnityEvent<ItemType> OnItemExpired;
        public UnityEvent<ItemType, float, Vector2> OnItemSpawned; // type, lifetime, normalized position

        private Dictionary<ItemType, float> activeTimers = new Dictionary<ItemType, float>();
        private float spawnTimer;
        private float nextSpawnTime;

        // AutoTap state
        private float autoTapTimer;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (OnItemCollected == null) OnItemCollected = new UnityEvent<ItemType>();
            if (OnItemExpired == null) OnItemExpired = new UnityEvent<ItemType>();
            if (OnItemSpawned == null) OnItemSpawned = new UnityEvent<ItemType, float, Vector2>();
            ResetSpawnTimer();
        }

        private void Update()
        {
            // Tick active item durations
            var expired = new List<ItemType>();
            var keys = new List<ItemType>(activeTimers.Keys);
            foreach (var key in keys)
            {
                activeTimers[key] -= Time.deltaTime;
                if (activeTimers[key] <= 0f)
                    expired.Add(key);
            }
            foreach (var key in expired)
            {
                activeTimers.Remove(key);
                HandleItemExpiry(key);
                OnItemExpired?.Invoke(key);
                Debug.Log($"[ItemSystem] Item expired: {key}");
            }

            // AutoTap effect
            if (IsActive(ItemType.AutoTap))
            {
                autoTapTimer += Time.deltaTime;
                float interval = 1f / autoTapRate;
                while (autoTapTimer >= interval)
                {
                    autoTapTimer -= interval;
                    var player = FindObjectOfType<PlayerController>();
                    if (player != null) player.OnTap();
                }
            }
            else
            {
                autoTapTimer = 0f;
            }

            // Spawn timer
            spawnTimer += Time.deltaTime;
            if (spawnTimer >= nextSpawnTime)
            {
                SpawnRandomItem();
                ResetSpawnTimer();
            }
        }

        /// <summary>Check if an item effect is currently active.</summary>
        public bool IsActive(ItemType type) => activeTimers.ContainsKey(type);

        /// <summary>Get remaining duration for an active item. 0 if not active.</summary>
        public float GetRemainingTime(ItemType type) =>
            activeTimers.ContainsKey(type) ? activeTimers[type] : 0f;

        /// <summary>Get all currently active items and their remaining times.</summary>
        public Dictionary<ItemType, float> GetActiveItems() => new Dictionary<ItemType, float>(activeTimers);

        /// <summary>Get the score multiplier from active items.</summary>
        public int GetScoreMultiplier()
        {
            int mult = 1;
            if (IsActive(ItemType.DoublePoints)) mult *= 2;
            return mult;
        }

        /// <summary>Get the damage multiplier from active items (DamageBoost = 1.5x).</summary>
        public float GetDamageMultiplier()
        {
            float mult = 1f;
            if (IsActive(ItemType.DamageBoost)) mult *= 1.5f;
            return mult;
        }

        /// <summary>Get combo bonus multiplier from active items.</summary>
        public int GetComboBonusMultiplier()
        {
            return IsActive(ItemType.ComboBooster) ? 2 : 1;
        }

        /// <summary>Whether opponent simulation should be frozen.</summary>
        public bool AreOpponentsFrozen() => IsActive(ItemType.FreezeOpponents);

        /// <summary>Collect an item — activate its effect.</summary>
        public void CollectItem(ItemType type)
        {
            var data = ItemDefinitions.Get(type);
            Debug.Log($"[ItemSystem] Collected: {data.Name}");

            if (data.Duration > 0f)
            {
                // Duration-based: set or refresh timer
                activeTimers[type] = data.Duration;
                ApplyBuffStart(type);
            }
            else
            {
                // Instant effect
                ApplyInstantEffect(type);
            }

            OnItemCollected?.Invoke(type);
        }

        private void ApplyBuffStart(ItemType type)
        {
            var cm = ChargeManager.Instance;
            if (cm == null) return;

            switch (type)
            {
                case ItemType.ChargeRush:
                    cm.SetRechargeMultiplier(2f);
                    break;
                case ItemType.MaxCapacity:
                    cm.SetBuffMaxCharges(15);
                    break;
                case ItemType.RapidFire:
                    cm.SetFreeTaps(true);
                    break;
                case ItemType.Overcharge:
                    cm.SetBuffMaxCharges(20);
                    cm.SetCharges(20);
                    break;
            }
        }

        private void HandleItemExpiry(ItemType type)
        {
            var cm = ChargeManager.Instance;
            if (cm == null) return;

            switch (type)
            {
                case ItemType.ChargeRush:
                    cm.ClearRechargeMultiplier();
                    break;
                case ItemType.MaxCapacity:
                    cm.ClearBuffMaxCharges();
                    break;
                case ItemType.RapidFire:
                    cm.SetFreeTaps(false);
                    break;
                case ItemType.Overcharge:
                    cm.ClearBuffMaxCharges();
                    break;
            }
        }

        private void ApplyInstantEffect(ItemType type)
        {
            switch (type)
            {
                case ItemType.ScoreBomb:
                    if (LeaderboardManager.Instance != null)
                        LeaderboardManager.Instance.AddPlayerScore(500);
                    break;
                case ItemType.InstantReload:
                    if (ChargeManager.Instance != null)
                        ChargeManager.Instance.Refill();
                    break;
            }
        }

        private void SpawnRandomItem()
        {
            var values = (ItemType[])System.Enum.GetValues(typeof(ItemType));
            var type = values[Random.Range(0, values.Length)];
            var pos = new Vector2(Random.Range(0.15f, 0.85f), Random.Range(0.4f, 0.8f));
            OnItemSpawned?.Invoke(type, itemLifetime, pos);
            Debug.Log($"[ItemSystem] Spawned: {type} at ({pos.x:F2}, {pos.y:F2})");
        }

        private void ResetSpawnTimer()
        {
            spawnTimer = 0f;
            nextSpawnTime = Random.Range(minSpawnInterval, maxSpawnInterval);
        }
    }
}
