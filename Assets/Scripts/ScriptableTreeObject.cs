using System;
using System.Collections.Generic;
using UnityEngine;


namespace ContextualMenuData {
    /// <summary>
    /// The ScriptableTreeObject class creates an instance of non-binary Tree Data Structure that manages a list of SerializableNode at a basic level.
    /// It is implementing some methods that allow to add or remove nodes, but that also return useful information like GetChildren, GetParent and so on.
    /// </summary>
    /// <remarks>
    /// Note 1 : This is the parent class for ScriptableMenuStructure.
    /// Note 2 : There is a specifity regarding common Tree Data Structure : The node children are added at the end of the list of their parent node. 
    /// For this reason, we need to resort to methods like NextIndex or ShiftIndexes...
    /// Note 3 : The way that node Id are designed and managed, node Levels and Getter could rely solely on string manipulation. 
    /// But I chose not to rely on this because strings could proove unstable depending on systems and language preferences.
    /// </remarks>
    // Remove the following attribute if you want to use the class ouside a Unity context
    [CreateAssetMenu(fileName = "NewTree", menuName = "TreeObject/NewTree", order = 1)]
    [Serializable]
    public class ScriptableTreeObject : ScriptableObject {

        /// <summary>
        /// This class create unit nodes that can be serialized by Unity
        /// </summary>
        /// <remarks>
        /// Using a reference to a list of SerializableNodes inside this class would lead to inception problems and errors in Unity serialization
        /// </remarks>
        [Serializable]
        public class SerializableNode {

            public string Id;
            public int ChildCount;
            public int IndexOfFirstChild;

            public SerializableNode() {
                Id                = "@";
                ChildCount        = 0;
                IndexOfFirstChild = 1;
            }

            public SerializableNode(string _id, int _indexOfFirstChild) {
                Id                = _id;
                ChildCount        = 0;
                IndexOfFirstChild = _indexOfFirstChild;
            }

            public int GetIndex() {
                return IndexOfFirstChild - 1;
            }

        }

        /// <summary>
        /// This list is keeping record of all Serialized Nodes. 
        /// It is accessed by the custom editor to build the structure and by the MenuManager to get data and instantiate UI Buttons
        /// </summary>
        public List<SerializableNode> serializedNodes = new List<SerializableNode>(1) { new SerializableNode() };

#region virtual METHODS
        /// <summary>
        /// Simple initialization that ensures there is at least a root node after clearing
        /// </summary>
        public virtual void Init() {
            serializedNodes = new List<SerializableNode>(1) { new SerializableNode() };
        }

        /// <summary>
        /// This method is essentially used in Editors to clear data
        /// </summary>
        public virtual void ClearAll() {
            //Debug.Log("<b>ScriptableTreeObject</b> ClearAll");
            serializedNodes.Clear();

            serializedNodes = new List<SerializableNode>(1) { new SerializableNode() };
        }

        /// <summary>
        /// In the ScriptableTreeObject class, a node is added at the end of the other children of its parent node.
        /// </summary>
        /// <remarks>
        /// The string Id is automatically processed based on UTF8 convention 
        /// </remarks>
        /// <param name="_parentNode">The SerializableNode to which we want to add a child</param>
        /// <returns>The index of the new node in the serializableNodes list</returns>
        public virtual int AddNode(SerializableNode _parentNode) {
            //Debug.Log("<b>ScriptableTreeObject</b> AddNode to " + GetNodeInfo(_parentNode));

            int parentIndex  = serializedNodes.IndexOf(_parentNode);
            int newNodeIndex = GetNextIndex(parentIndex);
            _parentNode.ChildCount++;

            //(e.g : @aa is the first child of the node id @a, and @ab is the second child and so on...)
            byte start       = Convert.ToByte('a');
            char indexToChar = Convert.ToChar(start + _parentNode.ChildCount - 1);
            string newId     = _parentNode.Id + indexToChar;
            //Debug.Log("newNodeIndex " + newNodeIndex);

            SerializableNode newNode = new SerializableNode(newId, newNodeIndex);

            serializedNodes.Insert(newNodeIndex, newNode);
            // We need to recalculate all IndexOFFirstChild values in the nodes following this one in serializedNodes list
            int len = serializedNodes.Count;
            for (int i = newNodeIndex; i < len; i++) {
                serializedNodes[i].IndexOfFirstChild++;
            }

            //Debug.Log(GetNodeInfo(newNode));
            return newNodeIndex;
        }

