using System;
using UnityEngine;
using UnityEditor;
using ContextualMenu;
using CustomMenu.Events;
using CustomMenu.Tools;

namespace ContextualMenuData {
    //[CustomEditor(typeof(ScriptableMenuStructure))]
    [CanEditMultipleObjects]
    public class ScriptableMenuStructureEditor : Editor {

        ScriptableMenuStructure managerScript;
        private SerializedProperty eventsListInManager;
        //private SerializedProperty      listProp;

        private ScriptableTreeObject.SerializableNode node;
        private ButtonModel button;
        private GUIStyle titleStyle, foldoutStyle1, foldoutStyle2;
        private bool isSetup = false;
        private int buttonCount;
        private int level;
        private int nextIndex;

        void OnEnable() {
            managerScript = (ScriptableMenuStructure)target;

            //managerScript.Init();
        }

        bool SetupReferences() {

            buttonCount = managerScript.serializedNodes.Count;
            nextIndex   = 0;

            SerializedObject so = new SerializedObject(MenuEventManager.Instance);
            eventsListInManager = so.FindProperty("ActionEvents");

            titleStyle    = new GUIStyle();
            titleStyle.fontStyle = FontStyle.Bold;

            foldoutStyle1 = new GUIStyle(EditorStyles.foldout);
            foldoutStyle2 = new GUIStyle(EditorStyles.foldout);

            foldoutStyle1.fixedWidth = 50;
            foldoutStyle1.fontStyle = FontStyle.Bold;
            foldoutStyle2.fontStyle = FontStyle.Bold;

            //managerScript.AreDetailsVisible.RemoveRange(buttonCount, managerScript.AreDetailsVisible.Count - buttonCount);

            return true;
        }

        public override void OnInspectorGUI() {

            if (managerScript.serializedNodes == null || managerScript.serializedNodes.Count == 0)
                managerScript.Init();

            if (!isSetup) {
                isSetup = SetupReferences();
            }

            serializedObject.Update();

            if (buttonCount == 0) {
                return;
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Menu for ", titleStyle, GUILayout.MaxWidth(100));
            managerScript.Context = (EContext)EditorGUILayout.EnumPopup(managerScript.Context);
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Clear")) {
                Undo.RecordObject(target, "Clear data");
                managerScript.ClearAll();
                serializedObject.ApplyModifiedProperties();
                return;
            }

            Display(0, true);

            if (GUI.changed) {
                EditorUtility.SetDirty(target);
            }
            serializedObject.ApplyModifiedProperties();
        }

        void Display(int _index, bool _isRootNode = false) {

            serializedObject.Update();

            if (!isSetup) {
                isSetup = SetupReferences();
                return;
            }

            if (managerScript.serializedNodes.Count <= _index) {
                return;
            }

            int[] subIndexes;
            int len = managerScript.serializedNodes.Count;
            node   = managerScript.serializedNodes[_index];
            button = managerScript.Buttons[_index];
            level  = managerScript.GetLevel(node);
            nextIndex = managerScript.GetNextIndex(_index, out subIndexes);

            EditorGUI.indentLevel = level;

            /** Header (Current Button) **/
            EditorGUILayout.BeginHorizontal();
            try {
                managerScript.AreDetailsVisible[_index] = EditorGUILayout.Foldout(managerScript.AreDetailsVisible[_index], "Level " + level, foldoutStyle1);
            }
            catch {
                Debug.Log(_index);
                Debug.Log(managerScript.AreDetailsVisible.Count);
                Debug.Log(managerScript.serializedNodes.Count);
            }

            button.name = EditorGUILayout.TextField(button.name, GUILayout.MinWidth(100));
            EditorGUILayout.LabelField("Id : " + node.Id + "(" + node.ChildCount + " children)");

            if (level > 0 && GUILayout.Button("-", GUILayout.MinWidth(30))) {
                managerScript.RemoveButton(button);
                EditorUtility.SetDirty(target);
                isSetup = false;
                return;
            }

            if (GUILayout.Button("Add Sub-Menu", GUILayout.MaxWidth(100))) {
                managerScript.AddNode(node);
                managerScript.AreDetailsVisible[_index] = true;
                EditorUtility.SetDirty(target);
                isSetup = false;
                return;
            }

            GUILayout.EndHorizontal();
            /** End of the header **/

            /** Details of the Button **/
            if (managerScript.AreDetailsVisible[_index]) {

            GUILayout.BeginHorizontal();

                EditorGUI.BeginChangeCheck();

                EditorGUILayout.LabelField("Icon", GUILayout.MaxWidth(80));

                Sprite icon = (Sprite)EditorGUILayout.ObjectField(button.Icon, typeof(Sprite), false);

                if (EditorGUI.EndChangeCheck()) {
                    button.Icon = icon;
                    EditorUtility.SetDirty(target);
                    isSetup = false;
                    return;
                }

                GUILayout.EndHorizontal();

            Texture2D texture = button.Icon ? button.Icon.texture : Texture2D.blackTexture;

                GUILayout.BeginHorizontal();
                GUILayout.BeginHorizontal(texture, GUIStyle.none, GUILayout.MaxHeight(30));
                EditorGUILayout.LabelField(GUIContent.none, GUILayout.MaxWidth(1),GUILayout.MinHeight(50));
                GUILayout.EndHorizontal();

                if (eventsListInManager != null && eventsListInManager.arraySize > _index) {
                    SerializedProperty eventInterface = eventsListInManager.GetArrayElementAtIndex(_index);
                    EditorGUILayout.PropertyField(eventInterface);

                    serializedObject.ApplyModifiedProperties();
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();

                //managerScript.AreSubLevelVisible[_index] = EditorGUILayout.Foldout(managerScript.AreSubLevelVisible[_index], "Sub Menus", foldoutStyle2);

            GUILayout.EndHorizontal();

                for (int i = 0; i < node.ChildCount; i++) {
                    try {
                        Display(subIndexes[i]);
                    }
                    catch {
                        return;
                        //Debug.LogError("Error when accessing subnodes of " + managerScript.GetNodeInfo(node));
                        //Debug.LogError("                    with Index " + i);
                    }
                }

            }

            if (_index > 0)
                Display(nextIndex);

        }

        private void OnDisable() {

        }

    }
}
