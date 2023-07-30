using System;
using System.Collections;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SceneSelector.Editor
{
    public class SceneSelector : EditorWindow
    {
        internal const string KEY_BUILD_SETTING_DIRTY = "KEY_SCENE_SELECTOR_BUILD_SETTING_DIRTY";
        internal const string KEY_BUILD_SETTING_LAST_STATE = "KEY_SCENE_SELECTOR_BUILD_SETTING_LAST_STATE";

        public enum PlayMode
        {
            CustomFirstScene,
            CustomSceneList,
            UseBuildSetting
        }

        [Serializable]
        public struct BuildSettingSceneList
        {
            public BuildSettingScene[] settings;

            public BuildSettingSceneList(BuildSettingScene[] settings)
            {
                this.settings = settings;
            }
        }

        [Serializable]
        public struct BuildSettingScene
        {
            public bool enabled;
            public string sceneAssetPath;

            public BuildSettingScene(bool enabled, string sceneAssetPath)
            {
                this.enabled = enabled;
                this.sceneAssetPath = sceneAssetPath;
            }
        }

        private static SceneSelector Instance = default;

        [SerializeField] private PlayMode m_CurrentPlayMode;
        [SerializeField] private SceneAsset m_CustomFirstScene = default;
        [SerializeField] private CustomSceneListScriptableObject m_CurrentSceneList = default;
        [SerializeField] private VisualTreeAsset m_VisualTreeAsset = default;

        private Button buttonPlay;
        private Button buttonApplySceneListToBuildSetting;
        private EnumField enumPlayMode;
        private ObjectField objectFieldCustomFirstScene;
        private ObjectField objectFieldCustomSceneList;

        [MenuItem("Window/General/SceneSelector")]
        public static void ShowWindow()
        {
            var window = GetWindow<SceneSelector>();
            window.titleContent = new GUIContent("SceneSelector");
        }

        [InitializeOnLoadMethod]
        private static void OnProjectLoadedInEditor()
        {
            EditorApplication.playModeStateChanged += OnEditorPlayModeStateChanged;
            EditorApplication.wantsToQuit += OnEditorWantsToQuit;
        }

        private static void OnEditorPlayModeStateChanged(PlayModeStateChange state)
        {
            bool shouldResetEditorState = (state == PlayModeStateChange.EnteredEditMode);

            if (shouldResetEditorState)
            {
                EditorSceneManager.playModeStartScene = null;
                CheckAndRestoreBuildSettingSceneList();
            }

            bool shouldEnableUI = (state != PlayModeStateChange.EnteredPlayMode);

            if (Instance)
            {
                Instance.rootVisualElement.SetEnabled(shouldEnableUI);
            }
        }

        private static bool OnEditorWantsToQuit()
        {
            CheckAndRestoreBuildSettingSceneList();
            return true;
        }

        private static void BackupBuildSettingSceneList()
        {
            var settings = EditorBuildSettings.scenes.Select(x =>
            {
                return new BuildSettingScene(x.enabled, x.path);
            });

            var buildSettingSceneList = new BuildSettingSceneList(settings.ToArray());
            string json = JsonUtility.ToJson(buildSettingSceneList);

            EditorPrefs.SetString(KEY_BUILD_SETTING_LAST_STATE, json);
        }

        private static void CheckAndRestoreBuildSettingSceneList()
        {
            bool shouldRestoreBuildSettingSceneList = EditorPrefs.GetBool(KEY_BUILD_SETTING_DIRTY, false);

            if (shouldRestoreBuildSettingSceneList)
            {
                RestoreBuildSettingSceneList();
                EditorPrefs.SetBool(KEY_BUILD_SETTING_DIRTY, false);
            }
        }

        private static void RestoreBuildSettingSceneList()
        {
            string json = EditorPrefs.GetString(KEY_BUILD_SETTING_LAST_STATE, "");

            if (string.IsNullOrEmpty(json))
            {
                return;
            }

            var buildSettingSceneList = JsonUtility.FromJson<BuildSettingSceneList>(json);
            var settings = buildSettingSceneList.settings.Select(x =>
            {
                return new EditorBuildSettingsScene(x.sceneAssetPath, x.enabled);
            });

            EditorBuildSettings.scenes = settings.ToArray();
        }

        private void OnEnable()
        {
            if (Instance == null)
            {
                Instance = this;
            }
        }

        public void CreateGUI()
        {
            VisualElement root = rootVisualElement;
            Initialize(root);
            ResetViewState();
        }

        private void Initialize(VisualElement root)
        {
            bool canLoadVisualTree = LoadVisualTreeFromAsset(root);

            if (!canLoadVisualTree)
            {
                return;
            }

            enumPlayMode = root.Q<EnumField>("enumPlayMode");
            objectFieldCustomFirstScene = root.Q<ObjectField>("objectFieldCustomFirstScene");
            objectFieldCustomSceneList = root.Q<ObjectField>("objectFieldCustomSceneList");
            buttonPlay = root.Q<Button>("buttonPlay");
            buttonApplySceneListToBuildSetting = root.Q<Button>("buttonApplySceneListToBuildSetting");

            bool isValid = (buttonPlay != null)
                    && (buttonApplySceneListToBuildSetting != null)
                    && (enumPlayMode != null)
                    && (objectFieldCustomFirstScene != null)
                    && (objectFieldCustomSceneList != null);

            if (!isValid)
            {
                Debug.LogError($"{nameof(SceneSelector)}: not all visual tree valid");
                return;
            }

            enumPlayMode.SetValueWithoutNotify(m_CurrentPlayMode);
            enumPlayMode.RegisterValueChangedCallback((e) =>
            {
                var selectedPlayMode = (PlayMode)e.newValue;
                m_CurrentPlayMode = selectedPlayMode;
                ResetViewState();
            });

            objectFieldCustomFirstScene.value = m_CustomFirstScene;
            objectFieldCustomFirstScene.RegisterValueChangedCallback((e) =>
            {
                m_CustomFirstScene = (SceneAsset)e.newValue;
            });

            objectFieldCustomSceneList.value = m_CurrentSceneList;
            objectFieldCustomSceneList.RegisterValueChangedCallback((e) =>
            {
                m_CurrentSceneList = (CustomSceneListScriptableObject)e.newValue;
            });

            buttonPlay.clicked += () =>
            {
                switch (m_CurrentPlayMode)
                {
                    case PlayMode.CustomFirstScene:
                        {
                            if (m_CustomFirstScene)
                            {
                                EnterPlayModeWithScene(m_CustomFirstScene);
                            }
                            else
                            {
                                EditorUtility.DisplayDialog("Error", "No scene selected for playing.", "OK");
                                objectFieldCustomFirstScene.Focus();
                            }
                        }
                        break;

                    case PlayMode.CustomSceneList:
                        {
                            if (m_CurrentSceneList)
                            {
                                var currentScene = m_CurrentSceneList
                                                        ?.settings
                                                        ?.FirstOrDefault(x => (x.enabled == true) && (x.scene != null))
                                                        ?.scene;

                                if (currentScene)
                                {
                                    try
                                    {
                                        BackupBuildSettingSceneList();
                                        m_CurrentSceneList.ApplyToBuildSetting();

                                        EditorPrefs.SetBool(KEY_BUILD_SETTING_DIRTY, true);
                                        EnterPlayModeWithScene(currentScene);
                                    }
                                    catch (Exception e)
                                    {
                                        RestoreBuildSettingSceneList();
                                        EditorPrefs.SetBool(KEY_BUILD_SETTING_DIRTY, false);

                                        EditorUtility.DisplayDialog("Error", e.Message, "OK");
                                        Debug.LogException(e);
                                    }
                                }
                                else
                                {
                                    EditorUtility.DisplayDialog("Error", "No valid active scene in custom scene list.\nNeed to have at least one scene enabled", "OK");
                                    EditorGUIUtility.PingObject(m_CurrentSceneList);
                                }
                            }
                            else
                            {
                                EditorUtility.DisplayDialog("Error", "No custom scene list found.", "OK");
                                objectFieldCustomSceneList.Focus();
                            }
                        }
                        break;

                    case PlayMode.UseBuildSetting:
                        {
                            var currentScenePath = EditorBuildSettings
                                                    .scenes
                                                    ?.FirstOrDefault(x => x.enabled == true)
                                                    ?.path;

                            var currentScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(currentScenePath);

                            if (currentScene)
                            {
                                EnterPlayModeWithScene(currentScene);
                            }
                            else
                            {
                                EditorUtility.DisplayDialog("Error", "No valid active scene in custom scene list.", "OK");
                                EditorGUIUtility.PingObject(m_CurrentSceneList);
                            }
                        }
                        break;

                    default:
                        break;
                }
            };

            buttonApplySceneListToBuildSetting.clicked += () =>
            {
                if (!m_CurrentSceneList)
                {
                    EditorUtility.DisplayDialog("Error", "No custom scene list found.", "OK");
                    objectFieldCustomSceneList.Focus();
                    return;
                }

                bool isConfirm = EditorUtility.DisplayDialog("Warning", "Are you sure to apply custom scene list to BuildSetting?", "Apply", "Cancel");

                if (!isConfirm)
                {
                    return;
                }

                try
                {
                    m_CurrentSceneList.ApplyToBuildSetting();
                    EditorUtility.DisplayDialog("Success", "Scene list in BuildSetting has changed", "OK");
                }
                catch (Exception e)
                {
                    EditorUtility.DisplayDialog("Error", e.Message, "OK");
                    Debug.LogException(e);
                }
            };
        }

        private bool LoadVisualTreeFromAsset(VisualElement root)
        {
            if (m_VisualTreeAsset)
            {
                VisualElement visualElement = m_VisualTreeAsset.Instantiate();
                root.Add(visualElement);
                return true;
            }

            Debug.LogError($"{nameof(SceneSelector)}: Cannot find VisualTreeAsset of SceneSelector, SceneSelector won't function...");
            return false;
        }

        private void ResetViewState()
        {
            objectFieldCustomFirstScene.style.display = (m_CurrentPlayMode == PlayMode.CustomFirstScene) ? DisplayStyle.Flex : DisplayStyle.None;
            objectFieldCustomSceneList.style.display = (m_CurrentPlayMode == PlayMode.CustomSceneList) ? DisplayStyle.Flex : DisplayStyle.None;
            buttonApplySceneListToBuildSetting.style.display = (m_CurrentPlayMode == PlayMode.CustomSceneList) ? DisplayStyle.Flex : DisplayStyle.None;

            objectFieldCustomFirstScene.value = m_CustomFirstScene;
            objectFieldCustomSceneList.value = m_CurrentSceneList;
        }

        private void EnterPlayModeWithScene(SceneAsset sceneAsset)
        {
            if (!Application.isPlaying)
            {
                EditorSceneManager.playModeStartScene = sceneAsset;
                EditorApplication.isPlaying = true;
            }
        }
    }
}

