using UnityEngine;
using TrustEXE.Tutorial;
namespace TrustEXE.Vigilancia {
 public sealed class VigilanciaAssets:ScriptableObject {
  public TutorialAssets shared;
  public Sprite[] maps,cameras,alarmCameras;
  public Texture2D[] evaAlarm;
  public AudioClip music;
 }
}
