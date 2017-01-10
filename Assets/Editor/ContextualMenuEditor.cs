using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using ContextualMenu;
using CustomMenu.Events;

namespace ContextualMenuData {
    [CustomEditor(typeof(ScriptableMenuStructure))]
    [CanEditMultipleObjects]
    public class ContextualMenuEditor : Editor {

        private ScriptableMenuStructure targetScript;
        private ReorderableList     nodesListProp;
        private SerializedProperty  buttonsListProp;
        private SerializedProperty  contextProp;
        private SerializedObject    eventsListObject;
        private SerializedProperty  eventsListProp;

        private float lineHeight     = EditorGUIUtility.singleLineHeight * 1.5f;
        private float nameFieldWidth = 150f;
        private float idFieldWidth   = 50f;
        private float iconFieldWidth = 200.0f;
        private GUIStyle titleStyle  = new GUIStyle();

        private bool   isReodering     = false;
        private bool   isEventModified = false;
        private int    selectedIndex   = -1;
        private int[]  nodeIndexesToReorder;
        private string nodeIndexesToReorderToString;

        private int debugIndex = -1;

        private void OnEnable() {

            targetScript = (ScriptableMenuStructure)target;
            
            contextProp  = serializedObject.FindProperty("Context");

            eventsListObject = new SerializedObject(MenuEventManager.Instance);
            eventsListProp   = eventsListObject.FindProperty("ActionEvents");

            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.fontSize = 14;

            nodesListProp   = new ReorderableList(serializedObject, serializedObject.FindProperty("serializedNodes"), true, true, true, true);
            buttonsListProp = serializedObject.FindProperty("Buttons");
            
            nodesListProp.headerHeight  = lineHeight;
            nodesListProp.elementHeight = lineHeight;
            nodesListProp.footerHeight  = lineHeight;

            nodesListProp.drawHeaderCallback  = (Rect rect) => { EditorGUI.LabelField(rect, "Buttons in menu", titleStyle); };

            nodesListProp.drawElementCallback = drawElement;

            nodesListProp.onAddCallback       = OnAdd;

            nodesListProp.onRemoveCallback    = OnRemove;

            nodesListProp.onReorderCallback   = OnReorder;

            nodesListProp.onSelectCallback    = OnSelect;

            nodesListProp.onChangedCallback   = OnChange;

            nodesListProp.elementHeightCallback = elementHeight;

        }

        private void drawElement(Rect rect, int index, bool isActive, bool isFocused) {
            //Debug.Log("<b>ContextualMenuEditor</b> drawElement");

            var button = buttonsListProp.GetArrayElementAtIndex(index);
            var node   = nodesListProp.serializedProperty.GetArrayElementAtIndex(index);

            rect.y += 2;

            rect.x += 10 * (node.FindPropertyRelative("Id").stringValue.Length - 1);

            previewIcon(rect, index);
            DrawField(button, rect, nameFieldWidth  , "name");
            DrawField(button, rect, idFieldWidth    , "id"    , nameFieldWidth);
            DrawField(node  , rect, idFieldWidth    , "Id"    , nameFieldWidth + idFieldWidth);
            DrawField(button, rect, iconFieldWidth  , "Icon"  , nameFieldWidth + idFieldWidth * 2);

            if (targetScript.AreDetailsVisible[index]) {
                rect.y += lineHeight;
                rect.x = 20;

                int eventIndex = MenuEventManager.Instance.GetActionEventIndex(targetScript.Context, targetScript.Buttons[index].Id);
                if (eventIndex < 0) {
                    eventIndex = 0;
                }
                if(eventIndex >= eventsListProp.arraySize) {
                    Debug.Log("eventIndex >= eventsListProp.arraySize");
                    eventsListObject = new SerializedObject(MenuEventManager.Instance);
                    eventsListProp   = eventsListObject.FindProperty("ActionEvents");
                    nodesListProp.index = index;
                }

                EditorGUI.BeginChangeCheck();
                try {
                    EditorGUI.PropertyField(rect, eventsListProp.GetArrayElementAtIndex(eventIndex).FindPropertyRelative("ButtonEvent"));
                }
                catch {
                    Debug.Log("Failed to GetArrayElementAtIndex " + eventIndex);
                    return;
                }
                
                if (EditorGUI.EndChangeCheck()) {
                    MenuEventManager.Instance.ModifyButtonEvent(targetScript.Context, targetScript.Buttons[index].Id);
                    
                    serializedObject.ApplyModifiedProperties();
                }

            }
            
        }

