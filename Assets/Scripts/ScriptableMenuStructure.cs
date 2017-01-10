using System;
using System.Collections.Generic;
using UnityEngine;
using ContextualMenu;
using CustomMenu.Tools;

namespace ContextualMenuData {

    /// <summary>
    /// The ScriptableMenuStructure inherits from ScriptableTreeObject and it holds all needed data to create and manage a Contextual Menu with a MenuManager instance.
    /// It is implementing some methods that allow to synchronized the list of ButtonModel and the one of SerializableNode when adding, removing of reordering elements.
    /// </summary>
    /// <remarks>
    /// The way that Ids are designed and managed, the synchronisation could rely solely on string manipulation, but I chose not to rely on this because strings could proove unstable depending on systems and language preferences.
    /// </remarks>
    [CreateAssetMenu(fileName = "NewMenuStructure", menuName = "ContextualMenu/NewMenuStructure", order = 1)]
    [Serializable]
    public class ScriptableMenuStructure : ScriptableTreeObject {

#region Fieds
        /// <summary>
        /// This list is keeping record of all ButtonModel and it is synchronised with the serializedNode list of the base class (ScriptableTreeObject)
        /// It is accessed by the custom editor to build the structure and by the MenuManager to get data and instantiate UI Buttons
        /// </summary>
        public List<ButtonModel> Buttons = new List<ButtonModel>(1) { new ButtonModel("@") };

        /// <summary>
        /// The Context allows a mapping between a Selectable component and this data structure
        /// </summary>
        public EContext Context;

        /// <summary>
        /// This list of boolean values are only needed in an old version of the Custom Editor (ScriptableMenuStructureEditor) in order to toggle subnode visibility
        /// It will be removed in a future version
        /// </summary>
        [SerializeField]private List<bool> areDetailsVisible  = new List<bool>(1) { true };

        public List<bool> AreDetailsVisible {
            get { return areDetailsVisible; }
            set { areDetailsVisible = value; }
        }
        #endregion Fieds

        #region Global Management METHODS

        /// <summary>
        /// This method ensures that there is at least a root node in the hierarchy
        /// </summary>
        /// <remarks>
        ///  This is called from the Custom Editor.
        /// </remarks>
        public override void Init() {
            base.Init();

            Buttons             = new List<ButtonModel>(1) { new ButtonModel("@") };
            areDetailsVisible   = new List<bool>(1) { true };
        }

        /// <summary>
        /// Remove nodes and buttons data in order to get a fresh new structure.
        /// </summary>
        /// <remarks>
        ///  This is called from the Custom Editor.
        /// </remarks>
        public override void ClearAll() {
            base.ClearAll();

            Buttons.Clear();
            areDetailsVisible.Clear();

            Buttons = new List<ButtonModel>(1) { new ButtonModel("@") };
            areDetailsVisible = new List<bool>(1) { true };
        }

        #endregion Global Management METHODS

        #region Lists Management METHODS

        /// <summary>
        /// First add a new SerializableNode in the serializedNodes list of the base class then do the same in the button list at the same index
        /// </summary>
        /// <param name="_parentNode">The SerializableNode related to the parent button</param>
        /// <returns>The index (int) of the new node in the serializedNodes list</returns>
        public override int AddNode(SerializableNode _parentNode) {
            //Debug.Log("<b>ScriptableMenuStructure</b> AddNode from " + GetNodeInfo(_parentNode));

            int index     = base.AddNode(_parentNode);
            string nodeId = serializedNodes[index].Id;

            ButtonModel newBtn = new ButtonModel(nodeId);

            Buttons.Insert(index, newBtn);

            // Update the visibility in the Custom Editor with the toggle buttons
            areDetailsVisible.Insert(index, false);

            return index;
        }

        /// <summary>
        /// Remove a button in the Buttons list, as well as the related SerializableNode and al the sub nodes and sub buttons
        /// </summary>
        /// <param name="_btnModel">The ButtonModel to remove</param>
        public void RemoveButton(ButtonModel _btnModel) {
            //Debug.Log("<b>ScriptableMenuStructure</b> RemoveButton " + _btnModel.ToString());

            SerializableNode node       = GetNodeFromButton(_btnModel);
            SerializableNode parentNode = GetParent(node);

            // First we register the node to remove and its children, then we will create fresh new lists
            UpdateLists(RemoveNode(node, true), parentNode);
  
            return;
        }

        /// <summary>
        /// This method creates new lists and affect them to the old ones.
        /// It is called when removing buttons and their related nodes.
        /// </summary>
        /// <param name="_indexesToRemove">The indexes to remove in both serializedNodes and Buttons</param>
        /// <param name="_parentNode">The parent SerializableNode that is removed at the top</param>
        protected override void UpdateLists(List<int> _indexesToRemove, SerializableNode _parentNode = null) {
            //Debug.Log("<b>ScriptableMenuStructure</b> UpdateLists ");

            // First we purge the SerializableNode list
            base.UpdateLists(_indexesToRemove, _parentNode);

            List<ButtonModel> updatedBtnList  = new List<ButtonModel>();
            List<bool>        updatedBoolList = new List<bool>();

            int lastIndex = _indexesToRemove[_indexesToRemove.Count - 1];
            int len = Buttons.Count;

            // Then we create a new Button list ignoring the indexes to be removed
            for (int i = 0; i < len; i++) {
                if (!_indexesToRemove.Contains(i)) {
                    updatedBtnList.Add(Buttons[i]);
                    updatedBoolList.Add(areDetailsVisible[i]);
                }
            }

            Buttons.Clear();
            Buttons = updatedBtnList;

            areDetailsVisible.Clear();
            areDetailsVisible = updatedBoolList;

            // At last we update the id to synchronise the buttons and nodes data 
            len = Buttons.Count;
            for (int i = 0; i < len; i++) {
                Buttons[i].Id = serializedNodes[i].Id;
            }
        }

