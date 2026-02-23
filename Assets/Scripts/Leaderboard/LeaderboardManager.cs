using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace LeaderboardGame
{
    /// <summary>
    /// Core leaderboard system. Manages entries, sorting, and rank updates.
    /// This is the heart of the game — the leaderboard IS the game.
    /// </summary>
    public class LeaderboardManager : MonoBehaviour
    {
        public static LeaderboardManager Instance { get; private set; }

        [Header("Config")]
        [SerializeField] private int maxVisibleEntries = 100;
        [SerializeField] private float updateInterval = 0.5f;

        [Header("Events")]
        public UnityEvent<List<LeaderboardEntry>> OnLeaderboardUpdated;
        public UnityEvent<int> OnPlayerRankChanged;

        private List<LeaderboardEntry> entries = new List<LeaderboardEntry>();
        private LeaderboardEntry localPlayer;
        private float updateTimer;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Initialize events (required for runtime-created components)
            if (OnLeaderboardUpdated == null)
                OnLeaderboardUpdated = new UnityEvent<List<LeaderboardEntry>>();
            if (OnPlayerRankChanged == null)
                OnPlayerRankChanged = new UnityEvent<int>();
        }

        private void Start()
        {
            InitializeWithFakeData();
        }

        private void Update()
        {
            updateTimer += Time.deltaTime;
            if (updateTimer >= updateInterval)
            {
                updateTimer = 0f;
                SimulateOtherPlayers();
                SortAndRank();
            }
        }

        /// <summary>
        /// Add score to the local player. This is how you "play" the game.
        /// </summary>
        public void AddPlayerScore(int amount)
        {
            if (localPlayer == null) return;
            
            int oldRank = localPlayer.Rank;
            localPlayer.Score += amount;
            SortAndRank();

            if (localPlayer.Rank != oldRank)
            {
                OnPlayerRankChanged?.Invoke(localPlayer.Rank);
            }
        }

        public LeaderboardEntry GetLocalPlayer() => localPlayer;
        public List<LeaderboardEntry> GetEntries() => entries;
        public int GetPlayerRank() => localPlayer?.Rank ?? -1;

        private void SortAndRank()
        {
            entries.Sort();
            for (int i = 0; i < entries.Count; i++)
            {
                entries[i].Rank = i + 1;
            }
            OnLeaderboardUpdated?.Invoke(entries);
        }

        private void InitializeWithFakeData()
        {
            // Create local player
            localPlayer = new LeaderboardEntry("local", "YOU", 0, true);
            entries.Add(localPlayer);

            // Populate with AI players
            string[] names = {
                "xXSlayerXx", "ProGamer99", "NoobMaster", "ShadowWolf",
                "PixelQueen", "BotLord", "CryptoKing", "NightOwl42",
                "SpeedDemon", "ChillGuy", "TryHard101", "CasualKaren",
                "MLG_Toast", "RageQuitter", "AFK_Andy", "SweatyPalms",
                "LagMaster", "360NoScope", "EzClap", "TouchGrass",
                "Smurf_Alt", "PogChamp", "GigaChad", "Ratio_King",
                "L_Collector", "WinStreak", "Carried", "1TapGod",
                "Boosted", "HardStuck"
            };

            for (int i = 0; i < names.Length; i++)
            {
                int score = Random.Range(50, 5000);
                entries.Add(new LeaderboardEntry($"bot_{i}", names[i], score));
            }

            SortAndRank();
        }

        /// <summary>
        /// Simulate other players gaining score over time to create pressure.
        /// </summary>
        private void SimulateOtherPlayers()
        {
            foreach (var entry in entries)
            {
                if (entry.IsLocalPlayer) continue;
                
                // Random chance of gaining points
                if (Random.value < 0.3f)
                {
                    entry.Score += Random.Range(1, 15);
                }
            }
        }
    }
}
