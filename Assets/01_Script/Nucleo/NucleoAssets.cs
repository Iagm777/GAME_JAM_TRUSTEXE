using UnityEngine;
using TrustEXE.Tutorial;
namespace TrustEXE.Nucleo {
 public sealed class NucleoAssets:ScriptableObject {
  public TutorialAssets shared;
  public Sprite[] maps;
  public Texture2D[] warning,alarm;
  public AudioClip music;
 }
}
