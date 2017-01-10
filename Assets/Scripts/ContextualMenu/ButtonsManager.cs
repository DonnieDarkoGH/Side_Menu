using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using ContextualMenuData;
using CustomMenu.Events;

namespace ContextualMenu {

    /// <summary>
    /// This MonoBehaviour class manages the states and the behaviour of ButtonModel instances of the list in the MenuManager.
    /// </summary>
    public class ButtonsManager : MonoBehaviour {

#region FIELDS
        /// <summary>
        /// This enum lists the possible states that described the action of a ButtonModelAction listener
        /// </summary>
        public enum EButtonActionState {
            NONE = 0,
            CLICKED,
            RELEASED,
            ANIMATION_STARTED,
            ANIMATION_ENDED,
            RETRACTING,
            EXPANDING,
            DESTROYED
        }

        /// <summary>
        /// This listener is used by the MenuEventManager to hook events related to the ButtonModels instances
        /// </summary>
        private UnityAction<ButtonModel, EButtonActionState> ButtonModelAction;

        /// <summary>
        /// This reference to the ScriptableMenuStructure data object will probably be replaced by another one in the MenuManager instance
        /// </summary>
        [SerializeField] private ScriptableMenuStructure menu;

        /// <summary>
        /// There can be only one active button in this version, but this will evolve with multitouch
        /// This reference is used in most methods of this class
        /// </summary>
        private ButtonModel   currentActiveButton = null;

        /// <summary>
        /// The ButtonModel children of the current active ButtonModel are mainly accessed this way when retracting the a sub-menu
        /// </summary>
        private ButtonModel[] currentSubButtons;

        /// <summary>
        /// Managing a Stack of the string Id that are active is better than a list in this case, because it ensures that we can't have a Button inactive if its children are active.
        /// </summary>
        private Stack<string> activePathIdInMenu  = new Stack<string>();
        
        /// <summary>
        /// Gett the level of the higher active button by returnin the number of elements in the stack
        /// </summary>
        public int CurrentLevel {
            get { return activePathIdInMenu.Count; }
        }

        /// <summary>
        /// Property of the private currentSubButtons field
        /// The ButtonModel children of the current active ButtonModel are mainly accessed this way when retracting the a sub-menu
        /// </summary>
        public ButtonModel[] CurrentSubButtons {
            get { return currentSubButtons; }
        }

        #endregion FIELDS

        #region Global Management METHODS

        /// <summary>
        /// Initialize the events sent by clicking on buttons by using the MenuEventManager and a reference to each one of the ButtonModel instances
        /// </summary>
        /// <param name="_menu">the ScriptableMenuStructure related to the current context</param>
        public void Init(ScriptableMenuStructure _menu) {
            //Debug.Log("<b>ButtonsManager</b> Init");

            if (_menu == null) {
                Debug.LogError("No context for menu");
                return;
            }

            menu = _menu;

            ResetAllButtons();

            if (ButtonModelAction == null) {
                ButtonModelAction = new UnityAction<ButtonModel, EButtonActionState>(HandleButtonAction);
            }

            // Each EButtonActionState corresponds to a given behaviour
            int len = menu.Buttons.Count;
            for (int i = 0; i < len; i++) {
                MenuEventManager.StartListening(menu.Buttons[i], EButtonActionState.CLICKED            , ButtonModelAction);
                //MenuEventManager.StartListening(menu.Buttons[i], EButtonActionState.ANIMATION_STARTED, ButtonModelAction);
                MenuEventManager.StartListening(menu.Buttons[i], EButtonActionState.ANIMATION_ENDED    , ButtonModelAction);
                MenuEventManager.StartListening(menu.Buttons[i], EButtonActionState.RETRACTING         , MenuManager.Instance.ButtonsManagerAction);
                MenuEventManager.StartListening(menu.Buttons[i], EButtonActionState.EXPANDING          , MenuManager.Instance.ButtonsManagerAction);

            }

            activePathIdInMenu.Clear();
        }

