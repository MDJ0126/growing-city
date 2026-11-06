using System;
using System.Collections.Generic;
using UnityEngine;
namespace GrowingCity
{
    public sealed class CityDefense
    {
        sealed class Shot {public DefenseMissile Model;public AircraftUnit Target;public GameObject Root;public LineRenderer Trail;}
        readonly Transform parent;readonly float cell;readonly Material bodyMaterial,trailMaterial;
        readonly Func<IEnumerable<Lot>> buildings;readonly Func<Lot,Vector3> roof;readonly Func<IEnumerable<AircraftUnit>> aircraft;
        readonly Action<Vector3,float> explosion;readonly Action<AircraftUnit,bool> hit;
        readonly List<Shot> shots=new List<Shot>();readonly List<AircraftUnit> targets=new List<AircraftUnit>();readonly List<Lot> lots=new List<Lot>();
        readonly Dictionary<int,double> nextFire=new Dictionary<int,double>();
        double scanAt,cleanAt;int cursor;
        public CityDefense(Transform parent,float cell,Material bodyMaterial,Material trailMaterial,Func<IEnumerable<Lot>> buildings,Func<Lot,Vector3> roof,Func<IEnumerable<AircraftUnit>> aircraft,Action<Vector3,float> explosion,Action<AircraftUnit,bool> hit){this.parent=parent;this.cell=cell;this.bodyMaterial=bodyMaterial;this.trailMaterial=trailMaterial;this.buildings=buildings;this.roof=roof;this.aircraft=aircraft;this.explosion=explosion;this.hit=hit;}
        static AirVector A(Vector3 v)=>new AirVector(v.x,v.y,v.z);
        static Vector3 V(AirVector v)=>new Vector3((float)v.X,(float)v.Y,(float)v.Z);
        Vector3 Aim(AircraftUnit target,Vector3 missilePosition,double missileSpeed){float lead=Mathf.Clamp(Vector3.Distance(target.Position,missilePosition)/(float)Math.Max(.01,missileSpeed),.15f,.65f);return target.Position+Vector3.ClampMagnitude(target.Velocity,cell*12)*lead;}
        public void Tick(float dt,double now)
        {
            if(dt<=0)return;targets.Clear();foreach(var unit in aircraft())if(unit.Targetable)targets.Add(unit);
            for(int i=shots.Count-1;i>=0;i--){var shot=shots[i];bool boom=shot.Model.Tick(dt,shot.Target.Targetable?A(Aim(shot.Target,V(shot.Model.Position),shot.Model.Speed)):shot.Model.Goal,shot.Target.Targetable);float collisionTime=1;
                foreach(var unit in targets){var from=V(shot.Model.Previous)-unit.Previous;var to=V(shot.Model.Position)-unit.Position;var delta=to-from;float t=delta.sqrMagnitude<.0001f?0:Mathf.Clamp01(-Vector3.Dot(from,delta)/delta.sqrMagnitude);if((from+delta*t).magnitude<cell*.12f){boom=true;collisionTime=Mathf.Min(collisionTime,t);}}
                shot.Root.transform.position=V(shot.Model.Position);shot.Root.transform.rotation=Quaternion.LookRotation(V(shot.Model.Direction));shot.Trail.SetPosition(0,V(shot.Model.Position));shot.Trail.SetPosition(1,V(shot.Model.Position-shot.Model.Direction*(cell*.5)));
                if(!boom)continue;var point=Vector3.Lerp(V(shot.Model.Previous),V(shot.Model.Position),collisionTime);explosion(point,(float)shot.Model.Radius);
                foreach(var unit in targets)if(unit.Targetable&&Vector3.Distance(Vector3.Lerp(unit.Previous,unit.Position,collisionTime),point)<=shot.Model.Radius+cell*.2){bool alive=unit.Health.Alive;if(unit.Hit(shot.Model.Damage)>0)hit(unit,alive&&!unit.Health.Alive);}
                UnityEngine.Object.Destroy(shot.Root);shots.RemoveAt(i);
            }
            if(now<scanAt||targets.Count==0||shots.Count>=24)return;scanAt=now+.25;lots.Clear();foreach(var lot in buildings())if(CityProgression.CanLaunchMissiles(lot))lots.Add(lot);if(lots.Count==0)return;
            if(now>=cleanAt){cleanAt=now+2;var live=new HashSet<int>();foreach(var lot in lots)live.Add(lot.Generation);var old=new List<int>();foreach(var id in nextFire.Keys)if(!live.Contains(id))old.Add(id);foreach(var id in old)nextFire.Remove(id);}
            int launched=0,visited=0;for(int n=0;n<lots.Count&&launched<3&&shots.Count<24;n++){int index=(cursor+n)%lots.Count;visited++;var lot=lots[index];if(!nextFire.TryGetValue(lot.Generation,out double ready)){nextFire[lot.Generation]=now+.2+(lot.Generation%9)*.11;continue;}if(now<ready)continue;
                var position=roof(lot);AircraftUnit chosen=null;float nearest=float.MaxValue;foreach(var unit in targets){float distance=Vector3.Distance(unit.Position,position);if(distance<nearest&&distance<cell*(6+lot.Tier)){nearest=distance;chosen=unit;}}
                if(chosen==null)continue;var speed=cell*(2+.5*(lot.Tier+1));var model=new DefenseMissile(lot.Tier,cell,A(position),A(chosen.Position+Vector3.ClampMagnitude(chosen.Velocity,cell*12)*Mathf.Clamp(nearest/(float)speed,.15f,.65f)));
                var root=new GameObject("Level "+model.Level+" limited tracking missile");root.transform.SetParent(parent,false);root.transform.position=position;var body=GameObject.CreatePrimitive(PrimitiveType.Capsule);body.transform.SetParent(root.transform,false);body.transform.localRotation=Quaternion.Euler(90,0,0);body.transform.localScale=new Vector3(cell*.055f,cell*.16f,cell*.055f);UnityEngine.Object.Destroy(body.GetComponent<Collider>());body.GetComponent<Renderer>().sharedMaterial=bodyMaterial;
                var trail=root.AddComponent<LineRenderer>();trail.sharedMaterial=trailMaterial;trail.positionCount=2;trail.useWorldSpace=true;trail.widthMultiplier=cell*.06f;shots.Add(new Shot{Model=model,Target=chosen,Root=root,Trail=trail});nextFire[lot.Generation]=now+3.8/(1+lot.Tier*.2);launched++;
            }cursor=(cursor+visited)%lots.Count;
        }
        public void Dispose(){foreach(var shot in shots)if(shot.Root)UnityEngine.Object.Destroy(shot.Root);shots.Clear();nextFire.Clear();}
    }
}
