using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace GrowingCity
{
    // Only generated road instances participate; source assets and animated buildings are untouched.
    public sealed class CityRoadBatch
    {
        readonly Transform parent;
        readonly List<Mesh> meshes=new List<Mesh>();
        GameObject root;
        public CityRoadBatch(Transform parent){this.parent=parent;}
        public void Rebuild(IEnumerable<GameObject> roads)
        {
            Dispose();
            var groups=new Dictionary<Material,List<CombineInstance>>();
            foreach(var road in roads)foreach(var filter in road.GetComponentsInChildren<MeshFilter>()){
                var renderer=filter.GetComponent<MeshRenderer>();var mesh=filter.sharedMesh;
                if(!renderer||!mesh||!mesh.isReadable)continue;
                var materials=renderer.sharedMaterials;
                // Preserve renderers with nonstandard material/submesh arrangements.
                if(materials.Length!=mesh.subMeshCount)continue;
                bool valid=true;foreach(var material in materials)if(!material)valid=false;if(!valid)continue;
                for(int i=0;i<materials.Length;i++){
                    if(!groups.TryGetValue(materials[i],out var list)){list=new List<CombineInstance>();groups.Add(materials[i],list);}
                    list.Add(new CombineInstance{mesh=mesh,subMeshIndex=i,transform=parent.worldToLocalMatrix*filter.transform.localToWorldMatrix});
                }
                renderer.enabled=false;
            }
            root=new GameObject("Batched roads — by material");root.transform.SetParent(parent,false);
            foreach(var group in groups){
                var mesh=new Mesh{name="Generated road batch",indexFormat=IndexFormat.UInt32};meshes.Add(mesh);mesh.CombineMeshes(group.Value.ToArray(),true,true);
                var part=new GameObject(group.Key.name);part.transform.SetParent(root.transform,false);part.AddComponent<MeshFilter>().sharedMesh=mesh;part.AddComponent<MeshRenderer>().sharedMaterial=group.Key;
            }
        }
        public void Dispose(){if(root){root.SetActive(false);Object.Destroy(root);}foreach(var mesh in meshes)if(mesh)Object.Destroy(mesh);meshes.Clear();}
    }
}
