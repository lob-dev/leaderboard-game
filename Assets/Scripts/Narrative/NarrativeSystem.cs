using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using TMPro;

namespace LeaderboardGame
{
    /// <summary>
    /// The Board is alive. It watches. It comments. It remembers.
    /// Now with: Observer Effect, Proximity Rivalry, You Are Becoming.
    /// </summary>
    public class NarrativeSystem : MonoBehaviour
    {
        public static NarrativeSystem Instance { get; private set; }

        // Board awareness text
        private TextMeshProUGUI boardVoiceText;
        private CanvasGroup boardVoiceCG;

        // Playstyle tracking ("You Are Becoming")
        private List<float> tapTimestamps = new List<float>();
        private int totalTaps = 0;
        private float sessionStartTime;
        private float lastTapTime;
        private float longestIdleStretch;
        private int burstCount;
        private string currentTitle = "";

        // Rank-bounce tracking for "Nomad" detection
        private List<int> rankHistory = new List<int>();
        private float lastRankRecordTime;
        private int rankDirectionChanges = 0;

        // Board awareness state
        private float lastMessageTime;
        private float messageInterval = 8f;
        private float idleStartTime;
        private bool hasGreeted;
        private int lastKnownRank = -1;
        private int fastestClimbInSession = 0;
        private int lastClimbCheckRank = -1;

        // Rival system (proximity)
        private string currentRivalId = "";
        private int rivalSwapCount = 0;

        public UnityEvent<string> OnStoryMoment;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            sessionStartTime = Time.time;
            lastTapTime = Time.time;
            idleStartTime = Time.time;
            lastRankRecordTime = Time.time;
            if (OnStoryMoment == null) OnStoryMoment = new UnityEvent<string>();
        }

        public void Init(TextMeshProUGUI voiceText, CanvasGroup voiceCG)
        {
            boardVoiceText = voiceText;
            boardVoiceCG = voiceCG;
            StartCoroutine(BoardVoiceLoop());
            StartCoroutine(WaitForManager());
        }

        private IEnumerator WaitForManager()
        {
            while (LeaderboardManager.Instance == null) yield return null;
            LeaderboardManager.Instance.OnPlayerRankChanged.AddListener(OnRankChanged);
        }

        private void OnDisable()
        {
            if (LeaderboardManager.Instance != null)
                LeaderboardManager.Instance.OnPlayerRankChanged.RemoveListener(OnRankChanged);
        }

        // === OBSERVER EFFECT ===

