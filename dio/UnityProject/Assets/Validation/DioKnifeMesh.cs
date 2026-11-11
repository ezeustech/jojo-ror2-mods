using System.Collections.Generic;
using UnityEngine;
namespace EyesOfHeavenDio
{
    public static class DioKnifeMesh
    {
        public static Mesh Create()
        {
            var vertices=new List<Vector3>();var colors=new List<Color>();var triangles=new List<int>();
            void Face(Vector3 a,Vector3 b,Vector3 c,Color color)
            {int i=vertices.Count;vertices.Add(a);vertices.Add(b);vertices.Add(c);colors.Add(color);colors.Add(color);colors.Add(color);triangles.Add(i);triangles.Add(i+1);triangles.Add(i+2);}
            void Box(Vector3 center,Vector3 half,Color color)
            {
                var p=new Vector3[8];for(int i=0;i<8;i++)p[i]=center+new Vector3((i&1)==0?-half.x:half.x,(i&2)==0?-half.y:half.y,(i&4)==0?-half.z:half.z);
                int[] faces={0,2,3,0,3,1,4,5,7,4,7,6,0,4,6,0,6,2,1,3,7,1,7,5,0,1,5,0,5,4,2,6,7,2,7,3};
                for(int i=0;i<faces.Length;i+=3)Face(p[faces[i]],p[faces[i+1]],p[faces[i+2]],color);
            }
            var tip=new Vector3(0,0,.65f);var l=new Vector3(-.09f,0,.08f);var r=new Vector3(.09f,0,.08f);
            var ridge=new Vector3(0,.035f,.12f);var back=new Vector3(0,-.035f,.12f);
            Face(tip,l,ridge,new Color(.8f,.85f,.9f));Face(tip,ridge,r,Color.white);
            Face(tip,back,l,new Color(.5f,.58f,.65f));Face(tip,r,back,new Color(.7f,.75f,.8f));
            Face(l,back,ridge,Color.gray);Face(r,ridge,back,Color.gray);
            Box(new Vector3(0,0,.065f),new Vector3(.14f,.04f,.025f),new Color(.7f,.72f,.75f));
            Box(new Vector3(0,0,-.105f),new Vector3(.047f,.038f,.14f),new Color(.12f,.09f,.07f));
            for(int i=0;i<5;i++)Box(new Vector3(0,0,-.22f+i*.05f),new Vector3(.052f,.043f,.008f),new Color(.3f,.24f,.18f));
            Box(new Vector3(0,0,-.26f),new Vector3(.06f,.045f,.025f),new Color(.65f,.68f,.72f));
            var mesh=new Mesh{name="DioBeveledThrowingKnife"};mesh.SetVertices(vertices);mesh.SetColors(colors);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
    }
}
