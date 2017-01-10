using UnityEngine;
using UnityEditor;
using ContextualMenu;
using CustomMenu.Events;
using System.Reflection;


public class EventParameterWindow : EditorWindow {

    //GameObject   target = null;
    Component[]  components;
    string[]     componentsName;
    int          componentIndex;
    GUIContent   targetLabel = new GUIContent("Pick a GameObject");
    GUIStyle     textStyle = new GUIStyle("Popup");
    MethodInfo[] methodsInComponent;
    
    int          methodIndex;
    //bool isSetup = false;

    static void Init() {
        EventParameterWindow myWindow = EditorWindow.GetWindow<EventParameterWindow>();
        myWindow.InitParameters();
        myWindow.Show();
    }

    public void InitParameters(ButtonModel _btnModel = null) {
        Debug.Log("<b>EventParameterWindow</b> InitParameters");

        textStyle.fixedHeight = 20;
        textStyle.fontSize    = 12;
    }

    private void OnGUI() {

        EditorGUI.BeginChangeCheck();
        GameObject Target = (GameObject)EditorGUILayout.ObjectField(targetLabel, MenuEventManager.Instance.TargetForEventData, typeof(GameObject), true);
        if (EditorGUI.EndChangeCheck()) {
            MenuEventManager.Instance.TargetForEventData = Target;
        }

        components  = Target.GetComponents<Component>();
        int len     = components.Length;
        componentsName = new string[len];

        for (int i =0; i < len; i++) {
            componentsName[i] = components[i].name + "(" + components[i].GetType().ToString() + ")";
        }

        componentIndex      = EditorGUILayout.Popup("Select Component", componentIndex, componentsName, textStyle);

        methodsInComponent  = components[componentIndex].GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

        methodIndex         = EditorGUILayout.Popup("Select Callback", methodIndex, ListMethodsInComponents(methodsInComponent), textStyle);

    }

    private string[] ListMethodsInComponents(MethodInfo[] methodsInComponent) {

        int len              = methodsInComponent.Length;
        string[] methodsDesc = new string[len];

        string methName;
        string methParameter;
        string methReturnType;
        ParameterInfo[] parameters;

        for (int i = 0; i < len; i++) {
            methName = methodsInComponent[i].Name;
            methParameter = "(";

            methReturnType = "(" + methodsInComponent[i].ReturnType.Name + ") ";
            parameters = methodsInComponent[i].GetParameters();
            int paramCount = parameters.Length;
            string s;
            for (int j = 0; j < paramCount; j++) {
                s = j == 0 ? "" : ", ";
                methParameter += s + parameters[j].ParameterType.ToString();
            }
            methParameter += ")";

            methodsDesc[i] = methReturnType + methName + methParameter;
        }

        return methodsDesc;
    }

    private void OnDisable() {
        Debug.Log("<b>EventParameterWindow</b> OnDisable");
    }

}