        /// <summary>
        /// This recursive method registers the indexes to remove when we want to suppress a SerializedNode, its children and all the other related children 
        /// </summary>
        /// <param name="_node">The SerializableNode we want to remove</param>
        /// <returns>An array of indexes values corresponding to the nodes that are suppressed, including thi one</returns>
        public virtual List<int> RemoveNode(SerializableNode _node, bool _isParentNode = false) {
            //Debug.Log("<b>ScriptableTreeObject</b> RemoveNode " + GetNodeInfo(_node));

            int       index           = _node.IndexOfFirstChild - 1;
            List<int> indexesToRemove = new List<int>(1) { index };

            if (index == 0) {
                Debug.LogError("You cannot delete root node !"); // Remove this instruction if you want to use this class outside a Unity context
                return indexesToRemove;
            }

            int count = _node.ChildCount;

            SerializableNode[] childNode = GetChildren(_node);
            int it = 0;
            while (it < count) {
                indexesToRemove.AddRange(RemoveNode(childNode[it]));
                it++;
            }

            if(!_isParentNode)// Just keep track of the new Childcount of the parent SerializedNode
               return indexesToRemove;

            GetParent(_node).ChildCount--;

            return indexesToRemove;
        }

        /// <summary>
        /// TO DO
        /// </summary>
        /// <param name="_indexesToRemove"></param>
        /// <param name="_parentNode"></param>
        protected virtual void UpdateLists(List<int> _indexesToRemove, SerializableNode _parentNode = null) {
            //Debug.Log("<b>ScriptableTreeObject</b> UpdateLists ");

            List<SerializableNode> updatedNodeList = new List<SerializableNode>();

            int offset    = _indexesToRemove.Count;
            int lastIndex = _indexesToRemove[_indexesToRemove.Count - 1];
            int len       = serializedNodes.Count;

            
            for (int i = 0; i < len; i++) {
                if (i >= lastIndex) {
                    serializedNodes[i].IndexOfFirstChild -= offset;
                }
                if (!_indexesToRemove.Contains(i)) {
                    updatedNodeList.Add(serializedNodes[i]);
                }
            }

            serializedNodes.Clear();
            serializedNodes = updatedNodeList;

            if(_parentNode != null) {
                UpdateChildNodesId(_parentNode);
            }

        }

#endregion virtual METHODS

#region Getter METHODS
        /// <summary>
        /// This method is used to keep track of node.childCount in RemoveNode
        /// </summary>
        /// <param name="_node">The SerializableNode whose parent we want to get</param>
        /// <returns>The node parent as a SerializableNode</returns>
        public SerializableNode GetParent(SerializableNode _node) {
            //Debug.Log("<b>ScriptableTreeObject</b> GetParent of " + _node.ToString());

            int len        = serializedNodes.Count;
            int childIndex = _node.IndexOfFirstChild - 1;
            int nodeLevel  = GetLevel(_node);
            SerializableNode parentNode = null;

            // Iterate through the list of SerializableNode and register the ones that are immediately of superior level
            for (int i = 0; i < len; i++) {
                if(serializedNodes[i].ChildCount > 0) {
                    if (GetLevel(serializedNodes[i]) == nodeLevel - 1) {
                        parentNode = serializedNodes[i];
                    }
                }
                if(i == childIndex) {
                    break;
                }
            }

            return parentNode;
        }

        /// <summary>
        /// Gets an array of SerializableNode instances that are the first level children of the given SerializableNode
        /// </summary>
        /// <param name="_node">The SerializableNode whose children we want to get</param>
        /// <returns>An array of SerializableNode</returns>
        public SerializableNode[] GetChildren(SerializableNode _node) {
            //Debug.Log("<b>ScriptableTreeObject</b> GetChildren of " + GetNodeInfo(_node));

            int count = _node.ChildCount;
            //int index = serializedNodes.IndexOf(_node);
            int index = _node.IndexOfFirstChild - 1;

            if (count == 0) {
                return new SerializableNode[0];
            }

            int len = serializedNodes.Count;

            SerializableNode[] children    = new SerializableNode[count];
            SerializableNode   currentNode = _node;

            int it = 0; // A counter value to keep track of the number of children that are registered

            // Using a Stack data structure, we are counting the number of SerializedNode at each level.
            // The current node is at level 0, and we are registering each of node of the level 1 until we have find each one of the children
            Stack<int> counters = new Stack<int>();

            for (int i = index; i < len; i++) {
                currentNode = serializedNodes[i];

                if (counters.Count == 1) {
                    //Debug.Log("it " + it + ", " + GetNodeInfo(currentNode));
                    children[it++] = currentNode;
                    if (it == count) {
                        break;
                    }
                }

                if (currentNode.ChildCount > 0) {
                    counters.Push(currentNode.ChildCount);
                }
                else {
                    int n = 0;
                    while (n == 0 && counters.Count > 0) {
                        n = counters.Pop() - 1; // We decrease the number of children of the current level
                        if (n > 0) {
                            counters.Push(n);   // and we register the new count
                        }
                    }
                }
            }

            return children;
        }

