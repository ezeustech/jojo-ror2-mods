using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
public static class ValidateSmashBundle
{
 public static void Run() {
  BuildDiavolo.Build();
  AssetBundle bundle=AssetBundle.LoadFromFile(Path.GetFullPath("../dist/Assets/diavoloassets"));
  var prefab=bundle.LoadAsset<GameObject>("Assets/Generated/5kcr01Visual.prefab");
  var root=Object.Instantiate(prefab);var anim=root.GetComponentInChildren<Animation>();
  AnimationClip clip=bundle.LoadAsset<AnimationClip>("Assets/Generated/ASBRGroundSmash.anim");
  if(!clip || !clip.legacy || clip.length<.5f)throw new System.Exception("Bundled smash clip unavailable: exists="+(bool)clip+" legacy="+(clip ? clip.legacy.ToString() : "none")+" length="+(clip ? clip.length.ToString() : "none")+" assets="+string.Join(",",bundle.GetAllAssetNames()));
  Transform[] bones=root.GetComponentsInChildren<Transform>();
  clip.SampleAnimation(anim.gameObject,0);Vector3[] start=bones.Select(b=>b.position).ToArray();
  clip.SampleAnimation(anim.gameObject,clip.length*.6f);
  float movement=0;for(int i=0;i<bones.Length;i++)movement+=Vector3.Distance(start[i],bones[i].position);
  if(movement<.01f)throw new System.Exception("Smash curves do not animate bundled skeleton: "+movement);
  if(!bundle.LoadAsset<Shader>("Assets/BodyAura.shader"))throw new System.Exception("Missing aura shader");
  File.WriteAllText("../docs/smash-bundle-validation.txt","PASS: direct runtime asset path loads legacy smash clip ("+clip.length+"s); summed bone movement="+movement+"; aura shader included.");
  Object.DestroyImmediate(root);bundle.Unload(true); AuditMoves.Run();
 }
}
