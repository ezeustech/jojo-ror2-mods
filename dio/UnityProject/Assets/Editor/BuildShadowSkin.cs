using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class BuildShadowSkin
{
    public static void Build()
    {
        AssetDatabase.Refresh();
        var fadeShader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/Feedback/DioShadowStand.shader");
        if(!fadeShader)throw new Exception("Missing Shadow World fade shader");
        var fadeTexture=(TextureImporter)AssetImporter.GetAtPath("Assets/ShadowTextures/t_3wld01_body01_d_#1_0.png");
        fadeTexture.alphaSource=TextureImporterAlphaSource.FromInput;
        fadeTexture.alphaIsTransparency=true;fadeTexture.SaveAndReimport();
        Directory.CreateDirectory("Assets/Generated/ShadowAnimations");
        var report=new List<string>();
        foreach(string code in new[]{"3dio41","shadowworld"})
        {
            bool stand=code=="shadowworld";
            string sourceCode=stand?"3wld01":"3dio01",targetCode=stand?"3wld01":"3dio41";
            string path="Assets/Models/"+code+".fbx";
            var importer=(ModelImporter)AssetImporter.GetAtPath(path);
            importer.animationType=ModelImporterAnimationType.Legacy;importer.importAnimation=false;
            importer.importCameras=false;importer.importLights=false;importer.isReadable=true;importer.globalScale=.01f;importer.assetBundleName="eohdio";importer.SaveAndReimport();
            var root=new GameObject(code+"Visual");
            var model=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path),root.transform);model.name=code;
            foreach(var r in model.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                r.sharedMaterials=r.sharedMaterials.Select(m=>{
                    string texture=stand?(m.name.Contains("eye_")?"t_3wld01_iris_0":"t_3wld01_body01_d_#1_0"):"t_3dio41_body01_d_#1_0";
                    var mat=new Material(stand?fadeShader:Shader.Find("Standard")){name=code+"_"+m.name,mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ShadowTextures/"+texture+".png")};
                    if(!mat.mainTexture)throw new Exception("Missing shadow texture "+texture);
                    mat.SetFloat("_Glossiness",.15f);
                    string matPath="Assets/Generated/"+mat.name+".mat";
                    var saved=AssetDatabase.LoadAssetAtPath<Material>(matPath);
                    if(saved){EditorUtility.CopySerialized(mat,saved);UnityEngine.Object.DestroyImmediate(mat);return saved;}
                    AssetDatabase.CreateAsset(mat,matPath);return mat;
                }).ToArray();
            }
            var animation=model.GetComponent<Animation>()??model.AddComponent<Animation>();
            var bones=model.GetComponentsInChildren<Transform>().GroupBy(t=>t.name).ToDictionary(g=>g.Key,g=>g.First());
            AnimationClip idle=null;int count=0;
            foreach(var original in AssetDatabase.LoadAllAssetsAtPath("Assets/Models/"+sourceCode+".fbx").OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")))
            {
                var clip=new AnimationClip{name=original.name.Replace(sourceCode,targetCode),legacy=true,frameRate=original.frameRate,wrapMode=original.wrapMode};
                var bindings=new List<EditorCurveBinding>();var curves=new List<AnimationCurve>();
                foreach(var binding in AnimationUtility.GetCurveBindings(original))
                {
                    string name=binding.path.Split('/').Last().Replace(sourceCode,targetCode);
                    if(!bones.TryGetValue(name,out var bone))continue;
                    var mapped=binding;mapped.path=AnimationUtility.CalculateTransformPath(bone,model.transform);
                    bindings.Add(mapped);curves.Add(AnimationUtility.GetEditorCurve(original,binding));
                }
                if(bindings.Count==0){report.Add("Skipped accessory/facial-only clip: "+clip.name);UnityEngine.Object.DestroyImmediate(clip);continue;}
                AnimationUtility.SetEditorCurves(clip,bindings.ToArray(),curves.ToArray());clip.EnsureQuaternionContinuity();
                string outPath="Assets/Generated/ShadowAnimations/"+code+"_"+clip.name.Replace('|','_').Replace(':','_')+".anim";
                var saved=AssetDatabase.LoadAssetAtPath<AnimationClip>(outPath);
                if(saved){EditorUtility.CopySerialized(clip,saved);UnityEngine.Object.DestroyImmediate(clip);clip=saved;}else AssetDatabase.CreateAsset(clip,outPath);
                animation.AddClip(clip,clip.name);count++;
                if(clip.name.EndsWith(stand?"_bas_s_nut":"_bas_nut"))idle=clip;
            }
            if(!idle)throw new Exception("Missing idle for "+code);idle.SampleAnimation(model,0);
            Bounds bounds=default;bool first=true;
            foreach(var r in model.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var mesh=new Mesh();r.BakeMesh(mesh);
                foreach(var v in mesh.vertices){Vector3 p=r.transform.TransformPoint(v);if(first){bounds=new Bounds(p,Vector3.zero);first=false;}else bounds.Encapsulate(p);}
                r.localBounds=mesh.bounds;r.updateWhenOffscreen=true;UnityEngine.Object.DestroyImmediate(mesh);
            }
            if(first||bounds.size.y<=0)throw new Exception("Empty skin");
            if(stand)
            {
                // Bake the fade into vertex alpha in the neutral pose. CPU skinning
                // changes positions/normals only, so the mask follows the leg bones.
                Directory.CreateDirectory("Assets/Generated/ShadowMeshes");
                foreach(var r in model.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    var posed=new Mesh();r.BakeMesh(posed);
                    var vertices=posed.vertices;var colors=new Color[vertices.Length];
                    for(int i=0;i<colors.Length;i++)
                    {
                        float height=(r.transform.TransformPoint(vertices[i]).y-bounds.min.y)/bounds.size.y;
                        float alpha=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.36f,.59f,height));
                        colors[i]=new Color(1,1,1,alpha);
                    }
                    var mesh=UnityEngine.Object.Instantiate(r.sharedMesh);mesh.colors=colors;
                    string meshPath="Assets/Generated/ShadowMeshes/"+r.name.Replace(' ','_')+".asset";
                    var saved=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                    if(saved){EditorUtility.CopySerialized(mesh,saved);UnityEngine.Object.DestroyImmediate(mesh);mesh=saved;}else AssetDatabase.CreateAsset(mesh,meshPath);
                    r.sharedMesh=mesh;UnityEngine.Object.DestroyImmediate(posed);
                }
            }
            float scale=1.9f/bounds.size.y;model.transform.localScale*=scale;model.transform.localPosition=new Vector3(-bounds.center.x,-bounds.min.y,-bounds.center.z)*scale;
            string prefab="Assets/Generated/"+code+"Visual.prefab";PrefabUtility.SaveAsPrefabAsset(root,prefab);AssetImporter.GetAtPath(prefab).assetBundleName="eohdio";
            report.Add(code+": "+count+" animations; height="+bounds.size.y+" scale="+scale);
            UnityEngine.Object.DestroyImmediate(root);
        }
        AssetImporter.GetAtPath("Assets/ShadowTextures/medal_img_costume_3dio_d_2p_0.png").assetBundleName="eohdio";
        AssetDatabase.SaveAssets();
        File.WriteAllLines("../docs/shadow-build.txt",report);
        if(!BuildPipeline.BuildAssetBundles("../dist/Assets",BuildAssetBundleOptions.ChunkBasedCompression,BuildTarget.StandaloneWindows64))throw new Exception("Shadow bundle failed");
        ValidateShadowSkin.Run();
    }
}
