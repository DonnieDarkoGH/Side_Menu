using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace ContextualMenuData {
    [CustomEditor(typeof(ScriptableTreeObject))]
    [CanEditMultipleObjects]
    public class ScriptableTreeObjectEditor : Editor {

        ScriptableTreeObject managerScript;

        public int Index = 0;

        void OnEnable() {
            managerScript = (ScriptableTreeObject)target;
        }

        public override void OnInspectorGUI() {

            if (managerScript.serializedNodes == null || managerScript.serializedNodes.Count == 0)
                managerScript.Init();

            serializedObject.Update();
            EditorGUILayout.LabelField("Count :" + managerScript.serializedNodes.Count);

            if (GUILayout.Button("Clear")) {
                Undo.RecordObject(target, "Clear data");
                managerScript.ClearAll();
                serializedObject.ApplyModifiedProperties();
                return;
            }

            Display(managerScript.serializedNodes[0], true);

            if (GUI.changed) {
                EditorUtility.SetDirty(target);
            }
            serializedObject.ApplyModifiedProperties();

        }

        void Display(ScriptableTreeObject.SerializableNode _node, bool _isRootNode = false) {

            serializedObject.Update();

            if (_node == null) {
                return;
            }

            EditorGUI.indentLevel = managerScript.GetLevel(_node);

            int[] childIndexes;
            int index = managerScript.serializedNodes.IndexOf(_node);
            managerScript.GetNextIndex(index, out childIndexes);

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(managerScript.GetNodeInfo(_node));

            if (GUILayout.Button("Remove")) {
                managerScript.RemoveNode(_node);
                serializedObject.ApplyModifiedProperties();
                return;
            }

            if (GUILayout.Button("Add Sub-Menu")) {
                managerScript.AddNode(_node);
                serializedObject.ApplyModifiedProperties();
                return;
            }
            GUILayout.EndHorizontal();

            if (index < managerScript.serializedNodes.Count - 1) {
                Display(managerScript.serializedNodes[index + 1]);
            }

        }

    }
}
