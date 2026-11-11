using System;
using System.IO;
using UnityEngine;
public static class ValidateRespawn
{
    public static void Run()
    {
        AssetBundle bundle = AssetBundle.LoadFromFile("../dist/Assets/diavoloassets");
        if (!bundle) throw new Exception("Missing bundle");
        Shader shader = bundle.LoadAsset<Shader>("Assets/CrimsonModel.shader");
        if (!shader || !shader.isSupported) throw new Exception("Unsupported model shader");
        foreach (string code in new[] { "5dvl01", "5kcr01" }) {
            GameObject prefab = bundle.LoadAsset<GameObject>("Assets/Generated/"+code+"Visual.prefab");
            for (int spawn=0;spawn<8;spawn++) {
                GameObject visual = UnityEngine.Object.Instantiate(prefab);
                var owned = new Diavolo.EohMaterials(); owned.Apply(visual,shader);
                foreach (SkinnedMeshRenderer skin in visual.GetComponentsInChildren<SkinnedMeshRenderer>()) {
                    foreach(Material material in skin.sharedMaterials) if(!material || !material.shader || !material.shader.isSupported || !material.mainTexture) throw new Exception("Invalid material on spawn "+spawn);
                    var surface = new GameObject("CPU surface"); surface.transform.SetParent(skin.transform,false);
                    var mesh=new Mesh();skin.BakeMesh(mesh);
                    surface.AddComponent<MeshFilter>().sharedMesh=mesh;
                    surface.AddComponent<MeshRenderer>().sharedMaterials=skin.sharedMaterials;
                    skin.enabled=false;
                }
                if(spawn==7) {
                    var cameraObject=new GameObject("respawn test camera");var camera=cameraObject.AddComponent<Camera>();
                    camera.transform.position=new Vector3(2,1.5f,4);camera.transform.LookAt(new Vector3(0,1,0));camera.orthographic=true;camera.orthographicSize=1.5f;
                    camera.backgroundColor=Color.gray;camera.clearFlags=CameraClearFlags.SolidColor;
                    var target=new RenderTexture(480,640,24);camera.targetTexture=target;camera.Render();RenderTexture.active=target;
                    var image=new Texture2D(480,640,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,480,640),0,0);image.Apply();
                    File.WriteAllBytes("../docs/respawn-"+code+".png",image.EncodeToPNG());
                    RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(cameraObject);
                }
                owned.Dispose(true); owned.Dispose(true);
                foreach (MeshFilter surface in visual.GetComponentsInChildren<MeshFilter>()) UnityEngine.Object.DestroyImmediate(surface.sharedMesh);
                UnityEngine.Object.DestroyImmediate(visual);
                foreach(Renderer renderer in prefab.GetComponentsInChildren<Renderer>(true)) foreach(Material material in renderer.sharedMaterials)
                    if(!material || !material.mainTexture || !material.shader) throw new Exception("Shared prefab was damaged by cleanup");
            }
        }
        bundle.Unload(true);
        File.WriteAllText("../docs/respawn-validation.txt","PASS: 8 spawn/destroy cycles per model; owned cleanup is idempotent; prefab materials/textures preserved; dedicated shader supported; eighth spawn rendered. In-game respawn still requires verification.");
    }
}
