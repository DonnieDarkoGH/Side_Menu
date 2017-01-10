using UnityEngine;
using ContextualMenuData;
using CustomMenu.Tools;

namespace ContextualMenu {
    
    /// <summary>
    /// This enum can have as many values needed in the gameplay
    /// This is where designers should define their own Context
    /// </summary>
    //public enum EContext {
    //    None = 0,
    //    Pawn,
    //    Board,
    //    Object,
    //    Other,
    //}

    /// <summary>
    /// This Interface can be implemented by any behaviour that can produce a Contextual menu when selected
    /// </summary>
    public interface ISelectable {

        /// <summary>
        /// The results of the selection (generally initializes a new contextual menu) 
        /// </summary>
        /// <param name="inputPosition">The position in world space where the raycast hit the object</param>
        void       HandleSelection(Vector3 inputPosition);

        /// <summary>
        /// Returns the gameObject whose monobehaviour implements the interface
        /// </summary>
        /// <returns></returns>
        GameObject GetGameObject();
    }

    /// <summary>
    /// Example base component for a Selectable object
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Selectable : MonoBehaviour, ISelectable {

        /// <summary>
        /// The name of the context corresponding to a ScriptableMenuStructure (that is made by using the custom editor)
        /// </summary>
        public EContext context;

        /// <summary>
        /// Initialize a new Contextual menu corresponding to the EContext value of this instance, by using the MenuManager singleton instance
        /// </summary>
        /// <param name="_inputPosition">The position in world space where the raycast hit the object</param>
        public void HandleSelection(Vector3 _inputPosition) {
            //Debug.Log("<b>Selectable</b> HandleSelection in " + _inputPosition);

            // Note that this instruction only returns the Scriptable Object that are loaded, and that the loading is done in the Awake method of the MenuManager
            ScriptableMenuStructure[] menus = Resources.FindObjectsOfTypeAll<ScriptableMenuStructure>();

            int len = menus.Length;
            for(int i = 0; i < len; i++) {
                if (menus[i].Context == context) {
                    // If several ScriptableMenuStructure have the same Context value, it will choose only the first one
                    MenuManager.Instance.InitializeMenuContext(menus[i]); 
                    return;
                }
            }

        }

        /// <summary>
        /// Returns the gameObject whose monobehaviour implements the interface
        /// </summary>
        /// <returns>The GameObject that get this component</returns>
        public GameObject GetGameObject() {
            return gameObject;
        }

    }
}
