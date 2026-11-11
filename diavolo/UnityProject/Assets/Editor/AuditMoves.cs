using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
public static class AuditMoves
{
    public static void Run()
    {
        var report = new List<string>();
        var bundle = AssetBundle.LoadFromFile(Path.GetFullPath("../dist/Assets/diavoloassets"));
        foreach (string code in new[] { "5dvl01", "5kcr01" }) {
            var model = UnityEngine.Object.Instantiate(bundle.LoadAsset<GameObject>("Assets/Generated/"+code+"Visual.prefab"));
            var animation = model.GetComponentInChildren<Animation>();
            foreach (Transform bone in model.GetComponentsInChildren<Transform>()) report.Add(code+" BONE "+bone.name+" parent="+(bone.parent?bone.parent.name:"")+" local="+bone.localPosition+" rotation="+bone.localEulerAngles);
            var hair = code == "5dvl01" ? new Diavolo.EohHairPose(model) : null; var skins = model.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (var skin in skins) {
                var surface = new GameObject("audit surface"); surface.transform.SetParent(skin.transform,false);
                surface.AddComponent<MeshFilter>().sharedMesh = new Mesh();
                surface.AddComponent<MeshRenderer>().sharedMaterials = skin.sharedMaterials.Select(m=> { var u=new Material(Shader.Find("Unlit/Texture")); u.mainTexture=m.mainTexture; return u; }).ToArray();
                skin.enabled=false;
            }
            var camObj=new GameObject("audit camera"); var cam=camObj.AddComponent<Camera>();
            cam.transform.position=new Vector3(3,1.6f,5); cam.transform.LookAt(new Vector3(0,1,0)); cam.orthographic=true; cam.orthographicSize=1.6f;
            cam.clearFlags=CameraClearFlags.SolidColor; cam.backgroundColor=new Color(.12f,.12f,.14f);
            var target=new RenderTexture(256,320,24);cam.targetTexture=target;
            foreach (string suffix in new[]{"_spa_02_st","_spa_02_lp","_spa_02_ed"}) {
                var state=animation.Cast<AnimationState>().FirstOrDefault(s=>s.name.EndsWith(suffix));if(state==null)continue;
                var sheet=new Texture2D(256*5,320,TextureFormat.RGB24,false);
                for(int f=0;f<5;f++) {
                    state.clip.SampleAnimation(animation.gameObject,state.length*f/5f); if(hair != null) hair.Apply(Vector3.back);
                    foreach(var skin in skins){var mesh=skin.GetComponentInChildren<MeshFilter>().sharedMesh;skin.BakeMesh(mesh);mesh.RecalculateBounds();}
                    cam.Render();RenderTexture.active=target;sheet.ReadPixels(new Rect(0,0,256,320),f*256,0);
                }
                sheet.Apply();File.WriteAllBytes("../docs/corrected-"+code+suffix+".png",sheet.EncodeToPNG());UnityEngine.Object.DestroyImmediate(sheet);
            }
            RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(camObj);UnityEngine.Object.DestroyImmediate(model);
        }
        File.WriteAllLines("../docs/rig-audit.txt",report); bundle.Unload(true);
    }
}
