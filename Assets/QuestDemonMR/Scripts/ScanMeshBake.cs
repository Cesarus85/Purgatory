using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
namespace QuestDemonMR
{
    // Each mesh has exactly one bake owner. No worker mutates a live collider;
    // reset/destruction waits asynchronously before releasing native mesh memory.
    public sealed class ScanMeshBake:IDisposable
    {
        Mesh _mesh;
        public Task Work {get;}
        public static Mesh CreateMesh(LiveScanGeometry.Surface surface)
        {
            var mesh=new Mesh{name="LiveScanChunk",indexFormat=surface.Vertices.Length>65535?IndexFormat.UInt32:IndexFormat.UInt16};
            mesh.vertices=surface.Vertices;mesh.normals=surface.Normals;mesh.triangles=surface.Indices;mesh.RecalculateBounds();
            return mesh;
        }
        public ScanMeshBake(LiveScanGeometry.Surface surface)
        {
            _mesh=CreateMesh(surface);var id=_mesh.GetInstanceID();
            Work=surface.Indices.Length==0?Task.CompletedTask:Task.Run(()=>Physics.BakeMesh(id,false,LiveRoomScanner.ScanCooking));
        }
        public Mesh Take()
        {
            if(!Work.IsCompletedSuccessfully)throw new InvalidOperationException("Scan bake not ready");
            var mesh=_mesh;_mesh=null;return mesh;
        }
        public async void Dispose()
        {
            var mesh=_mesh;_mesh=null;if(mesh==null)return;
            try{await Work;}catch{ /* caller reports failure; still release this mesh */ }
            if(mesh!=null){if(Application.isPlaying)UnityEngine.Object.Destroy(mesh);else UnityEngine.Object.DestroyImmediate(mesh);}
        }
    }
}
