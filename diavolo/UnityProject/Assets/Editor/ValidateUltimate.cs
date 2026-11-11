using UnityEngine;
using UnityEditor;
using System.IO;
public static class ValidateUltimate
{
 public static void Run() {
  int checks=0;
  foreach(float height in new[]{1.2f,2.5f,12f,30f}) {
   Mesh mesh=Diavolo.SpikeMesh.Create(height,height*.06f);
   if(Mathf.Abs(mesh.bounds.size.y-height)>.001f)throw new System.Exception("Spike height mismatch");checks++;
   foreach(int index in mesh.triangles)if(index<0 || index>=mesh.vertexCount)throw new System.Exception("Invalid triangle");checks++;
   if(mesh.bounds.min.y!=0)throw new System.Exception("Ground origin mismatch");checks++; Object.DestroyImmediate(mesh);
  }
  var root=new GameObject("Ultimate preview");
  Material mat=new Material(AssetDatabase.LoadAssetAtPath<Shader>("Assets/CrimsonModel.shader"));mat.color=new Color(.22f,.14f,.17f);
  for(int i=0;i<3;i++){var go=new GameObject("Spike"+i);go.transform.SetParent(root.transform);go.transform.position=new Vector3(0,-.55f,(i-1)*.65f);go.AddComponent<MeshFilter>().sharedMesh=Diavolo.SpikeMesh.Create(2.5f,.16f);go.AddComponent<MeshRenderer>().sharedMaterial=mat;}
  GameObject plane=GameObject.CreatePrimitive(PrimitiveType.Cube);plane.transform.localScale=new Vector3(8,.1f,8);plane.transform.position=Vector3.down*.05f;
  Camera camera=new GameObject("Preview camera").AddComponent<Camera>();camera.transform.position=new Vector3(4,3,-5);camera.transform.LookAt(new Vector3(0,.9f,0));camera.backgroundColor=new Color(.12f,.12f,.14f);camera.clearFlags=CameraClearFlags.SolidColor;
  RenderTexture rt=new RenderTexture(800,800,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;Texture2D image=new Texture2D(800,800);image.ReadPixels(new Rect(0,0,800,800),0,0);image.Apply();File.WriteAllBytes("../docs/ultimate-spikes.png",image.EncodeToPNG());
  File.WriteAllText("../docs/ultimate-validation.txt","PASS: "+checks+" mesh checks across four victim sizes. Three aligned spikes, bases buried 22 percent. Gameplay placement/network tests pending.");
 }
}
