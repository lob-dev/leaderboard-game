using UnityEngine;
using System.Runtime.InteropServices;

namespace LeaderboardGame
{
    /// <summary>
    /// Audio manager — bridges to Web Audio API via jslib plugin for WebGL,
    /// with procedural fallback for editor/standalone.
    /// Singleton. Created by SceneBuilder.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void SoundPlugin_Init();
        [DllImport("__Internal")] private static extern void SoundPlugin_Resume();
        [DllImport("__Internal")] private static extern void SoundPlugin_PlayTap(int comboLevel);
        [DllImport("__Internal")] private static extern void SoundPlugin_PlayComboEscalation(int comboLevel);
        [DllImport("__Internal")] private static extern void SoundPlugin_PlayRankUp(int milestone);
        [DllImport("__Internal")] private static extern void SoundPlugin_PlaySwoosh(bool isPositive);
        [DllImport("__Internal")] private static extern void SoundPlugin_StartAmbient();
        [DllImport("__Internal")] private static extern void SoundPlugin_StopAmbient();
        [DllImport("__Internal")] private static extern void SoundPlugin_SetMasterVolume(float vol);
#else
        // Stubs for editor — no audio in non-WebGL builds
        private static void SoundPlugin_Init() { }
        private static void SoundPlugin_Resume() { }
        private static void SoundPlugin_PlayTap(int comboLevel) { }
        private static void SoundPlugin_PlayComboEscalation(int comboLevel) { }
        private static void SoundPlugin_PlayRankUp(int milestone) { }
        private static void SoundPlugin_PlaySwoosh(bool isPositive) { }
        private static void SoundPlugin_StartAmbient() { }
        private static void SoundPlugin_StopAmbient() { }
        private static void SoundPlugin_SetMasterVolume(float vol) { }
#endif

        private bool initialized = false;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            InitAudio();
        }

        private void InitAudio()
        {
            SoundPlugin_Init();
            initialized = true;
            // Start ambient music after a short delay to allow AudioContext to initialize
            Invoke(nameof(StartAmbient), 0.5f);
        }

        /// <summary>Resume audio context (call on first user interaction if needed).</summary>
        public void ResumeAudio()
        {
            SoundPlugin_Resume();
        }

        /// <summary>Play tap sound. comboLevel 0-5.</summary>
        public void PlayTap(int comboLevel = 0)
        {
            if (!initialized) return;
            SoundPlugin_Resume(); // Ensure context is running (browser autoplay policy)
            SoundPlugin_PlayTap(comboLevel);
        }

        /// <summary>Play combo escalation sound (1-5).</summary>
        public void PlayCombo(int comboLevel)
        {
            if (!initialized) return;
            SoundPlugin_PlayComboEscalation(comboLevel);
        }

        /// <summary>Play rank up celebration. milestone: 10, 5, 3, or 1.</summary>
        public void PlayRankUp(int milestone)
        {
            if (!initialized) return;
            SoundPlugin_PlayRankUp(milestone);
        }

        /// <summary>Play overtake swoosh. Positive = you passed someone.</summary>
        public void PlayOvertakeSwoosh(bool positive = true)
        {
            if (!initialized) return;
            SoundPlugin_PlaySwoosh(positive);
        }

        /// <summary>Start ambient background music.</summary>
        public void StartAmbient()
        {
            if (!initialized) return;
            SoundPlugin_StartAmbient();
        }

        /// <summary>Stop ambient music.</summary>
        public void StopMusic()
        {
            SoundPlugin_StopAmbient();
        }

        /// <summary>Set master volume (0-1).</summary>
        public void SetMasterVolume(float vol)
        {
            SoundPlugin_SetMasterVolume(vol);
        }
    }
}
