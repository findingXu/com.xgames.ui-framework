using System;
using System.Collections.Generic;
using UnityEngine;

namespace XGames.UIFramework
{
    [Serializable]
    public class UIComponentNode
    {
        public string fieldName;
        public GameObject gameObject;
        public Component component;
    }

    public class UIContext : MonoBehaviour
    {
#if UNITY_EDITOR
        [HideInInspector] 
        public string prefabPath;
        [HideInInspector] 
        public string bindHash; 
        [HideInInspector] 
        public string bindFile;
        [HideInInspector] 
        public List<UIComponentNode> lstComponentsInEditor = new();
#endif
        
        [HideInInspector] 
        public List<Component> lstComponents = new();
        public UIAnimator uiAnimator;
    }
}