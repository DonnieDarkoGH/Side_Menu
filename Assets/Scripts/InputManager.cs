using System.Collections;
using UnityEngine;
using ContextualMenu;


namespace CustomMenu.Tools {

    /// <summary>
    /// This class creates an instance that receives inputs and then triggers the correct behaviours
    /// </summary>
    public class InputManager : MonoBehaviour {

#region FIELDS

        /// <summary>
        /// This is the position of the click, be it a mouse or a touch input
        /// </summary>
        private Vector2 InputPosition;

        // not used right noww
        //private bool    isLockedOnPosition = false;

        /// <summary>
        /// This parameter is storing the Time.time value when a click is done, then it is used to calculate interpolations
        /// </summary>
        private float   timerStart;

        // Those 2 fields are only used in the developement process to display gizmos
        private Vector3 HitCamLoc;
        private Vector3 TargetCamLoc;

#endregion FIELDS

#region Monobehaviour METHODS

        /// <summary>
        /// Standard callback from the Monobehaviour base class
        /// </summary>
        void Update() {

            if (Input.GetKeyUp(KeyCode.Mouse0)) {
                InputPosition = Input.mousePosition;
                IsContextObjectSelected();
            }

            //HitLoc = GetCameraTargetPoint();
        }

        /// <summary>
        /// Standard callback from the Monobehaviour base class
        /// </summary>
        private void OnGUI() {

            if (GUI.Button(new Rect(10, 10, 200, 50), "QUIT")) {
                Application.Quit();
            }

        }

        /// <summary>
        /// Standard callback from the Monobehaviour base class
        /// </summary>
        /// <remarks>Display raycast and calculation results</remarks>
        private void OnDrawGizmos() {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(HitCamLoc, 0.25f);
            Gizmos.DrawSphere(TargetCamLoc, 0.25f);
        }

#endregion Monobehaviour METHODS

#region Selection behaviour METHODS

        /// <summary>
        /// Send a raycast and select the first gameobject with a Selectable component, then it calls CenterObjectInScreen() to move the camera
        /// </summary>
        /// <returns>True if the raycast hit a Selectable gameobject</returns>
        private bool IsContextObjectSelected() {

            // Consume the input if it hit a menu button
            if (MenuManager.Instance.isInputInButton) {
                MenuManager.Instance.isInputInButton = false;
                return false;
            }
                
            Ray inputRay = Camera.main.ScreenPointToRay(InputPosition);
            RaycastHit hit;

            if (Physics.Raycast(inputRay, out hit, Mathf.Infinity)) {
                ISelectable IObj = hit.collider.gameObject.GetComponent<ISelectable>();
                if(IObj != null) {
                    CenterObjectInScreen(hit.collider.transform, IObj);
                }
            }

            return false;
        }


        /// <summary>
        /// This method calculate a new position for the camera so that a selected gameObject becomes center in the screen
        /// </summary>
        /// <param name="_objToCenter">The Selectable object that is selected</param>
        /// <param name="_selectableObj">The ISelectable that will pass in the end in order to create a Context for the menu</param>
        private void CenterObjectInScreen(Transform _objToCenter, ISelectable _selectableObj) {

            // if a menu is unfolded for another Selectable object, we kill it
            MenuManager.Instance.KillMenu();

            // Intersection of the Camera forward direction with the ground plane
            HitCamLoc    = GetProjectionOnGround(Camera.main.transform, Camera.main.transform.forward);
            // Projection of the Selected gameObject center on the ground plane 
            TargetCamLoc = GetProjectionOnGround(_objToCenter, -Vector3.up);

            // Calculation of the camera target position at the end of the animation
            Vector3 Offset       = TargetCamLoc - HitCamLoc;
            Vector3 TargetCamPos = Camera.main.transform.position + Offset;

            // Start timer and coroutine to interpolate the camera position
            timerStart = Time.time;
            StartCoroutine(MoveObject(Camera.main.transform, TargetCamPos, _selectableObj));

        }

        /// <summary>
        /// Calculation method that returns a Vector3 position that results of a projection of a transform along a Vector3 direction
        /// </summary>
        /// <param name="_transformToProject">The transform to be projected on the ground plane</param>
        /// <param name="_projDir">The direction of the projection</param>
        /// <returns>The projected position</returns>
        private Vector3 GetProjectionOnGround(Transform _transformToProject, Vector3 _projDir) {

            // The direction is always passing through the center of the camera
            Transform camTr       = Camera.main.transform;
            Ray       camRay      = new Ray(_transformToProject.position, _projDir);
            // Note that we could enter the plane as a parameter in order to get a method useful for any projection !
            Plane     groundPlane = new Plane(Vector3.up, Vector3.zero);
            float     rayDistance;

            if(groundPlane.Raycast(camRay,out rayDistance)) {
                return camRay.GetPoint(rayDistance);
            }

            return Vector3.zero;
        }

        /// <summary>
        /// This coroutine interpolates the position of a transform until it meets a target position, then it validates the selection of a Selectable object
        /// </summary>
        /// <param name="_objTr">The transform to move</param>
        /// <param name="_targetPosition">The destination</param>
        /// <param name="_selectableObj">The Selectable object that will be selected</param>
        /// <returns></returns>
        IEnumerator MoveObject(Transform _objTr, Vector3 _targetPosition, ISelectable _selectableObj) {

            Vector3 startPosition = _objTr.position;
            float frac = 0.0f;

            do {
                frac = (Time.time - timerStart) * (Time.time - timerStart) * 10.0f;
                _objTr.position = Vector3.Lerp(startPosition, _targetPosition, frac);
                yield return 0;
            } while (frac <= 1);

            _objTr.position = _targetPosition;
            _selectableObj.HandleSelection(_targetPosition);
        }

#endregion Selection behaviour METHODS

    }
}

