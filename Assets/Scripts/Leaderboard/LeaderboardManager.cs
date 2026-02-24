using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace LeaderboardGame
{
    /// <summary>
    /// Core leaderboard system. Manages entries, sorting, and rank updates.
    /// Supports both offline (bot) mode and online (SpacetimeDB) mode.
    /// </summary>
    public class LeaderboardManager : MonoBehaviour
    {
        public static LeaderboardManager Instance { get; private set; }

        [Header("Config")]
        [SerializeField] private int maxVisibleEntries = 100;
        [SerializeField] private float updateInterval = 0.5f;

        [Header("Mode")]
        [Tooltip("When true, uses SpacetimeDB for online multiplayer. When false, uses local bots.")]
        [SerializeField] private bool onlineMode = true;
        [Tooltip("Keep bots alongside online players for a fuller leaderboard")]
        [SerializeField] private bool fillWithBots = true;
        [SerializeField] private int minBotsWhenOnline = 10;

        [Header("Events")]
        public UnityEvent<List<LeaderboardEntry>> OnLeaderboardUpdated;
        public UnityEvent<int> OnPlayerRankChanged;

        private List<LeaderboardEntry> entries = new List<LeaderboardEntry>();
        private List<LeaderboardEntry> botEntries = new List<LeaderboardEntry>();
        private LeaderboardEntry localPlayer;
        private float updateTimer;
        private bool isOnline;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (OnLeaderboardUpdated == null)
                OnLeaderboardUpdated = new UnityEvent<List<LeaderboardEntry>>();
            if (OnPlayerRankChanged == null)
                OnPlayerRankChanged = new UnityEvent<int>();
        }

        private void Start()
        {
            if (onlineMode && SpacetimeDBManager.Instance != null)
            {
                // Wait for SpacetimeDB connection
                SpacetimeDBManager.Instance.OnConnectedToServer += OnServerConnected;
                SpacetimeDBManager.Instance.OnDisconnectedFromServer += OnServerDisconnected;
                
                // Start in offline mode until connected
                InitializeWithFakeData();
            }
            else
            {
                InitializeWithFakeData();
            }
        }

        private void OnDestroy()
        {
            if (SpacetimeDBManager.Instance != null)
            {
                SpacetimeDBManager.Instance.OnConnectedToServer -= OnServerConnected;
                SpacetimeDBManager.Instance.OnDisconnectedFromServer -= OnServerDisconnected;
            }
        }

        private void Update()
        {
            updateTimer += Time.deltaTime;
            if (updateTimer >= updateInterval)
            {
                updateTimer = 0f;
                if (!isOnline)
                {
                    SimulateOtherPlayers();
                }
                SortAndRank();
            }
        }

        private void OnServerConnected()
        {
            Debug.Log("[LeaderboardManager] Server connected - switching to online mode");
            isOnline = true;
        }

        private void OnServerDisconnected()
        {
            Debug.Log("[LeaderboardManager] Server disconnected - falling back to offline mode");
            isOnline = false;
            // Keep current entries as-is, start simulating bots again
        }

        /// <summary>
        /// Called by SpacetimeDBManager when player data arrives from the server.
        /// Replaces online player entries while keeping bots if configured.
        /// </summary>
        public void SyncFromServer(List<SpacetimeDB.Types.Player> serverPlayers, SpacetimeDB.Identity? localIdentity)
        {
            entries.Clear();

            // Add server players
            foreach (var sp in serverPlayers)
            {
                bool isLocal = localIdentity.HasValue && sp.Identity == localIdentity.Value;
                var entry = new LeaderboardEntry(
                    sp.Identity.ToString(),
                    sp.Name,
                    (int)sp.Score,
                    isLocal
                );
                entry.IsOnlinePlayer = true;

                if (isLocal)
                {
                    localPlayer = entry;
                }

                entries.Add(entry);
            }

            // Fill with bots if configured
            if (fillWithBots && botEntries.Count > 0)
            {
                int onlineCount = entries.Count;
                int botsToAdd = Mathf.Max(0, minBotsWhenOnline - onlineCount);
                botsToAdd = Mathf.Min(botsToAdd, botEntries.Count);
                
                for (int i = 0; i < botsToAdd; i++)
                {
                    entries.Add(botEntries[i]);
                }
            }

            // Ensure we have a local player entry even if server hasn't registered us yet
            if (localPlayer == null)
            {
                localPlayer = new LeaderboardEntry("local", "YOU", 0, true);
                entries.Add(localPlayer);
            }

            SortAndRank();
        }

        /// <summary>
        /// Add score to the local player. This is how you "play" the game.
        /// In online mode, also sends the score to the server.
        /// </summary>
        public void AddPlayerScore(int amount)
        {
            if (localPlayer == null) return;

            int oldRank = localPlayer.Rank;

            if (isOnline && SpacetimeDBManager.Instance != null)
            {
                // Server-authoritative: send to server, update will come back via subscription
                SpacetimeDBManager.Instance.SendScoreToServer(amount);
                
                // Optimistic local update for responsiveness
                localPlayer.Score += amount;
            }
            else
            {
                localPlayer.Score += amount;
            }

            SortAndRank();

            if (localPlayer.Rank != oldRank)
            {
                OnPlayerRankChanged?.Invoke(localPlayer.Rank);
            }
        }

        public LeaderboardEntry GetLocalPlayer() => localPlayer;
        public List<LeaderboardEntry> GetEntries() => entries;
        public int GetPlayerRank() => localPlayer?.Rank ?? -1;
        public bool IsOnlineMode => isOnline;

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
                var bot = new LeaderboardEntry($"bot_{i}", names[i], score);
                entries.Add(bot);
                botEntries.Add(bot);
            }

            SortAndRank();
        }

        /// <summary>
        /// Simulate other players gaining score over time to create pressure.
        /// Only active in offline mode.
        /// </summary>
        private void SimulateOtherPlayers()
        {
            foreach (var entry in entries)
            {
                if (entry.IsLocalPlayer) continue;
                if (entry.IsOnlinePlayer) continue;

                if (ItemSystem.Instance != null && ItemSystem.Instance.AreOpponentsFrozen())
                    continue;
                if (Random.value < 0.3f)
                {
                    entry.Score += Random.Range(1, 15);
                }
            }
        }
    }
}
