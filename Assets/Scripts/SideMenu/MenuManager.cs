using UnityEngine;
using UnityEngine.UI;
using CustomMenu.Tools;
using ContextualMenuData;
using CustomMenu.Events;

namespace CustomMenu.SideMenu {
    /// <summary>
    /// This class create a singleton instance to manage menu behaviours, based on the data stored in a ScriptableMenuStructure
    /// </summary>
    public class MenuManager : MonoBehaviour {

        #region private Fields

        /// <summary>
        /// This enum value determines the kind of calculation to tween the animations of the buttons in the method TweenPosition()
        /// </summary>
        [SerializeField]
        private EContext Context;


        /// <summary>
        /// This enum value determines the kind of calculation to tween the animations of the buttons in the method TweenPosition()
        /// </summary>
        [SerializeField]
        private ETweenMode TweeningMode;


        /// <summary>
        /// Speed factor for the animation of the moving buttons
        /// </summary>
        [SerializeField]
        [Range(1.0f, 10.0f)]
        private float tweenSpeed = 3.5f;

        /// <summary>
        /// When TweeningMode == ETweenMode.CURVE, we are using this animation curve that can be customized for better results
        /// </summary>
        [SerializeField]
        private AnimationCurve TweeningCurve = AnimationCurve.Linear(0.0f, 0.0f, 1.0f, 1.0f);

        /// <summary>
        /// The prefab used to instantiate buttons in the side panel
        /// </summary>
        [SerializeField]
        private GameObject buttonPrefab;

        private GameObject buttonsContainer;

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

        public float TweenSpeed {
            get { return tweenSpeed; }
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
            Debug.Log("<b>MenuManager</b> Awake");

            Resources.LoadAll<ScriptableMenuStructure>("");

            // Initialization of the singleton Instance
            if (instance == null) {
                instance = this;
            }

            ScriptableMenuStructure[] menus = Resources.FindObjectsOfTypeAll<ScriptableMenuStructure>();

            int len = menus.Length;
            for (int i = 0; i < len; i++) {
                Debug.Log(menus[i].Context);
                if (menus[i].Context == Context) {
                    // If several ScriptableMenuStructure have the same Context value, it will choose only the first one
                    InitializeMenuContext(menus[i]);
                    return;
                }
            }
        }

        /// <summary>
        /// Called by Selectable instances when their gameobject is clicked.
        /// The ScriptableMenuStructure param
        /// </summary>
        /// <param name="_menu">The ScriptableMenuStructure that contains all the structure of the contextual menu related to the Selectable instance that called this method</param>
        public void InitializeMenuContext(ScriptableMenuStructure _menu) {
            Debug.Log("<b>MenuManager</b> InitializeMenuContext");

            // Singleton can be initialized here if the access is needed before Awake() is invoked
            if (instance == null) {
                instance = this;
            }

            // Uncomment when the reference to the menu is kept here
            //menu = _menu; 

            if (MenuEventManager.Instance) { }; // Force initialization of the MenuEventManager singleton

            buttonsContainer = GameObject.FindGameObjectWithTag("SideButtonContainer");

            ContextualMenu.ButtonModel[] buttons  = _menu.GetChildren(_menu.Buttons[0]);

            foreach(var btn in buttons) {
                CreateButton(btn);
            }

        }

            #endregion Initialization Methods

        private void CreateButton(ContextualMenu.ButtonModel _btnModel) {

            GameObject go = Instantiate(buttonPrefab, buttonsContainer.transform);
            go.GetComponentsInChildren<Image>()[1].sprite = _btnModel.Icon;

        }

    }
}
