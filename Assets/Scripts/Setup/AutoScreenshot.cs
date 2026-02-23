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
            // Wait for scene to build and a few frames of gameplay
            yield return new WaitForSeconds(3f);
            
            // Simulate some taps
            if (LeaderboardManager.Instance != null)
            {
                for (int i = 0; i < 20; i++)
                {
                    LeaderboardManager.Instance.AddPlayerScore(10);
                    yield return new WaitForSeconds(0.1f);
                }
            }

            yield return new WaitForSeconds(1f);
            
            string path = System.IO.Path.Combine(Application.dataPath, "..", "screenshot.png");
            ScreenCapture.CaptureScreenshot(path, 2); // 2x supersampling
            Debug.Log("Screenshot saved to: " + path);
            
            yield return new WaitForSeconds(2f);
            Application.Quit();
        }
    }
}
