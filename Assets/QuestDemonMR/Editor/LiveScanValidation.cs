using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace QuestDemonMR.Editor
{
    public static class LiveScanValidation
    {
        private static int _checks;
        private static void Require(bool ok,string reason)
        { if(!ok)throw new InvalidOperationException("Live scan: "+reason); _checks++; Debug.Log("QDMR_SCAN_CHECK "+reason); }

        public static void Validate()
        {
            _checks=0;
            var preview=Shader.Find("QuestDemonMR/LiveScanSurface");
            Require(preview!=null&&!ShaderUtil.ShaderHasError(preview),"preview/occlusion shader has no import errors");
            Require(LiveRoomScanner.MeshLayer!=29,"room geometry isolated from portal dimension layer");
            Require(LiveScanGeometry.Key(new Vector3(-.01f,0,0)).x==-1,"negative world coordinates use floor, not truncation");
            Require(!LiveScanGeometry.IsKnown(new Vector2(1,0)),"unknown is not free");
            var sample=LiveScanGeometry.Fuse(default,.12f);
            Require(!LiveScanGeometry.IsKnown(sample),"single observation is not trusted");
            sample=LiveScanGeometry.Fuse(sample,.12f);
            Require(LiveScanGeometry.IsKnown(sample)&&Mathf.Abs(sample.x-.12f)<1e-6f,"repeated measurements become known");
            Require(LiveScanGeometry.Fuse(sample,-1)==sample,"occluded voxels preserve prior evidence");
            Require(LiveScanGeometry.Fuse(sample,float.NaN)==sample,"invalid depth does not clear geometry");
            sample=new Vector2(-.12f,6);
            for(var i=0;i<8;i++)sample=LiveScanGeometry.Fuse(sample,.24f);
            Require(sample.x>.1f&&sample.y<=6,"new free-space evidence removes moved furniture with bounded weight");
            var unknown=new Vector2[LiveScanGeometry.SampleCount];
            Require(LiveScanGeometry.Build(unknown).Indices.Length==0,"unobserved volume creates no collider");
            var field=new Vector2[unknown.Length];
            for(var i=0;i<field.Length;i++)field[i]=new Vector2(.24f,3);
            Require(LiveScanGeometry.Build(field).Indices.Length==0,"observed free volume creates no phantom boundary");
            for(var axis=0;axis<3;axis++)
            {
                const float plane=.615f;
                for(var z=0;z<17;z++)for(var y=0;y<17;y++)for(var x=0;x<17;x++)
                    field[LiveScanGeometry.Index(x,y,z)]=new Vector2((axis==0?x:axis==1?y:z)*LiveScanGeometry.Voxel-plane,3);
                var surface=LiveScanGeometry.Build(field);
                Require(surface.Vertices.Length>0,"interpolated plane produces a surface axis="+axis);
                var accurate=true;var outward=true;
                for(var i=0;i<surface.Vertices.Length;i++)
                { accurate&=Mathf.Abs(surface.Vertices[i][axis]-plane)<.0001f;outward&=surface.Normals[i][axis]>.999f; }
                Require(accurate,"sub-voxel surface interpolation axis="+axis);
                Require(outward,"collision winding faces observed free side axis="+axis);
                var host=new GameObject("ScanColliderValidation"){layer=LiveRoomScanner.MeshLayer};
                var mesh=new Mesh{indexFormat=IndexFormat.UInt32};
                try
                {
                    mesh.vertices=surface.Vertices;mesh.triangles=surface.Indices;mesh.RecalculateBounds();
                    host.AddComponent<MeshCollider>().sharedMesh=mesh;Physics.SyncTransforms();
                    var direction=axis==0?Vector3.right:axis==1?Vector3.up:Vector3.forward;
                    var origin=Vector3.one*.6f;origin[axis]=1.1f;
                    Require(Physics.Raycast(origin,-direction,out var hit,1,LiveRoomScanner.MeshMask)&&Mathf.Abs(hit.point[axis]-plane)<.001f,
                        "actual MeshCollider contacts interpolated surface axis="+axis);
                }
                finally { UnityEngine.Object.DestroyImmediate(host);UnityEngine.Object.DestroyImmediate(mesh); }
            }
            ValidateGpu(); ValidateGameplayQueries();
            Debug.Log("QDMR_SCAN_VALIDATION_OK checks="+_checks);
        }

        private static void ValidateGpu()
        {
            Require(SystemInfo.supportsComputeShaders&&SystemInfo.supportsAsyncGPUReadback,"editor GPU supports real compute/readback validation");
            var compute=UnityEngine.Object.Instantiate(Resources.Load<ComputeShader>("Spatial/LiveScan"));
            var texture=new Texture2DArray(64,64,2,TextureFormat.RFloat,false,true);
            var buffer=new ComputeBuffer(LiveScanGeometry.SampleCount,8);
            var carving=new ComputeBuffer(LiveScanGeometry.SampleCount,4);carving.SetData(new uint[LiveScanGeometry.SampleCount]);
            var points=new ComputeBuffer(48*48,16);
            try
            {
                var matrix=Matrix4x4.Perspective(90,1,.1f,10)*Matrix4x4.Scale(new Vector3(1,1,-1));
                var clip=matrix*new Vector4(0,0,2,1);var depth=(clip.z/clip.w+1)*.5f;
                var pixels=new float[64*64];for(var i=0;i<pixels.Length;i++)pixels[i]=depth;
                texture.SetPixelData(pixels,0,0);texture.SetPixelData(pixels,0,1);texture.Apply();
                compute.SetMatrix("_MapToWorld",Matrix4x4.identity);compute.SetMatrix("_WorldToDepth",matrix);compute.SetMatrix("_DepthToWorld",matrix.inverse);
                compute.SetVector("_ZParams",new Vector4(-2*10*.1f/9.9f,-10.1f/9.9f,0,0));
                compute.SetVector("_Head",Vector4.zero);compute.SetInt("_Width",64);compute.SetInt("_Height",64);
                compute.SetFloat("_Voxel",LiveScanGeometry.Voxel);compute.SetFloat("_Band",LiveScanGeometry.Band);
                var kernel=compute.FindKernel("Discover");compute.SetTexture(kernel,"_Depth",texture);compute.SetBuffer(kernel,"_Points",points);
                compute.Dispatch(kernel,6,6,1);
                var request=AsyncGPUReadback.Request(points);request.WaitForCompletion();
                Require(!request.hasError,"GPU depth discovery readback succeeds");
                var result=request.GetData<Vector4>();var accurate=true;var count=0;
                foreach(var p in result)if(p.w>.5f){accurate&=Mathf.Abs(p.z-2)<.001f;count++;}
                Require(accurate&&count>1000,"depth reconstruction uses matching Meta NDC convention");
                kernel=compute.FindKernel("Integrate");compute.SetTexture(kernel,"_Depth",texture);compute.SetBuffer(kernel,"_Field",buffer);
                compute.SetBuffer(kernel,"_CarveEvidence",carving);
                compute.SetVector("_ChunkOrigin",new Vector4(-.64f,-.64f,1.28f,0));
                buffer.SetData(new Vector2[LiveScanGeometry.SampleCount]);
                compute.Dispatch(kernel,3,3,3);compute.Dispatch(kernel,3,3,3);
                request=AsyncGPUReadback.Request(buffer);request.WaitForCompletion();
                Require(!request.hasError,"GPU TSDF readback succeeds");
                var values=request.GetData<Vector2>();
                Require(values[LiveScanGeometry.Index(8,8,5)].x>.20f&&values[LiveScanGeometry.Index(8,8,5)].y==2,"GPU confirms free space before surface");
                Require(Mathf.Abs(values[LiveScanGeometry.Index(8,8,9)].x)<.002f,"GPU zero crossing at measured wall");
                Require(values[LiveScanGeometry.Index(8,8,16)].y==0,"GPU does not invent evidence behind surface");
                var wall=values[LiveScanGeometry.Index(8,8,9)];uint evidence=0;var expected=wall;
                clip=matrix*new Vector4(0,0,4,1);depth=(clip.z/clip.w+1)*.5f;
                for(var i=0;i<pixels.Length;i++)pixels[i]=depth;texture.SetPixelData(pixels,0,0);texture.SetPixelData(pixels,0,1);texture.Apply();compute.SetTexture(kernel,"_Depth",texture);
                var discovery=compute.FindKernel("Discover");compute.SetTexture(discovery,"_Depth",texture);compute.Dispatch(discovery,6,6,1);
                var bg=AsyncGPUReadback.Request(points);bg.WaitForCompletion();
                Require(!bg.hasError&&Mathf.Abs(bg.GetData<Vector4>()[24+48*24].z-4)<.001f,"background test really supplies changed GPU depth");
                for(var i=1;i<=3;i++)
                {
                    expected=LiveScanGeometry.FuseStable(expected,.24f,ref evidence);
                    compute.SetTexture(kernel,"_Depth",texture);compute.SetBuffer(kernel,"_Field",buffer);compute.SetBuffer(kernel,"_CarveEvidence",carving);
                    compute.Dispatch(kernel,3,3,3);request=AsyncGPUReadback.Request(buffer);request.WaitForCompletion();
                    var actual=request.GetData<Vector2>()[LiveScanGeometry.Index(8,8,9)];
                    var evidenceRead=AsyncGPUReadback.Request(carving);evidenceRead.WaitForCompletion();
                    Debug.Log("QDMR_CARVE_GPU observation="+i+" expected="+expected.ToString("F6")+" actual="+actual.ToString("F6")+" gpu_evidence="+evidenceRead.GetData<uint>()[LiveScanGeometry.Index(8,8,9)]);
                    Require(Vector2.Distance(expected,actual)<.001f,"CPU/GPU persistent-carving parity observation "+i);
                    if(i<3)Require(actual==wall,"transient background cannot tear existing GPU wall "+i);
                    else Require(actual.x>wall.x+.04f,"persistent background can still remove obsolete wall");
                }
                clip=matrix*new Vector4(0,0,2,1);depth=(clip.z/clip.w+1)*.5f;
                for(var i=0;i<pixels.Length;i++)pixels[i]=depth;texture.SetPixelData(pixels,0,0);texture.SetPixelData(pixels,0,1);texture.Apply();compute.SetTexture(kernel,"_Depth",texture);carving.SetData(new uint[LiveScanGeometry.SampleCount]);
                compute.SetVector("_ChunkOrigin",new Vector4(-.64f,-.64f,-2,0));buffer.SetData(new Vector2[LiveScanGeometry.SampleCount]);
                compute.Dispatch(kernel,3,3,3);request=AsyncGPUReadback.Request(buffer);request.WaitForCompletion();
                var allUnknown=true;foreach(var s in request.GetData<Vector2>())allUnknown&=s.y==0;
                Require(allUnknown,"outside-frustum volume stays unknown");
                foreach(var yaw in new[]{45f,90f,178f})
                {
                    var frame=Matrix4x4.TRS(new Vector3(3,.4f,-2),Quaternion.Euler(0,yaw,0),Vector3.one);
                    compute.SetMatrix("_MapToWorld",frame);compute.SetMatrix("_WorldToDepth",matrix*frame.inverse);compute.SetMatrix("_DepthToWorld",frame*matrix.inverse);
                    compute.SetVector("_Head",frame.MultiplyPoint3x4(Vector3.zero));compute.SetVector("_ChunkOrigin",new Vector4(-.64f,-.64f,1.28f,0));
                    buffer.SetData(new Vector2[LiveScanGeometry.SampleCount]);compute.Dispatch(kernel,3,3,3);compute.Dispatch(kernel,3,3,3);
                    request=AsyncGPUReadback.Request(buffer);request.WaitForCompletion();Require(!request.hasError,"transformed GPU readback "+yaw);
                    var transformed=request.GetData<Vector2>();
                    Require(transformed[LiveScanGeometry.Index(8,8,5)].x>.20f&&transformed[LiveScanGeometry.Index(8,8,5)].y==2,"world depth integrates canonical free voxel "+yaw);
                    Require(Mathf.Abs(transformed[LiveScanGeometry.Index(8,8,9)].x)<.002f&&transformed[LiveScanGeometry.Index(8,8,16)].y==0,"transformed surface and occluded voxels remain correct "+yaw);
                }
            }
            finally {buffer.Release();carving.Release();points.Release();UnityEngine.Object.DestroyImmediate(texture);UnityEngine.Object.DestroyImmediate(compute);}
        }

        private static void ValidateGameplayQueries()
        {
            var host=new GameObject("LiveMapValidation");
            var scanner=host.AddComponent<LiveRoomScanner>();
            Vector2[] Plane(int chunkY,bool wall=false)
            {
                var f=new Vector2[LiveScanGeometry.SampleCount];
                for(var z=0;z<17;z++)for(var y=0;y<17;y++)for(var x=0;x<17;x++)
                {
                    var distance=y*.08f+chunkY*1.28f-.16f;
                    if(wall)distance=Mathf.Min(distance,.95f-x*.08f);
                    f[LiveScanGeometry.Index(x,y,z)]=new Vector2(Mathf.Clamp(distance,-.24f,.24f),3);
                }
                return f;
            }
            try
            {
                scanner.ImportValidationChunk(Vector3Int.zero,Plane(0),.16f);
                scanner.ImportValidationChunk(Vector3Int.up,Plane(1),.16f);
                var foot=new Vector3(.64f,.16f,.64f);
                foreach(var radius in new[]{.25f,.30f,.34f})
                    Require(scanner.IsWalkable(foot,radius),"confirmed floor supports body radius "+radius);
                Require(LiveRoomGrid.IsClear(null,foot,.3f),"navigation uses live map without a saved room");
                Require(!scanner.IsWalkable(new Vector3(3,.16f,3),.25f),"unscanned floor cannot authorize a spawn");
                Require(scanner.TrySample(foot+Vector3.up*.5f,out var sample)&&sample.x>0,"retained free-space query does not depend on current depth FOV");
                scanner.ImportValidationChunk(Vector3Int.zero,Plane(0,true),.16f);
                scanner.ImportValidationChunk(Vector3Int.up,Plane(1,true),.16f);
                Require(!scanner.IsWalkable(foot,.34f),"measured wall blocks large body clearance");
                Require(!scanner.SegmentClear(new Vector3(.5f,.6f,.64f),new Vector3(1.1f,.6f,.64f)),"shared collision mesh blocks traversing a wall");
                scanner.ImportValidationChunk(Vector3Int.zero,Plane(0),.16f);
                scanner.ImportValidationChunk(Vector3Int.up,Plane(1),.16f);
                Require(scanner.IsWalkable(foot,.34f),"updated free evidence removes obsolete wall collider");
            }
            finally {UnityEngine.Object.DestroyImmediate(host);}
            Require(!LiveRoomScanner.Active,"scanner cleanup clears singleton and owned map");
        }

        public static void ValidateAndBuild()
        {
            QuestDemonProjectBuilder.PrepareProject();
            FollowupV17Validation.Validate(); StartupValidation.Validate(); Validate();
            QuestDemonProjectBuilder.BuildAndroidLiveScanPrepared();
        }
    }
}
