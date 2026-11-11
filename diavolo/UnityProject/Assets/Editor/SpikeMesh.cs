using UnityEngine;
namespace Diavolo { public static class SpikeMesh {
        public static Mesh Create(float height,float radius)
        {
            const int sides=10;
            Vector3[] vertices=new Vector3[sides*2+2];int[] triangles=new int[sides*12];
            for(int i=0;i<sides;i++) {
                float angle=i*Mathf.PI*2/sides;Vector3 edge=new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius);
                vertices[i]=edge;vertices[i+sides]=edge*.8f+Vector3.up*(height*.72f);
            }
            int tip=sides*2,bottom=tip+1;vertices[tip]=Vector3.up*height;vertices[bottom]=Vector3.zero;
            int n=0;
            for(int i=0;i<sides;i++) {
                int j=(i+1)%sides;
                triangles[n++]=i;triangles[n++]=i+sides;triangles[n++]=j;
                triangles[n++]=j;triangles[n++]=i+sides;triangles[n++]=j+sides;
                triangles[n++]=i+sides;triangles[n++]=tip;triangles[n++]=j+sides;
                triangles[n++]=i;triangles[n++]=j;triangles[n++]=bottom;
            }
            Mesh mesh=new Mesh {name="Grounded impalement spike"};mesh.vertices=vertices;mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
} }
