using UnityEngine;
using UnityEngine.Events;
using ContextualMenuData;
using CustomMenu.Events;

namespace ContextualMenu {
    /// <summary>
    /// This class create a singleton instance to manage menu behaviours, based on the data stored in a ScriptableMenuStructure
    /// </summary>
    public class MenuManager : MonoBehaviour {

#region private Fields
        /// <summary>
        /// This enum value determines the kind of calculation to tween the animations of the buttons in the method TweenPosition()
        /// </summary>
        enum ETweenMode { LINEAR, EASE_IN, CURVE }
        [SerializeField] private ETweenMode TweeningMode;

        /// <summary>
        /// This delegate invokes HandleButtonsManagerAction() when a button is clicked in order to expand or retract a sub-menu
        /// </summary>
        /// <remarks>
        /// Event listeners are mapped with ButtonModel instances thanks to the EventMenuManager but the mapping itsel is done in the ButtonsManager class
        /// </remarks>
        public UnityAction<ButtonModel, ButtonsManager.EButtonActionState> ButtonsManagerAction;

        /// <summary>
        /// This boolean prevents the raycasting in the InputManager when the user is clicking on a button
        /// </summary>
        public bool isInputInButton = false;

        // TO DO : Use this reference instead of the one in the ButtonManager
        //[SerializeField] private ScriptableMenuStructure menu;

        /// <summary>
        /// We keep a reference on the ButtonsManager instance that handle ButtonModel instances
        /// </summary>
        [SerializeField] private ButtonsManager    buttonsManagerRef = null;

        /// <summary>
        /// This prefab is teh one used to instantiate UI Button gameobjects
        /// </summary>
        [SerializeField] private GameObject        buttonPrefab      = null;
        
        /// <summary>
        /// Spacing factor between a button and its children when they finish expanding
        /// </summary>
        [SerializeField] [Range(1.0f, 5.0f)]  private float spacing     = 1.5f;

        /// <summary>
        /// Speed factor for the animation of the moving buttons
        /// </summary>
        [SerializeField] [Range(1.0f, 10.0f)] private float tweenSpeed  = 3.5f;

        /// <summary>
        /// When TweeningMode == ETweenMode.CURVE, we are using this animation curve that can be customized for better results
        /// </summary>
        [SerializeField] private AnimationCurve TweeningCurve = AnimationCurve.Linear(0.0f, 0.0f, 1.0f,1.0f);

        /// <summary>
        /// Just used to avoid killing a menu when it is not used
        /// </summary>
        private bool isInUse = false;

#endregion private Fields

#region Singleton
        /// <summary>
        /// Instance of the class GameManager - Singleton pattern
        /// </summary>
        /// <value>The Instance of the class</value>
        private static MenuManager instance;

        public static MenuManager Instance {
            get {
                if (!instance) {
                    instance = FindObjectOfType<MenuManager>();

                    if (!instance) {
                        Debug.LogError("A MenuManager script is needed in the scene !");
                    }
                }

                return instance;
            }
        }
#endregion Singleton

#region Properties
        public float Spacing {
            get {
                if(buttonsManagerRef == null || buttonsManagerRef.CurrentLevel > 0) {
                    return spacing;
                }
                else {
                    return spacing * 0.8f; // Spacing between a button and its children is reduced starting from the second level
                }
            }
        }

        public float TweenSpeed {
            get {
                return tweenSpeed;
            }
        }
#endregion Properties

#region Initialization Methods
        /// <summary>
        /// Standard callback method from the Monobehaviour class. 
        /// </summary>
        /// <remarks>
        /// Loads all ScriptableMenuStructure data object because they're loaded only when accessed 
        /// </remarks>
        private void Awake() {
            //Debug.Log("<b>MenuManager</b> Awake");

            Resources.LoadAll<ScriptableMenuStructure>("");
            
            // Initialization of the singleton Instance
            if (instance == null) {
                instance = this;
            }
        }

        /// <summary>
        /// Called by Selectable instances when their gameobject is clicked.
        /// The ScriptableMenuStructure param
        /// </summary>
        /// <param name="_menu">The ScriptableMenuStructure that contains all the structure of the contextual menu related to the Selectable instance that called this method</param>
        public void InitializeMenuContext(ScriptableMenuStructure _menu) {
            //Debug.Log("<b>MenuManager</b> InitializeMenuContext");

            // Singleton can be initialized here if the access is needed before Awake() is invoked
            if (instance == null) {
                instance = this;
            }

            // Uncomment when the reference to the menu is kept here
            //menu = _menu; 

            if (MenuEventManager.Instance) { }; // Force initialization of the MenuEventManager singleton

            // Listener for the ButtonsManagerAction that are declared in the ButtonManager class
            if (ButtonsManagerAction == null) {
                ButtonsManagerAction = new UnityAction<ButtonModel, ButtonsManager.EButtonActionState>(HandleButtonsManagerAction);
            }

            // The ButtonManager instance is managing the behaviour of ButtonModel instances
            if (buttonsManagerRef == null) {
                buttonsManagerRef = GetComponent<ButtonsManager>();
            }
            buttonsManagerRef.Init(_menu);

            // This creates the first UI button
            GameObject newBtnGo = Instantiate(buttonPrefab, Vector3.zero, Quaternion.identity, transform) as GameObject;
            PieButton  newBtn   = newBtnGo.GetComponent<PieButton>();

            if (newBtn != null) { // and then it expands its children
                newBtn.GetComponent<RectTransform>().anchoredPosition = Vector3.zero;
                newBtn.Init(_btnModel: buttonsManagerRef.GetRootButton(), _angularPos: 0, _isLinked: false);
                newBtn.HandleClick(); // as if it was clicked
            }

            isInUse = true; // That means this menu can be killed if the KillMenu method is invoked
        }

#endregion Initialization Methods