        /// <summary>
        /// This method is used by the custom editor to get all related sub children when reordering the node
        /// </summary>
        /// <param name="_node">The SerializableNode whose children we want to get</param>
        /// <returns>An array of SerializableNode</returns>
        public int[] GetNodeAndSubChildrenId(SerializableNode _node) {
            //Debug.Log("<b>ScriptableTreeObject</b> GetNodeAndSubChildrenId of " + GetNodeInfo(_node));

            int       len = serializedNodes.Count;
            int       level     = GetLevel(_node);
            int       nextIndex = _node.IndexOfFirstChild;
            List<int> nodesId   = new List<int>(1) { nextIndex - 1};

            if (nextIndex < len) {
                SerializableNode nextNode = serializedNodes[nextIndex];

                while (GetLevel(nextNode) > level) {

                    nodesId.Add(nextIndex);
                    nextIndex++;

                    if (nextIndex < len)
                        nextNode = serializedNodes[nextIndex];
                    else
                        break;
                }
            }

            return nodesId.ToArray();
        }

        /// <summary>
        /// TO DO
        /// </summary>
        /// <param name="_indexesToMove"></param>
        /// <param name="_startIndex"></param>
        public virtual int ReorderElements(int[] _indexesToMove, int _startIndex) {
            //Debug.Log("<b>ScriptableTreeObject</b> ReorderElements starting from " + _startIndex);

            bool isMovedBefore    = _startIndex < _indexesToMove[0];
            int len               = _indexesToMove.Length;
            int indexOfFirstChild = serializedNodes[_startIndex].IndexOfFirstChild + _startIndex - _indexesToMove[0];

            serializedNodes[_startIndex].IndexOfFirstChild = indexOfFirstChild;
            if (len < 2) {        
                return isMovedBefore ? _startIndex : _startIndex - len + 1;
            }

            var nodes = new SerializableNode[len - 1];

            if (isMovedBefore) {
                for (int i = 0; i < len - 1; i++) {
                    nodes[i] = serializedNodes[_indexesToMove[i + 1]];
                }
                serializedNodes.RemoveRange(_indexesToMove[1], len - 1);
                serializedNodes.InsertRange(_startIndex + 1, nodes);
            }
            else {
                for (int i = 0; i < len - 1; i++) {
                    nodes[i] = serializedNodes[_indexesToMove[i]];
                }
                serializedNodes.InsertRange(_startIndex + 1, nodes);
                serializedNodes.RemoveRange(_indexesToMove[0], len - 1);
            }

            int nodeCount = serializedNodes.Count;
            for(int i =0; i < nodeCount; i++) {
                serializedNodes[i].IndexOfFirstChild = i + 1;
            }

            return isMovedBefore ? _startIndex : _startIndex - len + 1;
        }

        /// <summary>
        /// This method is essentially used to display node and buttons in the Custom Editor
        /// </summary>
        /// <remarks>
        /// We use a Stack data structure to keep tarck of the level, in a way similar as what we do in GetChildren 
        /// </remarks>
        /// <param name="_node">The node whose level we want to get</param>
        /// <returns>The level as an obsolute value (0 being the root level)</returns>
        public int GetLevel(SerializableNode _node) {

            SerializableNode currentNode;
            int        nodeIndex = serializedNodes.IndexOf(_node);
            Stack<int> counters  = new Stack<int>();

            for (int i = 0; i < nodeIndex; i++) {
                currentNode = serializedNodes[i];

                if (currentNode.ChildCount > 0) {
                    counters.Push(currentNode.ChildCount);
                }
                else {
                    int n = 0;
                    while (n == 0 && counters.Count > 0) {
                        n = counters.Pop() - 1; // We decrease the number of children of the current level
                        if (n > 0) {
                            counters.Push(n);   // and we register the new count
                        }
                    }
                }
            }

            return counters.Count;

        }