        /// <summary>
        /// Sets each ButtonModel of the MenuManager list to its default state
        /// </summary>
        /// <remarks>Useful when killing a menu</remarks>
        public void ResetAllButtons() {
            //Debug.Log("<b>ButtonsManager</b> ResetAllButtons");

            StopListening();

            int len = menu.Buttons.Count;
            for (int i = 0; i < len; i++) {
                menu.Buttons[i].Reset();
            }
        }

        /// <summary>
        /// Simple getter to get a reference to the first element of the Buttons list of the MenuManager
        /// </summary>
        /// <returns></returns>
        public ButtonModel GetRootButton() {
            //Debug.Log("<b>ButtonsManager</b> GetRootButton");

            return menu.Buttons[0];
        }

        /// <summary>
        /// Unsuscribes all the events by calling the adequate static method in MenuEventManager to avoid redundancy when creating new Contextual Menu
        /// </summary>
        /// <remarks>It's important to call this method from OnDisable and OnDestroy </remarks>
        private void StopListening() {
            //Debug.Log("<b>ButtonsManager</b> StopListening");
            int len = menu.Buttons.Count;
            for (int i = 0; i < len; i++) {
                MenuEventManager.StopListening(menu.Buttons[i], EButtonActionState.CLICKED, ButtonModelAction);
                //MenuEventManager.StopListening(menu.Buttons[i], EButtonActionState.ANIMATION_STARTED, ButtonModelAction);
                MenuEventManager.StopListening(menu.Buttons[i], EButtonActionState.ANIMATION_ENDED, ButtonModelAction);

                MenuEventManager.StopListening(menu.Buttons[i], EButtonActionState.RETRACTING, MenuManager.Instance.ButtonsManagerAction);
                MenuEventManager.StopListening(menu.Buttons[i], EButtonActionState.EXPANDING, MenuManager.Instance.ButtonsManagerAction);
            }

        }

#endregion Global Management METHODS

#region Event Handler METHODS

        /// <summary>
        /// This listener dispatch ButtonModel events according to the state argument of the event
        /// </summary>
        /// <param name="_btnModel">The ButtonModel sender of the event</param>
        /// <param name="_btnActionState">the argument that allows to choose the action</param>
        private void HandleButtonAction(ButtonModel _btnModel, EButtonActionState _btnActionState) {
            //Debug.Log("HandleButtonAction on " + _btnModel.Id + " : " + _btnActionState.ToString());

            switch (_btnActionState) {

                case EButtonActionState.CLICKED:
                    HandleClick(_btnModel);
                    break;

                case EButtonActionState.ANIMATION_ENDED:
                    HandleEndOfAnimation(_btnModel);
                    break;

                default:
                    break;
            }
        }

        /// <summary>
        /// This method is called by the HandleButtonAction() listener each time that a Button ends its animation, so we can go to the next sequence
        /// </summary>
        /// <param name="_btnModel">The ButtonModel whose animation ended</param>
        public void HandleEndOfAnimation(ButtonModel _btnModel) {
            //Debug.Log("<b>ButtonsManager</b> HandleEndOfAnimation for " + _btnModel.ToString());

            switch (_btnModel.State) {

                case ButtonModel.EState.RETRACTING:
                    _btnModel.Reset();
                    // When a submenu if retracting because of another one unfolding, we first retract the other and then call SwitchMenu to go on
                    if (currentActiveButton.State == ButtonModel.EState.FOLDING || currentActiveButton.State == ButtonModel.EState.UNFOLDING) {
                        SwitchMenu();
                    }
                    break;

                case ButtonModel.EState.DEPLOYING:
                    _btnModel.State = ButtonModel.EState.IN_PLACE;

                    if (!activePathIdInMenu.Contains(currentActiveButton.Id)) {
                        activePathIdInMenu.Push(currentActiveButton.Id);
                    }
                    break;

                default:
                    break;
            }

            // Once all animations ended, the currentActiveButton state is switching to IN_PLACE
            if (!IsAnimationProcessing() && currentActiveButton != null) {
                currentActiveButton.State = ButtonModel.EState.IN_PLACE;
            }

            // Now we can click and raycast a gameObject
            MenuManager.Instance.isInputInButton = false;
        }

