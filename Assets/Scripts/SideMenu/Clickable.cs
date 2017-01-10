using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using ContextualMenu;

namespace CustomMenu.SideMenu {

    /// <summary>
    /// This Interface can be implemented by any behaviour that can produce a Contextual menu when selected
    /// </summary>
    public interface IClickable : IPointerClickHandler {

        /// <summary>
        /// The results of the click (generally initializes a new side menu) 
        /// </summary>
        void HandleClick();

        /// <summary>
        /// Returns the gameObject whose monobehaviour implements the interface
        /// </summary>
        /// <returns></returns>
        GameObject GetGameObject();
    }

    [RequireComponent(typeof(Image))]
    public class Clickable : MonoBehaviour, IClickable {

        [SerializeField] private ButtonModel btnModel;

        // Use this for initialization
        void Start() {

        }

        // Update is called once per frame
        void Update() {

        }

        public GameObject GetGameObject() {
            return gameObject;
        }

        public void HandleClick() {
            throw new NotImplementedException();
        }

        public void OnPointerClick(PointerEventData eventData) {
            Debug.Log("<b>Selectable</b> OnPointerClick, dragging  : " + eventData.dragging);

            if (eventData.IsScrolling())
                return;

            Debug.Log("Click !");
        }
    }
}