        /// <summary>
        /// Calculate visibility and cost multiplier for each entry based on rank position.
        /// Higher rank = more visible = harder to damage (costlier to climb past).
        /// </summary>
        public void UpdateVisibility(List<LeaderboardEntry> entries)
        {
            int total = entries.Count;
            if (total == 0) return;

            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                int rank = i + 1;
                float percentile = 1f - ((float)(rank - 1) / total); // 1.0 = #1, 0.0 = last

                e.Visibility = percentile;

                // Cost multiplier: bottom 50% = 0.8x, middle = 1.0x, top 10 = 2.5x
                if (total > 0 && rank <= 10 && rank <= Mathf.CeilToInt(total * 0.15f))
                {
                    // Top tier: exponential scaling from 1.5x to 2.5x
                    float topFraction = (float)(11 - rank) / 10f; // 1.0 for #1, 0.1 for #10
                    e.CostMultiplier = 1.5f + topFraction * 1.0f; // 1.5 to 2.5
                }
                else if (percentile < 0.5f)
                {
                    // Bottom 50%: cheaper to climb
                    e.CostMultiplier = 0.8f;
                }
                else
                {
                    // Middle: normal with slight scaling
                    float midFraction = (percentile - 0.5f) / 0.35f; // 0 to ~1 across middle
                    e.CostMultiplier = 1.0f + Mathf.Clamp01(midFraction) * 0.5f;
                }
            }
        }

        /// <summary>
        /// Get the cost multiplier for the local player's current position.
        /// Used by PlayerController to scale tap effectiveness.
        /// </summary>
        public float GetLocalPlayerCostMultiplier()
        {
            if (LeaderboardManager.Instance == null) return 1f;
            var player = LeaderboardManager.Instance.GetLocalPlayer();
            return player?.CostMultiplier ?? 1f;
        }

        // === PROXIMITY RIVALRY ===

        /// <summary>
        /// Players within 3 ranks of the local player become auto-rivals.
        /// Rivalries dissolve when distance > 3 ranks.
        /// </summary>
        public void UpdateRivals(List<LeaderboardEntry> entries)
        {
            LeaderboardEntry player = null;
            int playerIdx = -1;

            // Clear all rivalry state
            for (int i = 0; i < entries.Count; i++)
            {
                entries[i].IsRival = false;
                entries[i].RivalIds.Clear();
                entries[i].RivalCount = 0;
                if (entries[i].IsLocalPlayer) { player = entries[i]; playerIdx = i; }
            }

            if (player == null || playerIdx < 0) return;

            // Mark entries within 3 ranks as rivals
            List<string> playerRivals = new List<string>();
            for (int i = 0; i < entries.Count; i++)
            {
                if (i == playerIdx) continue;
                if (entries[i].IsGhost) continue;

                int distance = Mathf.Abs(i - playerIdx);
                if (distance <= 3)
                {
                    entries[i].IsRival = true;
                    playerRivals.Add(entries[i].PlayerId);

                    // Track rival changes for narrative
                    if (!string.IsNullOrEmpty(currentRivalId) && entries[i].PlayerId != currentRivalId)
                    {
                        rivalSwapCount++;
                    }
                }
            }

            player.RivalIds = playerRivals;
            player.RivalCount = playerRivals.Count;

            // Track primary rival (closest above)
            if (playerIdx > 0 && !entries[playerIdx - 1].IsGhost)
                currentRivalId = entries[playerIdx - 1].PlayerId;
        }

        /// <summary>
        /// Check if a target is a rival of the local player (for damage bonus).
        /// </summary>
        public bool IsRivalOf(string targetId)
        {
            if (LeaderboardManager.Instance == null) return false;
            var player = LeaderboardManager.Instance.GetLocalPlayer();
            return player != null && player.RivalIds.Contains(targetId);
        }

        // === YOU ARE BECOMING ===

        public void RecordTap()
        {
            float now = Time.time;
            tapTimestamps.Add(now);
            totalTaps++;

            if (now - lastTapTime < 0.2f)
                burstCount++;

            float idleDuration = now - lastTapTime;
            if (idleDuration > longestIdleStretch)
                longestIdleStretch = idleDuration;

            lastTapTime = now;
            idleStartTime = now;

            // Trim old timestamps (keep last 60s)
            while (tapTimestamps.Count > 0 && tapTimestamps[0] < now - 60f)
                tapTimestamps.RemoveAt(0);

            EvaluatePlaystyle();
        }

        private void RecordRankForBounce(int rank)
        {
            float now = Time.time;
            if (now - lastRankRecordTime < 1f) return; // throttle
            lastRankRecordTime = now;

            rankHistory.Add(rank);

            // Keep last 20 rank snapshots
            while (rankHistory.Count > 20)
                rankHistory.RemoveAt(0);

            // Count direction changes (bouncing detection)
            if (rankHistory.Count >= 3)
            {
                rankDirectionChanges = 0;
                for (int i = 2; i < rankHistory.Count; i++)
                {
                    int prev = rankHistory[i - 1] - rankHistory[i - 2];
                    int curr = rankHistory[i] - rankHistory[i - 1];
                    if ((prev > 0 && curr < 0) || (prev < 0 && curr > 0))
                        rankDirectionChanges++;
                }
            }
        }

        private void EvaluatePlaystyle()
        {
            float sessionDuration = Time.time - sessionStartTime;
            if (sessionDuration < 5f || totalTaps < 10) return;

            float tapsPerSecond = tapTimestamps.Count / Mathf.Min(60f, sessionDuration);
            float burstRatio = (float)burstCount / totalTaps;

            string newTitle;

            // NOMAD: bounces around a lot (rank direction changes)
            if (rankDirectionChanges >= 5)
            {
                newTitle = "Nomad";
            }
            // STRIKER: aggressive, fast tapping
            else if (tapsPerSecond > 3.5f || (tapsPerSecond > 2.5f && burstRatio > 0.25f))
            {
                newTitle = "Striker";
            }
            // STALWART: slow, steady, patient climber
            else if (tapsPerSecond <= 2.5f && longestIdleStretch < 20f)
            {
                newTitle = "Stalwart";
            }
            // Fallback based on behavior
            else if (longestIdleStretch > 20f && tapsPerSecond < 1.5f)
            {
                newTitle = "Stalwart"; // patient = stalwart
            }
            else if (tapsPerSecond > 2.5f)
            {
                newTitle = "Striker";
            }
            else
            {
                newTitle = "Stalwart"; // default to stalwart until behavior diverges
            }

            if (newTitle != currentTitle)
            {
                string oldTitle = currentTitle;
                currentTitle = newTitle;
                if (LeaderboardManager.Instance != null)
                {
                    var player = LeaderboardManager.Instance.GetLocalPlayer();
                    if (player != null) player.PlaystyleTitle = currentTitle;
                }

                // Narrative moment on title change
                if (!string.IsNullOrEmpty(oldTitle) && oldTitle != newTitle)
                {
                    OnStoryMoment?.Invoke($"You are becoming: {newTitle}");
                }
            }
        }

        public string GetCurrentTitle() => currentTitle;

        // === BOARD SELF-AWARENESS ===

        private void OnRankChanged(int newRank)
        {
            if (lastKnownRank == -1) { lastKnownRank = newRank; lastClimbCheckRank = newRank; return; }

            int climbed = lastKnownRank - newRank;
            if (climbed > fastestClimbInSession)
                fastestClimbInSession = climbed;

            lastKnownRank = newRank;
            RecordRankForBounce(newRank);
        }

        private IEnumerator BoardVoiceLoop()
        {
            yield return new WaitForSeconds(2f);

            if (!hasGreeted)
            {
                hasGreeted = true;
                int totalEntries = LeaderboardManager.Instance != null ? LeaderboardManager.Instance.GetEntries().Count : 0;
                int rank = LeaderboardManager.Instance != null ? LeaderboardManager.Instance.GetPlayerRank() : -1;
                if (rank > 0)
                    ShowBoardMessage($"Welcome. You are entry #{rank} of {totalEntries}. The Board watches.", 5f);
                else
                    ShowBoardMessage("Welcome to the Board. You are a name and a number. Begin.", 5f);
            }

            while (true)
            {
                yield return new WaitForSeconds(messageInterval + Random.Range(-2f, 3f));
                string msg = GenerateContextualMessage();
                if (!string.IsNullOrEmpty(msg))
                    ShowBoardMessage(msg, 4f);
            }
        }

        private string GenerateContextualMessage()
        {
            if (LeaderboardManager.Instance == null) return null;
            var player = LeaderboardManager.Instance.GetLocalPlayer();
            if (player == null) return null;

            int rank = player.Rank;
            int totalEntries = LeaderboardManager.Instance.GetEntries().Count;
            float idleTime = Time.time - lastTapTime;
            float sessionTime = Time.time - sessionStartTime;

            List<string> candidates = new List<string>();

            // Observer Effect awareness
            if (player.CostMultiplier > 2f)
            {
                candidates.Add("The spotlight burns. Every tap costs more up here.");
                candidates.Add($"Visibility: {Mathf.RoundToInt(player.Visibility * 100)}%. Everyone is watching you struggle.");
            }
            else if (player.CostMultiplier < 0.9f)
            {
                candidates.Add("Down here, nobody notices. Climb cheaply while you can.");
                candidates.Add("The shadows are kind. Low visibility, low cost.");
            }

            // Proximity Rivalry awareness
            if (player.RivalCount > 0)
            {
                candidates.Add($"{player.RivalCount} rival{(player.RivalCount > 1 ? "s" : "")} in striking distance. Use it.");
                candidates.Add("Red means close. Close means dangerous. For them AND you.");
            }
            if (player.RivalCount >= 3)
            {
                candidates.Add("You're surrounded by rivals. A bloodbath zone.");
            }

            // Playstyle awareness
            if (currentTitle == "Striker")
            {
                candidates.Add("Striker. The Board trembles with each tap.");
                candidates.Add("You hit fast and hard. They feel you coming.");
            }
            else if (currentTitle == "Stalwart")
            {
                candidates.Add("Stalwart. Patient. Inevitable.");
                candidates.Add("Slow and steady. The others underestimate you.");
            }
            else if (currentTitle == "Nomad")
            {
                candidates.Add("Nomad. You belong nowhere. You belong everywhere.");
                candidates.Add("Bouncing around. The Board can't predict you.");
            }

            // Idle awareness
            if (idleTime > 30f)
            {
                candidates.Add($"You've been still for {Mathf.FloorToInt(idleTime)}s. The others haven't.");
                candidates.Add("Stillness is a choice. But the cost of climbing grows.");
            }
            else if (idleTime > 10f)
            {
                candidates.Add("You've paused. Thinking, or giving up?");
            }

            // Rank awareness
            if (rank == 1)
            {
                candidates.Add("You're at the top. Maximum visibility. Maximum cost. Maximum target.");
            }
            else if (rank <= 3)
            {
                candidates.Add($"#{rank}. The air is thin up here. Every tap costs more.");
            }
            else if (rank <= 10)
            {
                candidates.Add($"#{rank}. Visible now. The Observer Effect kicks in.");
            }
            else if (rank > totalEntries / 2)
            {
                candidates.Add($"#{rank} of {totalEntries}. The bottom half. Cheap to climb. Use it.");
            }

            if (lastClimbCheckRank > 0 && rank < lastClimbCheckRank - 3)
            {
                candidates.Add($"You've climbed {lastClimbCheckRank - rank} ranks. The Board notices.");
                lastClimbCheckRank = rank;
            }

            // Ghost awareness
            int ghostCount = 0;
            foreach (var e in LeaderboardManager.Instance.GetEntries())
                if (e.IsGhost) ghostCount++;
            if (ghostCount > 0 && Random.value < 0.2f)
                candidates.Add($"{ghostCount} ghosts on this board. They were all someone once.");

            if (candidates.Count == 0) return null;
            return candidates[Random.Range(0, candidates.Count)];
        }

        private void ShowBoardMessage(string message, float duration)
        {
            if (boardVoiceText == null) return;
            StopCoroutine("FadeBoardMessage");
            boardVoiceText.text = message;
            StartCoroutine(FadeBoardMessage(duration));
        }

        private IEnumerator FadeBoardMessage(float duration)
        {
            if (boardVoiceCG == null) yield break;
            float fadeIn = 0.5f;
            float t = 0;
            while (t < fadeIn) { t += Time.deltaTime; boardVoiceCG.alpha = Mathf.Lerp(0f, 0.85f, t / fadeIn); yield return null; }
            boardVoiceCG.alpha = 0.85f;
            yield return new WaitForSeconds(duration);
            float fadeOut = 1f;
            t = 0;
            while (t < fadeOut) { t += Time.deltaTime; boardVoiceCG.alpha = Mathf.Lerp(0.85f, 0f, t / fadeOut); yield return null; }
            boardVoiceCG.alpha = 0f;
        }

        // === AURA COLORS ===

        public void UpdateAuras(List<LeaderboardEntry> entries)
        {
            foreach (var entry in entries)
            {
                if (entry.IsGhost)
                {
                    entry.AuraColor = new Color(0.3f, 0.3f, 0.4f, 0.15f);
                }
                else if (entry.IsLocalPlayer)
                {
                    if (currentTitle == "Striker")
                        entry.AuraColor = new Color(1f, 0.3f, 0.1f, 0.25f);
                    else if (currentTitle == "Stalwart")
                        entry.AuraColor = new Color(0.3f, 0.7f, 1f, 0.2f);
                    else if (currentTitle == "Nomad")
                        entry.AuraColor = new Color(0.6f, 1f, 0.3f, 0.2f);
                    else
                        entry.AuraColor = new Color(1f, 0.84f, 0f, 0.15f);
                }
                else if (entry.IsRival)
                {
                    entry.AuraColor = new Color(1f, 0.15f, 0.2f, 0.25f); // strong red rival glow
                }
                else if (entry.Rank <= 3)
                {
                    entry.AuraColor = new Color(0.6f, 0.4f, 1f, 0.1f);
                }
                else
                {
                    entry.AuraColor = Color.clear;
                }
            }
        }
    }
}
