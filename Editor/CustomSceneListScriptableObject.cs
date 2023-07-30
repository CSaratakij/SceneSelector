using System;
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

        internal void ApplyToBuildSetting()
        {
            if (settings == null)
            {
                return;
            }

            bool isNonValidSceneFound = settings.Any(x => (x.scene == null));

            if (isNonValidSceneFound)
            {
                throw new InvalidOperationException("Cannot include empty scene from custom scene list to BuildSetting, please remove empty scene element to proceed.");
            }

            var editorBuildSettingsScenes = settings.Select(x =>
            {
                string scenePath = AssetDatabase.GetAssetPath(x.scene);
                return new EditorBuildSettingsScene(scenePath, x.enabled);
            });

            bool isDuplicateSceneFound = (editorBuildSettingsScenes.Select(x => x.guid).Distinct().Count()) != editorBuildSettingsScenes.Count();

            if (isDuplicateSceneFound)
            {
                throw new InvalidOperationException("Duplicate scene in settings found, please remove any duplicate scene to proceed.");
            }

            EditorBuildSettings.scenes = editorBuildSettingsScenes.ToArray();
        }

        internal void ImportFromBuildSetting()
        {
            var newSetting = EditorBuildSettings.scenes.Where(x => IsSceneAssetExist(x.path))
            .Select(x =>
            {
                var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(x.path);
                bool enabled = x.enabled;
                return new Setting(enabled, sceneAsset);
            });

            settings = newSetting.ToArray();
        }

        internal void CleanUp()
        {
            if (settings == null)
            {
                return;
            }

            var validSettings = settings.Where(x => (x.scene != null))
                                    .GroupBy(x => x.scene)
                                    .Select(x => x.First());

            settings = validSettings.ToArray();
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

