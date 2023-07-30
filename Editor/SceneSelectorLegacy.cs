using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace SceneSelector.Editor
{
#if SCENE_SELECTOR_ENABLE_LEGACY
    public class SceneSelectorLegacy : EditorWindow
    {
        [SerializeField] private bool isShowAllScene = false;
        [SerializeField] private bool isUseBuildSetting = false;
        [SerializeField] private Vector2 scrollPos;
        [SerializeField] private List<SceneAsset> scenes = new List<SceneAsset>();
        [SerializeField] private SceneAsset customMainScene;
        [SerializeField] private SceneAsset targetEditorScene;

        private static string editModeScene = "";

        [MenuItem("Window/General/SceneSelector (Legacy)")]
        public static void ShowWindow()
        {
            GetWindow(typeof(SceneSelectorLegacy));
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            var shouldResetSceneInEditMode = (state == PlayModeStateChange.EnteredEditMode);

            if (shouldResetSceneInEditMode)
            {
                EditorSceneManager.playModeStartScene = (SceneAsset)AssetDatabase.LoadAssetAtPath(editModeScene, typeof(SceneAsset));
            }
        }

        private void OnEnable()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        private void OnGUI()
        {
            GUIHandler();
        }

        private void OnInspectorUpdate()
        {
            Repaint();
        }

        private void GUIHandler()
        {
            titleContent.text = "SceneSelector (Legacy)";
            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
            PlayHandler();
            EditorGUILayout.Space();
            ShowAllSceneAsset();
            EditorGUI.EndDisabledGroup();
        }

        private void PlayHandler()
        {
            GUILayout.Label("Play", EditorStyles.boldLabel);
            isUseBuildSetting = EditorGUILayout.Toggle("Use Build Setting", isUseBuildSetting);

            if (!isUseBuildSetting)
            {
                customMainScene = (SceneAsset)EditorGUILayout.ObjectField(
                        new GUIContent("Scene:"),
                        customMainScene,
                        typeof(SceneAsset),
                        false);
            }

            if (GUILayout.Button("Play", GUILayout.Height(40)))
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    if (isUseBuildSetting)
                    {
                        if (EditorBuildSettings.scenes.Length > 0)
                        {
                            var path = EditorBuildSettings.scenes[0].path;
                            var objExpectScene = (SceneAsset)AssetDatabase.LoadAssetAtPath(path, typeof(SceneAsset));

                            if (objExpectScene)
                            {
                                customMainScene = objExpectScene;
                            }
                            else
                            {
                                customMainScene = null;
                                EditorUtility.DisplayDialog("Error", "Can't load the first scene in Build Setting.", "OK");
                                isUseBuildSetting = false;
                            }
                        }
                        else
                        {
                            customMainScene = null;
                            EditorUtility.DisplayDialog("Error", "No scene in Build Setting..", "OK");
                            isUseBuildSetting = false;
                        }
                    }

                    if (customMainScene != null)
                    {
                        editModeScene = EditorSceneManager.GetActiveScene().path;
                        EditorSceneManager.playModeStartScene = customMainScene;
                        EditorApplication.isPlaying = true;
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Error", "No scene selected for playing.", "OK");
                    }
                }
            }
        }

        private void ShowAllSceneAsset()
        {
            GUILayout.Label("Editor", EditorStyles.boldLabel);
            isShowAllScene = EditorGUILayout.Foldout(isShowAllScene, "Scenes");

            if (isShowAllScene)
            {
                if (GUILayout.Button("Refresh"))
                {
                    scenes.Clear();
                    var assetsGUID = AssetDatabase.FindAssets("t:SceneAsset");

                    foreach (var id in assetsGUID)
                    {

                        var path = AssetDatabase.GUIDToAssetPath(id);
                        var asset = (SceneAsset)AssetDatabase.LoadAssetAtPath(path, typeof(SceneAsset));

                        scenes.Add(asset);
                    }
                }

                EditorGUILayout.Space();
                scrollPos = EditorGUILayout.BeginScrollView(scrollPos, false, false);

                for (int i = 0; i < scenes.Count; i++)
                {
                    scenes[i] = (SceneAsset)EditorGUILayout.ObjectField(new GUIContent(""),
                            scenes[i],
                            typeof(SceneAsset),
                            false);
                }

                EditorGUILayout.EndScrollView();
            }
        }

        /*
        [MenuItem("Edit/Play first scene %#p")]
        private static void StartPlayFirstScene()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                return;
            }

            if (EditorBuildSettings.scenes.Length <= 0)
            {
                EditorUtility.DisplayDialog("Error", "Can't load the first scene in Build Setting.", "OK");
                return;
            }

            var path = EditorBuildSettings.scenes[0].path;
            var objExpectScene = (SceneAsset)AssetDatabase.LoadAssetAtPath(path, typeof(SceneAsset));

            if (objExpectScene == null)
            {
                EditorUtility.DisplayDialog("Error", "No scene has found in Build Setting", "OK");
                return;
            }

            editModeScene = EditorSceneManager.GetActiveScene().path;
            EditorSceneManager.playModeStartScene = objExpectScene;

            EditorApplication.isPlaying = true;
        }
        */
    }
#endif
}

