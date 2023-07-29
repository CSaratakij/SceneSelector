using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SceneSelector.Editor
{
    [CreateAssetMenu(fileName = "CustomSceneList", menuName = "SceneSelector/CustomSceneList", order = 1)]
    public class CustomSceneListScriptableObject : ScriptableObject
    {
        [Serializable]
        public class Setting
        {
            public bool enabled;
            public SceneAsset scene;

            public Setting(bool enabled, SceneAsset sceneAsset)
            {
                this.enabled = enabled;
                this.scene = sceneAsset;
            }
        }

        public Setting[] settings;

        public void ApplyToBuildSetting()
        {
            bool isConfirm = EditorUtility.DisplayDialog("Warning", "Are you sure to apply custom scene list to BuildSetting?", "Apply", "Cancel");

            if (!isConfirm)
            {
                return;
            }

            bool isNonValidSceneFound = settings.Any(x => (x.scene == null));

            if (isNonValidSceneFound)
            {
                EditorUtility.DisplayDialog("Error", "Cannot include empty scene from custom scene list to BuildSetting, please remove empty scene element to proceed.", "OK");
                return;
            }

            var editorBuildSettingsScenes = settings.Select(x =>
            {
                string scenePath = AssetDatabase.GetAssetPath(x.scene);
                return new EditorBuildSettingsScene(scenePath, x.enabled);
            });

            bool isDuplicateSceneFound = (editorBuildSettingsScenes.Select(x => x.guid).Distinct().Count()) != editorBuildSettingsScenes.Count();

            if (isDuplicateSceneFound)
            {
                EditorUtility.DisplayDialog("Error", "Duplicate scene in settings found, please remove any duplicate scene to proceed.", "OK");
                return;
            }

            EditorBuildSettings.scenes = editorBuildSettingsScenes.ToArray();
            EditorUtility.DisplayDialog("Success", "Scene list in BuildSetting has changed", "OK");
        }

        public void ImportFromBuildSetting()
        {
            bool isConfirm = EditorUtility.DisplayDialog("Warning", "Are you sure to import scene list from BuildSetting? \nThis will replace your existing custom scene list setting.", "Import", "Cancel");

            if (!isConfirm)
            {
                return;
            }

            var newSetting = EditorBuildSettings.scenes.Where(x => IsSceneAssetExist(x.path))
            .Select(x =>
            {
                var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(x.path);
                bool enabled = x.enabled;
                return new Setting(enabled, sceneAsset);
            });

            settings = newSetting.ToArray();
        }

        private bool IsSceneAssetExist(string assetPath)
        {
#if UNITY_2021_1_OR_NEWER
            return !string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(assetPath, AssetPathToGUIDOptions.OnlyExistingAssets));
#else
            var asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(assetPath);
            return (asset != null);
#endif
        }
    }
}

