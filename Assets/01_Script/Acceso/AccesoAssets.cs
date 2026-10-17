using UnityEngine;
using TrustEXE.Tutorial;

namespace TrustEXE.Acceso
{
    public sealed class AccesoAssets : ScriptableObject
    {
        public TutorialAssets shared;
        public Sprite[] maps;
        public Texture2D[] evaWarning;
        public AudioClip ambience;
    }
}
