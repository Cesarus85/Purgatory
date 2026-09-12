using System;
using System.IO;
using UnityEngine;
namespace QuestDemonMR
{
    // Local, bounded two-session evidence survives quitting the app and logcat rotation.
    public static class RoomTrackingTrace
    {
        static string _file;static int _lines;
        public static void Begin()
        {
            if(!Application.isPlaying)return;
            try
            {
                var root=Path.Combine(Application.persistentDataPath,"room-tracking");Directory.CreateDirectory(root);
                _file=Path.Combine(root,"current.log");
                if(File.Exists(_file))File.Copy(_file,Path.Combine(root,"previous.log"),true);
                File.WriteAllText(_file,"Purgatory "+Application.version+" start="+DateTime.UtcNow.ToString("O")+"\n");_lines=0;
            }
            catch(Exception e){_file=null;Debug.LogWarning("QDMR_TRACKING_TRACE_UNAVAILABLE "+e.Message);}
        }
        public static void Write(string message)
        {
            Debug.Log("QDMR_TRACKING "+message);
            if(_file==null||_lines++>=1000)return;
            try{File.AppendAllText(_file,DateTime.UtcNow.ToString("O")+" "+message+"\n");}catch(IOException){_file=null;}
        }
        public static string Pose(Pose p)=>"p="+p.position.ToString("F4")+" q="+p.rotation.ToString("F5");
    }
}
