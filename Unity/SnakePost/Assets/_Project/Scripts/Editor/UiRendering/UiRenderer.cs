using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SnakePost.UIRendering
{
    /// <summary>Writes <see cref="UiRender"/> assets to trimmed, transparent PNG sprites.</summary>
    internal static class UiRenderer
    {
        internal const string Root = "Assets/_Project/Art/UI/Renders";

        [MenuItem("Tools/Snake Post/Render All UI Renders")]
        internal static void RenderAll() => Save(FindAll(), "Rendering UI sprites");

        internal static List<UiRender> FindAll()
        {
            var renders = new List<UiRender>();
            foreach (var guid in AssetDatabase.FindAssets($"t:{nameof(UiRender)}"))
            {
                var render = AssetDatabase.LoadAssetAtPath<UiRender>(AssetDatabase.GUIDToAssetPath(guid));
                if (render != null)
                    renders.Add(render);
            }

            return renders;
        }

        internal static void Save(IList<UiRender> renders, string title)
        {
            try
            {
                for (var i = 0; i < renders.Count; i++)
                {
                    if (renders[i].model == null || renders[i].view == null)
                        continue;
                    EditorUtility.DisplayProgressBar(title, renders[i].name, (float)i / renders.Count);
                    Save(renders[i]);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        /// <summary>
        /// Renders every frame, trims all of them to one shared rectangle and overwrites the PNGs in place.
        /// Only the pixels change: file names and asset IDs stay, so UI Images pick up the new picture, and
        /// import settings are only set up for brand-new files (a designer's later changes are kept).
        /// </summary>
        internal static void Save(UiRender render)
        {
            var frames = new Texture2D[render.FrameCount];
            using (var stage = new UiRenderStage(render))
            {
                stage.Refit();
                for (var i = 0; i < frames.Length; i++)
                    frames[i] = stage.ReadFrame(i, 1f);
            }

            try
            {
                var crop = SharedCrop(frames, render.view.padding);
                Directory.CreateDirectory(render.OutputFolder);
                for (var i = 0; i < frames.Length; i++)
                {
                    var path = render.OutputPath(i);
                    var isNew = !File.Exists(path);
                    var trimmed = Crop(frames[i], crop);
                    File.WriteAllBytes(path, trimmed.EncodeToPNG());
                    Object.DestroyImmediate(trimmed);

                    AssetDatabase.ImportAsset(path);
                    if (!isNew)
                        continue;
                    var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.alphaIsTransparency = true;
                    importer.mipmapEnabled = false;
                    importer.SaveAndReimport();
                }
            }
            finally
            {
                foreach (var frame in frames)
                    Object.DestroyImmediate(frame);
            }
        }

        // Union of every frame's visible pixels, grown by the padding (may extend past the render's edge).
        private static RectInt SharedCrop(Texture2D[] frames, int padding)
        {
            int xMin = int.MaxValue, yMin = int.MaxValue, xMax = int.MinValue, yMax = int.MinValue;
            foreach (var frame in frames)
            {
                var pixels = frame.GetPixels32();
                for (var y = 0; y < frame.height; y++)
                for (var x = 0; x < frame.width; x++)
                {
                    if (pixels[y * frame.width + x].a <= 2)
                        continue;
                    xMin = Mathf.Min(xMin, x);
                    yMin = Mathf.Min(yMin, y);
                    xMax = Mathf.Max(xMax, x);
                    yMax = Mathf.Max(yMax, y);
                }
            }

            if (xMax < xMin)
                return new RectInt(0, 0, frames[0].width, frames[0].height);
            return new RectInt(xMin - padding, yMin - padding, xMax - xMin + 1 + 2 * padding, yMax - yMin + 1 + 2 * padding);
        }

        private static Texture2D Crop(Texture2D source, RectInt rect)
        {
            var result = new Texture2D(rect.width, rect.height, TextureFormat.RGBA32, false);
            var pixels = new Color32[rect.width * rect.height];
            var from = source.GetPixels32();
            for (var y = 0; y < rect.height; y++)
            for (var x = 0; x < rect.width; x++)
            {
                int sx = rect.x + x, sy = rect.y + y;
                if (sx >= 0 && sy >= 0 && sx < source.width && sy < source.height)
                    pixels[y * rect.width + x] = from[sy * source.width + sx];
            }

            result.SetPixels32(pixels);
            result.Apply();
            return result;
        }
    }

    /// <summary>
    /// A hidden stage for one UI render: the model, its camera, light and shadow floor. It sits far below the
    /// open scene (beyond any camera's far clip) on a layer of its own, is never saved, and is only switched on
    /// for the instant of a render, so scenes are never changed. Keep one alive for fast live previews.
    /// </summary>
    internal sealed class UiRenderStage : System.IDisposable
    {
        private const int MaxSize = 4096;
        // shortcut: borrows the last user layer, which this project leaves unnamed; name it in Tags & Layers if that changes.
        private const int Layer = 31;
        private static readonly Vector3 Origin = new(0f, -20000f, 0f);

        private readonly UiRender _render;
        private readonly GameObject _root;
        private readonly GameObject _model;
        private readonly GameObject _ground;
        private readonly Camera _camera;
        private readonly Light _light;
        private readonly Material _catcher;
        private RenderTexture _target;
        private Bounds _bounds;
        private Bounds _framing;
        private float _tanX;
        private float _tanY;

        public UiRenderStage(UiRender render)
        {
            _render = render;
            _root = new GameObject("UI Render Stage") { hideFlags = HideFlags.HideAndDontSave };
            _root.transform.position = Origin;
            _model = Object.Instantiate(render.model, _root.transform);
            foreach (var skinned in _model.GetComponentsInChildren<SkinnedMeshRenderer>())
                skinned.updateWhenOffscreen = true; // bounds follow the animation, for framing

            _camera = new GameObject("Camera").AddComponent<Camera>();
            _camera.transform.SetParent(_root.transform);
            _camera.enabled = false;
            _camera.cullingMask = 1 << Layer;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = Color.clear;

            _light = new GameObject("Light").AddComponent<Light>();
            _light.transform.SetParent(_root.transform);
            _light.type = LightType.Directional;
            _light.cullingMask = 1 << Layer;
            _light.shadows = LightShadows.Soft;
            _light.shadowStrength = 1f;

            _catcher = new Material(Shader.Find("Hidden/SnakePost/Shadow Catcher"));
            _catcher.SetTexture("_PencilMap", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/PaperLook/PencilPaper.png"));
            _ground = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.DestroyImmediate(_ground.GetComponent<Collider>());
            _ground.transform.SetParent(_root.transform);
            _ground.GetComponent<MeshRenderer>().sharedMaterial = _catcher;

            foreach (var t in _root.GetComponentsInChildren<Transform>(true))
            {
                t.gameObject.layer = Layer;
                t.gameObject.hideFlags = HideFlags.HideAndDontSave;
            }

            _root.SetActive(false);
        }

        /// <summary>Re-reads the render and its view, and fits the camera around every frame and the shadow.</summary>
        public void Refit()
        {
            var view = _render.view;
            _root.SetActive(true); // renderer bounds are only valid on active objects
            try
            {
                Pose(0);
                _bounds = BoundsOf(_model);
                for (var i = 1; i < _render.FrameCount; i++)
                {
                    Pose(i);
                    _bounds.Encapsulate(BoundsOf(_model));
                }
            }
            finally
            {
                _root.SetActive(false);
            }

            var rotation = Quaternion.Euler(view.cameraRotation);
            var lightDirection = TurnedLight(Quaternion.Euler(view.lightRotation) * Vector3.forward, rotation, _render.uiRotation);

            // Frame the shadow too: the model's box, projected along the light onto the ground.
            var shadow = view.shadowStrength > 0f && _render.groundShadow;
            _framing = _bounds;
            if (shadow && lightDirection.y < -.1f)
                for (var corner = 0; corner < 8; corner++)
                {
                    var c = Corner(_bounds, corner);
                    _framing.Encapsulate(c + lightDirection * ((c.y - _bounds.min.y) / -lightDirection.y));
                }

            // Aimed at the model, not the model plus shadow, so the model's size never depends on its shadow.
            var position = _bounds.center - rotation * Vector3.forward * view.distance;
            var toCamera = Quaternion.Inverse(rotation);
            _tanX = _tanY = 0f;
            for (var corner = 0; corner < 8; corner++)
            {
                var p = toCamera * (Corner(_framing, corner) - position);
                if (p.z <= 0f)
                    throw new System.InvalidOperationException(
                        $"{_render.name}: the model is larger than the view's camera distance. Increase Distance on '{view.name}'.");
                _tanX = Mathf.Max(_tanX, Mathf.Abs(p.x) / p.z);
                _tanY = Mathf.Max(_tanY, Mathf.Abs(p.y) / p.z);
            }

            _camera.transform.SetPositionAndRotation(position, rotation);
            _camera.fieldOfView = 2f * Mathf.Atan(_tanY) * Mathf.Rad2Deg;
            _camera.aspect = _tanX / _tanY;
            _camera.nearClipPlane = .05f;
            _camera.farClipPlane = view.distance * 4f + _framing.size.magnitude;

            _light.transform.rotation = Quaternion.LookRotation(lightDirection);
            _light.color = view.lightColor;
            _light.intensity = view.lightIntensity;

            // An invisible floor that only shows the shadow, as see-through pencil shading in the PNG.
            _ground.SetActive(shadow);
            _ground.transform.SetPositionAndRotation(new Vector3(_bounds.center.x, _bounds.min.y, _bounds.center.z), Quaternion.Euler(90f, 0f, 0f));
            _ground.transform.localScale = Vector3.one * (_bounds.size.magnitude * 8f + 10f);
            _catcher.SetFloat("_Strength", view.shadowStrength);
            _catcher.SetFloat("_Hatching", view.shadowHatching);

            // The view's own ambient light, given to each model renderer directly (not the scene's ambient
            // setting), so renders look the same whichever scene is open.
            var ambient = new SphericalHarmonicsL2();
            ambient.AddAmbientLight(view.ambientColor.linear);
            var block = new MaterialPropertyBlock();
            block.CopySHCoefficientArraysFrom(new[] { ambient });
            foreach (var renderer in _model.GetComponentsInChildren<Renderer>(true))
            {
                renderer.lightProbeUsage = LightProbeUsage.CustomProvided;
                renderer.SetPropertyBlock(block);
            }
        }

        /// <summary>Output size in pixels at the given detail (1 = the size the PNG is rendered at, before trimming).</summary>
        public Vector2Int Size(float detail)
        {
            var view = _render.view;
            var width = 2f * view.distance * _tanX * view.pixelsPerMeter * detail;
            var height = 2f * view.distance * _tanY * view.pixelsPerMeter * detail;
            var shrink = Mathf.Min(1f, MaxSize / Mathf.Max(width, height));
            return new Vector2Int(Mathf.Max(8, Mathf.RoundToInt(width * shrink)), Mathf.Max(8, Mathf.RoundToInt(height * shrink)));
        }

        /// <summary>Renders one frame into the stage's own texture (valid until the next render). Call Refit first.</summary>
        public RenderTexture Render(int frame, float detail)
        {
            var size = Size(detail);
            if (_target == null || _target.width != size.x || _target.height != size.y)
            {
                if (_target != null)
                    Object.DestroyImmediate(_target);
                // No MSAA on the target: URP anti-aliases in its own buffer and resolves into this one.
                _target = new RenderTexture(new RenderTextureDescriptor(size.x, size.y, RenderTextureFormat.ARGB32, 24)
                {
                    sRGB = QualitySettings.activeColorSpace == ColorSpace.Linear
                }) { hideFlags = HideFlags.HideAndDontSave };
            }

            _camera.targetTexture = _target;
            var view = _render.view;
            var sceneSun = RenderSettings.sun;
            var asyncShaders = ShaderUtil.allowAsyncCompilation;
            var pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            var shadowDistance = pipeline != null ? pipeline.shadowDistance : 0f;
            var shadowCascades = pipeline != null ? pipeline.shadowCascadeCount : 1;
            var shadowResolution = pipeline != null ? pipeline.mainLightShadowmapResolution : 0;
            _root.SetActive(true);
            try
            {
                Pose(frame);
                // For this one render: URP always treats the scene's sun as the main (shadow-casting) light, so the
                // stage light takes that role, and the whole shadow map is spent on the stage. Scene lights don't
                // reach the stage: they sit on layers the stage camera doesn't see. Never touch scene objects here
                // (even a restored property change marks the scene as modified). All restored below.
                RenderSettings.sun = _light;
                // A shader still compiling in the background would be skipped, e.g. losing the shadow.
                ShaderUtil.allowAsyncCompilation = false;

                if (pipeline != null)
                {
                    pipeline.shadowDistance = view.distance + _framing.extents.magnitude + .5f;
                    pipeline.shadowCascadeCount = 1;
                    pipeline.mainLightShadowmapResolution = 4096;
                }

                _camera.Render();
            }
            finally
            {
                RenderSettings.sun = sceneSun;
                ShaderUtil.allowAsyncCompilation = asyncShaders;
                if (pipeline != null)
                {
                    pipeline.shadowDistance = shadowDistance;
                    pipeline.shadowCascadeCount = shadowCascades;
                    pipeline.mainLightShadowmapResolution = shadowResolution;
                }

                _root.SetActive(false);
            }

            return _target;
        }

        /// <summary>Renders one frame and copies it into a new texture. The caller destroys it.</summary>
        public Texture2D ReadFrame(int frame, float detail)
        {
            var target = Render(frame, detail);
            var previous = RenderTexture.active;
            RenderTexture.active = target;
            var texture = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            texture.Apply();
            RenderTexture.active = previous;
            return texture;
        }

        /// <summary>The model's local axes as seen on screen (x right, y up), for an orientation gizmo.</summary>
        public Vector2 ScreenAxis(Vector3 localAxis)
        {
            var world = Quaternion.Euler(_render.modelRotation) * localAxis;
            var onScreen = Quaternion.Inverse(Quaternion.Euler(_render.view.cameraRotation)) * world;
            return new Vector2(onScreen.x, onScreen.y);
        }

        private void Pose(int frame)
        {
            var count = _render.FrameCount;
            var spin = _render.turntable ? 360f * frame / count : 0f;
            _model.transform.SetPositionAndRotation(Origin, Quaternion.Euler(_render.modelRotation) * Quaternion.Euler(0f, spin, 0f));
            _model.transform.localScale = _render.modelScale;
            if (_render.animation == null)
                return;
            var progress = count > 1 ? (float)frame / (count - 1) : 0f;
            var normalized = Mathf.Lerp(_render.clipRange.x, _render.clipRange.y, progress);
            _render.animation.SampleAnimation(_model, _render.animation.length * Mathf.Clamp01(normalized));
        }

        /// <summary>
        /// For a sprite the UI turns by <paramref name="uiRotation"/> degrees (counter-clockwise): a light whose
        /// shadows point that much clockwise on screen, at the same on-screen length. Once the UI turns the sprite,
        /// its shadow falls like every other sprite's.
        /// </summary>
        internal static Vector3 TurnedLight(Vector3 light, Quaternion camera, float uiRotation)
        {
            var forward = camera * Vector3.forward;
            if (Mathf.Approximately(uiRotation % 360f, 0f) || Mathf.Abs(forward.y) < .05f || light.y > -.05f)
                return light;
            // A shadow lands light / -light.y away per meter of height. Turn the on-screen part of that offset, then
            // pick the depth part so the light drops as much as before: same shadow direction (turned) and length.
            var offset = (Vector2)(Quaternion.Euler(0f, 0f, -uiRotation) * (Quaternion.Inverse(camera) * light));
            var drop = offset.x * (camera * Vector3.right).y + offset.y * (camera * Vector3.up).y;
            return (camera * new Vector3(offset.x, offset.y, (light.y - drop) / forward.y)).normalized;
        }

        private static Bounds BoundsOf(GameObject model)
        {
            var renderers = model.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                throw new System.InvalidOperationException($"'{model.name}' has no renderers to draw.");

            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers)
                bounds.Encapsulate(renderer.bounds);
            return bounds;
        }

        private static Vector3 Corner(Bounds b, int i) => b.center + Vector3.Scale(b.extents,
            new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));

        public void Dispose()
        {
            Object.DestroyImmediate(_root);
            if (_target != null)
                Object.DestroyImmediate(_target);
            Object.DestroyImmediate(_catcher);
        }
    }
}
