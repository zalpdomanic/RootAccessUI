using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace RootAccess.UI.Editor
{
    public static class RootAccessAssetSetup
    {
        [MenuItem("Tools/Root Access UI/Read integration guide")]
        public static void Guide()=>AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/RootAccessUI/README-FIRST.txt"));
        [MenuItem("Tools/Root Access UI/Check URP and font prerequisites")]
        public static void Check()
        {
            var pipeline=GraphicsSettings.currentRenderPipeline;
            bool urp=pipeline!=null&&pipeline.GetType().Name.Contains("UniversalRenderPipeline");
            bool fontShader=AssetDatabase.GUIDToAssetPath("fe393ace9b354375a9cb14cdbbc28be4").Length>0;
            EditorUtility.DisplayDialog("Root Access UI prerequisites","URP: "+(urp?"Ready":"Select your project's URP asset")+"\nTMP Essential font shader: "+(fontShader?"Ready":"Import TMP Essential Resources")+"\n\nNo project settings were changed. Read README-FIRST for input and adapter setup.","OK");
        }
        [MenuItem("Tools/Root Access UI/Import TMP Essential Resources if missing")]
        public static void ImportFonts()
        {
            if(AssetDatabase.GUIDToAssetPath("fe393ace9b354375a9cb14cdbbc28be4").Length>0){EditorUtility.DisplayDialog("TextMesh Pro","The required shader is already installed. Existing TMP settings were kept.","OK");return;}
            var path=Directory.GetFiles("Library/PackageCache","TMP Essential Resources.unitypackage",SearchOption.AllDirectories).FirstOrDefault();
            if(path==null){EditorUtility.DisplayDialog("TextMesh Pro","Install Unity UI / TextMesh Pro for your Unity version, then import TMP Essential Resources from Unity's TextMesh Pro menu.","OK");return;}
            AssetDatabase.ImportPackage(path,true);
        }
    }
}