        /// <summary>
        /// This method moves a group of Buttons defined by their Id and place them at a given index 
        /// </summary>
        /// <param name="_indexesToMove">The list of ButtonModel indexes in the Buttons list</param>
        /// <param name="_startIndex">The index in the Buttons list where the first of the moved buttons should find its new place</param>
        /// <returns>The _startIndex in the Buttons list after the reordering occured</returns>
        /// <remarks>
        /// This method is called from the ReorderableList callbacks of the custom editor
        /// </remarks>
        public override int ReorderElements(int[] _indexesToMove, int _startIndex) {
            //Debug.Log("<b>ScriptableMenuStructure</b> ReorderElements from " + _startIndex);

            int ind = _startIndex;
            ind = base.ReorderElements(_indexesToMove, _startIndex); // First of all the SerializableNode list is reordered

            bool isMovedBefore = _startIndex <= _indexesToMove[0]; // Calculation is a bit different if buttons are moved toward the end of the beginning of the list
            int  len  = _indexesToMove.Length;

            // We create a list of ButtonModel based on their indexes in the Buttons list
            var  btns = new ButtonModel[len];

            for (int i = 0; i < len; i++) {
                btns[i] = Buttons[_indexesToMove[i]];
            }

            if (isMovedBefore) { // First we delete the ButtonModel instances to be moved (Otherwise their indexes are shifted)
                Buttons.RemoveRange(_indexesToMove[0], len);
                Buttons.InsertRange(_startIndex, btns); //then we insert them at the start index
            }
            else { // We do the reverse to avoid exceeding the list capacity when accessing an index
                Buttons.InsertRange(_startIndex + 1, btns);
                Buttons.RemoveRange(_indexesToMove[0], len);
            }

            return ind;
        }

#endregion Lists Management METHODS

#region Getter METHODS
        /// <summary>
        /// Just gets a ButtonModel instance from its string Id
        /// </summary>
        /// <param name="_id">The string Id field that is the same for the related SerializableNode</param>
        /// <returns>The ButtonModel instance that we are looking for</returns>
        public ButtonModel GetButtonfromId(string _id) {
            //Debug.Log("<b>ScriptableMenuStructure</b> GetButtonfromId " + _id);

            for (int i = 0; i < Buttons.Count; i++) {
                if (Buttons[i].Id.Equals(_id)){
                    return Buttons[i];
                }
            }

            return null;
        }

        /// <summary>
        /// Just gets a ButtonModel instance from its related node in the hierarchy, using their Id value.
        /// </summary>
        /// <param name="_node">The SerializableNode we have access to</param>
        /// <returns>The ButtonModel instance that we are looking for</returns>
        /// <remarks>
        /// This method is no longer use because the list are synchronized
        /// </remarks>
        public ButtonModel GetButtonFromNode(SerializableNode _node) {
            //Debug.Log("<b>ScriptableMenuStructure</b> GetButtonFromNode " + _node.ToString());

            byte   len = (byte)Buttons.Count;
            string id  = _node.Id;

            for (byte i = 0; i < len; i++) {
                if (Buttons[i].Id.Equals(id)) {
                    return Buttons[i];
                }
            }

            return null;
        }


        /// <summary>
        /// Get a SerializableNode instance in the serializableNodes list from a ButtonModel instance in the Buttons list 
        /// </summary>
        /// <param name="_btnModel">The ButtonModel instance we have access to</param>
        /// <returns>The SerializableNode instance that we are looking for</returns>
        /// <remarks>
        /// This method is generally called when operations are processed on the hierarchy from a given ButtonModel. We first access to the SerializableNode in order to process the changes.
        /// </remarks>
        public SerializableNode GetNodeFromButton(ButtonModel _btnModel) {
            //Debug.Log("<b>ScriptableMenuStructure</b> GetNodeFromButton of " + _btnModel.ToString());

            return serializedNodes[Buttons.IndexOf(_btnModel)];
        }

        /// <summary>
        /// Gets an array of ButtonModel instances that are the first level children of the given button
        /// </summary>
        /// <param name="_btnModel">The ButtonModel instance we have access to</param>
        /// <returns>the ButtonModel instances</returns>
        /// <remarks>This method is used to get access to the buttons when retracting/unfolding them in cascade</remarks>
        public ButtonModel[] GetChildren(ButtonModel _btnModel) {
            //Debug.Log("<b>ScriptableMenuStructure</b> GetChildren of " + _btnModel.ToString());

            SerializableNode   node     = GetNodeFromButton(_btnModel);
            SerializableNode[] subNodes = GetChildren(node);
            
            int len = subNodes.Length;
            //Debug.Log(node.ToString() + " : has " + len + " children");
            ButtonModel[] subButtons = new ButtonModel[len];

            int index;

            for (int i = 0; i < len; i++) {
                //Debug.Log("i = " + i + ", subNodes[i] = " + subNodes[i].ToString());
                index = subNodes[i].IndexOfFirstChild - 1;
                //Debug.Log("subButtons[i] : Buttons " + index);
                subButtons[i] = Buttons[index];
            }

            return subButtons;
        }

#endregion Getter METHODS

    }
}