        /// <summary>
        /// This method calculate the index inside the serializableNodes list where we can insert a new node
        /// This child node is at the end of the SerializableNode children of the considered node.
        /// </summary>
        /// <remarks>
        /// This method is used to build the data in the Custom Editor
        /// </remarks>
        /// <param name="_parentIndex">The index of the parent SerializableNode in the serializableNodes list</param>
        /// <returns>The index of the serializableNodes list where we will add a new child for this node</returns>
        public int GetNextIndex(int _parentIndex) {
            int[] optionalOut;

            return GetNextIndex(_parentIndex, out optionalOut);
        }

        /// <summary>
        /// This overloading method calculate the indexes of the children as an out parameter
        /// </summary>
        /// <param name="_parentIndex">The index of the considered node in the serializableNodes list</param>
        /// <param name="childIndexes">an array of indexes corresponding to the direct children of the considered node</param>
        /// <returns>The index of the serializableNodes list where we will add a new child for this node</returns>
        public int GetNextIndex(int _parentIndex, out int[] childIndexes) {
            //if (Application.isPlaying) {Debug.Log("<b>ScriptableTreeObject</b> NextIndex of " + _parentIndex);}

            List<int> indexes = new List<int>();

            if (_parentIndex < 0) {
                childIndexes = indexes.ToArray();
                return 0;
            }

            int childCount = 0;
            if (serializedNodes.Count > _parentIndex) {
                try {
                    childCount = serializedNodes[_parentIndex].ChildCount;
                }
                catch {
                    Debug.LogError("Cannot reach parentIndex " + _parentIndex);
                }
            }

            for (int i = 0; i < childCount; i++) {
                if (i == 0) {
                    indexes.Add(_parentIndex + 1);
                    _parentIndex = GetNextIndex(_parentIndex + 1);
                }
                else {
                    _parentIndex = GetNextIndex(_parentIndex);
                }
                if( i != childCount - 1) {
                    indexes.Add(_parentIndex);
                }
            }

            childIndexes = indexes.ToArray();

            if (childCount == 0) {
                return _parentIndex + 1;
            }
            else {
                return _parentIndex;
            }
        }
#endregion Getter METHODS

#region TOOLS
        /// <summary>
        /// Getting information about a node from the ScriptableTreeObject class allows us to have more complete ones 
        /// </summary>
        /// <remarks>
        /// Used in Debug.log
        /// </remarks>
        /// <param name="_node">The node we want details about</param>
        /// <returns>A string with Level, Id, index, Childcountn IndexOfFirstChild, NextIndex (for a new child) and indexes of Children</returns>
        public string GetNodeInfo(SerializableNode _node) {

            if (_node == null) {
                return "Null";
            }

            int[] childIndexes;
            int idx    = serializedNodes.IndexOf(_node);
            int level  = GetLevel(_node);
            int nextId = GetNextIndex(idx, out childIndexes);

            string strChildIdx = ",children : ";
            foreach (int i in childIndexes) {
                strChildIdx += i.ToString() + ",";
            }

            return "Level " + level + "- Node Id" + _node.Id + " (Idx: " + idx + ", Children: " + _node.ChildCount + ", 1stChild: " + _node.IndexOfFirstChild + ", NextIdx :" + nextId + strChildIdx + ")";
        }

        private string GetIdFromIndex(int _indexInParent, string _parentId) {
            
            //(e.g : @aa is the first child of the node id @a, and @ab is the second child and so on...)
            byte start       = Convert.ToByte('a');
            char indexToChar = Convert.ToChar(start + _indexInParent);
            //Debug.Log("nodeIndex " + _parentId + indexToChar);
            return _parentId + indexToChar;
        }

        private void UpdateChildNodesId(SerializableNode _parentNode) {

            string _parentId    = _parentNode.Id;
            int[] childIndexes;

            GetNextIndex(_parentNode.IndexOfFirstChild - 1, out childIndexes);

            SerializableNode childNode;
            for(int i = 0; i < _parentNode.ChildCount; i++) {
                childNode = serializedNodes[childIndexes[i]];
                serializedNodes[childIndexes[i]].Id = GetIdFromIndex(i, _parentId);
                if (childNode.ChildCount > 0) {
                    UpdateChildNodesId(childNode);
                }
            }
        }

#endregion TOOLS
    }
}