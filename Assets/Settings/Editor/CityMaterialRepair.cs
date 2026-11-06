using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class CityMaterialRepair
{
    const string Root = "QA/Materials/";
    [Serializable] public class Entry { public string name, shader, path, guid; public long localId; public bool supported; public int renderers; }
    [Serializable] public class Audit { public string scene, pipeline; public int missingSlots; public Entry[] materials; }
    [MenuItem("Tools/Growing City/Materials/Audit Current Scene")]
    public static void AuditCurrentScene() => WriteAudit("current");
    static Renderer[] Renderers() => UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(r=>r.gameObject.scene.IsValid() && !EditorSceneManager.IsPreviewScene(r.gameObject.scene)).ToArray();
    static void WriteAudit(string label)
    {
        var renderers = Renderers();
        var groups = renderers.SelectMany(r=>r.sharedMaterials).Where(m=>m).GroupBy(m=>m);
        var audit = new Audit { scene=EditorSceneManager.GetActiveScene().path, pipeline=GraphicsSettings.currentRenderPipeline?.GetType().FullName,
            missingSlots=renderers.Sum(r=>r.sharedMaterials.Count(m=>!m)), materials=groups.Select(g=>{
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(g.Key,out string guid,out long localId);
                return new Entry {name=g.Key.name, shader=g.Key.shader?g.Key.shader.name:"MISSING", supported=g.Key.shader && g.Key.shader.isSupported,
                    path=AssetDatabase.GetAssetPath(g.Key),guid=guid,localId=localId,renderers=g.Count()}; }).ToArray() };
        Directory.CreateDirectory(Root); File.WriteAllText(Root+label+"-audit.json",JsonUtility.ToJson(audit,true));
    }
    static void Repair()
    {
        var active = EditorSceneManager.GetActiveScene();
        if (!active.path.StartsWith("Assets/polyperfect/Low Poly Epic City/- Demo Scenes/",StringComparison.Ordinal)) throw new InvalidOperationException("Expected vendor demo source scene.");
        if (active.isDirty) throw new InvalidOperationException("The open scene has unsaved changes; preserving it.");
        if (!File.Exists(Root+"before-audit.json")) throw new InvalidOperationException("Run audit first.");
        const string scenePath="Assets/Scenes/City - Day URP.unity";
        const string materialPath="Assets/Settings/Materials/City Default URP.mat";
        if (File.Exists(scenePath) || File.Exists(materialPath)) throw new InvalidOperationException("Output exists; refusing to overwrite.");
        Directory.CreateDirectory("Assets/Settings/Materials"); AssetDatabase.Refresh();
        if (!AssetDatabase.CopyAsset(active.path,scenePath)) throw new IOException("Scene copy failed.");
        EditorSceneManager.OpenScene(scenePath);
        var original=Renderers().SelectMany(r=>r.sharedMaterials).First(m=>m && m.shader.name=="Standard" && AssetDatabase.GetAssetPath(m)=="Resources/unity_builtin_extra");
        var replacement=new Material(Shader.Find("Universal Render Pipeline/Lit")) {name="City Default URP"};
        replacement.SetColor("_BaseColor",original.GetColor("_Color"));
        replacement.SetFloat("_Metallic",original.GetFloat("_Metallic"));
        replacement.SetFloat("_Smoothness",original.GetFloat("_Glossiness"));
        AssetDatabase.CreateAsset(replacement,materialPath);
        var changed=new List<string>();
        foreach(var renderer in Renderers())
        {
            var materials=renderer.sharedMaterials; bool edited=false;
            for(int i=0;i<materials.Length;i++) if(materials[i]==original) { materials[i]=replacement; edited=true; }
            if(!edited) continue;
            renderer.sharedMaterials=materials;
            PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            EditorUtility.SetDirty(renderer);
            changed.Add(renderer.name);
        }
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        if(!EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene())) throw new IOException("Scene save failed.");
        AssetDatabase.SaveAssets();
        File.WriteAllLines(Root+"replaced-renderers.txt",changed);
        WriteAudit("after");
        File.WriteAllText(Root+"repair-complete.txt", $"Replaced {changed.Count} renderers. Saved {scenePath}. Original scene unchanged.");
    }
    static void CreateVariants()
    {
        const string vendor="Assets/polyperfect/Low Poly Epic City/";
        var replacement=AssetDatabase.LoadAssetAtPath<Material>("Assets/Settings/Materials/City Default URP.mat");
        if(!replacement) throw new InvalidOperationException("Repair material missing");
        var results=new List<string>();
        foreach(var guid in AssetDatabase.FindAssets("t:Prefab",new[]{vendor.TrimEnd('/')}))
        {
            var path=AssetDatabase.GUIDToAssetPath(guid);
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if(!prefab || !prefab.GetComponentsInChildren<Renderer>(true).Any(r=>r.sharedMaterials.Any(IsDefault))) continue;
            var output="Assets/Prefabs/City URP/"+path.Substring(vendor.Length);
            if(File.Exists(output)) continue;
            Directory.CreateDirectory(Path.GetDirectoryName(output)); AssetDatabase.Refresh();
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab);
            try {
                foreach(var renderer in instance.GetComponentsInChildren<Renderer>(true)) {
                    var mats=renderer.sharedMaterials; bool changed=false;
                    for(int i=0;i<mats.Length;i++) if(IsDefault(mats[i])) {mats[i]=replacement;changed=true;}
                    if(changed) {renderer.sharedMaterials=mats;PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);}
                }
                var saved=PrefabUtility.SaveAsPrefabAsset(instance,output);
                if(!saved || saved.GetComponentsInChildren<Renderer>(true).Any(r=>r.sharedMaterials.Any(IsDefault))) throw new IOException("Variant verification failed: "+output);
                results.Add(path+" => "+output);
            } finally { UnityEngine.Object.DestroyImmediate(instance); }
        }
        AssetDatabase.SaveAssets(); File.WriteAllLines(Root+"variants.txt",results);
        File.WriteAllText(Root+"variants-complete.txt",$"Saved and verified {results.Count} prefab variants.");
    }
    static bool IsDefault(Material m) => m && m.shader && m.shader.name=="Standard" && AssetDatabase.GetAssetPath(m)=="Resources/unity_builtin_extra";
    static void LinkVariants()
    {
        var scene=EditorSceneManager.GetActiveScene();
        if(scene.path!="Assets/Scenes/City - Day URP.unity" || scene.isDirty) throw new InvalidOperationException("Expected saved project-owned scene");
        var map=new Dictionary<UnityEngine.Object,UnityEngine.Object>();
        foreach(var line in File.ReadAllLines(Root+"variants.txt")) {
            var parts=line.Split(new[]{" => "},StringSplitOptions.None);
            map[AssetDatabase.LoadAssetAtPath<GameObject>(parts[0])]=AssetDatabase.LoadAssetAtPath<GameObject>(parts[1]);
        }
        int count=0;
        foreach(var component in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MonoBehaviour>(true)).Where(c=>c)) {
            var serialized=new SerializedObject(component); var property=serialized.GetIterator();
            while(property.Next(true)) if(property.propertyType==SerializedPropertyType.ObjectReference && property.objectReferenceValue && map.TryGetValue(property.objectReferenceValue,out var replacement)) {property.objectReferenceValue=replacement;count++;}
            if(serialized.ApplyModifiedPropertiesWithoutUndo()) PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        }
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        File.WriteAllText(Root+"linked-spawners.txt",$"Remapped {count} prefab references to verified project-owned variants.");
    }
}
