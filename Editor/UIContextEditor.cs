using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Assertions;

namespace XGames.UIFramework
{
    [CustomEditor(typeof(UIContext))]
    public class UIContextEditor : UnityEditor.Editor
    {
        private UIContext _target;
        private SerializedProperty _prefabPath;
        private SerializedProperty _bindHash;
        private SerializedProperty _bindFile;
        private SerializedProperty _lstComponentsInEditor;
        private SerializedProperty _lstComponents;
        private SerializedProperty _uiAnimator;

        private void OnEnable()
        {
            _target = (UIContext)target;
            _prefabPath = serializedObject.FindProperty("prefabPath");
            _bindHash = serializedObject.FindProperty("bindHash");
            _bindFile = serializedObject.FindProperty("bindFile");
            _lstComponentsInEditor = serializedObject.FindProperty("lstComponentsInEditor");
            _lstComponents = serializedObject.FindProperty("lstComponents");
            _uiAnimator = serializedObject.FindProperty("uiAnimator");

            if (string.IsNullOrEmpty(_bindHash.stringValue) && EditorSceneManager.IsPreviewSceneObject(_target.gameObject))
            {
                _bindHash.stringValue = NewBindHash();
                Save(); 
            }
        }
        
        public override void OnInspectorGUI()
        {
            DrawTopShortcutMenu();
            base.OnInspectorGUI();
        }
        
        private void DrawTopShortcutMenu()
        {
            DrawTopMenu();
            DrawBindComponents();
            DrawAddComponentArea();
        }

        private void DrawTopMenu()
        {
            GUILayout.Label($"ID: {_bindHash.stringValue}", new GUIStyle(EditorStyles.whiteLabel)
            {
                fontStyle = FontStyle.Bold,
            });
            GUILayout.BeginHorizontal();
            var isRefreshHash = GUILayout.Button("Refresh ID", new GUIStyle(EditorStyles.miniButton)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fixedWidth = 80,
            });
            if (isRefreshHash && EditorUtility.DisplayDialog("", "Please confirm the generation of a new hash ID?", "Yes", "Cancel"))
            {
                _bindHash.stringValue = NewBindHash();
                Save();
                TipsGoPhaseHash();
            }

            var filePath = _bindFile.stringValue;
            var isUpdateCode = GUILayout.Button("Update Code", new GUIStyle(EditorStyles.miniButton)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
            });
            if (isUpdateCode)
            {
                UpdateScriptCode();
            }
            
