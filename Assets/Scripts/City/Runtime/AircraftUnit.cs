using System.Collections.Generic;
using UnityEngine;
namespace GrowingCity
{
    public sealed class AircraftUnit
    {
        sealed class Surface {public Renderer Renderer;public int Index;public Color Color;public MaterialPropertyBlock Original,Flash;}
        readonly List<Surface> surfaces=new List<Surface>();
        public readonly GameObject Body;
        public readonly AircraftHealth Health;
        public Vector3 Position,Previous,Velocity;
        public bool Seen;
        public float CrashAge=1;
        float hitAge=1;
        public bool Targetable=>Body&&Health.Alive&&!Health.Escaped;
        public AircraftUnit(GameObject body,double health,double armor,Vector3 position)
        {
            Body=body;Health=new AircraftHealth(health,armor);Position=Previous=position;body.transform.position=position;
            foreach(var renderer in body.GetComponentsInChildren<Renderer>()){var mats=renderer.sharedMaterials;for(int i=0;i<mats.Length;i++){var original=new MaterialPropertyBlock();var flash=new MaterialPropertyBlock();renderer.GetPropertyBlock(original,i);renderer.GetPropertyBlock(flash,i);surfaces.Add(new Surface{Renderer=renderer,Index=i,Original=original,Flash=flash,Color=mats[i]&&mats[i].HasProperty("_BaseColor")?mats[i].GetColor("_BaseColor"):Color.white});}}
        }
        public void Move(Vector3 point,Quaternion rotation,float dt)
        {
            if(!Targetable)return;Previous=Position;Position=point;Velocity=dt>0?(Position-Previous)/dt:Vector3.zero;Body.transform.SetPositionAndRotation(point,rotation);
        }
        public double Hit(double damage)
        {
            double hit=Health.Hit(damage);if(hit>0){hitAge=0;if(!Health.Alive)CrashAge=0;}return hit;
        }
        public bool Arrive(){if(!Health.Arrive())return false;Body.SetActive(false);return true;}
        public void Visual(float dt)
        {
            if(!Body)return;
            if(!Health.Alive){CrashAge+=dt;Body.transform.position+=Vector3.down*dt*12;Body.transform.Rotate(new Vector3(70,30,100)*dt);Body.transform.localScale*=Mathf.Pow(.08f,dt);if(CrashAge>=.8f)Body.SetActive(false);}
            if(hitAge>=.28f)return;hitAge+=dt;float strength=1-Mathf.Clamp01(hitAge/.28f);
            if(Targetable)Body.transform.position=Position+Vector3.right*(Mathf.Sin(hitAge*100)*strength*.6f);
            foreach(var s in surfaces){if(!s.Renderer)continue;if(strength<=0)s.Renderer.SetPropertyBlock(s.Original.isEmpty?null:s.Original,s.Index);else{s.Flash.SetColor("_BaseColor",Color.Lerp(s.Color,new Color(1,.04f,.02f),strength));s.Renderer.SetPropertyBlock(s.Flash,s.Index);}}
        }
    }
}
