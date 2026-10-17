using System;
using UnityEngine;
using TrustEXE.Menu;

namespace TrustEXE.Tutorial
{
    public sealed class TutorialAssets : ScriptableObject
    {
        [Serializable] public sealed class Character
        {
            public string id;
            public Sprite idle;
            public Sprite[] down, up, left, right;
        }
        public TrustMenuCatalog menu;
        public Sprite[] maps, consoles, nodes, switchesOff, switchesOn, doorsClosed, doorsOpen;
        public Character[] characters;
        public AudioClip music;
    }
}
