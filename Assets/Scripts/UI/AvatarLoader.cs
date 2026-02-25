using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace LeaderboardGame
{
    /// <summary>
    /// Downloads and caches player avatar images from DiceBear API.
    /// Each player gets a unique robot avatar based on their player ID.
    /// </summary>
    public static class AvatarLoader
    {
        private static Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
        private static HashSet<string> loading = new HashSet<string>();

        // DiceBear avatars - "bottts" style gives fun robot faces
        private static string GetAvatarUrl(string seed)
        {
            string safeSeed = UnityEngine.Networking.UnityWebRequest.EscapeURL(seed);
            return $"https://api.dicebear.com/9.x/bottts-neutral/png?seed={safeSeed}&size=128&backgroundColor=transparent";
        }

        /// <summary>
        /// Load avatar for a player. Sets the image immediately if cached,
        /// otherwise starts a coroutine to download it.
        /// Returns true if avatar was set from cache (immediate).
        /// </summary>
        public static bool LoadAvatar(string playerId, Image targetImage, MonoBehaviour coroutineHost)
        {
            if (string.IsNullOrEmpty(playerId))
                return false;

            if (cache.TryGetValue(playerId, out Sprite cached))
            {
                targetImage.sprite = cached;
                targetImage.color = Color.white;
                return true;
            }

            // Start download if not already loading
            if (!loading.Contains(playerId))
            {
                loading.Add(playerId);
                coroutineHost.StartCoroutine(DownloadAvatar(playerId));
            }

            return false;
        }

        /// <summary>
        /// Try to get a cached avatar sprite. Returns null if not yet loaded.
        /// </summary>
        public static Sprite GetCached(string playerId)
        {
            if (!string.IsNullOrEmpty(playerId) && cache.TryGetValue(playerId, out Sprite s))
                return s;
            return null;
        }

        /// <summary>
        /// Apply cached avatar to an Image if available, otherwise keep the current look.
        /// Call this during UI refresh to apply avatars that finished downloading.
        /// </summary>
        public static void ApplyIfCached(string playerId, Image avatarImage)
        {
            if (string.IsNullOrEmpty(playerId)) return;
            if (cache.TryGetValue(playerId, out Sprite s))
            {
                avatarImage.sprite = s;
                avatarImage.color = Color.white;
            }
        }

        private static IEnumerator DownloadAvatar(string playerId)
        {
            string url = GetAvatarUrl(playerId);

            using (var request = UnityWebRequestTexture.GetTexture(url))
            {
                request.timeout = 10;
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    var texture = DownloadHandlerTexture.GetContent(request);
                    var sprite = Sprite.Create(
                        texture,
                        new Rect(0, 0, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f),
                        100f
                    );
                    cache[playerId] = sprite;
                }
                else
                {
                    Debug.LogWarning($"[AvatarLoader] Failed to load avatar for {playerId}: {request.error}");
                }

                loading.Remove(playerId);
            }
        }

        /// <summary>
        /// Preload avatars for a list of player IDs.
        /// </summary>
        public static void Preload(IEnumerable<string> playerIds, MonoBehaviour coroutineHost)
        {
            foreach (var id in playerIds)
            {
                if (!string.IsNullOrEmpty(id) && !cache.ContainsKey(id) && !loading.Contains(id))
                {
                    loading.Add(id);
                    coroutineHost.StartCoroutine(DownloadAvatar(id));
                }
            }
        }
    }
}
