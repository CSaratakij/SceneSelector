using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace SceneSelector.Editor
{
    [CustomEditor(typeof(CustomSceneListScriptableObject))]
    public class CustomSceneListScriptableObjectEditorWindow : UnityEditor.Editor
    {
        private bool isShowHelper = false;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.Space();
            DrawHelperTools();
        }

        private void DrawHelperTools()
        {
            isShowHelper = EditorGUILayout.Foldout(isShowHelper, "Tools");

            if (!isShowHelper)
            {
                return;
            }

            if (GUILayout.Button("Import from Build Settings", GUILayout.Height(30.0f)))
            {
                ImportFromBuildSettingHandler();
            }

            if (GUILayout.Button("Appy to Build Settings", GUILayout.Height(30.0f)))
            {
                ApplyToBuildSettingHandler();
            }

            if (GUILayout.Button("Clean Up", GUILayout.Height(30.0f)))
            {
                CleanUpHandler();
            }
        }

        private void ImportFromBuildSettingHandler()
        {
            bool isConfirm = EditorUtility.DisplayDialog("Warning", "Are you sure to import scene list from BuildSetting? \nThis will replace your existing custom scene list setting.", "Import", "Cancel");

            if (!isConfirm)
            {
                return;
            }

            var scriptableObject = (CustomSceneListScriptableObject)serializedObject.targetObject;

            if (scriptableObject)
            {
                scriptableObject.ImportFromBuildSetting();
            }
        }

        private void ApplyToBuildSettingHandler()
        {
            bool isConfirm = EditorUtility.DisplayDialog("Warning", "Are you sure to apply custom scene list to BuildSetting?", "Apply", "Cancel");

            if (!isConfirm)
            {
                return;
            }

            try
            {
                var scriptableObject = (CustomSceneListScriptableObject)serializedObject.targetObject;
                scriptableObject.ApplyToBuildSetting();

                EditorUtility.DisplayDialog("Success", "Scene list in BuildSetting has changed", "OK");
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("Error", e.Message, "OK");
                Debug.LogException(e);
            }
        }

        private void CleanUpHandler()
        {
            bool isConfirm = EditorUtility.DisplayDialog("Warning", "This will remove duplicate and empty scenes from the scene list.\nYou might need to re-order scene index", "CleanUp", "Cancel");

            if (!isConfirm)
            {
                return;
            }

            var scriptableObject = (CustomSceneListScriptableObject)serializedObject.targetObject;

            if (scriptableObject)
            {
                scriptableObject.CleanUp();
            }
        }
    }
}
