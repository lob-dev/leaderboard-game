using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SpacetimeDB;
using SpacetimeDB.Types;

namespace LeaderboardGame
{
    /// <summary>
    /// Manages the connection to SpacetimeDB and syncs leaderboard data.
    /// Attach to the same GameObject as LeaderboardManager.
    /// </summary>
    public class SpacetimeDBManager : MonoBehaviour
    {
        public static SpacetimeDBManager Instance { get; private set; }

        [Header("SpacetimeDB Config")]
        [SerializeField] private string host = "http://localhost:3000";
        [SerializeField] private string moduleName = "leaderboard-game";
        [SerializeField] private string playerName = "";
        [SerializeField] private bool autoConnect = true;

        public bool IsConnected { get; private set; }
        public Identity? LocalIdentity { get; private set; }

        public event Action OnConnectedToServer;
        public event Action OnDisconnectedFromServer;
        public event Action OnPlayersUpdated;

        private DbConnection conn;
        private bool subscriptionApplied;
        private const string AUTH_TOKEN_KEY = "spacetimedb_auth_token";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (string.IsNullOrEmpty(playerName))
            {
                playerName = "Player_" + UnityEngine.Random.Range(1000, 9999);
            }
        }

        private void Start()
        {
            if (autoConnect)
            {
                Connect();
            }
        }

        private void OnDestroy()
        {
            Disconnect();
        }

        public void Connect()
        {
            if (conn != null) return;

            Debug.Log($"[SpacetimeDB] Connecting to {host}/{moduleName}...");

            try
            {
                string savedToken = PlayerPrefs.GetString(AUTH_TOKEN_KEY, "");

                // Module name is included in the URI for newer SpacetimeDB SDK versions
                string uri = host.TrimEnd('/') + "/" + moduleName;
                var builder = DbConnection.Builder()
                    .WithUri(uri)
                    .OnConnect(HandleConnect)
                    .OnConnectError(HandleConnectError)
                    .OnDisconnect(HandleDisconnect);

                if (!string.IsNullOrEmpty(savedToken))
                {
                    builder = builder.WithToken(savedToken);
                }

                conn = builder.Build();

                // Register table callbacks
                conn.Db.Player.OnInsert += OnPlayerInsert;
                conn.Db.Player.OnUpdate += OnPlayerUpdate;
                conn.Db.Player.OnDelete += OnPlayerDelete;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SpacetimeDB] Failed to connect: {e.Message}");
            }
        }

        public void Disconnect()
        {
            if (conn != null && conn.IsActive)
            {
                conn.Disconnect();
            }
            conn = null;
            IsConnected = false;
        }

        private void HandleConnect(DbConnection connection, Identity identity, string authToken)
        {
            Debug.Log($"[SpacetimeDB] Connected! Identity: {identity}");
            LocalIdentity = identity;
            IsConnected = true;

            // Save token for reconnection
            PlayerPrefs.SetString(AUTH_TOKEN_KEY, authToken);
            PlayerPrefs.Save();

            // Subscribe to all players
            connection.SubscriptionBuilder()
                .OnApplied(OnSubscriptionApplied)
                .OnError((ctx, e) => Debug.LogError($"[SpacetimeDB] Subscription error: {e.Message}"))
                .SubscribeToAllTables();

            OnConnectedToServer?.Invoke();
        }

        private void HandleConnectError(Exception e)
        {
            Debug.LogError($"[SpacetimeDB] Connection error: {e.Message}");
            IsConnected = false;
        }

        private void HandleDisconnect(DbConnection connection, Exception e)
        {
            if (e != null)
            {
                Debug.LogWarning($"[SpacetimeDB] Disconnected with error: {e.Message}");
            }
            else
            {
                Debug.Log("[SpacetimeDB] Disconnected.");
            }
            IsConnected = false;
            subscriptionApplied = false;
            OnDisconnectedFromServer?.Invoke();
        }

        private void OnSubscriptionApplied(SubscriptionEventContext ctx)
        {
            Debug.Log("[SpacetimeDB] Subscription applied, setting player name...");
            subscriptionApplied = true;

            // Set our name on the server
            ctx.Reducers.SetName(playerName);

            // Sync existing players to leaderboard
            SyncAllPlayersToLeaderboard();
        }

        private void OnPlayerInsert(EventContext ctx, Player player)
        {
            Debug.Log($"[SpacetimeDB] Player joined: {player.Name} (score: {player.Score})");
            if (subscriptionApplied)
            {
                SyncAllPlayersToLeaderboard();
            }
        }

        private void OnPlayerUpdate(EventContext ctx, Player oldPlayer, Player newPlayer)
        {
            if (subscriptionApplied)
            {
                SyncAllPlayersToLeaderboard();
            }
        }

        private void OnPlayerDelete(EventContext ctx, Player player)
        {
            Debug.Log($"[SpacetimeDB] Player removed: {player.Name}");
            if (subscriptionApplied)
            {
                SyncAllPlayersToLeaderboard();
            }
        }

        /// <summary>
        /// Push all SpacetimeDB player data into the LeaderboardManager.
        /// </summary>
        private void SyncAllPlayersToLeaderboard()
        {
            if (LeaderboardManager.Instance == null || conn == null) return;

            var players = new List<Player>();
            foreach (var p in conn.Db.Player.Iter())
            {
                players.Add(p);
            }

            LeaderboardManager.Instance.SyncFromServer(players, LocalIdentity);
        }

        /// <summary>
        /// Send a score update to the server.
        /// </summary>
        public void SendScoreToServer(int amount)
        {
            if (conn == null || !IsConnected || !subscriptionApplied) return;

            // Clamp to server limit
            int clamped = Mathf.Clamp(amount, 1, 500);
            conn.Reducers.AddScore(clamped);
        }

        /// <summary>
        /// Update the player's display name.
        /// </summary>
        public void SetPlayerName(string name)
        {
            playerName = name;
            if (conn != null && IsConnected && subscriptionApplied)
            {
                conn.Reducers.SetName(name);
            }
        }
    }
}
