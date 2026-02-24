using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using TMPro;

namespace LeaderboardGame
{
    /// <summary>
    /// The Board is alive. It watches. It comments. It remembers.
    /// This system drives self-aware UI text, playstyle detection, 
    /// rival highlighting, and story moments.
    /// </summary>
    public class NarrativeSystem : MonoBehaviour
    {
        public static NarrativeSystem Instance { get; private set; }

        // Board awareness text (the voice of the Board)
        private TextMeshProUGUI boardVoiceText;
        private CanvasGroup boardVoiceCG;
        
        // Playstyle tracking
        private List<float> tapTimestamps = new List<float>();
        private int totalTaps = 0;
        private float sessionStartTime;
        private float lastTapTime;
        private float longestIdleStretch;
        private int burstCount; // rapid tap sequences
        private string currentTitle = "";
        
        // Board awareness state
        private float lastMessageTime;
        private float messageInterval = 8f; // seconds between board messages
        private float idleStartTime;
        private bool hasGreeted;
        private int lastKnownRank = -1;
        private int fastestClimbInSession = 0;
        private int lastClimbCheckRank = -1;

        // Rival system
        private string currentRivalId = "";
        private int rivalSwapCount = 0;

        // Story moments
        public UnityEvent<string> OnStoryMoment; // fired when something narratively interesting happens

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            sessionStartTime = Time.time;
            lastTapTime = Time.time;
            idleStartTime = Time.time;
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

        // === PLAYSTYLE TRACKING ===

        public void RecordTap()
        {
            float now = Time.time;
            tapTimestamps.Add(now);
            totalTaps++;
            
            // Detect burst tapping (3+ taps in 0.5s)
            if (now - lastTapTime < 0.2f)
            {
                burstCount++;
            }
            
            // Track idle stretches
            float idleDuration = now - lastTapTime;
            if (idleDuration > longestIdleStretch)
                longestIdleStretch = idleDuration;
            
            lastTapTime = now;
            idleStartTime = now;
            
            // Trim old timestamps (keep last 60s)
            while (tapTimestamps.Count > 0 && tapTimestamps[0] < now - 60f)
                tapTimestamps.RemoveAt(0);

            // Re-evaluate playstyle
            EvaluatePlaystyle();
        }