        /// <summary>
        /// Destroy all current buttons and reset values
        /// </summary>
        public void KillMenu() {
            //Debug.Log("<b>MenuManager</b> KillMenu");

            if (!isInUse)
                return;

            buttonsManagerRef.ResetAllButtons();

            foreach(var pieBtn in GetComponentsInChildren<PieButton>()) {
                GameObject go = pieBtn.gameObject;
                Destroy(go);
            }

            isInUse = false;
        }

        /// <summary>
        /// Creates a new ScriptableMenuStructure to hold data of a Contextual Menu
        /// </summary>
        public void CreateNewMenu() {
            //Debug.Log("<b>MenuManager</b> CreateNewMenu");

            ScriptableMenuStructure.CreateInstance("ScriptableMenuStructure");
        }

        /// <summary>
        /// Handle the ButtonsManagerAction that are declared in the ButtonManager class depending of the EButtonActionState of the sender.
        /// This listener only triggers animations, not the effective action of the button
        /// </summary>
        /// <param name="_btnModel">The ButtonModel component of the UI Button that sent the event</param>
        /// <param name="_btnStateAction">The State of this ButtonModel component</param>
        private void HandleButtonsManagerAction(ButtonModel _btnModel, ButtonsManager.EButtonActionState _btnStateAction) {
            //Debug.Log("<b>MenuManager</b> HandleButtonsManagerAction of " + _btnModel.ToString() + "(" + _btnStateAction + ")");

            switch (_btnStateAction) {

                case ButtonsManager.EButtonActionState.RETRACTING:
                    FoldUpMenu(_btnModel, buttonsManagerRef.CurrentSubButtons);
                    break;

                case ButtonsManager.EButtonActionState.EXPANDING:
                    UnfoldMenu(_btnModel, buttonsManagerRef.CurrentSubButtons);
                    break;

                default:
                    break;
            }

        }

        /// <summary>
        /// Calculate the positions of a button children and create them in order to expand a sub menu
        /// </summary>
        /// <param name="_button">The ButtonModel that was clicked</param>
        /// <param name="_subButtons">The ButtonModel children as defined by the Tree Node hierarchy</param>
        private void UnfoldMenu(ButtonModel _button, ButtonModel[] _subButtons) {
            //Debug.Log("<b>MenuManager</b> UnfoldMenu from " + _button.ToString());

            int len = _subButtons.Length;
            if (len == 0) // useless if there's no children
                return;

            // Position of a sub button is based on the orientation of its parent
            float   baseAngle  = _button.BaseAngle;
            Vector3 startPoint = _button.EndPoint;
            float   angularPos = 0.0f;

            for (int i = 0; i < len; i++) {
                angularPos = GetAngleByIndex(i, len, baseAngle);
                _subButtons[i].State = ButtonModel.EState.DEPLOYING;
                // Create the sub button at the position of its parents before it moves
                CreateButtonObject(_subButtons[i], startPoint, angularPos);
            }
        }

        /// <summary>
        /// Launches the retractations a group of buttons. Used when clicking on a button whose children are already expanded.
        /// </summary>
        /// <param name="_button">Not used at this point</param>
        /// <param name="_subButtons">The ButtonModel instances that are to be retracted</param>
        /// <remarks>This method doesn't really launch the animation in itself, it triggers the event that will do it</remarks>
        private void FoldUpMenu(ButtonModel _button, ButtonModel[] _subButtons) {
            //Debug.Log("<b>MenuManager</b> FoldUpMenu to " + _button.ToString());

            int len = _subButtons.Length;
            for (int i = 0; i < len; i++) {

                if(_subButtons[i].State != ButtonModel.EState.IN_PLACE) {
                    continue;
                }

                _subButtons[i].State = ButtonModel.EState.RETRACTING;
                MenuEventManager.TriggerEvent(_subButtons[i], ButtonsManager.EButtonActionState.ANIMATION_STARTED);
            }
        }

