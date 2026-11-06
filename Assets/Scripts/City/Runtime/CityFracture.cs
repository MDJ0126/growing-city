using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace GrowingCity
{
    // Shares read-only source geometry. Project shader clips it into moving sections;
    // no imported mesh data, prefab or material is mutated, and no readable-mesh import is needed.
    public static class CityFracture
    {
        public static int Emit(Transform model,Transform parent,Vector3 blast,float cell,int seed,int bands,Material material,Material trailMaterial,Camera camera,Action<GameObject> register)
        {
            if(!model||!material)return 0;
            var sources=new List<MeshFilter>();foreach(var filter in model.GetComponentsInChildren<MeshFilter>())if(filter.sharedMesh&&filter.GetComponent<MeshRenderer>())sources.Add(filter);
            if(sources.Count==0)return 0;if(sources.Count>8)sources.RemoveRange(8,sources.Count-8);
            Bounds bounds=default;bool first=true;foreach(var source in sources){var frame=model.worldToLocalMatrix*source.transform.localToWorldMatrix;foreach(var p in Corners(source.sharedMesh.bounds)){var point=frame.MultiplyPoint3x4(p);if(first){bounds=new Bounds(point,Vector3.zero);first=false;}else bounds.Encapsulate(point);}}
            int count=0;bands=Mathf.Clamp(bands,2,3);
            for(int row=0;row<bands;row++)for(int side=0;side<2;side++){
                var min=bounds.min;var max=bounds.max;min.y=Mathf.Lerp(bounds.min.y,bounds.max.y,row/(float)bands);max.y=Mathf.Lerp(bounds.min.y,bounds.max.y,(row+1)/(float)bands);min.x=Mathf.Lerp(bounds.min.x,bounds.max.x,side*.5f);max.x=Mathf.Lerp(bounds.min.x,bounds.max.x,(side+1)*.5f);
                var local=new Bounds((min+max)*.5f,max-min);Bounds world=default;bool start=true;foreach(var p in Corners(local)){var point=model.TransformPoint(p);if(start){world=new Bounds(point,Vector3.zero);start=false;}else world.Encapsulate(point);}
                var chunk=new GameObject("Building structural section "+row+"/"+side);chunk.transform.SetParent(parent,false);chunk.transform.position=world.center;
                foreach(var source in sources){var src=source.GetComponent<MeshRenderer>();var part=new GameObject("Original facade section");part.transform.SetParent(chunk.transform,false);part.transform.position=source.transform.position;part.transform.rotation=source.transform.rotation;part.transform.localScale=source.transform.lossyScale;part.AddComponent<MeshFilter>().sharedMesh=source.sharedMesh;var renderer=part.AddComponent<MeshRenderer>();var originals=src.sharedMaterials;var mats=new Material[originals.Length];for(int i=0;i<mats.Length;i++)mats[i]=material;renderer.sharedMaterials=mats;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
                    for(int i=0;i<mats.Length;i++){var block=new MaterialPropertyBlock();block.SetVector("_ChunkMin",min-Vector3.one*.001f);block.SetVector("_ChunkMax",max+Vector3.one*.001f);block.SetMatrix("_ChunkToFrame",model.worldToLocalMatrix*source.transform.localToWorldMatrix);var original=originals[i];block.SetColor("_BaseColor",original&&original.HasProperty("_BaseColor")?original.GetColor("_BaseColor"):Color.white);if(original&&original.HasProperty("_BaseMap")){block.SetTexture("_BaseMap",original.GetTexture("_BaseMap")?original.GetTexture("_BaseMap"):Texture2D.whiteTexture);var scale=original.GetTextureScale("_BaseMap");var offset=original.GetTextureOffset("_BaseMap");block.SetVector("_BaseMap_ST",new Vector4(scale.x,scale.y,offset.x,offset.y));}renderer.SetPropertyBlock(block,i);}
                }
                var collider=chunk.AddComponent<BoxCollider>();collider.size=Vector3.Max(world.size*.78f,Vector3.one*.15f);var body=chunk.AddComponent<Rigidbody>();body.mass=2;body.linearDamping=.08f;body.angularDamping=.12f;body.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;
                var outward=world.center-blast;outward.y=0;if(outward.sqrMagnitude<.1f)outward=Vector3.right;float angle=(seed%71+count*53)*Mathf.Deg2Rad;outward=(outward.normalized+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*.65f).normalized;
                var velocity=outward*cell*(float)DestructionMotion.HorizontalSpeed(count,seed)+Vector3.up*cell*(float)DestructionMotion.UpSpeed(count,seed);
                if(count==bands*2-1&&camera){var viewport=camera.WorldToViewportPoint(world.center);if(viewport.z>0){var edge=camera.ViewportToWorldPoint(new Vector3(seed%2==0?1.25f:-.25f,viewport.y,viewport.z));var across=edge-world.center;across.y=0;velocity=Vector3.ClampMagnitude(across/2.4f,cell*12)+Vector3.up*cell*2.8f;}}
                body.AddForce(velocity,ForceMode.VelocityChange);body.AddTorque(new Vector3(3+side*4,5+row*3,7-side*3),ForceMode.VelocityChange);
                if(count%3==0&&trailMaterial){var trail=chunk.AddComponent<TrailRenderer>();trail.sharedMaterial=trailMaterial;trail.time=.35f;trail.minVertexDistance=.4f;trail.widthMultiplier=.18f;}
                register(chunk);UnityEngine.Object.Destroy(chunk,4.2f+(count%3)*.35f);count++;
            }
            return count;
        }
        static IEnumerable<Vector3> Corners(Bounds b){for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)yield return b.center+Vector3.Scale(b.extents,new Vector3(x,y,z));}
    }
}