            var isOpenFile = GUILayout.Button("Edit Code", new GUIStyle(EditorStyles.miniButton)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
            });
            if (isOpenFile)
            {
                if (!File.Exists(filePath))
                {
                    TipsGoPhaseHash();
                }
                else
                {
                    Process.Start("code", filePath);
                }
            }
            GUILayout.EndHorizontal();
        }
        
         private void DrawBindComponents()
        {
            for (int index = 0, size = _lstComponentsInEditor.arraySize; index < size; ++index)
            {
                var node = _lstComponentsInEditor.GetArrayElementAtIndex(index);
                
                GUILayout.BeginHorizontal();
                DrawNodeIndex(index, node);
                DrawFieldName(index, node);
                DrawBindingComponent(index, node);
                DrawDeleteButton(index, node);
                GUILayout.EndHorizontal();
            }
        }

        private void DrawNodeIndex(int index, SerializedProperty node)
        {
            var isPing = GUILayout.Button($"({index})", new GUIStyle(EditorStyles.miniButton)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fixedWidth = 36,
            });
            if (isPing)
            {
                var go = node.FindPropertyRelative("gameObject").objectReferenceValue as GameObject;
                if(go) EditorGUIUtility.PingObject(go);
            }
        }

        private void DrawFieldName(int index, SerializedProperty node)
        {
            var fieldName = node.FindPropertyRelative("fieldName");
            var newFieldName = EditorGUILayout.TextField(fieldName.stringValue, new GUIStyle(EditorStyles.textField)
            {
                fontStyle = FontStyle.Bold,
            });
            if (newFieldName != fieldName.stringValue)
            {
                fieldName.stringValue = newFieldName;
                Save();
            }
        }

        private void DrawBindingComponent(int index, SerializedProperty node)
        {
            var go = node.FindPropertyRelative("gameObject").objectReferenceValue as GameObject;
            if(go == null) return;
            
            var component = node.FindPropertyRelative("component");
            var componentRef = component.objectReferenceValue as Component;
            
            var lstComponentName = new List<string>();
            var lstHasComponents = go.GetComponents<Component>();
            var indexRef = 0;
            for (int i = 0, len = lstHasComponents.Length; i < len; ++i)
            {
                var comp = lstHasComponents[i];
                if (comp == componentRef) indexRef = i;

                var typeName = comp.GetType().FullName;
                if (typeName == null) continue;

                var compName = typeName.Split(".").Last();
                lstComponentName.Add(compName);
            }

            var popUpStyle = new GUIStyle(EditorStyles.popup)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
            };
            var indexSelect = EditorGUILayout.Popup(indexRef, lstComponentName.ToArray(), popUpStyle);
            
            if (indexSelect != indexRef)
            {
                var bindComponent = _lstComponents.GetArrayElementAtIndex(index);
                bindComponent.objectReferenceValue = lstHasComponents[indexSelect];
                component.objectReferenceValue = lstHasComponents[indexSelect];
                Save();
            }
        }

        private void DrawDeleteButton(int nodeIndex, SerializedProperty node)
        {
            var needDelete = GUILayout.Button("x", new GUIStyle(EditorStyles.miniButton)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
            });
            if (needDelete)
            {
                _lstComponentsInEditor.DeleteArrayElementAtIndex(nodeIndex);
                _lstComponents.DeleteArrayElementAtIndex(nodeIndex);
                Save();
            }
        }

        private void DrawAddComponentArea()
        {
            var rect = EditorGUILayout.GetControlRect(true, 40);
            GUI.Box(rect, "");
            GUI.Label(rect, "Drag and drop the game object to bind it!", new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
            });
            
            if (!rect.Contains(Event.current.mousePosition)) return;
            
            DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
            if (Event.current.type != EventType.DragExited) return;
            
            var lstObjects = DragAndDrop.objectReferences;
            for (var i = 0; i < lstObjects.Length; ++i)
            {
                var instance = lstObjects[i] as GameObject;
                if (!instance)
                {
                    return;
                }
                
                var nodeCount = _lstComponentsInEditor.arraySize;
                _lstComponentsInEditor.InsertArrayElementAtIndex(nodeCount);

                var node = _lstComponentsInEditor.GetArrayElementAtIndex(nodeCount);
                var fieldName = node.FindPropertyRelative("fieldName");
                var go = node.FindPropertyRelative("gameObject");
                var component = node.FindPropertyRelative("component");
                fieldName.stringValue = instance.name;
                go.objectReferenceValue = instance;
                component.objectReferenceValue = instance.transform;

                _lstComponents.InsertArrayElementAtIndex(nodeCount);
                var compRef = _lstComponents.GetArrayElementAtIndex(nodeCount);
                compRef.objectReferenceValue = instance.transform;
                Save();
            }
        }

        #region Logic
         private string NewBindHash()
        {
            var prefab = _target.gameObject;
            Assert.IsTrue(EditorSceneManager.IsPreviewSceneObject(prefab), "Please open the prefabricated scene to perform this operation!");
            var prefabStage = PrefabStageUtility.GetPrefabStage(prefab);
            var hash = AssetDatabase.GetAssetDependencyHash(prefabStage.assetPath);
            return hash.ToString();
        }
        
        private void TipsGoPhaseHash()
        {
            GUIUtility.systemCopyBuffer = string.Format(UIEditorConfig.BindHashTag, _target.bindHash, "\n//...\n");
            EditorUtility.DisplayDialog("", "The hash ID has been copied to the clipboard. Please proceed to paste.", "确认");
        }
        
        private void UpdateScriptCode()
        {
            var prefab = _target.gameObject;
            Assert.IsTrue(EditorSceneManager.IsPreviewSceneObject(prefab), "Please open the prefabricated scene to perform this operation!");
            var prefabStage = PrefabStageUtility.GetPrefabStage(prefab);
            var assetPath = prefabStage.assetPath;

            var isValidPrefab = false;
            foreach (var prefabDirectory in UIEditorConfig.UIPrefabDirectory)
            {
                if (!assetPath.StartsWith(prefabDirectory)) continue;

                isValidPrefab = true;
                var ext = Path.GetExtension(assetPath);
                _prefabPath.stringValue = assetPath.Replace(prefabDirectory, "").Replace(ext, "");
                Save();
                break;
            }
            Assert.IsTrue(isValidPrefab, $"This asset is not in the specified directory, {assetPath}");
            
            var dicCache = UIEditorConfig.ReadBindingCache();
            if (!dicCache.TryGetValue(_bindHash.stringValue, out var bindFile) || !File.Exists(bindFile))
            {
                if (!TryFindTsScriptByHashCode(out bindFile))
                {
                    TipsGoPhaseHash();
                    return;
                }
            }
            
            var txtCode = File.ReadAllText(bindFile);
            var regex = string.Format(UIEditorConfig.BindHashTag, _bindHash.stringValue, "[\\s\\S]*");
            var result = Regex.Match(txtCode, regex);
            if (!result.Success)
            {
                if (!TryFindTsScriptByHashCode(out bindFile))
                {
                    TipsGoPhaseHash();
                    return;
                }
            }
            var oldCode = result.Groups[0].Value;
            var newCode = UIContextPrinter.GenerateCode(_target);
            txtCode = txtCode.Replace(oldCode, newCode);
            File.WriteAllText(bindFile, txtCode);
            if (EditorUtility.DisplayDialog("", "The latest code has been generated. Do you want to open the corresponding file?", "Yes", "Cancel"))
            {
                Process.Start("code", bindFile);
            }
        }

        private bool TryFindTsScriptByHashCode(out string bindFile)
        {
            foreach (var directory in UIEditorConfig.UICodeDirectory)
            {
                var codeDirectory = Path.Combine(Application.dataPath, directory);
                var lstFiles = Directory.GetFiles(codeDirectory, "*.*", SearchOption.AllDirectories).Where(file =>
                {
                    var ext = Path.GetExtension(file);
                    return ext is ".ts";
                });
                
                foreach (var file in lstFiles)
                {
                    var filePath = Path.GetFullPath(file);
                    var txt = File.ReadAllText(filePath);
                        
                    var isMatch = Regex.IsMatch(txt, _bindHash.stringValue, RegexOptions.IgnorePatternWhitespace);
                    if (!isMatch) continue;
                    
                    bindFile = filePath;
                    _bindFile.stringValue = bindFile;
                    Save();

                    var dicCache = UIEditorConfig.ReadBindingCache();
                    dicCache[_bindHash.stringValue] = bindFile;
                    UIEditorConfig.SaveBindingCache(dicCache);
                    return true;
                }
            }

            bindFile = string.Empty;
            return false;
        }
        
        private void Save()
        {
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            var prefab = _target.gameObject;
            var prefabStage = PrefabStageUtility.GetPrefabStage(prefab);
            PrefabUtility.SaveAsPrefabAsset(prefab, prefabStage.assetPath);
            AssetDatabase.Refresh();
        }
        #endregion
    }
}