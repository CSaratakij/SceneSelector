using System;
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
            public bool enabled = true;
            public SceneAsset scene = default;
        }

        public Setting[] settings;
    }
}

