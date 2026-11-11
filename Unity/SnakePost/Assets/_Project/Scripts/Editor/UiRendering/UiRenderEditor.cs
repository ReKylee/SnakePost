using UnityEditor;
using UnityEngine;

namespace SnakePost.UIRendering
{
    /// <summary>Inspector for a UI render: the fields, plus a shortcut into the UI Render Studio window.</summary>
    [CustomEditor(typeof(UiRender)), CanEditMultipleObjects]
    internal sealed class UiRenderEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("Open in UI Render Studio", GUILayout.Height(28f)))
                UiRenderStudioWindow.Open((UiRender)target);
            EditorGUILayout.Space();
            DrawDefaultInspector();
            if (GUILayout.Button(targets.Length > 1 ? $"Render {targets.Length} Assets" : "Render"))
                foreach (UiRender each in targets)
                    if (each.model != null && each.view != null)
                        UiRenderer.Save(each);
        }

        /// <summary>
        /// Copies the angle and scale of the object selected in the Hierarchy, as the Main Camera sees it,
        /// so the render shows the same side and tilt as in the scene.
        /// </summary>
        internal static void MatchSceneObject(UiRender render)
        {
            var selected = Selection.activeTransform;
            var camera = Camera.main;
            if (selected == null || EditorUtility.IsPersistent(selected) || camera == null)
            {
                EditorUtility.DisplayDialog("Match Scene Selection",
                    "Select the object in the Hierarchy (not in the Project window) of a scene with a Main Camera, " +
                    "e.g. the UiRenderStudio scene. It is matched as seen from that camera.", "OK");
                return;
            }

            var renderers = selected.GetComponentsInChildren<Renderer>();
            var center = selected.position;
            if (renderers.Length > 0)
            {
                var bounds = renderers[0].bounds;
                foreach (var renderer in renderers)
                    bounds.Encapsulate(renderer.bounds);
                center = bounds.center;
            }

            var look = Quaternion.LookRotation(center - camera.transform.position, camera.transform.up);
            Undo.RecordObject(render, "Match Scene Selection");
            render.modelRotation = (Quaternion.Euler(render.view.cameraRotation) * Quaternion.Inverse(look) * selected.rotation).eulerAngles;
            render.modelScale = selected.lossyScale;
            EditorUtility.SetDirty(render);
        }
    }
}