        /// <summary>
        /// Instantiate a UI Button and initialize its components (RectTransfom and PieButton)
        /// </summary>
        /// <param name="_button">The ButtonModel that contains the data needed to initialize the other components</param>
        /// <param name="_startPoint">The position where it is created</param>
        /// <param name="_angularPos">The orientation of its parent</param>
        /// <param name="_idToIgnore">Not used at this point</param>
        private void CreateButtonObject(ButtonModel _button, Vector3 _startPoint, float _angularPos, string _idToIgnore = "&") {
            //Debug.Log("<b>MenuManager</b> CreateSubButtons from " + _node.Id);

            if (_button.Id.Equals(_idToIgnore))
                return;

            _button.BaseAngle = _angularPos;

            GameObject newBtn = Instantiate(buttonPrefab, Vector3.zero, Quaternion.identity) as GameObject;

            newBtn.gameObject.name = "btn " + _button.Id;
            newBtn.gameObject.transform.SetParent(transform);
            newBtn.GetComponent<RectTransform>().anchoredPosition = _startPoint;

            PieButton btnComp = newBtn.GetComponent<PieButton>();
            btnComp.Init(_button, _angularPos, true);
        }

        #region Calculation methods

        /// <summary>
        /// Interpolates a Vector3 position with 2 points and a delta time ratio (between 0 and 1)
        /// Choses between 1 of 3 tweening modes (see TweeningMode enum field) and call the appropriate method
        /// </summary>
        /// <param name="_startPoint">The starting Vector3 position of the animation</param>
        /// <param name="_targetPoint">The ending Vector3 position</param>
        /// <param name="_delta">a delta time used to interpolate the current position</param>
        /// <returns>the current Vector3 position resulting of the calculation</returns>
        public Vector3 TweenPosition(Vector3 _startPoint, Vector3 _targetPoint, float _delta) {

            Vector3 movingPosition = Vector3.zero;

            switch (TweeningMode) {
                case ETweenMode.LINEAR:
                    movingPosition = Linear(_startPoint, _targetPoint, _delta);
                    break;

                case ETweenMode.EASE_IN:
                    movingPosition = EaseIn(_startPoint, _targetPoint, _delta);
                    break;

                case ETweenMode.CURVE:
                    movingPosition = Curve(_startPoint, _targetPoint, _delta);
                    break;

                default:
                    break;
            }

            return movingPosition;
        }

        /// <summary>
        /// Simple linear interpolation of a Vector3 position
        /// </summary>
        /// <param name="_startPoint">The starting Vector3 position of the animation</param>
        /// <param name="_targetPoint">The ending Vector3 position</param>
        /// <param name="_delta">a delta time used to interpolate the current position</param>
        /// <returns>the current Vector3 position resulting of the calculation</returns>
        private Vector3 Linear(Vector3 _startPoint, Vector3 _targetPoint, float _delta) {

            return Vector3.Lerp(_startPoint, _targetPoint, _delta);
        }


        /// <summary>
        /// Simple squared interpolation of a Vector3 position
        /// </summary>
        /// <param name="_startPoint">The starting Vector3 position of the animation</param>
        /// <param name="_targetPoint">The ending Vector3 position</param>
        /// <param name="_delta">a delta time used to interpolate the current position</param>
        /// <returns>the current Vector3 position resulting of the calculation</returns>
        private Vector3 EaseIn(Vector3 _startPoint, Vector3 _targetPoint, float _delta) {

            return Vector3.Lerp(_startPoint, _targetPoint, _delta * _delta);
        }


        /// <summary>
        /// Interpolation of a Vector3 position based on a curve as defined by the TweeningCurve Animation Curve
        /// </summary>
        /// <param name="_startPoint">The starting Vector3 position of the animation</param>
        /// <param name="_targetPoint">The ending Vector3 position</param>
        /// <param name="_delta">a delta time used to interpolate the current position</param>
        /// <returns>the current Vector3 position resulting of the calculation</returns>
        private Vector3 Curve(Vector3 _startPoint, Vector3 _targetPoint, float _delta) {

            return (_targetPoint - _startPoint) * TweeningCurve.Evaluate(_delta) + _startPoint;
        }


        /// <summary>
        /// Calculate the absolute orientation that must be given to a child button, depending of its place amongst the other children, 
        /// the number of sub-buttons and the orientation of the parent button
        /// </summary>
        /// <param name="_index">Relative index of the child button in the ButtonModel Array of its parent</param>
        /// <param name="_btnNumber">The total number of child buttons</param>
        /// <param name="_baseAngle">The absolute orientation of the parent button</param>
        /// <returns>The absolute orientation that must be given to a child button</returns>
        private float GetAngleByIndex(int _index, int _btnNumber, float _baseAngle) {
            //Debug.Log("<b>MenuManager</b> GetAngleByIndex for " + _btnNumber + " buttons");

            float baseInc = buttonsManagerRef.CurrentLevel > 0 ? 30.0f : 45.0f;

            // Angle value is 45° for up to 7 buttons, then it is divided by 2 for each full range of 8 buttons
            float angularInc = baseInc / (1 + _btnNumber / 8);
            // 1st index is in line with the parent, then right, then left and so on... 
            int sign = _index % 2 == 0 ? -1 : 1;
            int inc = Mathf.CeilToInt(_index * 0.5f) * sign;

            return inc * angularInc + _baseAngle;
        }

        #endregion Calculation Methods

    }

}
