using UnityEngine;
using UnityEngine.SceneManagement;

namespace TrustEXE.Core
{
    /// <summary>
    /// Cross-scene session state shared by every sector: one shared pool of lives per
    /// level, the reason a run failed, and routing to the Resultado scene
    /// (Victoria / Derrota). Statics are seeded before each scene load and survive
    /// scene switches; the subsystem registration only clears them at process start.
    /// </summary>
    public static class GameSession
    {
        public const int MaxLives = 3;
        public static int Lives = MaxLives;
        public static string Level = "";
        public static string FailureReason = "";
        public static bool Defeated;
        public static bool Victorious;

        // Result screen statistics (copied before switching scenes).
        public static float Elapsed;
        public static int Strikes;
        public static int Hints;
        public static int Cooperation;
        public static int Ending; // 0 AISLAR, 1 RESTAURAR
        public static string EndingText = "";

        // Test-only hook: state validations run outside Play Mode must not load scenes.
        public static bool SuppressSceneLoad;

        /// Call from each sector's Start. Resets the shared pool only when the
        /// player enters a different level, so an in-level restart keeps the pool.
        public static void BeginLevel(string level)
        {
            if (Level != level) Lives = MaxLives;
            Level = level;
        }

        /// Consumes one shared life. Returns true when the pool is empty.
        public static bool LoseLife()
        {
            if (Lives > 0) Lives--;
            return Lives <= 0;
        }

        /// Routes to the Derrota screen, remembering the exact reason.
        public static void Defeat(string reason)
        {
            FailureReason = reason;
            Defeated = true;
            Victorious = false;
            if (!SuppressSceneLoad) SceneManager.LoadSceneAsync("Resultado");
        }

        /// Routes to the Victoria screen with the final stats.
        public static void Victory(string endingText, int ending, float elapsed, int strikes, int hints, int cooperation)
        {
            EndingText = endingText;
            Ending = ending;
            Elapsed = elapsed;
            Strikes = strikes;
            Hints = hints;
            Cooperation = cooperation;
            Victorious = true;
            Defeated = false;
            if (!SuppressSceneLoad) SceneManager.LoadSceneAsync("Resultado");
        }

        /// Reintentar nivel: restores the full pool and reloads the failed sector.
        public static void RetryLevel()
        {
            var level = Level;
            Lives = MaxLives;
            Level = "";
            Defeated = Victorious = false;
            if (SuppressSceneLoad) return;
            if (!string.IsNullOrEmpty(level)) SceneManager.LoadSceneAsync(level);
            else SceneManager.LoadSceneAsync("MainMenu");
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset() { Lives = MaxLives; Level = ""; FailureReason = ""; Defeated = Victorious = false; SuppressSceneLoad = false; }
    }
}