using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace SceneSelector.Editor
{
    public class SceneSelector : EditorWindow
    {
        [SerializeField]
        bool isShowAllScene = false;

        [SerializeField]
        bool isUseBuildSetting = false;

        [SerializeField]
        Vector2 scrollPos;

        [SerializeField]
        List<SceneAsset> scenes = new List<SceneAsset>();

        [SerializeField]
        SceneAsset customMainScene;

        [SerializeField]
        SceneAsset targetEditorScene;

        static string editModeScene;

        [MenuItem("Window/General/SceneSelector")]
        public static void ShowWindow()
        {
            EditorWindow.GetWindow(typeof(SceneSelector));
        }

        void OnEnable()
        {
            EditorApplication.playModeStateChanged += _OnPlayModeStateChanged;
        }

        void OnGUI()
        {
            _GUIHandler();
        }

        void OnInspectorUpdate()
        {
            Repaint();
        }

        void _GUIHandler()
        {
            titleContent.text = "SceneSelector";
            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
            _PlayHandler();
            EditorGUILayout.Space();
            _ShowAllSceneAsset();
            EditorGUI.EndDisabledGroup();
        }

        static void _OnPlayModeStateChanged(PlayModeStateChange state)
        {
            var shouldResetSceneInEditMode = (state == PlayModeStateChange.EnteredEditMode);

            if (shouldResetSceneInEditMode) {
                EditorSceneManager.playModeStartScene = (SceneAsset)AssetDatabase.LoadAssetAtPath(editModeScene, typeof(SceneAsset));
            }
        }

        [MenuItem("Edit/Play first scene %#p")]
        static void StartPlayFirstScene()
        {
            if (EditorApplication.isPlaying) {
                EditorApplication.isPlaying = false;
                return;
            }

            if (EditorBuildSettings.scenes.Length <= 0) {
                EditorUtility.DisplayDialog("Error", "Can't load the first scene in Build Setting.", "OK");
                return;
            }

            var path = EditorBuildSettings.scenes[0].path;
            var objExpectScene = (SceneAsset)AssetDatabase.LoadAssetAtPath(path, typeof(SceneAsset));

            if (objExpectScene == null) {
                EditorUtility.DisplayDialog("Error", "No scene has found in Build Setting", "OK");
                return;
            }

            editModeScene = EditorSceneManager.GetActiveScene().path;
            EditorSceneManager.playModeStartScene = objExpectScene;

            EditorApplication.isPlaying = true;
        }

        void _PlayHandler()
        {
            GUILayout.Label ("Play", EditorStyles.boldLabel);
            isUseBuildSetting = EditorGUILayout.Toggle("Use Build Setting", isUseBuildSetting);

            if (!isUseBuildSetting) {
                customMainScene = (SceneAsset)EditorGUILayout.ObjectField(
                        new GUIContent("Scene:"),
                        customMainScene,
                        typeof(SceneAsset),
                        false);
            }

            if (GUILayout.Button("Play", GUILayout.Height(40)))
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode) {

                    if (isUseBuildSetting) {

                        if (EditorBuildSettings.scenes.Length > 0) {

                            var path = EditorBuildSettings.scenes[0].path;
                            var objExpectScene = (SceneAsset)AssetDatabase.LoadAssetAtPath(path, typeof(SceneAsset));

                            if (objExpectScene) {
                                customMainScene = objExpectScene;
                            }
                            else {
                                customMainScene = null;
                                EditorUtility.DisplayDialog("Error", "Can't load the first scene in Build Setting.", "OK");
                                isUseBuildSetting = false;
                            }

                        }
                        else {
                            customMainScene = null;
                            EditorUtility.DisplayDialog("Error", "No scene in Build Setting..", "OK");
                            isUseBuildSetting = false;
                        }
                    }

                    if (customMainScene != null) {
                        editModeScene = EditorSceneManager.GetActiveScene().path;
                        EditorSceneManager.playModeStartScene = customMainScene;
                        EditorApplication.isPlaying = true;
                    }
                    else {
                        EditorUtility.DisplayDialog("Error", "No scene selected for playing.", "OK");
                    }
                }
            }
        }

        void _ShowAllSceneAsset()
        {
            GUILayout.Label("Editor", EditorStyles.boldLabel);
            isShowAllScene = EditorGUILayout.Foldout(isShowAllScene, "Scenes");

            if (isShowAllScene) {

                if (GUILayout.Button("Refresh")) {

                    scenes.Clear();
                    var assetsGUID = AssetDatabase.FindAssets("t:SceneAsset");

                    foreach (var id in assetsGUID) {

                        var path = AssetDatabase.GUIDToAssetPath(id);
                        var asset = (SceneAsset)AssetDatabase.LoadAssetAtPath(path, typeof(SceneAsset));

                        scenes.Add(asset);
                    }
                }

                EditorGUILayout.Space();

                scrollPos = EditorGUILayout.BeginScrollView(scrollPos, false, false);

                for (int i = 0; i < scenes.Count; i++) {
                    scenes[i] = (SceneAsset)EditorGUILayout.ObjectField(new GUIContent(""),
                            scenes[i],
                            typeof(SceneAsset),
                            false);
                }

                EditorGUILayout.EndScrollView();
            }
        }
    }

}
