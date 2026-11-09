using UnityEditor;
using UnityEngine;

namespace HeurekaGames.AssetHunterPRO
{
    internal class AH_TreeViewTotalsInfo
    {
        public const float Height = 20f;

        private long totalFileSize;
        private long totalAssetSize;
        private bool includeAssetSize;

        private int selectedCount;
        private long selectedFileSize;
        private long selectedAssetSize;

        internal void SetTotals(long fileSize, long assetSize, bool includeAssetSize)
        {
            this.totalFileSize = fileSize;
            this.totalAssetSize = assetSize;
            this.includeAssetSize = includeAssetSize;
        }

        internal void SetSelectedTotals(int count, long fileSize, long assetSize)
        {
            this.selectedCount = count;
            this.selectedFileSize = fileSize;
            this.selectedAssetSize = assetSize;
        }

        internal void OnGUITotals(Rect rect)
        {
            GUI.Box(rect, GUIContent.none, EditorStyles.toolbar);

            string label = "Total — " + sizesLabel(totalFileSize, totalAssetSize);

            if (selectedCount > 0)
                label += $"     Selected ({selectedCount}) — " + sizesLabel(selectedFileSize, selectedAssetSize);

            GUIContent content = new GUIContent(label, "Total: combined size of every asset currently matching the Used/Unused/All filter and search text above.\nSelected: combined size of the currently selected items (folders counted recursively, respecting the same filter).");

            GUI.Label(rect, content, EditorStyles.toolbarButton);
        }

        private string sizesLabel(long fileSize, long assetSize)
        {
            return includeAssetSize
                ? $"Disc size: {AH_Utils.GetSizeAsString(fileSize)}, Asset size: {AH_Utils.GetSizeAsString(assetSize)}"
                : $"Disc size: {AH_Utils.GetSizeAsString(fileSize)}";
        }
    }
}
