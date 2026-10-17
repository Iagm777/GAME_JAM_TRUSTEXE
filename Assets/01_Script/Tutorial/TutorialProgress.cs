using System;
using System.IO;
using UnityEngine;

namespace TrustEXE.Tutorial
{
    public enum Lesson { Movement, Clues, Sequence, Exit, Complete }
    /// <summary>Deterministic puzzle state, independent of rendering and input devices.</summary>
    [Serializable] public sealed class TutorialProgress
    {
        public bool[] moved = new bool[2];
        public bool terminalRead, nodeRead;
        public int sequence;
        public bool[] atExit = new bool[2];
        public bool completed;
        public bool DoorOpen => sequence==2;
        public Lesson Lesson => completed?Lesson.Complete:DoorOpen?Lesson.Exit:terminalRead&&nodeRead?Lesson.Sequence:moved[0]&&moved[1]?Lesson.Clues:Lesson.Movement;
        public bool ReadClue(int player)
        {
            if(!moved[0]||!moved[1]) return false;
            if(player==0) terminalRead=true; else nodeRead=true; return true;
        }
        public bool PressSwitch(int player,int value)
        {
            if(!terminalRead||!nodeRead||DoorOpen) return false;
            if((sequence==0&&player==0&&value==2)||(sequence==1&&player==1&&value==2)) { sequence++; return true; }
            sequence=0; return false;
        }
        public bool CanComplete => DoorOpen&&atExit[0]&&atExit[1];
    }
    [Serializable] public sealed class TutorialSave
    {
        public int version=1;
        public string sector="Tutorial", nextSector="Acceso", explorer, operatorId;
        public Vector2 explorerPosition, operatorPosition;
        public TutorialProgress progress;
        public string savedUtc;
    }
    public static class TutorialCheckpoint
    {
        public static string FilePath => Path.Combine(Application.persistentDataPath,"tutorial-checkpoint.json");
        public static void Write(TutorialSave save,string path=null)
        {
            if(save.progress==null||!save.progress.CanComplete) throw new InvalidOperationException("Both players must reach the exit.");
            path=path??FilePath; var temporary=path+".tmp";
            File.WriteAllText(temporary,JsonUtility.ToJson(save,true));
            if(File.Exists(path)) File.Replace(temporary,path,null); else File.Move(temporary,path);
        }
        public static bool TryRead(out TutorialSave save,string path=null)
        {
            save=null;
            try
            {
                path=path??FilePath; if(!File.Exists(path)) return false;
                var data=JsonUtility.FromJson<TutorialSave>(File.ReadAllText(path));
                if(data==null||data.version!=1||data.sector!="Tutorial"||data.progress==null||
                   data.progress.moved==null||data.progress.moved.Length!=2||data.progress.atExit==null||data.progress.atExit.Length!=2||
                   !data.progress.CanComplete||!data.progress.completed||string.IsNullOrEmpty(data.explorer)||string.IsNullOrEmpty(data.operatorId)||data.explorer==data.operatorId) return false;
                save=data; return true;
            }
            catch(Exception e) when(e is IOException||e is ArgumentException||e is UnauthorizedAccessException) { return false; }
        }
    }
}
