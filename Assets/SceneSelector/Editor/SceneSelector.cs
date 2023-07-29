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
        public enum PlayMode
        {
            CustomFirstScene,
            CustomSceneList,
            UseBuildSetting
        }

        private static string EditModeSceneAssetPath = "";
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

        private static void OnEditorPlayModeStateChanged(PlayModeStateChange state)
        {
            bool shouldResetSceneInEditMode = (state == PlayModeStateChange.EnteredEditMode);

            if (shouldResetSceneInEditMode)
            {
                EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(EditModeSceneAssetPath);

                // TODO : restore build setting if use custom scene list
                // here...
            }

            bool shouldEnableUI = (state != PlayModeStateChange.EnteredPlayMode);

            if (Instance)
            {
                Instance.rootVisualElement.SetEnabled(shouldEnableUI);
            }
        }

        private void OnEnable()
        {
            EditorApplication.playModeStateChanged += OnEditorPlayModeStateChanged;

            if (Instance == null)
            {
                Instance = this;
            }
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnEditorPlayModeStateChanged;
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

                    // TODO : custom scene list should be able to 
                    // 1) temporary apply custom scene list to build setting
                    // 2) restore build setting scene list after exit playmode
                    case PlayMode.CustomSceneList:
                        {
                            if (m_CurrentSceneList)
                            {
                                var currentScene = m_CurrentSceneList
                                                        ?.settings
                                                        ?.FirstOrDefault(x => x.enabled == true)
                                                        ?.scene;

                                if (currentScene)
                                {
                                    // TODO : backup build setting to restore after-ward
                                    EnterPlayModeWithScene(currentScene);
                                }
                                else
                                {
                                    EditorUtility.DisplayDialog("Error", "No valid active scene in custom scene list.", "OK");
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
                if (m_CurrentSceneList)
                {
                    m_CurrentSceneList.ApplyToBuildSetting();
                }
                else
                {
                    EditorUtility.DisplayDialog("Error", "No custom scene list found.", "OK");
                    objectFieldCustomSceneList.Focus();
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
                EditModeSceneAssetPath = EditorSceneManager.GetActiveScene().path;
                EditorSceneManager.playModeStartScene = sceneAsset;
                EditorApplication.isPlaying = true;
            }
        }

        // TODO
        private void BackupBuildSettingSceneList()
        {

        }

        // TODO
        private void RestoreBuildSettingSceneList()
        {

        }
    }
}

