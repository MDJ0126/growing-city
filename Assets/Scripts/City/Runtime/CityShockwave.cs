using UnityEngine;

namespace GrowingCity
{
    public sealed class CityShockwave : MonoBehaviour
    {
        float radius,age,duration;
        Transform flash;
        Transform cloud;
        LineRenderer ring;
        readonly Vector3[] points=new Vector3[65];
        public void InitializeEvolution(float size,Material material)
        {
            radius=size;duration=.65f;
            ring=gameObject.AddComponent<LineRenderer>();ring.sharedMaterial=material;ring.useWorldSpace=false;ring.positionCount=points.Length;ring.widthMultiplier=.7f;
        }
        public void Initialize(float blastRadius,bool nuclear,Material flashMaterial,Material ringMaterial)
        {
            radius=blastRadius;duration=nuclear?1.8f:1.05f;
            var orb=GameObject.CreatePrimitive(PrimitiveType.Sphere);orb.name="Flash";orb.transform.SetParent(transform,false);Destroy(orb.GetComponent<Collider>());orb.GetComponent<Renderer>().sharedMaterial=flashMaterial;flash=orb.transform;
            ring=gameObject.AddComponent<LineRenderer>();ring.sharedMaterial=ringMaterial;ring.useWorldSpace=false;ring.positionCount=points.Length;ring.widthMultiplier=nuclear?1.5f:.7f;
            if(nuclear){var cloudRoot=new GameObject("Rising mushroom cloud");cloud=cloudRoot.transform;cloud.SetParent(transform,false);AddCloudPart(new Vector3(0,1,0),new Vector3(.35f,2,.35f),ringMaterial);AddCloudPart(new Vector3(0,2,0),new Vector3(1.8f,.7f,1.8f),flashMaterial);}
        }
        void Update()
        {
            age+=Time.deltaTime;float t=Mathf.Clamp01(age/duration);float expansion=1-Mathf.Pow(1-t,3);
            if(flash){float s=Mathf.Max(.001f,1-age/.32f)*radius*.65f;flash.localScale=Vector3.one*s;flash.localPosition=Vector3.up*(s*.2f);}
            for(int i=0;i<points.Length;i++){float angle=i/(float)(points.Length-1)*Mathf.PI*2;points[i]=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*radius*expansion;}
            ring.SetPositions(points);ring.widthMultiplier=(1-t)*1.4f;
            if(cloud){float grow=Mathf.Sin(t*Mathf.PI)*radius*.3f;cloud.localScale=Vector3.one*Mathf.Max(.001f,grow);cloud.localPosition=Vector3.up*t*radius*.2f;}
            if(t>=1)Destroy(gameObject);
        }
        void AddCloudPart(Vector3 position,Vector3 scale,Material material){var part=GameObject.CreatePrimitive(PrimitiveType.Sphere);part.transform.SetParent(cloud,false);part.transform.localPosition=position;part.transform.localScale=scale;Destroy(part.GetComponent<Collider>());part.GetComponent<Renderer>().sharedMaterial=material;}
    }
}
