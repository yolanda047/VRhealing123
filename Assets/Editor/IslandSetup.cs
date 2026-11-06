using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.IO;

[InitializeOnLoad]
public static class IslandSetup {
    static IslandSetup(){EditorApplication.delayCall+=EnsureScene;}
    [MenuItem("Island/生成并打开游戏场景")]
    public static void EnsureScene(){
        const string path="Assets/Scenes/Island.unity";
        if(File.Exists(path))return;
        Directory.CreateDirectory("Assets/Scenes");
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        new GameObject("Island Game").AddComponent<IslandGame>();
        EditorSceneManager.SaveScene(scene,path);
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(path,true)};
        AssetDatabase.SaveAssets();
    }
}
public class IslandTextureImporter:AssetPostprocessor {
    void OnPreprocessTexture(){
        if(!assetPath.StartsWith("Assets/Resources/Island/"))return;
        var t=(TextureImporter)assetImporter;t.isReadable=true;t.filterMode=FilterMode.Point;
        t.mipmapEnabled=false;t.textureCompression=TextureImporterCompression.Uncompressed;
        t.npotScale=TextureImporterNPOTScale.None;t.maxTextureSize=2048;
    }
}