        private void OnAdd(ReorderableList rlist) {
            //Debug.Log("<b>ContextualMenuEditor</b> OnAdd");

            if (rlist.index< 0) {
                targetScript.AddNode(targetScript.serializedNodes[0]);
            }
            else {
                targetScript.AddNode(targetScript.serializedNodes[rlist.index]);
            }

        }

        private void OnRemove(ReorderableList rlist) {
            //Debug.Log("<b>ContextualMenuEditor</b> OnRemove");

            if (EditorUtility.DisplayDialog("Warning!", "Are you sure you want to delete this button ?", "Yes", "No")) {
                targetScript.RemoveButton(targetScript.Buttons[rlist.index]);
                serializedObject.ApplyModifiedProperties();
            }
        }

        private void OnReorder(ReorderableList rlist) {
            //Debug.Log("<b>ContextualMenuEditor</b> OnReorder");
            
            isReodering = selectedIndex != rlist.index;
        }

        private void OnSelect(ReorderableList rlist) {
            //Debug.Log("<b>ContextualMenuEditor</b> OnSelect");

            selectedIndex = rlist.index;

            var node      = targetScript.serializedNodes[rlist.index];

            nodeIndexesToReorder = targetScript.GetNodeAndSubChildrenId(node);

            if (targetScript.AreDetailsVisible[rlist.index]) {
                targetScript.AreDetailsVisible[rlist.index] = false;
            }

            nodeIndexesToReorderToString = "";
            for (int i = 0; i < nodeIndexesToReorder.Length; i++) {
                nodeIndexesToReorderToString += nodeIndexesToReorder[i] + ",";
            }
        }

        private void OnChange(ReorderableList rlist) {
            //Debug.Log("<b>ContextualMenuEditor</b> OnChange");

            int ind = rlist.index;
            if (isReodering) {
                ind = targetScript.ReorderElements(nodeIndexesToReorder, ind);
                isReodering = false;
            }

            nodesListProp.index = ind;
        }

        private float elementHeight(int index) {
            //Debug.Log("<b>ContextualMenuEditor</b> elementHeight on index " + index);

            float heightfactor = targetScript.AreDetailsVisible[index] ? 4.5f : 1f;

            return lineHeight * heightfactor;
        }


        public override void OnInspectorGUI() {
            serializedObject.Update();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Selected Index       : " + selectedIndex);
            EditorGUILayout.LabelField("Event Index          : " + debugIndex);
            //EditorGUILayout.LabelField("nodeIndexesToReorder : " + nodeIndexesToReorderToString);
            GUILayout.EndHorizontal();
            
            EditorGUILayout.PropertyField(contextProp);

            if (GUILayout.Button("Clear")) {
                targetScript.ClearAll();
                nodesListProp.index  = -1;
                nodeIndexesToReorder = new int[0];
                serializedObject.ApplyModifiedProperties();
                return;
            }

            nodesListProp.DoLayoutList();

            serializedObject.ApplyModifiedProperties();
        }

        private bool DrawField(SerializedProperty _prop, Rect _rectangle, float _width, string _fieldName, float _previousWidth = 0.0f) {

            Rect  rect   = new Rect (lineHeight + _rectangle.x + _previousWidth,
                                     _rectangle.y,
                                     _width,
                                     lineHeight);
            SerializedProperty findProp = _prop.FindPropertyRelative(_fieldName);

            return EditorGUI.PropertyField(rect, findProp, GUIContent.none);
        }

        private void previewIcon(Rect _rectangle, int _index) {

            Texture texture = targetScript.Buttons[_index].Icon ? targetScript.Buttons[_index].Icon.texture : Texture2D.blackTexture;

            Rect rect = new Rect(_rectangle.x, _rectangle.y, lineHeight, lineHeight);


            targetScript.AreDetailsVisible[_index] = EditorGUI.Foldout(rect, targetScript.AreDetailsVisible[_index], new GUIContent(texture));
            //EditorGUI.DrawTextureTransparent(rect, texture);

        }
    }
}
