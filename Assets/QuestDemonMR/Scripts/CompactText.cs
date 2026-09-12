using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
namespace QuestDemonMR
{
    public static class CompactText
    {
        public const float HudWidth=.78f;
        public static string Wrap(string value,int columns)
        {
            columns=Math.Max(4,columns);
            var lines=new List<string>();
            foreach(var paragraph in (value??string.Empty).Replace("\r","").Split('\n'))
            {
                var line=new StringBuilder();
                foreach(var word in paragraph.Split(new[]{' ','\t'},StringSplitOptions.RemoveEmptyEntries))
                {
                    var rest=word;
                    if(line.Length>0&&line.Length+1+rest.Length>columns){lines.Add(line.ToString());line.Clear();}
                    while(rest.Length>columns){lines.Add(rest.Substring(0,columns));rest=rest.Substring(columns);}
                    if(line.Length>0)line.Append(' ');
                    line.Append(rest);
                }
                lines.Add(line.ToString());
            }
            return string.Join("\n",lines);
        }
        public static void Set(TextMesh text,string content,int columns,float width,float characterSize)
        =>SetLiteral(text,HandRoles.Hint(content).Replace("LINKER STICK",HandRoles.Left?"RECHTER STICK":"LINKER STICK").Replace("RECHTEN STICK",HandRoles.Left?"LINKEN STICK":"RECHTEN STICK"),columns,width,characterSize);
        // Landmark A/B are names, not controller buttons; do not remap them for left-handed users.
        public static void SetLiteral(TextMesh text,string content,int columns,float width,float characterSize)
        {
            if(text==null)return;
            text.characterSize=characterSize;text.text=Wrap(content,columns);
            var renderer=text.GetComponent<MeshRenderer>();
            if(renderer==null)return;
            var measured=renderer.localBounds.size.x;
            if(measured>width)text.characterSize*=width/measured;
        }
        public static string Hud(int health,int wave,string ammo,bool twoHanded,int score,int record,bool running,string banner)
        {
            var state=$"LEBEN {health:000} · WELLE {wave}\n{ammo}"+(twoHanded?" · 2-HAND":"");
            if(!running)state+=$"\nPUNKTE {score} · REKORD {record}";
            return string.IsNullOrEmpty(banner)?state:Wrap(banner,27)+"\n"+state;
        }
        public static string Scan(string status,bool paused,bool full)
        {
            var content=status+"\nLINKER STICK: SCAN AN/AUS";
            if(paused)content+="\nRECHTEN STICK 2 S HALTEN:\nNEUER SCAN, RUNDE ZURÜCK";
            if(full)content+="\nSCAN-SPEICHER VOLL\nBereich bleibt spielbar";
            return Wrap(content,27);
        }
    }
}
