using UnityEngine;

namespace SnakePost.UIRendering
{
    /// <summary>
    /// Shared camera and lighting for UI renders, so every model is drawn from the same point of view.
    /// The perspective comes from the camera distance; the frame is fitted to each model automatically.
    /// Lighting lives here too, so renders look the same whichever scene is open.
    /// </summary>
    [CreateAssetMenu(menuName = "Snake Post/UI Render View", fileName = "UiRenderView")]
    public sealed class UiRenderView : ScriptableObject
    {
        [Tooltip("Camera rotation. The desk menu camera looks down at 46°.")]
        [SerializeField] internal Vector3 cameraRotation = new(46f, 0f, 0f);
        [Tooltip("Meters from the camera to the model. Same distance as the desk camera = same perspective as the scene.")]
        [SerializeField, Min(.5f)] internal float distance = 15f;
        [Tooltip("Detail level. Same value on every render keeps models at consistent relative sizes.")]
        [SerializeField, Min(10f)] internal float pixelsPerMeter = 220f;
        [Tooltip("Empty border around the trimmed render, in pixels.")]
        [SerializeField, Min(0)] internal int padding = 8;

        [Header("Light")]
        [SerializeField] internal Vector3 lightRotation = new(50f, -30f, 0f);
        [SerializeField] internal Color lightColor = new(.99f, .82f, .62f);
        [SerializeField, Min(0f)] internal float lightIntensity = 1.05f;
        [Tooltip("Soft light from all around (fills the shadow sides).")]
        [SerializeField] internal Color ambientColor = new(.663f, .6f, .525f);

        [Header("Shadow")]
        [Tooltip("Darkness of the shadow cast on the (invisible) ground under the model. 0 = no ground shadow.")]
        [SerializeField, Range(0f, 1f)] internal float shadowStrength = .55f;

        [ContextMenu("Copy From Scene Camera And Sun")]
        private void CopyFromScene()
        {
            UnityEditor.Undo.RecordObject(this, "Copy UI Render View From Scene");
            if (Camera.main != null)
                cameraRotation = Camera.main.transform.eulerAngles;
            if (RenderSettings.sun != null)
            {
                lightRotation = RenderSettings.sun.transform.eulerAngles;
                lightColor = RenderSettings.sun.color;
                lightIntensity = RenderSettings.sun.intensity;
            }
            ambientColor = RenderSettings.ambientLight;
            UnityEditor.EditorUtility.SetDirty(this);
        }
    }
}
