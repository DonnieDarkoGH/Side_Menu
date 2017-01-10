using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using CustomMenu.Tools;
using ContextualMenu;


namespace CustomMenu.Events {

    [System.Serializable]
    public class ButtonManagerEvent : UnityEvent<ButtonModel, ButtonsManager.EButtonActionState> {

    }

    [System.Serializable]
    public class ActionEvent {
        public EContext    MenuContext;
        public string      ButtonSenderId;
        public UnityEvent  ButtonEvent;

        public ActionEvent(EContext _context, string _btnId) {
            MenuContext     = _context;
            ButtonSenderId  = _btnId;

            ButtonEvent = new UnityEvent();
            //ButtonEvent.SetPersistentListenerState(0, UnityEventCallState.RuntimeOnly);
        }

        public ActionEvent() : this(EContext.None, "&") { }
    }

    public class MenuEventManager : MonoBehaviour {

        public GameObject TargetForEventData;
        public List<ActionEvent> ActionEvents;

        [SerializeField]private Dictionary<string     , ButtonManagerEvent> eventDictionnary;
        [SerializeField]private Dictionary<ButtonModel, UnityEvent>         unityEventDictionnary;

        private static MenuEventManager instance;
        public  static MenuEventManager Instance {
            get {
                if (!instance) {
                    instance = FindObjectOfType<MenuEventManager>();

                    if (!instance) {
                        Debug.LogError("You need a MenuEventManager script in your scene !");
                    }
                    else {
                        instance.Init();
                    }
                }
                return instance;
            }
        }

        private void Init() {

            if( eventDictionnary == null) {
                eventDictionnary = new Dictionary<string, ButtonManagerEvent>();
            }

            if (unityEventDictionnary == null) {
                unityEventDictionnary = new Dictionary<ButtonModel, UnityEvent>();
            }

        }

        public static void StartListening(ButtonModel _btnModel, ButtonsManager.EButtonActionState _actionName, UnityAction<ButtonModel, ButtonsManager.EButtonActionState> _listener) {
            //Debug.Log("<b>MenuEventManager</b> StartListening to " + _btnModel.Id);
            ButtonManagerEvent btnModelEvent   = null;
            string             btnModelEventId = _actionName + _btnModel.Id;

            if (instance.eventDictionnary.TryGetValue(btnModelEventId, out btnModelEvent)) {
                btnModelEvent.AddListener(_listener);
            }
            else {

                btnModelEvent = new ButtonManagerEvent();
                btnModelEvent.AddListener(_listener);
                instance.eventDictionnary.Add(btnModelEventId, btnModelEvent);
            }

        }

        public static void StopListening(ButtonModel _btnModel, ButtonsManager.EButtonActionState _actionName, UnityAction<ButtonModel, ButtonsManager.EButtonActionState> _listener) {
            //Debug.Log("<b>MenuEventManager</b> StopListening to " + _btnModel.Id);

            if (instance == null)
                return;

            ButtonManagerEvent btnModelEvent   = null;
            string             btnModelEventId = _actionName + _btnModel.Id;

            if (instance.eventDictionnary.TryGetValue(btnModelEventId, out btnModelEvent)) {
                btnModelEvent.RemoveListener(_listener);
            }
        }

        public static void TriggerEvent(ButtonModel _btnModel, ButtonsManager.EButtonActionState _actionName) {

            ButtonManagerEvent btnModelEvent   = null;
            string             btnModelEventId = _actionName + _btnModel.Id;

            if (instance.eventDictionnary.TryGetValue(btnModelEventId, out btnModelEvent)) {
                btnModelEvent.Invoke(_btnModel, _actionName);
            }

        }

        public bool TryButtonAction(EContext _context, string _btnModelId) {
            //Debug.Log("<b>MenuEventManager</b> TryButtonAction " + _btnModelId + " in " + _context);

            UnityEvent btnEvent;
            if(GetActionEventIndex(_context, _btnModelId, out btnEvent) > 0) {
                btnEvent.Invoke();
                return true;
            }

            return false;
        }

        public UnityEvent GetUnityEvent(EContext _context, string _btnId) {
            //Debug.Log("<b>MenuEventManager</b> GetUnityEvent for button " + _id + " in " + _context);

            int len = ActionEvents.Count;
            for (int i = 0; i < len; i++) {
                if(ActionEvents[i].MenuContext.Equals(_context) && ActionEvents[i].ButtonSenderId.Equals(_btnId)) {
                    return ActionEvents[i].ButtonEvent;
                }
            }

            return ActionEvents[0].ButtonEvent;
        }

        public int GetActionEventIndex(EContext _context, string _btnId) {
            //Debug.Log("<b>MenuEventManager</b> GetUnityEvent for button " + _id + " in " + _context);

            UnityEvent btnEvent;
            return GetActionEventIndex(_context, _btnId, out btnEvent);
        }

        public int GetActionEventIndex(EContext _context, string _btnId, out UnityEvent _btnEvent) {
            //Debug.Log("<b>MenuEventManager</b> GetUnityEvent for button " + _id + " in " + _context);

            int len = ActionEvents.Count;
            for (int i = 0; i < len; i++) {
                if (ActionEvents[i].MenuContext.Equals(_context) && ActionEvents[i].ButtonSenderId.Equals(_btnId)) {
                    _btnEvent = ActionEvents[i].ButtonEvent;
                    return i;
                }
            }

            _btnEvent = new UnityEvent();
            return -1;
        }

        [ExecuteInEditMode]
        public void ModifyButtonEvent(EContext _context, string _btnId) {
            Debug.Log("<b>MenuEventManager</b> ModifyButtonEvent for " + _btnId + " in " + _context);

            UnityEvent btnEvent;
            int eventIndex = GetActionEventIndex(_context, _btnId, out btnEvent);
            //int         eventIndex  = GetUnityEventIndex(_menu, _btnModelIndex);
            //ButtonModel btnModel    = _menu.Buttons[_btnModelIndex];

            if (eventIndex == -1) {
                AddNewButtonEvent(_context, _btnId);
                ActionEvents[ActionEvents.Count - 1].ButtonEvent.AddListener(() => {; });
                return;
            }

            //Debug.Log(eventIndex);
            //ActionEvents[eventIndex]        = _eventProperty as System.Object as UnityEvent;
            //unityEventDictionnary[btnModel] = ActionEvents[eventIndex];


        }

        [ExecuteInEditMode]
        public void AddNewButtonEvent(EContext _context, string _btnId) {
            Debug.Log("<b>MenuEventManager</b> AddNewButtonEvent for " + _btnId + " in " + _context);

            ActionEvents.Add(new ActionEvent(_context, _btnId));
            ActionEvents[0] = new ActionEvent();
        }

        public void RemoveNewButtonEvent(int _index) {
            Debug.Log("<b>MenuEventManager</b> RemoveNewButtonEvent at " + _index);

            ActionEvents.RemoveAt(_index);
        }

    }

}