        /// <summary>
        /// This method is called by the HandleButtonAction() each time a ButtonModel gameobject is clicked
        /// </summary>
        /// <param name="_btnModel">The ButtonModel that is clicked</param>
        private void HandleClick(ButtonModel _btnModel) {
            //Debug.Log("<b>ButtonsManager</b> HandleClick on " + _btnModel.ToString());

            if (IsAnimationProcessing()) // Wait till the end of the current animation
                return;

            currentActiveButton = _btnModel;

            // Change the state of the button (that is now the current active button)
            if (activePathIdInMenu.Contains(_btnModel.Id)) {
                _btnModel.State = ButtonModel.EState.FOLDING;
            }
            else {
                _btnModel.State = ButtonModel.EState.UNFOLDING;
            }
            // then launch the sequence
            SwitchMenu();

            // After that, we trigger the Button Action that affects the gameplay and the other objects
            MenuEventManager.Instance.TryButtonAction(menu.Context, _btnModel.Id);

        }

        #endregion Event Handler METHODS


        #region Sub-Menu Management METHODS

        /// <summary>
        /// Depending of the states of the currentActiveButton, this method retracts the sub-menu or expands them
        /// </summary>
        private void SwitchMenu() {
            //Debug.Log("<b>ButtonsManager</b> SwitchMenu");

            if (currentActiveButton == null)
                return;

            if (currentActiveButton.GetLevel() < activePathIdInMenu.Count) {
                RetractSubMenus();
            }
            else {
                if (currentActiveButton.State == ButtonModel.EState.UNFOLDING) {
                    UnfoldMenu();
                    activePathIdInMenu.Push(currentActiveButton.Id);
                }
                currentActiveButton.State = ButtonModel.EState.IN_PLACE;
            }

        }

        /// <summary>
        /// This method retract all sub-menus of the current active button
        /// </summary>
        /// <returns>True when its done</returns>
        private bool RetractSubMenus() {
            //Debug.Log("<b>ButtonsManager</b> RetractSubMenus");

            ButtonModel btnModel = menu.GetButtonfromId(activePathIdInMenu.Pop());
            currentSubButtons    = menu.GetChildren(btnModel);

            if(currentSubButtons.Length > 0) {
                MenuEventManager.TriggerEvent(btnModel, EButtonActionState.RETRACTING);
            }
            else {
                btnModel.State = ButtonModel.EState.IN_PLACE;
                SwitchMenu();
            }

            return true;
        }

        /// <summary>
        /// Triggers the event that will expands the sub-meu
        /// Currently the job in itself is done by the MenuManager instance
        /// </summary>
        private void UnfoldMenu() {
            //Debug.Log("<b>ButtonsManager</b> UnfoldMenu from " + _btnModel.ToString());

            currentSubButtons = menu.GetChildren(currentActiveButton);

            if (currentSubButtons.Length > 0) {
                    MenuEventManager.TriggerEvent(currentActiveButton, EButtonActionState.EXPANDING);
            }
        }

        /// <summary>
        /// Checks if one of the elements in the Buttons list of the menu manager is in an animation process
        /// </summary>
        /// <returns>True if at least one button is in an animation process</returns>
        private bool IsAnimationProcessing() {
            //Debug.Log("<b>ButtonsManager</b> IsAnimationProcessing");

            bool isProcessing = false;

            int len = menu.Buttons.Count;
            for (int i = 0; i < len; i++) {
                isProcessing = isProcessing || menu.Buttons[i].IsMoving;
            }

            //Debug.Log("IsAnimationProcessing : " + isProcessing);
            return isProcessing;
        }

#endregion Sub-Menu Management METHODS

#region private METHODS from Monobehaviour

        /// <summary>
        /// Standard callback from the Monobehaviour base class
        /// </summary>
        private void OnDisable() {
            //Debug.Log("<b>ButtonsManager</b> OnDisable");
            StopListening();
        }

        /// <summary>
        /// Standard callback from the Monobehaviour base class
        /// </summary>
        private void OnDestroy() {
            //Debug.Log("<b>ButtonsManager</b> OnDestroy");
            StopListening();
        }

#endregion private METHODS from Monobehaviour

    }
}
