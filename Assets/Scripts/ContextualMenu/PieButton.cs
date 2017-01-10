using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections;
using CustomMenu.Events;


namespace ContextualMenu {

    /// <summary>
    /// 
    /// </summary>
    public class PieButton : MonoBehaviour {
        /// <summary>
        /// 
        /// </summary>
        private UnityAction<ButtonModel, ButtonsManager.EButtonActionState> ButtonModelAction;
        /// <summary>
        /// 
        /// </summary>
        [SerializeField] private ButtonModel   btnModel;
        /// <summary>
        /// 
        /// </summary>
        [SerializeField] private RectTransform linkRef = null;
        /// <summary>
        /// 
        /// </summary>
        [SerializeField] private Image         icon = null;

        private Vector3 movingPosition  = Vector3.zero;
        private Vector3 origin          = Vector3.zero;
        private Vector3 startPoint      = Vector3.zero;
        private Vector3 targetPoint     = Vector3.zero;

        private RectTransform rectTransform;

        private float width     = 0;
        private float height    = 0;
        private float baseAngle = 0.0f;
        private float startTime = 0;
        private float distance  = 0.0f;
        private float distanceMini    = 0.0f;
        private bool  isLinkRetracted = false;

        public float BaseAngle {
            get { return baseAngle; }
        }

        public ButtonModel BtnModel {
            get { return btnModel; }
        }

        public Vector3 StartPoint {
            get {
                return startPoint;
            }

            set {
                startPoint = value;
            }
        }

        public Vector3 TargetPoint {
            get {
                return targetPoint;
            }

            set {
                targetPoint = value;
            }
        }

        // Use this for initialization
        public void Init(ButtonModel _btnModel, float _angularPos, bool _isLinked = false) {
            //Debug.Log("<b>PieButton</b> Init from model : " + _btnModel.ToString());

            if (ButtonModelAction == null) {
                ButtonModelAction = new UnityAction<ButtonModel, ButtonsManager.EButtonActionState>(StartAnimation);
            }

            btnModel = _btnModel;
            MenuEventManager.StartListening(btnModel, ButtonsManager.EButtonActionState.ANIMATION_STARTED, ButtonModelAction);

            if (rectTransform == null) {
                rectTransform = GetComponent<RectTransform>();
            }
            rectTransform.localScale = Vector3.one;

            if (icon == null) {
                icon = GetComponentsInChildren<Image>()[2];
            }
            if (btnModel.Icon) {
                icon.sprite = btnModel.Icon;
            }
                
            width     = rectTransform.rect.width;
            height    = rectTransform.rect.height;
            baseAngle = _angularPos;

            btnModel.OriginPoint = rectTransform.anchoredPosition;

            CalculateTrajectory();

            if (_isLinked) {
                CreateLink(_angularPos);
            }

            MenuEventManager.TriggerEvent(btnModel, ButtonsManager.EButtonActionState.ANIMATION_STARTED);
        }

        IEnumerator MoveButton(Vector3 _start, Vector3 _end) {

            while (TimeRatio() < 1) 
            {
                movingPosition = MenuManager.Instance.TweenPosition(_start, _end, TimeRatio());
                rectTransform.anchoredPosition = movingPosition;

                if (!isLinkRetracted)
                    UpdateLink();

                yield return null;
            }

            movingPosition                 = _end;
            rectTransform.anchoredPosition = movingPosition;

            Vector3 tmp = startPoint;
            startPoint  = targetPoint;
            targetPoint = tmp;

            isLinkRetracted = false;
            UpdateLink();

            if (btnModel.State == ButtonModel.EState.RETRACTING) {
                Destroy(gameObject.gameObject);
            }

            MenuEventManager.TriggerEvent(btnModel, ButtonsManager.EButtonActionState.ANIMATION_ENDED);

        }

        public void HandleClick() {
            //Debug.Log("<b>PieButton</b> HandleClick");

            MenuManager.Instance.isInputInButton = true;

            if (btnModel.State != ButtonModel.EState.IN_PLACE)
                return;

            MenuEventManager.TriggerEvent(btnModel, ButtonsManager.EButtonActionState.CLICKED);
            
            icon.gameObject.transform.SetAsLastSibling();
            gameObject.transform.SetAsLastSibling();
        }

        private void CalculateTrajectory() {
            //Debug.Log("<b>PieButton</b> CalculateTrajectory from " + btnModel.ToString());

            if(btnModel == null) {
                throw new System.Exception("Button model must be set in PieButton to calculate the trajectory");
            }

            startPoint = btnModel.OriginPoint;

            if (btnModel.State == ButtonModel.EState.IN_PLACE) {
                targetPoint = btnModel.OriginPoint;
            }
            else {
                float spacing   = MenuManager.Instance.Spacing;
                targetPoint     = btnModel.OriginPoint + new Vector3(height * spacing * Mathf.Sin(baseAngle * Mathf.Deg2Rad),
                                                                    width * spacing * Mathf.Cos(baseAngle * Mathf.Deg2Rad),
                                                                    0);
            }

            btnModel.SetEndPoint(targetPoint);

            startPoint =  btnModel.OriginPoint;
            targetPoint = btnModel.EndPoint;
        }

        public void StartAnimation(ButtonModel _btn, ButtonsManager.EButtonActionState _state) {
            //Debug.Log("<b>PieButton</b> StartAnimation for " + btnModel.ToString());

            startTime = Time.time;

            StartCoroutine(MoveButton(startPoint, targetPoint));
        }

        private void CreateLink(float _rotation) {
            //Debug.Log("<b>PieButton</b> CreateLink");

            linkRef.localRotation = Quaternion.Euler(0, 0, 180 - _rotation);

            float x = height * 0.5f * Mathf.Sin(_rotation * Mathf.Deg2Rad);
            float y = width * 0.5f * Mathf.Cos(_rotation * Mathf.Deg2Rad);
            Vector3 offset = new Vector3(x, y, 0) * 0.8f;

            origin  = btnModel.OriginPoint + offset;

            distanceMini = offset.magnitude;

        }

        private void UpdateLink() {
            //Debug.Log("<b>PieButton</b> UpdateLink (TweenRatio = " + _tweenRatio + ")");

            distance = Vector2.Distance(origin, movingPosition);

            if (distance <= distanceMini && btnModel.State == ButtonModel.EState.RETRACTING) {
                isLinkRetracted = true;
                return;
            }

            if (distance > distanceMini) {
                linkRef.sizeDelta = new Vector2(2, distance);
            }
            else {
                linkRef.sizeDelta = Vector2.zero;
            }

        }

        private float TimeRatio() {
            return (Time.time - startTime) * MenuManager.Instance.TweenSpeed ;// * (1.0f - 0.1f * btnModel.index);
        }

        private void OnDisable() {
            //Debug.Log("<b>PieButton</b> OnDisable");

            MenuEventManager.StopListening(btnModel, ButtonsManager.EButtonActionState.ANIMATION_STARTED, ButtonModelAction);
        }

        private void OnDestroy() {
            //Debug.Log("<b>PieButton</b> OnDestroy");

            MenuEventManager.StopListening(btnModel, ButtonsManager.EButtonActionState.ANIMATION_STARTED, ButtonModelAction);
        }

    }
}
