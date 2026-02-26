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
                SpacetimeDBManager.Instance.OnConnectedToServer += OnServerConnected;
                SpacetimeDBManager.Instance.OnDisconnectedFromServer += OnServerDisconnected;
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
        }

        public void SyncFromServer(List<SpacetimeDB.Types.Player> serverPlayers, SpacetimeDB.Identity? localIdentity)
        {
            entries.Clear();

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
                entry.IsOnlineConnected = sp.Online;
                entry.TotalTaps = sp.TotalTaps;
                entry.HighestScore = sp.HighestScore;

                if (isLocal)
                    localPlayer = entry;

                entries.Add(entry);
            }

            if (fillWithBots && botEntries.Count > 0)
            {
                int onlineCount = entries.Count;
                int botsToAdd = Mathf.Max(0, minBotsWhenOnline - onlineCount);
                botsToAdd = Mathf.Min(botsToAdd, botEntries.Count);
                for (int i = 0; i < botsToAdd; i++)
                    entries.Add(botEntries[i]);
            }

            if (localPlayer == null)
            {
                localPlayer = new LeaderboardEntry("local", "YOU", 0, true);
                entries.Add(localPlayer);
            }

            SortAndRank();
        }

        public void AddPlayerScore(int amount)
        {
            if (localPlayer == null) return;

            int oldRank = localPlayer.Rank;

            if (isOnline && SpacetimeDBManager.Instance != null)
            {
                SpacetimeDBManager.Instance.SendScoreToServer(amount);
                localPlayer.Score += amount;
            }
            else
            {
                localPlayer.Score += amount;
            }

            localPlayer.LastActiveTime = Time.time;

            // Notify narrative system
            if (NarrativeSystem.Instance != null)
                NarrativeSystem.Instance.RecordTap();

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
                var e = entries[i];
                // Track rank changes for animation
                if (e.PreviousRank != -1 && e.Rank != e.PreviousRank)
                {
                    if (i + 1 < e.PreviousRank) e.JustRankedUp = true;
                    else if (i + 1 > e.PreviousRank) e.JustRankedDown = true;
                    e.RankChangeTime = Time.time;
                }
                e.PreviousRank = e.Rank;
                e.Rank = i + 1;
            }

            // Synthesis mechanics
            if (NarrativeSystem.Instance != null)
            {
                NarrativeSystem.Instance.UpdateVisibility(entries);  // Observer Effect
                NarrativeSystem.Instance.UpdateRivals(entries);      // Proximity Rivalry
                NarrativeSystem.Instance.UpdateAuras(entries);       // Visual auras
            }

            OnLeaderboardUpdated?.Invoke(entries);
        }

        private void InitializeWithFakeData()
        {
            localPlayer = new LeaderboardEntry("local", "YOU", 0, true);
            entries.Add(localPlayer);

            // Active bot players
            string[] activeNames = {
                "xXSlayerXx", "ProGamer99", "ShadowWolf",
                "PixelQueen", "CryptoKing", "NightOwl42",
                "SpeedDemon", "ChillGuy", "TryHard101",
                "MLG_Toast", "EzClap", "GigaChad",
                "WinStreak", "1TapGod", "Boosted",
                "HardStuck", "Ratio_King", "PogChamp"
            };

            string[] activeTitles = {
                "Striker", "Stalwart", "Nomad",
                "Striker", "Stalwart", "Nomad",
                "Striker", "Stalwart", "Nomad",
                "Striker", "Stalwart", "Striker",
                "Striker", "Striker", "Nomad",
                "Stalwart", "Nomad", "Striker"
            };

            for (int i = 0; i < activeNames.Length; i++)
            {
                int score = Random.Range(50, 5000);
                var bot = new LeaderboardEntry($"bot_{i}", activeNames[i], score);
                bot.PlaystyleTitle = activeTitles[i % activeTitles.Length];
                entries.Add(bot);
                botEntries.Add(bot);
            }

            // Ghost entries — dead players with Last Words
            string[] ghostNames = {
                "RageQuitter", "AFK_Andy", "LagMaster",
                "CasualKaren", "SweatyPalms", "NoobMaster",
                "TouchGrass", "L_Collector", "Smurf_Alt"
            };

            string[] lastWords = {
                "I'll be back... I won't.",
                "afk 5 min (it's been 3 months)",
                "lag killed me, not you",
                "this game used to be fun",
                "my hands hurt. worth it? no.",
                "I was #1 once. for 2 seconds.",
                "going outside. heard it has good graphics.",
                "collected 847 L's. that's a record, right?",
                "this is my alt. my main is also dead."
            };

            string[] ghostReasons = {
                "Rage quit after losing #3",
                "Said 'brb' and never returned",
                "Blamed the servers",
                "Lost interest at rank #15",
                "RSI from tapping",
                "Brief glory, eternal rest",
                "Touched grass, never came back",
                "Accepted their fate",
                "Pretended this wasn't their main"
            };

            for (int i = 0; i < ghostNames.Length; i++)
            {
                int score = Random.Range(10, 800);
                var ghost = new LeaderboardEntry($"ghost_{i}", ghostNames[i], score);
                ghost.IsGhost = true;
                ghost.LastWords = lastWords[i % lastWords.Length];
                ghost.GhostReason = ghostReasons[i % ghostReasons.Length];
                ghost.PlaystyleTitle = ""; // ghosts have no title — they're gone
                entries.Add(ghost);
                botEntries.Add(ghost);
            }

            SortAndRank();
        }

        private void SimulateOtherPlayers()
        {
            foreach (var entry in entries)
            {
                if (entry.IsLocalPlayer) continue;
                if (entry.IsOnlinePlayer) continue;
                if (entry.IsGhost) continue; // Ghosts don't play

                if (ItemSystem.Instance != null && ItemSystem.Instance.AreOpponentsFrozen())
                    continue;
                // Expire powerup display
                if (entry.ActivePowerup.HasValue && Time.time >= entry.PowerupExpireTime)
                {
                    entry.ActivePowerup = null;
                }

                // Chance to pick up a powerup (roughly every ~10s per bot)
                if (!entry.ActivePowerup.HasValue && Random.value < 0.01f)
                {
                    var types = (ItemType[])System.Enum.GetValues(typeof(ItemType));
                    var picked = types[Random.Range(0, types.Length)];
                    var data = ItemDefinitions.Get(picked);
                    entry.ActivePowerup = picked;
                    // Show icon for duration (or 3s for instant items)
                    entry.PowerupExpireTime = Time.time + (data.Duration > 0f ? data.Duration : 3f);
                }

                if (Random.value < 0.3f)
                {
                    entry.Score += Random.Range(1, 15);
                    entry.LastActiveTime = Time.time;
                }
            }
        }
    }
}
