using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using UnityEngine;
namespace QuestDemonMR
{
    [Serializable] public sealed class RoomProfileInfo
    {
        public string id,name,anchor,updated;
        public Vector3 anchorPosition;public Quaternion anchorRotation;
        public float floor;public bool hasShrine;public Vector3 shrinePosition;public Quaternion shrineRotation;
        // Explicit physical landmarks, in the same immutable coordinates as the TSDF.
        public int calibrationVersion;public Vector3 pointA,pointB;
        public bool HasCalibration=>calibrationVersion==1;
    }
    public sealed class RoomProfileData
    {
        public RoomProfileInfo Info;
        public readonly List<(Vector3Int key,Vector2[] samples)> Chunks=new();
    }
    // One bounded, checksummed, atomically replaced file per slot. No camera images.
    public static class RoomProfileStore
    {
        public const int Slots=5;
        const int Version=2,MaxBytes=40*1024*1024;
#if UNITY_EDITOR
        internal static string TestRoot;
#endif
        public static string Root
        {
            get
            {
#if UNITY_EDITOR
                if(TestRoot!=null)return TestRoot;
#endif
                return Path.Combine(Application.persistentDataPath,"room-profiles");
            }
        }
        static string PathFor(string root,string id)
        {if(!int.TryParse(id,out var n)||n<1||n>Slots||id!=n.ToString())throw new InvalidDataException("Invalid profile slot");return Path.Combine(root,"room-"+id+".qroom");}
        public static void Validate(RoomProfileData data)
        {
            var i=data?.Info;if(i==null||!Guid.TryParse(i.anchor,out _)||string.IsNullOrEmpty(i.name)||i.name.Length>40)throw new InvalidDataException("Invalid room metadata");
            PathFor("",i.id);
            if(!Finite(i.anchorPosition)||!Rotation(i.anchorRotation)||!float.IsFinite(i.floor)||Mathf.Abs(i.floor)>1000||!Finite(i.shrinePosition)||!Rotation(i.shrineRotation))throw new InvalidDataException("Invalid room pose");
            if(i.calibrationVersion<0||i.calibrationVersion>1||i.HasCalibration&&!RoomCalibration.ValidPair(i.pointA,i.pointB))throw new InvalidDataException("Invalid room landmarks");
            if(data.Chunks.Count<1||data.Chunks.Count>LiveRoomScanner.MaxChunks)throw new InvalidDataException("Invalid room size");
            var keys=new HashSet<Vector3Int>();
            foreach(var c in data.Chunks)
            {
                if(!keys.Add(c.key)||Mathf.Abs((float)c.key.x)>1000||Mathf.Abs((float)c.key.y)>1000||Mathf.Abs((float)c.key.z)>1000||c.samples==null||c.samples.Length!=LiveScanGeometry.SampleCount)throw new InvalidDataException("Invalid chunk");
                foreach(var s in c.samples)if(!float.IsFinite(s.x)||!float.IsFinite(s.y)||Mathf.Abs(s.x)>LiveScanGeometry.Band+.001f||s.y<0||s.y>6.01f)throw new InvalidDataException("Invalid field");
            }
        }
        static bool Finite(Vector3 p)=>float.IsFinite(p.x)&&float.IsFinite(p.y)&&float.IsFinite(p.z)&&p.sqrMagnitude<10000000;
        static bool Rotation(Quaternion q)=>float.IsFinite(q.x)&&float.IsFinite(q.y)&&float.IsFinite(q.z)&&float.IsFinite(q.w)&&Mathf.Abs(q.x*q.x+q.y*q.y+q.z*q.z+q.w*q.w-1)<.02f;
        public static void Save(string root,RoomProfileData data)
        {
            Validate(data);Directory.CreateDirectory(root);var path=PathFor(root,data.Info.id);var pending=path+".pending";
            using var payload=new MemoryStream();
            using(var w=new BinaryWriter(payload,System.Text.Encoding.UTF8,true))
            {
                w.Write(Version);w.Write(JsonUtility.ToJson(data.Info));w.Write(data.Chunks.Count);
                foreach(var c in data.Chunks){w.Write(c.key.x);w.Write(c.key.y);w.Write(c.key.z);foreach(var s in c.samples){w.Write(s.x);w.Write(s.y);}}
            }
            var bytes=payload.ToArray();using var sha=SHA256.Create();var digest=sha.ComputeHash(bytes);
            using(var file=new FileStream(pending,FileMode.Create,FileAccess.Write,FileShare.None))
            {file.Write(digest,0,digest.Length);using(var zip=new GZipStream(file,System.IO.Compression.CompressionLevel.Fastest,true))zip.Write(bytes,0,bytes.Length);file.Flush(true);}
            if(File.Exists(path))File.Replace(pending,path,path+".previous");else File.Move(pending,path);
        }
        public static RoomProfileData Load(string root,string id)
        {
            var path=PathFor(root,id);if(new FileInfo(path).Length>MaxBytes)throw new InvalidDataException("Profile too large");
            using var file=File.OpenRead(path);var digest=new byte[32];if(file.Read(digest,0,32)!=32)throw new InvalidDataException("Short profile");
            using var zip=new GZipStream(file,CompressionMode.Decompress);using var payload=new MemoryStream();var buffer=new byte[65536];int n;
            while((n=zip.Read(buffer,0,buffer.Length))>0){if(payload.Length+n>MaxBytes)throw new InvalidDataException("Expanded profile too large");payload.Write(buffer,0,n);}
            var bytes=payload.ToArray();using var sha=SHA256.Create();var actual=sha.ComputeHash(bytes);for(var j=0;j<32;j++)if(actual[j]!=digest[j])throw new InvalidDataException("Profile checksum");
            payload.Position=0;using var r=new BinaryReader(payload);
            var version=r.ReadInt32();if(version!=1&&version!=Version)throw new InvalidDataException("Unsupported profile version");
            var json=r.ReadString();if(json.Length>4096)throw new InvalidDataException("Metadata too large");
            var data=new RoomProfileData{Info=JsonUtility.FromJson<RoomProfileInfo>(json)};var count=r.ReadInt32();
            if(count<1||count>LiveRoomScanner.MaxChunks)throw new InvalidDataException("Chunk limit");
            for(var k=0;k<count;k++){var key=new Vector3Int(r.ReadInt32(),r.ReadInt32(),r.ReadInt32());var field=new Vector2[LiveScanGeometry.SampleCount];for(var j=0;j<field.Length;j++)field[j]=new Vector2(r.ReadSingle(),r.ReadSingle());data.Chunks.Add((key,field));}
            if(payload.Position!=payload.Length||data.Info.id!=id)throw new InvalidDataException("Unexpected profile data");Validate(data);return data;
        }
        public static bool Exists(string root,string id)=>File.Exists(PathFor(root,id));
        public static void Trash(string root,string id)
        {var p=PathFor(root,id);if(File.Exists(p))File.Move(p,p+".deleted-"+DateTime.UtcNow.Ticks);}
    }
    public static class RoomAlignment
    {
        public static Pose MapToWorld(Pose savedAnchor,Pose localizedAnchor)=>Delta(localizedAnchor,savedAnchor);
        public static Pose Delta(Pose saved,Pose localized)
        {
            if(Vector3.Angle(saved.rotation*Vector3.up,localized.rotation*Vector3.up)>3)throw new InvalidOperationException("Anchor tilt mismatch");
            var q=Quaternion.Euler(0,Mathf.DeltaAngle(localized.rotation.eulerAngles.y,saved.rotation.eulerAngles.y),0);
            return new Pose(saved.position-q*localized.position,q);
        }
        public static Vector2 Revalidate(Vector2 old,float sdf)
        {
            if(old.y>=0)return LiveScanGeometry.Fuse(old,sdf);
            if(!float.IsFinite(sdf)||sdf< -LiveScanGeometry.Band)return old;
            var value=Mathf.Clamp(sdf,-LiveScanGeometry.Band,LiveScanGeometry.Band);
            return Mathf.Abs(old.x-value)<.06f?new Vector2(value,2):new Vector2(value,1);
        }
    }
}