        private void EvaluatePlaystyle()
        {
            float sessionDuration = Time.time - sessionStartTime;
            if (sessionDuration < 5f || totalTaps < 10) return; // too early

            float tapsPerSecond = tapTimestamps.Count / Mathf.Min(60f, sessionDuration);
            float burstRatio = (float)burstCount / totalTaps;

            string newTitle;

            if (tapsPerSecond > 4f && burstRatio > 0.3f)
                newTitle = "The Hammer";
            else if (tapsPerSecond > 3f)
                newTitle = "The Relentless";
            else if (longestIdleStretch > 15f && tapsPerSecond < 1.5f)
                newTitle = "The Scholar";
            else if (tapsPerSecond >= 1.5f && tapsPerSecond <= 3f && burstRatio < 0.15f)
                newTitle = "The Steady";
            else if (longestIdleStretch > 30f)
                newTitle = "The Watcher";
            else if (totalTaps > 200)
                newTitle = "The Devoted";
            else
                newTitle = "The Newcomer";

            if (newTitle != currentTitle)
            {
                currentTitle = newTitle;
                // Apply to local player
                if (LeaderboardManager.Instance != null)
                {
                    var player = LeaderboardManager.Instance.GetLocalPlayer();
                    if (player != null) player.PlaystyleTitle = currentTitle;
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
        }

        private IEnumerator BoardVoiceLoop()
        {
            // Opening greeting (delayed)
            yield return new WaitForSeconds(2f);
            
            if (!hasGreeted)
            {
                hasGreeted = true;
                int totalEntries = LeaderboardManager.Instance != null ? LeaderboardManager.Instance.GetEntries().Count : 0;
                int rank = LeaderboardManager.Instance != null ? LeaderboardManager.Instance.GetPlayerRank() : -1;
                if (rank > 0)
                    ShowBoardMessage($"Welcome. You are entry #{rank} of {totalEntries}. Begin.", 5f);
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

            // Weight different message types
            List<string> candidates = new List<string>();

            // Idle awareness
            if (idleTime > 30f)
            {
                candidates.Add($"You've been still for {Mathf.FloorToInt(idleTime)}s. The others haven't.");
                candidates.Add("Are you still there? The Board waits for no one.");
                candidates.Add("Stillness is a choice. But it costs the same as motion here.");
            }
            else if (idleTime > 10f)
            {
                candidates.Add("You've paused. Thinking, or giving up?");
            }

            // Rank awareness
            if (rank == 1)
            {
                candidates.Add("You're at the top. Everyone below you is hungry.");
                candidates.Add("Crown's heavy, isn't it?");
            }
            else if (rank <= 3)
            {
                candidates.Add($"#{rank}. Close enough to taste it. Close enough to lose it.");
                candidates.Add("The top 3 is where names become legends. Or cautionary tales.");
            }
            else if (rank <= 10)
            {
                candidates.Add($"#{rank} of {totalEntries}. You're in the spotlight now.");
                candidates.Add("Top 10. The ghosts below are watching you with envy.");
            }
            else if (rank > totalEntries / 2)
            {
                candidates.Add($"#{rank} of {totalEntries}. The bottom half. You can do better.");
                candidates.Add("You're closer to the ghosts than the crown.");
            }

            // Climbing awareness
            if (lastClimbCheckRank > 0 && rank < lastClimbCheckRank - 3)
            {
                candidates.Add($"You've climbed {lastClimbCheckRank - rank} ranks. The Board notices.");
                candidates.Add("You're climbing fast. They can feel you coming.");
                lastClimbCheckRank = rank;
            }

            // Playstyle awareness
            if (currentTitle == "The Hammer")
                candidates.Add("Such fury. The Board trembles with each tap.");
            else if (currentTitle == "The Scholar")
                candidates.Add("Patient. Calculating. The Board respects that.");
            else if (currentTitle == "The Watcher")
                candidates.Add("Watching won't save you. But it might teach you.");

            // Session time
            if (sessionTime > 120f && sessionTime < 130f)
                candidates.Add("Two minutes in. You're still here. That says something.");
            else if (sessionTime > 300f && sessionTime < 310f)
                candidates.Add("Five minutes. Most give up by now. You haven't.");

            // Ghost awareness
            int ghostCount = 0;
            foreach (var e in LeaderboardManager.Instance.GetEntries())
                if (e.IsGhost) ghostCount++;
            if (ghostCount > 0 && Random.value < 0.3f)
                candidates.Add($"{ghostCount} ghosts on this board. They were all someone once.");

            // Rival awareness
            if (!string.IsNullOrEmpty(currentRivalId))
            {
                foreach (var e in LeaderboardManager.Instance.GetEntries())
                {
                    if (e.PlayerId == currentRivalId)
                    {
                        candidates.Add($"You and {e.PlayerName} keep circling each other.");
                        break;
                    }
                }
            }

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

            // Fade in
            float fadeIn = 0.5f;
            float t = 0;
            while (t < fadeIn)
            {
                t += Time.deltaTime;
                boardVoiceCG.alpha = Mathf.Lerp(0f, 0.85f, t / fadeIn);
                yield return null;
            }
            boardVoiceCG.alpha = 0.85f;

            yield return new WaitForSeconds(duration);

            // Fade out
            float fadeOut = 1f;
            t = 0;
            while (t < fadeOut)
            {
                t += Time.deltaTime;
                boardVoiceCG.alpha = Mathf.Lerp(0.85f, 0f, t / fadeOut);
                yield return null;
            }
            boardVoiceCG.alpha = 0f;
        }

        // === RIVAL SYSTEM ===

        /// <summary>
        /// Called by LeaderboardManager after sorting. Identifies the closest 
        /// non-ghost entry to the player as their rival.
        /// </summary>
        public void UpdateRivals(List<LeaderboardEntry> entries)
        {
            LeaderboardEntry player = null;
            int playerIdx = -1;
            
            for (int i = 0; i < entries.Count; i++)
            {
                entries[i].IsRival = false;
                if (entries[i].IsLocalPlayer) { player = entries[i]; playerIdx = i; }
            }

            if (player == null || playerIdx < 0) return;

            // Rival = closest entry above you (or below if you're #1)
            LeaderboardEntry rival = null;
            if (playerIdx > 0 && !entries[playerIdx - 1].IsGhost)
                rival = entries[playerIdx - 1]; // entry just above
            else if (playerIdx < entries.Count - 1 && !entries[playerIdx + 1].IsGhost)
                rival = entries[playerIdx + 1]; // entry just below

            if (rival != null)
            {
                rival.IsRival = true;
                if (rival.PlayerId != currentRivalId)
                {
                    rivalSwapCount++;
                    currentRivalId = rival.PlayerId;
                }
            }
        }

        // === AURA COLORS ===

        /// <summary>
        /// Assign aura colors based on entry state and history.
        /// </summary>
        public void UpdateAuras(List<LeaderboardEntry> entries)
        {
            foreach (var entry in entries)
            {
                if (entry.IsGhost)
                {
                    entry.AuraColor = new Color(0.3f, 0.3f, 0.4f, 0.15f); // ghostly grey-blue
                }
                else if (entry.IsLocalPlayer)
                {
                    // Player aura based on playstyle
                    if (currentTitle == "The Hammer")
                        entry.AuraColor = new Color(1f, 0.3f, 0.1f, 0.2f); // fiery
                    else if (currentTitle == "The Scholar")
                        entry.AuraColor = new Color(0.3f, 0.5f, 1f, 0.2f); // cool blue
                    else if (currentTitle == "The Relentless")
                        entry.AuraColor = new Color(1f, 0.6f, 0f, 0.2f); // orange
                    else if (currentTitle == "The Steady")
                        entry.AuraColor = new Color(0.4f, 1f, 0.4f, 0.15f); // green
                    else
                        entry.AuraColor = new Color(1f, 0.84f, 0f, 0.15f); // default gold
                }
                else if (entry.IsRival)
                {
                    entry.AuraColor = new Color(1f, 0.2f, 0.3f, 0.2f); // red rival glow
                }
                else if (entry.Rank <= 3)
                {
                    entry.AuraColor = new Color(0.6f, 0.4f, 1f, 0.1f); // purple top 3
                }
                else
                {
                    entry.AuraColor = Color.clear;
                }
            }
        }
    }
}
