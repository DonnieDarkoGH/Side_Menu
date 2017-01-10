using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace ContextualMenu {

    /// <summary>
    /// This class holds the states of a UI button instance that is managed by a ButtonManager instance
    /// It is used a a field in the PieButton class in order to manage the behaviour of the GUI object
    /// </summary>
    [System.Serializable]
    public class ButtonModel {

#region Public FIELDS

        /// <summary>
        /// Possible states of a ButtonModel : Unfolding and Folding is only possible when it has children
        /// </summary>
        public enum EState { CREATED = 0, INITIALIZED, DEPLOYING, IN_PLACE, UNFOLDING, FOLDING, RETRACTING }

        /// <summary>
        /// Not really useful at this point, but it will be in future versions
        /// </summary>
        public string name = "New menu level ";

        /// <summary>
        /// The Sprite that will appear on screen and in the custom inspector
        /// </summary>
        public Sprite Icon;

#endregion Public FIELDS

#region Private FIELDS
        
        /// <summary>
        /// This id is of equal value to the one of the related SerializableNode in the data structure
        /// Its construction is enough to describe the position, level, parents and children in a UTF-8 managed system
        /// </summary>
        [SerializeField] private string id;

        /// <summary>
        /// This index refers to the ActionEvents list in the MenuEventManager. When activating this button, we triggers the UnityEvent at this index.
        /// </summary>
        [SerializeField] private int relatedEventIndex = -1;

        /// <summary>
        /// By managing ButtonModel states like in a finished state machine, we ensure that the beahviour is stable (we must complete a state before entering a new one)
        /// </summary>
        private EState  state         = EState.CREATED;

        /// <summary>
        /// Position on screen when the UI Button is first created, and it reverts to this position when it retracts
        /// </summary>
        private Vector3 originPoint   = Vector3.zero;

        /// <summary>
        /// Position on screen where the UI Button is moving when it is expanding from its parent
        /// </summary>
        private Vector3 endPoint      = Vector3.zero;

        /// <summary>
        /// Angular position relative to a vertical position in the Canvas
        /// </summary>
        private float   baseAngle     = 0.0f;

#endregion Private FIELDS

#region PROPERTIES

        /// <summary>
        /// This id is of equal value to the one of the related SerializableNode in the data structure
        /// Its construction is enough to describe the position, level, parents and children in a UTF-8 managed system
        /// </summary>
        public string Id {
            get { return id; }
            set { id = value; }
        }

        /// <summary>
        /// This index refers to the ActionEvents list in the MenuEventManager.
        /// When activating this button, we triggers the UnityEvent at this index.
        /// </summary>
        public int RelatedEventIndex {
            get { return relatedEventIndex; }
            set { relatedEventIndex = value; }
        }

        /// <summary>
        /// By managing ButtonModel states like in a finished state machine, we ensure that the beahviour is stable (we must complete a state before entering a new one)
        /// </summary>
        public EState State {
            get { return state; }
            set { state = value; }
        }

        /// <summary>
        /// Position on screen when the UI Button is first created, and it reverts to this position when it retracts
        /// </summary>
        public Vector3 OriginPoint {
            get { return originPoint; }
            set { originPoint = value; }
        }

        /// <summary>
        /// Position on screen where the UI Button is moving when it is expanding from its parent
        /// </summary>
        public Vector3 EndPoint {
            get { return endPoint; }
            set { endPoint = value; }
        }

        /// <summary>
        /// Angular position relative to a vertical position in the Canvas
        /// </summary>
        public float BaseAngle {
            get { return baseAngle; }
            set { baseAngle = value; }
        }

        /// <summary>
        /// Quick access to the root button using its Id
        /// </summary>
        public bool IsRoot {
            get { return id.Equals("@"); }
        }

        /// <summary>
        /// a moving buttons cannot received event that launch a new animation
        /// </summary>
        public bool IsMoving {
            get { return state == EState.RETRACTING || state == EState.DEPLOYING; }
        }

#endregion PROPERTIES

#region CONSTRUCTOR

        /// <summary>
        /// The ButtonModel Constructor produces a different instance for the root button
        /// </summary>
        /// <param name="_id">The string Id given by the related node</param>
        /// <param name="_name">The (optional) string name of the button</param>
        public ButtonModel(string _id, string _name = "") {
            id    = _id;

            if (string.IsNullOrEmpty(_name)) {
                name = "New menu level " + GetLevel();
            }
            else {
                name = _name;
            }
            
            if (IsRoot) {
                state = EState.IN_PLACE;
            }
            else {
                state = EState.INITIALIZED;
            }
           
        }

#endregion CONSTRUCTOR

#region Public METHODS

        /// <summary>
        /// Returns the model values in a default state
        /// </summary>
        /// <remarks>Generaly called by the ButtonManager instance</remarks>
        public void Reset() {
            if (IsRoot) {
                state         = EState.IN_PLACE;
            }
            else {
                state         = EState.CREATED;
            }

            originPoint     = Vector3.zero;
            endPoint        = Vector3.zero;
        }

        /// <summary>
        /// Quick access to the button model in the hierarchy by using its Id
        /// </summary>
        /// <returns>The level in the hierarchy (0 for Root level, 1 for its children and so on...)</returns>
        /// <remarks>TO DO : Replace this method by a getter in ScriptableMenuStructure </remarks>
        public byte GetLevel() {
            return (byte)(id.Length - 1);
        }

        /// <summary>
        /// Quick way of checking if this ButtonModel instance is a child of another one
        /// </summary>
        /// <param name="_id">The string Id of the object that could be the parent (could be a Node Id)</param>
        /// <returns>True if this ButtonModel instance is a child</returns>
        /// <remarks>Not used currently because it relies on string values</remarks>
        public bool IsChildOf(string _id) {
            return id.Contains(_id) && !id.Equals(_id);
        }

        /// <summary>
        /// Reaffect a target point used in an animation
        /// </summary>
        /// <param name="_targetPoint">The new end point</param>
        /// <remarks></remarks>
        public void SetEndPoint(Vector3 _targetPoint) {
            endPoint = _targetPoint;
        }

        /// <summary>
        /// Overriden version of the based ToString() method
        /// </summary>
        /// <returns>informations about Id, level, parentID and State of the ButtonModel</returns>
        public override string ToString() {
            return GetType() + " (ID : " + Id + ", Level : " + (Id.Length - 1) + ", parentID : " + Id.Substring(0, Id.Length - 1) + ", State : " + state + ")";
        }

#endregion Public METHODS
    }
}
