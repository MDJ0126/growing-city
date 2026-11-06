using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GrowingCity.Editor
{
    public static class CityPrototypeBuild
    {
        public const string ScenePath="Assets/Scenes/City Prototype.unity";
        [MenuItem("Tools/Growing City/Open First Prototype")]
        public static void OpenPrototype()
        {
            if(EditorSceneManager.GetActiveScene().isDirty) {
                Debug.LogWarning("The current scene has unsaved changes. Preserve those changes before opening the prototype.");return;
            }
            EditorSceneManager.OpenScene(ScenePath);
        }
        [MenuItem("Tools/Growing City/Build Web Prototype")]
        public static void BuildWeb()
        {
            if(!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL,BuildTarget.WebGL))throw new InvalidOperationException("Install the matching Unity Web Build Support module.");
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play before building.");
            var oldCompression=PlayerSettings.WebGL.compressionFormat;
            var oldBackground=PlayerSettings.runInBackground;
            var oldFallback=PlayerSettings.WebGL.decompressionFallback;
            try {
                // GitHub Pages cannot set Content-Encoding for Unity build resources.
                PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Gzip;
                PlayerSettings.WebGL.decompressionFallback=true;
                PlayerSettings.runInBackground=false;
                Directory.CreateDirectory("Builds/Web");
                var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {scenes=new[]{ScenePath},locationPathName="Builds/Web",target=BuildTarget.WebGL,options=BuildOptions.None});
                if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Web build failed: "+report.summary.result);
                Debug.Log("Web prototype built at Builds/Web. Serve this folder over HTTP; opening index.html as a file is not a web execution test.");
            } finally {PlayerSettings.WebGL.compressionFormat=oldCompression;PlayerSettings.WebGL.decompressionFallback=oldFallback;PlayerSettings.runInBackground=oldBackground;}
        }
    }
}
