using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;
namespace Diavolo
{
    // Explicit draws reuse the actual stage geometry, without changing collision or materials.
    public sealed class GroundHologram
    {
        private readonly Material material;
        private readonly CommandBuffer commands;
        private Camera camera;private float nextBuild;private Vector3 lastCenter;
        private readonly Dictionary<Renderer,MeshFilter> filters=new Dictionary<Renderer,MeshFilter>();
        private readonly Dictionary<Terrain,Mesh> terrainMeshes=new Dictionary<Terrain,Mesh>();
        public GroundHologram(Shader shader){material=new Material(shader);commands=new CommandBuffer {name="Diavolo faint ground hologram"};}
        public void Draw(Camera target,IEnumerable<KeyValuePair<Renderer,bool>> scenery,Terrain[] terrains,Vector3 center,float opacity){
            Detach();material.SetVector("_Center",center);material.SetFloat("_Opacity",opacity);
            if(Time.time<nextBuild && (center-lastCenter).sqrMagnitude<4f){camera=target;camera.AddCommandBuffer(CameraEvent.BeforeForwardAlpha,commands);return;}
            nextBuild=Time.time+.25f;lastCenter=center;commands.Clear();
            foreach(var pair in scenery){
                Renderer r=pair.Key;if(!r || pair.Value || !r.enabled || !(r is MeshRenderer) || r.bounds.SqrDistance(center)>2304)continue;
                if(r.bounds.min.y>center.y+5f || r.bounds.max.y<center.y-10f)continue;
                MeshFilter filter;if(!filters.TryGetValue(r,out filter)){filter=r.GetComponent<MeshFilter>();filters[r]=filter;}if(!filter || !filter.sharedMesh)continue;
                for(int sub=0;sub<filter.sharedMesh.subMeshCount;sub++)commands.DrawMesh(filter.sharedMesh,r.localToWorldMatrix,material,sub,0);
            }
            foreach(Terrain terrain in terrains){
                if(!terrain || !terrain.terrainData)continue;
                Bounds bounds=terrain.terrainData.bounds;bounds.center+=terrain.transform.position;if(bounds.SqrDistance(center)>2304)continue;
                Mesh mesh;if(!terrainMeshes.TryGetValue(terrain,out mesh)){mesh=MakeTerrain(terrain.terrainData);terrainMeshes.Add(terrain,mesh);}
                commands.DrawMesh(mesh,terrain.transform.localToWorldMatrix,material,0,0);
            }
            camera=target;camera.AddCommandBuffer(CameraEvent.BeforeForwardAlpha,commands);
        }
        private static Mesh MakeTerrain(TerrainData data){
            int n=Mathf.Min(data.heightmapResolution,129);var vertices=new Vector3[n*n];var normals=new Vector3[n*n];var triangles=new int[(n-1)*(n-1)*6];int t=0;
            for(int z=0;z<n;z++)for(int x=0;x<n;x++){float u=(float)x/(n-1),v=(float)z/(n-1);int index=z*n+x;vertices[index]=new Vector3(u*data.size.x,data.GetInterpolatedHeight(u,v),v*data.size.z);normals[index]=data.GetInterpolatedNormal(u,v);if(x<n-1 && z<n-1){triangles[t++]=index;triangles[t++]=index+n;triangles[t++]=index+1;triangles[t++]=index+1;triangles[t++]=index+n;triangles[t++]=index+n+1;}}
            var mesh=new Mesh {name="Erased terrain hologram",indexFormat=IndexFormat.UInt32};mesh.vertices=vertices;mesh.normals=normals;mesh.triangles=triangles;mesh.RecalculateBounds();return mesh;
        }
        public void Detach(){if(camera)camera.RemoveCommandBuffer(CameraEvent.BeforeForwardAlpha,commands);camera=null;}
        public void Dispose(){Detach();commands.Release();Object.Destroy(material);foreach(var mesh in terrainMeshes.Values)if(mesh)Object.Destroy(mesh);terrainMeshes.Clear();}
    }
}
