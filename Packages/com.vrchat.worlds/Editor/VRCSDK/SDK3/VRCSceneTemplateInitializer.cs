using System.IO;
using UnityEditor;
using UnityEditor.SceneTemplate;
using UnityEngine;

/// <summary>Initialize the default VRC world scene from a template when the project is first launched</summary>
[InitializeOnLoad]
public class VRCSceneTemplateInitializer
{
    private const string SceneTemplatePath = "Packages/com.vrchat.worlds/Editor/VRCSDK/SDK3/VRCDefaultWorldScene.scenetemplate";
    private const string ScenePath = "Assets/Scenes/VRCDefaultWorldScene.unity";
    private const string HasRunStateKey = "VRCSceneTemplateInitializer_HasRun";
    
    // called on editor launch or domain reload
    static VRCSceneTemplateInitializer()
    {
        // runs a single time when the project is launched
        bool hasRun = SessionState.GetBool(HasRunStateKey, false);
        if (!hasRun)
        {
            SessionState.SetBool(HasRunStateKey, true);

            // wait a tick to ensure the editor allows scene editing
            EditorApplication.delayCall += () =>
            {
                // init default scene if there are no other scene assets
                if (AssetDatabase.FindAssets("t:Scene", new[] { "Assets" }).Length == 0)
                {
                    var template = (SceneTemplateAsset)AssetDatabase.LoadAssetAtPath(SceneTemplatePath, typeof(SceneTemplateAsset));
                    if (template != null)
                    {
                        var result = SceneTemplateService.Instantiate(template, false, ScenePath);
                        Debug.Log($"Initialized {result.sceneAsset.name}");
                    }
                    else
                    {
                        Debug.LogError($"Failed to initialize VRChat default world scene because the file does not exist or is inaccessible: {SceneTemplatePath}");
                    }
                }
            };
        }
    }
}
