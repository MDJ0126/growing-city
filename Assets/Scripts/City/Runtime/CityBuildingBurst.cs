using UnityEngine;
namespace GrowingCity
{
    public sealed class CityBuildingBurst : MonoBehaviour
    {
        readonly Transform[] sparks=new Transform[8];
        readonly Vector3[] directions=new Vector3[8];
        float age,size;
        Transform flash,smoke;
        public void Initialize(float size,Material fire,Material dust,bool destroyed)
        {
            this.size=size;
            flash=Part("Building flash",PrimitiveType.Sphere,fire);
            smoke=Part("Rising dust",PrimitiveType.Sphere,dust);
            for(int i=0;i<sparks.Length;i++){
                float angle=i*Mathf.PI*2/sparks.Length;directions[i]=new Vector3(Mathf.Cos(angle),.5f+(i%3)*.3f,Mathf.Sin(angle));
                sparks[i]=Part("Hot fragments",PrimitiveType.Cube,fire);
            }
            if(!destroyed)this.size*=.5f;
        }
        Transform Part(string name,PrimitiveType type,Material material){var part=GameObject.CreatePrimitive(type);part.name=name;part.transform.SetParent(transform,false);Destroy(part.GetComponent<Collider>());part.GetComponent<Renderer>().sharedMaterial=material;return part.transform;}
        void Update()
        {
            age+=Time.deltaTime;float t=Mathf.Clamp01(age/.8f);
            flash.localScale=Vector3.one*size*Mathf.Max(0,1-age/.22f);
            smoke.localPosition=Vector3.up*size*t*.8f;smoke.localScale=Vector3.one*size*Mathf.Sin(t*Mathf.PI)*.8f;
            for(int i=0;i<sparks.Length;i++){sparks[i].localPosition=directions[i]*size*t*1.2f+Vector3.down*size*t*t*.8f;sparks[i].localScale=Vector3.one*size*.09f*(1-t);sparks[i].Rotate(new Vector3(90,130,60)*Time.deltaTime);}
            if(t>=1)Destroy(gameObject);
        }
    }
}
