using System;
using UnityEngine;

namespace TrustEXE.Menu
{
    [CreateAssetMenu(menuName = "TRUST.EXE/Menu catalog")]
    public sealed class TrustMenuCatalog : ScriptableObject
    {
        [Serializable] public sealed class Avatar
        {
            public string id;
            public Texture2D idle;
            public Texture2D[] walk;
            public Texture2D[] think;
        }
        public Avatar[] avatars;
        public Texture2D[] eva;
        public Texture2D backdrop;
        [Tooltip("Exact name or build path of the gameplay scene. It must be enabled in Build Profiles.")]
        public string tutorialScene = "Tutorial";
    }

    // Read these identities when spawning the two players in the Tutorial scene.
    public static class LocalParty
    {
        public static string Explorer { get; private set; }
        public static string Operator { get; private set; }
        public static bool IsReady => !string.IsNullOrEmpty(Explorer) && !string.IsNullOrEmpty(Operator);
        public static void Select(string explorer, string operatorId)
        {
            if (string.IsNullOrEmpty(explorer) || string.IsNullOrEmpty(operatorId) || explorer == operatorId)
                throw new ArgumentException("Select two different avatars.");
            Explorer = explorer;
            Operator = operatorId;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Clear() { Explorer = null; Operator = null; }
    }
}
