using System;
using UnityEngine;
namespace GrowingCity
{
    public sealed class CityAirstrike
    {
        readonly AirstrikePlan plan;
        readonly GameObject root;
        readonly GameObject[] aircraft;
        readonly Vector3[] offsets;
        public readonly AircraftUnit[] Units;
        readonly Camera camera;
        readonly Action<AircraftUnit> arrival;
        readonly Transform[,] bombs;
        readonly Material bombMaterial;
        readonly Action<Vector3,bool> impact;
        readonly Vector3 start,end;
        readonly Vector3 entry,exit;
        readonly float altitude,cellSize;
        readonly bool nuclear;
        double age;
        int released,impacted;
        double lastDrop=-10;
        Vector3 previousCenter;
        public Bounds FlightBounds {get;}
        public CityAirstrike(AirstrikePlan plan,Transform parent,GameObject[] aircraft,Vector3[] offsets,float altitude,float cellSize,bool nuclear,Material bombMaterial,Action<Vector3,bool> impact,Vector3 entry,Vector3 exit,double health,double armor,Camera camera,Action<AircraftUnit> arrival)
        {
            if(aircraft==null||aircraft.Length==0||aircraft.Length>7||offsets==null||offsets.Length!=aircraft.Length)throw new ArgumentException("Invalid formation");
            this.plan=plan;this.aircraft=aircraft;this.altitude=altitude;this.cellSize=cellSize;this.nuclear=nuclear;this.bombMaterial=bombMaterial;this.impact=impact;
            this.offsets=offsets;root=new GameObject("V formation bombing run");root.transform.SetParent(parent,false);foreach(var member in aircraft)member.transform.SetParent(root.transform,true);
            this.camera=camera;this.arrival=arrival;Units=new AircraftUnit[aircraft.Length];
            start=new Vector3((float)plan.StartX,altitude,(float)plan.StartZ);end=new Vector3((float)plan.EndX,altitude,(float)plan.EndZ);
            this.entry=entry;this.exit=exit;previousCenter=entry;for(int i=0;i<aircraft.Length;i++){aircraft[i].transform.rotation=Quaternion.LookRotation((start-entry).normalized);Units[i]=new AircraftUnit(aircraft[i],health,armor,entry+offsets[i]);}
            bombs=new Transform[plan.BombCount,aircraft.Length];var bounds=new Bounds(start,Vector3.one*cellSize);foreach(var offset in offsets){bounds.Encapsulate(entry+offset);bounds.Encapsulate(exit+offset);bounds.Encapsulate(end+offset);}FlightBounds=bounds;
        }
        public bool Tick(float dt)
        {
            age+=Math.Max(0,dt);
            Vector3 center;
            double first=plan.AttackStart,last=plan.AttackEnd;
            if(age<first)center=Vector3.Lerp(entry,start,(float)(age/first));
            else if(age<=last)center=Vector3.Lerp(start,end,(float)((age-first)/(last-first)));
            else center=Vector3.Lerp(end,exit,Mathf.Clamp01((float)((age-last)/(plan.Duration-last))));
            var velocity=center-previousCenter;previousCenter=center;
            var rotation=velocity.sqrMagnitude>.0001f?Quaternion.LookRotation(velocity.normalized):Quaternion.LookRotation((end-start).normalized);
            for(int i=0;i<aircraft.Length;i++){Units[i].Move(center+offsets[i],rotation,dt);Units[i].Visual(dt);}
            while(released<plan.BombCount&&age>=plan.ReleaseAt(released)){
                for(int member=0;member<aircraft.Length;member++){
                if(!Units[member].Targetable)continue;
                var bomb=GameObject.CreatePrimitive(PrimitiveType.Capsule);bomb.name=nuclear?"Falling nuclear bomb":"Falling bomb";bomb.transform.SetParent(root.transform,false);UnityEngine.Object.Destroy(bomb.GetComponent<Collider>());
                bomb.GetComponent<Renderer>().sharedMaterial=bombMaterial;bomb.transform.localScale=new Vector3(cellSize*.055f,cellSize*.12f,cellSize*.055f);bombs[released,member]=bomb.transform;
                lastDrop=age;}released++;
            }
            for(int i=impacted;i<released;i++)for(int member=0;member<aircraft.Length;member++)if(bombs[i,member]){
                float fall=Mathf.Clamp01((float)((age-plan.ReleaseAt(i))/plan.FallTime));bombs[i,member].position=new Vector3((float)plan.TargetX(i),altitude*(1-fall*fall),(float)plan.TargetZ(i))+offsets[member];
            }
            while(impacted<plan.BombCount&&age>=plan.ImpactAt(impacted)){
                int i=impacted++;for(int member=0;member<aircraft.Length;member++){if(bombs[i,member]){UnityEngine.Object.Destroy(bombs[i,member].gameObject);bombs[i,member]=null;
                impact(new Vector3((float)plan.TargetX(i),0,(float)plan.TargetZ(i))+offsets[member],nuclear);}}
            }
            bool finished=true;foreach(var unit in Units){if(unit.Targetable){var viewport=camera.WorldToViewportPoint(unit.Position);bool visible=viewport.z>0&&viewport.x>=0&&viewport.x<=1&&viewport.y>=0&&viewport.y<=1;unit.Seen|=visible;if(age>plan.AttackEnd&&((unit.Seen&&!visible)||age>=plan.Duration)){if(unit.Arrive())arrival(unit);}}if(unit.Targetable||(!unit.Health.Alive&&unit.CrashAge<.8f))finished=false;}
            if(finished&&age>=lastDrop+plan.FallTime+.1){Dispose();return true;}
            if(!plan.Complete(age)||!finished)return false;Dispose();return true;
        }
        public void Dispose(){if(root)UnityEngine.Object.Destroy(root);}
    }
}
