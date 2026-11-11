using UnityEngine;

namespace SnakePost.UIRendering
{
    /// <summary>
    /// One model rendered to transparent UI sprite(s) in Art/UI/Renders/&lt;Category&gt;.
    /// Use the Inspector buttons to match a scene object's angle, preview, and render.
    /// </summary>
    [CreateAssetMenu(menuName = "Snake Post/UI Render", fileName = "UiRender")]
    public sealed class UiRender : ScriptableObject
    {
        [Tooltip("Prefab or model to render, with the materials it should be drawn with.")]
        [SerializeField] internal GameObject model;
        [SerializeField] internal UiRenderView view;
        [Tooltip("Model rotation in front of the camera. 'Match Selected Scene Object' fills it in.")]
        [SerializeField] internal Vector3 modelRotation;
        [Tooltip("Model scale, as in the scene. With the view's Pixels Per Meter it sets the sprite's size.")]
        [SerializeField] internal Vector3 modelScale = Vector3.one;
        [Tooltip("Sub-folder of Assets/_Project/Art/UI/Renders the sprites are written to.")]
        [SerializeField] internal string category = "Misc";
        [Tooltip("Shadow on the invisible ground. Off for things that hang or lean, like the corkboard and its notes.")]
        [SerializeField] internal bool groundShadow = true;
        [Tooltip("Z rotation the UI gives this sprite. The light is turned the opposite way, so once rotated its shading and shadow match the other sprites.")]
        [SerializeField] internal float uiRotation;

        [Header("Frames (optional)")]
        [Tooltip("Render frames of this clip, evenly spread over the Clip Range (e.g. the folder opening).")]
        [SerializeField] internal AnimationClip animation;
        [Tooltip("Part of the clip to render, 0 = start, 1 = end. E.g. 0 to 0.5 when a clip opens then closes again.")]
        [SerializeField] internal Vector2 clipRange = new(0f, 1f);
        [Tooltip("Spin the model once around its up axis over the frames (e.g. perks in the picker).")]
        [SerializeField] internal bool turntable;
        [Tooltip("1 = a single sprite. More = a numbered sequence for a flipbook.")]
        [SerializeField, Min(1)] internal int frames = 1;

        internal string OutputFolder => $"{UiRenderer.Root}/{category}";
        internal int FrameCount => animation != null || turntable ? Mathf.Max(1, frames) : 1;

        /// <summary>Where frame <paramref name="frame"/> is written: Name.png, or Name_00.png… for a sequence.</summary>
        internal string OutputPath(int frame) =>
            FrameCount == 1 ? $"{OutputFolder}/{name}.png" : $"{OutputFolder}/{name}_{frame:00}.png";
    }
}
