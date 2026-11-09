#pragma warning disable 0618
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Object = UnityEngine.Object;
#if UNITY_6000_2_OR_NEWER
// Unity 6000.2 deprecated the non-generic IMGUI TreeView types in favour of generic
// ones keyed by an identifier type; TreeView<int> is behaviourally identical to the
// old TreeView. Aliased here so the implementation below stays version agnostic.
using UnityTreeViewState = UnityEditor.IMGUI.Controls.TreeViewState<int>;

#else
using UnityTreeView = UnityEditor.IMGUI.Controls.TreeView;
using UnityTreeViewItem = UnityEditor.IMGUI.Controls.TreeViewItem;
using UnityTreeViewState = UnityEditor.IMGUI.Controls.TreeViewState;
#endif

namespace HeurekaGames.AssetHunterPRO.BaseTreeviewImpl.DependencyGraph
{
    [Serializable]
    public class AH_DependencyGraphManager : ScriptableSingleton<AH_DependencyGraphManager>,
                                             ISerializationCallbackReceiver
    {
        [SerializeField] public bool IsDirty = true;
        [SerializeField] private UnityTreeViewState treeViewStateFrom;
        [SerializeField] private UnityTreeViewState treeViewStateTo;
        [SerializeField] private MultiColumnHeaderState multiColumnHeaderStateFrom;
        [SerializeField] private MultiColumnHeaderState multiColumnHeaderStateTo;

        [SerializeField] private Dictionary<string, List<string>> referencedFrom = new();

        [SerializeField] private Dictionary<string, List<string>> referenceTo = new();

        [SerializeField] private string selectedAssetGUID = "";
        [SerializeField] private string selectedAssetObjectName = "";
        [SerializeField] private Object selectedAssetObject;

        [SerializeField] private List<string> selectionHistory = new();
        [SerializeField] private int selectionHistoryIndex;
        [SerializeField] private AH_DepGraphTreeviewWithModel TreeViewModelFrom;
        [SerializeField] private AH_DepGraphTreeviewWithModel TreeViewModelTo;

        private bool lockedSelection;

        //Force window to refresh selection
        private bool requiresRefresh;

        public bool LockedSelection
        {
            get => lockedSelection;
            set
            {
                lockedSelection = value;

                //Update currently selected
                if (!lockedSelection)
                {
                    requiresRefresh = true;
                    UpdateSelectedAsset(Selection.activeObject);
                }
            }
        }

        public bool TraversingHistory { get; set; }

        public string SearchString
        {
            get => treeViewStateFrom.searchString;
            set
            {
                string tmp = treeViewStateFrom.searchString;

                treeViewStateFrom.searchString = treeViewStateTo.searchString = value;

                if (tmp != value)
                {
                    TreeViewModelTo.Reload();
                    TreeViewModelFrom.Reload();
                }
            }
        }

        //Return selected asset
        public Object SelectedAsset { get => selectedAssetObject; }

        public bool HasSelection
        {
            get => !string.IsNullOrEmpty(selectedAssetGUID);
        }

        public void OnEnable()
        {
            hideFlags = HideFlags.HideAndDontSave;
        }

        public void OnBeforeSerialize()
        {
            _keysFrom.Clear();
            _wrapperValuesFrom.Clear();

            foreach (var kvp in referencedFrom)
            {
                _keysFrom.Add(kvp.Key);
                _wrapperValuesFrom.Add(new(kvp.Value));
            }

            _keysTo.Clear();
            _wrapperValuesTo.Clear();

            foreach (var kvp in referenceTo)
            {
                _keysTo.Add(kvp.Key);
                _wrapperValuesTo.Add(new(kvp.Value));
            }
        }

        public void OnAfterDeserialize()
        {
            referencedFrom = new();

            for (int i = 0; i != Math.Min(_keysFrom.Count, _wrapperValuesFrom.Count); i++)
            {
                referencedFrom.Add(_keysFrom[i], _wrapperValuesFrom[i].list);
            }

            referenceTo = new();

            for (int i = 0; i != Math.Min(_keysTo.Count, _wrapperValuesTo.Count); i++)
            {
                referenceTo.Add(_keysTo[i], _wrapperValuesTo[i].list);
            }
        }

        //We clear history when project changes, as there are problems in identifying if history points to deleted assets
        internal void ResetHistory()
        {
            var obsoleteAssets = selectionHistory.FindAll(x => AssetDatabase.LoadMainAssetAtPath(x) == null);
            //Remove the objets that are no longer in asset db
            selectionHistory.RemoveAll(x => obsoleteAssets.Contains(x));

            int duplicateCount = obsoleteAssets.Count;

            for (int i = selectionHistory.Count - 1; i >= 0; i--)
            {
                //Find identical IDs directly after each other
                if (i > 0 && selectionHistory[i] == selectionHistory[i - 1])
                {
                    selectionHistory.RemoveAt(i);
                    duplicateCount++;
                }
            }

            //Reset history index to match new history
            selectionHistoryIndex -= duplicateCount;
        }

        public void Initialize(SearchField searchField, Rect multiColumnTreeViewRect)
        {
            int referenceID = 0;

            initTreeview(
                ref treeViewStateFrom, ref multiColumnHeaderStateFrom, multiColumnTreeViewRect, ref TreeViewModelFrom,
                searchField, referencedFrom, selectedAssetGUID, ref referenceID);

            initTreeview(
                ref treeViewStateTo, ref multiColumnHeaderStateTo, multiColumnTreeViewRect, ref TreeViewModelTo,
                searchField, referenceTo, selectedAssetGUID, ref referenceID);

            requiresRefresh = false;
        }

        public void RefreshReferenceGraph()
        {
            referenceTo = new();
            referencedFrom = new();

            string[] paths = AssetDatabase.GetAllAssetPaths();
            int pathCount = paths.Length;

            for (int i = 0; i < pathCount; i++)
            {
                string path = paths[i];

                if (AssetDatabase.IsValidFolder(path) || !path.StartsWith("Assets")) //Slow, could be done recusively
                    continue;

                if (EditorUtility.DisplayCancelableProgressBar(
                        "Creating Reference Graph", path, i / (float)pathCount))
                {
                    referenceTo = new();
                    referencedFrom = new();
                    break;
                }

                string[] allRefs = AssetDatabase.GetDependencies(path, false);
                string assetPathGuid = AssetDatabase.AssetPathToGUID(path);

                var newList = allRefs.Where(val => val != path)
                                     .Select(val => AssetDatabase.AssetPathToGUID(val)).ToList();

                //Store everything reference by this asset
                if (newList.Count > 0)
                    referenceTo.Add(assetPathGuid, newList);

                //Foreach asset refenced by this asset, store the connection
                foreach (string reference in allRefs)
                {
                    string refGuid = AssetDatabase.AssetPathToGUID(reference);

                    if (!referencedFrom.ContainsKey(refGuid))
                        referencedFrom.Add(refGuid, new());

                    referencedFrom[refGuid].Add(assetPathGuid);
                }
            }

            IsDirty = false;
            EditorUtility.ClearProgressBar();
        }

        private void initTreeview(
            ref UnityTreeViewState _treeViewState, ref MultiColumnHeaderState _headerState, Rect _rect,
            ref AH_DepGraphTreeviewWithModel _treeView, SearchField _searchField,
            Dictionary<string, List<string>> referenceDict, string assetGUID, ref int referenceID)
        {
            bool hasValidReferences;

            var treeModel = new TreeModel<AH_DepGraphElement>(
                getTreeViewData(referenceDict, assetGUID, out hasValidReferences, ref referenceID));

            // Check if it already exists (deserialized from window layout file or scriptable object)
            if (_treeViewState == null)
                _treeViewState = new();

            bool firstInit = _headerState == null;

            MultiColumnHeaderState headerState =
                AH_DepGraphTreeviewWithModel.CreateDefaultMultiColumnHeaderState(_rect.width);

            if (MultiColumnHeaderState.CanOverwriteSerializedFields(_headerState, headerState))
                MultiColumnHeaderState.OverwriteSerializedFields(_headerState, headerState);

            _headerState = headerState;

            AH_DepGraphHeader multiColumnHeader = new(_headerState);
            //if (firstInit)
            multiColumnHeader.ResizeToFit();

            _treeView = new(_treeViewState, multiColumnHeader, treeModel);
            _searchField.downOrUpArrowKeyPressed += _treeView.SetFocusAndEnsureSelectedItem;
        }

        internal void UpdateTreeData(
            ref AH_DepGraphTreeviewWithModel _treeView, Dictionary<string, List<string>> referenceDict,
            string assetGUID, ref int referenceID)
        {
            bool hasValidReferences;

            _treeView.treeModel.SetData(
                getTreeViewData(referenceDict, assetGUID, out hasValidReferences, ref referenceID));
        }

        internal bool HasCache() => referencedFrom.Count > 0 && referenceTo.Count > 0;

        internal bool HasHistory(int direction, out string tooltip)
        {
            int testIndex = selectionHistoryIndex + direction;
            bool validIndex = testIndex >= 0 && testIndex < selectionHistory.Count;

            tooltip = validIndex ? AssetDatabase.LoadMainAssetAtPath(selectionHistory[testIndex])?.name
                : string.Empty;

            //Validate that history contains that index
            return testIndex >= 0 && testIndex < selectionHistory.Count;
        }

        internal bool RequiresRefresh() => requiresRefresh;

        internal AH_DepGraphTreeviewWithModel GetTreeViewTo() => TreeViewModelTo;

        internal AH_DepGraphTreeviewWithModel GetTreeViewFrom() => TreeViewModelFrom;

        internal Dictionary<string, List<string>> GetReferencesTo() => referenceTo;

        internal string GetSelectedName() => selectedAssetObjectName;

        internal Dictionary<string, List<string>> GetReferencesFrom() => referencedFrom;

        public IList<AH_DepGraphElement> getTreeViewData(
            Dictionary<string, List<string>> referenceDict, string assetGUID, out bool success, ref int referenceID)
        {
            var treeElements = new List<AH_DepGraphElement>();

            int depth = -1;

            AH_DepGraphElement root = new("Root", depth, -1, "");
            treeElements.Add(root);

            var
                referenceQueue =
                    new Stack<string>(); //Since we are creating a tree we want the same asset to be referenced in any branch, but we do NOT want circular references

            var references = referenceDict.ContainsKey(assetGUID) ? referenceDict[assetGUID] : null;

            if (references != null && references.Count > AH_SettingsManager.Instance.MaxRefCount)
            {
                Debug.Log(
                    string.Format(
                        "AHPPRO: References capped to {0} due to settings. Actual count is {1}",
                        AH_SettingsManager.Instance.MaxRefCount, references.Count));

                references = references.GetRange(0, AH_SettingsManager.Instance.MaxRefCount);
            }

            if (references != null)
            {
                foreach (string item in references)
                {
                    addElement(treeElements, referenceDict, item, ref depth, ref referenceID, ref referenceQueue);
                }
            }

            success = treeElements.Count > 2; //Did we find any references (Contains more thatn 'root' and 'self')
            TreeElementUtility.ListToTree(treeElements);

            EditorUtility.ClearProgressBar();
            return treeElements;
        }

        private void addElement(
            List<AH_DepGraphElement> treeElements, Dictionary<string, List<string>> referenceDict, string assetGUID,
            ref int depth, ref int id, ref Stack<string> referenceStack)
        {
            string path = AssetDatabase.GUIDToAssetPath(assetGUID);
            string[] pathSplit = path.Split('/');

            if (referenceStack.Contains(path) || depth >= AH_SettingsManager.Instance.DependencyDepth - 1)
                return;

            depth++;

            treeElements.Add(new( /*path*/pathSplit.Last(), depth, id++, path));
            referenceStack.Push(path); //Add to stack to keep track of circular refs in branch

            var references = referenceDict.ContainsKey(assetGUID) ? referenceDict[assetGUID] : null;

            if (references != null)
            {
                foreach (string item in references)
                {
                    addElement(treeElements, referenceDict, item, ref depth, ref id, ref referenceStack);
                }
            }

            depth--;

            referenceStack.Pop();
        }

        public void SelectPreviousFromHistory()
        {
            selectionHistoryIndex--;
            SelectFromHistory(selectionHistoryIndex);
        }

        public void SelectNextFromHistory()
        {
            selectionHistoryIndex++;
            SelectFromHistory(selectionHistoryIndex);
        }

        private void SelectFromHistory(int index)
        {
            TraversingHistory = true;
            Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(selectionHistory[selectionHistoryIndex]);
        }

        internal void UpdateSelectedAsset(Object activeObject)
        {
            bool invalid = activeObject == null ||
                           AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(Selection.activeObject));

            if (invalid)
            {
                selectedAssetGUID = selectedAssetObjectName = string.Empty;
                selectedAssetObject = null;
            }
            else
            {
                selectedAssetGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(activeObject));
                selectedAssetObjectName = activeObject.name;

                selectedAssetObject =
                    AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(selectedAssetGUID));

                if (!TraversingHistory)
                    addToHistory();
            }

            TraversingHistory = false;
        }

        private void addToHistory()
        {
            //Remove the part of the history branch that are no longer needed
            if (selectionHistory.Count - 1 > selectionHistoryIndex)
                selectionHistory.RemoveRange(selectionHistoryIndex, selectionHistory.Count - selectionHistoryIndex);

            string path = AssetDatabase.GUIDToAssetPath(selectedAssetGUID);

            if (selectionHistory.Count == 0 || path != selectionHistory.Last())
            {
                selectionHistory.Add(AssetDatabase.GUIDToAssetPath(selectedAssetGUID));
                selectionHistoryIndex = selectionHistory.Count - 1;
            }
        }

        #region serializationHelpers

        [SerializeField] private List<string> _keysFrom = new();
        [SerializeField] private List<AH_WrapperList> _wrapperValuesFrom = new();

        [SerializeField] private List<string> _keysTo = new();
        [SerializeField] private List<AH_WrapperList> _wrapperValuesTo = new();

        #endregion
    }
}

#pragma warning restore 0618