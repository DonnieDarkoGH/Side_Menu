using UnityEditor;
using UnityEngine;

namespace ContextualMenu {
    [CustomEditor(typeof(MenuManager))]
    [CanEditMultipleObjects]
    public class MenuManagerEditor : Editor {

        private MenuManager managerScript;

        void OnEnable() {
            managerScript = (MenuManager)target;
        }

        // Use this for initialization
        public override void OnInspectorGUI() {

            DrawDefaultInspector();
            
            if(GUILayout.Button("Create new menu")) {
                managerScript.CreateNewMenu();
                
            }


        }
    }
}
