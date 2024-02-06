using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

namespace Happyrobot33.VRCSDKDocumentationProject
{
    class SettingsObject : ScriptableObject
    {
        public const string k_MyCustomSettingsPath = "Assets/VRCSDKDocumentationProject/Settings.asset";

//disable warning for unused fields
#pragma warning disable 0414
        [SerializeField]
        private bool m_minifyDocumentation = true;
        [SerializeField]
        private bool m_generateDocumentation = true;
        [SerializeField]
        private bool m_embedParamsInSummary = true;
        [SerializeField]
        private bool m_warnOnCannys = true;
#pragma warning restore 0414

        internal static SettingsObject GetOrCreateSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<SettingsObject>(k_MyCustomSettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<SettingsObject>();
                //make sure folder exists
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(k_MyCustomSettingsPath));
                AssetDatabase.CreateAsset(settings, k_MyCustomSettingsPath);
                AssetDatabase.SaveAssets();
            }
            return settings;
        }

        internal static SerializedObject GetSerializedSettings()
        {
            return new SerializedObject(GetOrCreateSettings());
        }
    }

    // Register a SettingsProvider using IMGUI for the drawing framework:
    static class SettingsIMGUI
    {
        [SettingsProvider]
        public static SettingsProvider SettingsProviderCreation()
        {
            // First parameter is the path in the Settings window.
            // Second parameter is the scope of this setting: it only appears in the Project Settings window.
            var provider = new SettingsProvider("Project/SettingsIMGUI", SettingsScope.Project)
            {
                // By default the last token of the path is used as display name if no label is provided.
                label = "VRC SDK Documentation Project",
                // Create the SettingsProvider and initialize its drawing (IMGUI) function in place:
                guiHandler = (searchContext) =>
                {
                    var settings = SettingsObject.GetSerializedSettings();
                    EditorGUILayout.PropertyField(settings.FindProperty("m_generateDocumentation"), new GUIContent("Generate XML Documentation"));
                    //disable gui interaction
                    GUI.enabled = settings.FindProperty("m_generateDocumentation").boolValue;
                    EditorGUILayout.PropertyField(settings.FindProperty("m_minifyDocumentation"), new GUIContent("Minify XML Documentation"));
                    EditorGUILayout.PropertyField(settings.FindProperty("m_embedParamsInSummary"), new GUIContent("Embed Params in Summary"));
                    EditorGUILayout.PropertyField(settings.FindProperty("m_warnOnCannys"), new GUIContent("Warn on Canny Comments in summary"));
                    GUI.enabled = true;
                    settings.ApplyModifiedPropertiesWithoutUndo();
                },

                // Populate the search keywords to enable smart search filtering and label highlighting:
                keywords = new HashSet<string>(new[] { "Documentation", "VRC", "SDK" })
            };

            return provider;
        }
    }
}
