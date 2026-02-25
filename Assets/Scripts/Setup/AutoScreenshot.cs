using UnityEngine;
using System.Collections;

namespace LeaderboardGame
{
    /// <summary>
    /// Takes a screenshot after the scene is built and some gameplay happens.
    /// Checks for command line arg --screenshot
    /// </summary>
    public class AutoScreenshot : MonoBehaviour
    {
        private void Start()
        {
            string[] args = System.Environment.GetCommandLineArgs();
            bool takeScreenshot = false;
            foreach (string arg in args)
            {
                if (arg == "--screenshot") takeScreenshot = true;
            }
            
            if (takeScreenshot)
            {
                StartCoroutine(TakeScreenshots());
            }
        }

        private IEnumerator TakeScreenshots()
        {
            string dir = System.IO.Path.Combine(Application.dataPath, "..", "screenshots");
            if (!System.IO.Directory.Exists(dir))
                System.IO.Directory.CreateDirectory(dir);

            // Wait for scene to build
            yield return new WaitForSeconds(3f);

            // Screenshot 1: Full charges (10/10)
            string path1 = System.IO.Path.Combine(dir, "01_charges_full.png");
            ScreenCapture.CaptureScreenshot(path1, 2);
            Debug.Log("[AutoScreenshot] Saved: " + path1);
            yield return new WaitForSeconds(1f);

            var player = FindObjectOfType<PlayerController>();
            
            // Tap 7 times to burn most charges (10 -> 3)
            if (player != null)
            {
                for (int i = 0; i < 7; i++)
                {
                    player.OnTap();
                    yield return new WaitForSeconds(0.08f);
                }
            }
            yield return new WaitForEndOfFrame();
            
            // Screenshot 2: Partially depleted (3/10)
            string path2 = System.IO.Path.Combine(dir, "02_charges_burning.png");
            ScreenCapture.CaptureScreenshot(path2, 2);
            Debug.Log("[AutoScreenshot] Saved: " + path2);
            yield return new WaitForSeconds(0.5f);

            // Tap 3 more to fully deplete
            if (player != null)
            {
                for (int i = 0; i < 5; i++)
                {
                    player.OnTap();
                    yield return new WaitForSeconds(0.05f);
                }
            }
            yield return new WaitForEndOfFrame();
            
            // Screenshot 3: Depleted (0/10)
            string path3 = System.IO.Path.Combine(dir, "03_charges_depleted.png");
            ScreenCapture.CaptureScreenshot(path3, 2);
            Debug.Log("[AutoScreenshot] Saved: " + path3);

            // Wait for recharge (1 charge/sec, wait 5 sec for ~5 charges)
            yield return new WaitForSeconds(5f);
            
            // Screenshot 4: Recharging (partial refill)
            string path4 = System.IO.Path.Combine(dir, "04_charges_recharging.png");
            ScreenCapture.CaptureScreenshot(path4, 2);
            Debug.Log("[AutoScreenshot] Saved: " + path4);

            // Wait for full recharge
            yield return new WaitForSeconds(6f);

            // Screenshot 5: Fully recharged
            string path5 = System.IO.Path.Combine(dir, "05_charges_recharged.png");
            ScreenCapture.CaptureScreenshot(path5, 2);
            Debug.Log("[AutoScreenshot] Saved: " + path5);

            // More gameplay for items - rapid tapping
            if (player != null)
            {
                for (int i = 0; i < 30; i++)
                {
                    player.OnTap();
                    yield return new WaitForSeconds(0.15f);
                }
            }
            yield return new WaitForSeconds(1f);

            // Screenshot 6: After gameplay (items may have appeared)
            string path6 = System.IO.Path.Combine(dir, "06_gameplay.png");
            ScreenCapture.CaptureScreenshot(path6, 2);
            Debug.Log("[AutoScreenshot] Saved: " + path6);

            // Legacy screenshot path
            string legacyPath = System.IO.Path.Combine(Application.dataPath, "..", "screenshot.png");
            ScreenCapture.CaptureScreenshot(legacyPath, 2);
            
            yield return new WaitForSeconds(2f);
            Debug.Log("[AutoScreenshot] All screenshots complete!");
            Application.Quit();
        }
    }
}
