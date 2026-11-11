using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SnakePost.UIRendering
{
    /// <summary>Renders <see cref="UiRender"/> assets to trimmed, transparent PNG sprites.</summary>
    internal static class UiRenderer
    {
        internal const string Root = "Assets/_Project/Art/UI/Renders";
        private const int MaxSize = 4096;
        // shortcut: borrows the last user layer, which this project leaves unnamed; name it in Tags & Layers if that changes.
        private const int StageLayer = 31;
        private static readonly Vector3 StagePosition = new(0f, -5000f, 0f);

        [MenuItem("Tools/Snake Post/Render All UI Renders")]
        internal static void RenderAll()
        {
            var renders = new System.Collections.Generic.List<UiRender>();
            foreach (var guid in AssetDatabase.FindAssets($"t:{nameof(UiRender)}"))
            {
                var render = AssetDatabase.LoadAssetAtPath<UiRender>(AssetDatabase.GUIDToAssetPath(guid));
                if (render.model != null && render.view != null)
                    renders.Add(render);
            }

            try
            {
                for (var i = 0; i < renders.Count; i++)
                {
                    EditorUtility.DisplayProgressBar("Rendering UI sprites", renders[i].name, (float)i / renders.Count);
                    Save(renders[i]);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        /// <summary>Renders every frame, trims all of them to one shared rectangle and writes them as sprites.</summary>
        internal static void Save(UiRender render)
        {
            var frames = RenderFrames(render, 1f);
            try
            {
                var crop = SharedCrop(frames, render.view.padding);
                Directory.CreateDirectory(render.OutputFolder);
                for (var i = 0; i < frames.Length; i++)
                {
                    var path = render.OutputPath(i);
                    var trimmed = Crop(frames[i], crop);
                    File.WriteAllBytes(path, trimmed.EncodeToPNG());
                    Object.DestroyImmediate(trimmed);

                    AssetDatabase.ImportAsset(path);
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

        /// <summary>One frame, trimmed, at a fraction of the final detail. The caller destroys it.</summary>
        internal static Texture2D Preview(UiRender render, float detail, int frame = 0)
        {
            var frames = RenderFrames(render, detail, Mathf.Clamp(frame, 0, render.FrameCount - 1));
            var preview = Crop(frames[0], SharedCrop(frames, 2));
            Object.DestroyImmediate(frames[0]);
            return preview;
        }

        // All frames, or only one (onlyFrame >= 0). The camera is always fitted to every frame, so a preview of
        // one frame is framed exactly like the saved sequence.
        private static Texture2D[] RenderFrames(UiRender render, float detail, int onlyFrame = -1)
        {
            var view = render.view;
            var count = render.FrameCount;
            // A hidden, unsaved stage far below the open scene, on a layer only its own camera and light see.
            var stage = new GameObject("UI Render Stage") { hideFlags = HideFlags.HideAndDontSave };
            stage.transform.position = StagePosition;
            var catcher = new Material(Shader.Find("Hidden/SnakePost/Shadow Catcher"));
            RenderTexture target = null;
            var sceneSun = RenderSettings.sun;
            var sceneLightMasks = new System.Collections.Generic.Dictionary<Light, int>();
            var pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            var shadowDistance = pipeline != null ? pipeline.shadowDistance : 0f;
            var shadowCascades = pipeline != null ? pipeline.shadowCascadeCount : 1;
            var shadowResolution = pipeline != null ? pipeline.mainLightShadowmapResolution : 0;
            try
            {
                var model = Object.Instantiate(render.model, stage.transform);
                foreach (var skinned in model.GetComponentsInChildren<SkinnedMeshRenderer>())
                    skinned.updateWhenOffscreen = true; // bounds follow the animation, for framing

                void Pose(int frame)
                {
                    var spin = render.turntable ? 360f * frame / count : 0f;
                    model.transform.SetPositionAndRotation(StagePosition,
                        Quaternion.Euler(render.modelRotation) * Quaternion.Euler(0f, spin, 0f));
                    model.transform.localScale = render.modelScale;
                    if (render.animation != null)
                    {
                        var progress = count > 1 ? (float)frame / (count - 1) : 0f;
                        var normalized = Mathf.Lerp(render.clipRange.x, render.clipRange.y, progress);
                        render.animation.SampleAnimation(model, render.animation.length * Mathf.Clamp01(normalized));
                    }
                }

                // One camera for all frames, fitted to everything the frames cover, so a flipbook doesn't jitter.
                Pose(0);
                var bounds = BoundsOf(model);
                for (var i = 1; i < count; i++)
                {
                    Pose(i);
                    bounds.Encapsulate(BoundsOf(model));
                }

                // Frame the shadow too: the model's box, projected along the light onto the ground.
                var framing = bounds;
                var lightDirection = Quaternion.Euler(view.lightRotation) * Vector3.forward;
                if (view.shadowStrength > 0f && render.groundShadow && lightDirection.y < -.1f)
                    for (var corner = 0; corner < 8; corner++)
                    {
                        var c = Corner(bounds, corner);
                        framing.Encapsulate(c + lightDirection * ((c.y - bounds.min.y) / -lightDirection.y));
                    }

                var cameraRotation = Quaternion.Euler(view.cameraRotation);
                var cameraPosition = framing.center - cameraRotation * Vector3.forward * view.distance;
                var toCamera = Quaternion.Inverse(cameraRotation);
                float tanX = 0f, tanY = 0f;
                for (var corner = 0; corner < 8; corner++)
                {
                    var p = toCamera * (Corner(framing, corner) - cameraPosition);
                    if (p.z <= 0f)
                        throw new System.InvalidOperationException(
                            $"{render.name}: the model is larger than the view's camera distance. Increase Distance on '{view.name}'.");
                    tanX = Mathf.Max(tanX, Mathf.Abs(p.x) / p.z);
                    tanY = Mathf.Max(tanY, Mathf.Abs(p.y) / p.z);
                }

                // Pixels per meter measured at the model's distance keeps every render at the same detail level.
                var width = Mathf.CeilToInt(2f * view.distance * tanX * view.pixelsPerMeter * detail);
                var height = Mathf.CeilToInt(2f * view.distance * tanY * view.pixelsPerMeter * detail);
                var shrink = Mathf.Min(1f, (float)MaxSize / Mathf.Max(width, height));
                width = Mathf.Max(8, Mathf.RoundToInt(width * shrink));
                height = Mathf.Max(8, Mathf.RoundToInt(height * shrink));

                var camera = new GameObject("Camera").AddComponent<Camera>();
                camera.transform.SetParent(stage.transform);
                camera.enabled = false;
                camera.cullingMask = 1 << StageLayer;
                camera.transform.SetPositionAndRotation(cameraPosition, cameraRotation);
                camera.fieldOfView = 2f * Mathf.Atan(tanY) * Mathf.Rad2Deg;
                camera.aspect = tanX / tanY;
                camera.nearClipPlane = .05f;
                camera.farClipPlane = view.distance * 4f + framing.size.magnitude;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.clear;

                var light = new GameObject("Light").AddComponent<Light>();
                light.transform.SetParent(stage.transform);
                light.type = LightType.Directional;
                light.cullingMask = 1 << StageLayer;
                light.transform.rotation = Quaternion.Euler(view.lightRotation);
                light.color = view.lightColor;
                light.intensity = view.lightIntensity;
                light.shadows = LightShadows.Soft;
                light.shadowStrength = 1f;

                if (view.shadowStrength > 0f && render.groundShadow)
                {
                    // An invisible floor that only shows the shadow, as see-through darkness in the PNG.
                    var ground = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    Object.DestroyImmediate(ground.GetComponent<Collider>());
                    ground.transform.SetParent(stage.transform);
                    ground.transform.SetPositionAndRotation(new Vector3(bounds.center.x, bounds.min.y, bounds.center.z),
                        Quaternion.Euler(90f, 0f, 0f));
                    ground.transform.localScale = Vector3.one * (bounds.size.magnitude * 8f + 10f);
                    catcher.SetFloat("_Strength", view.shadowStrength);
                    ground.GetComponent<MeshRenderer>().sharedMaterial = catcher;
                }

                foreach (var t in stage.GetComponentsInChildren<Transform>(true))
                {
                    t.gameObject.layer = StageLayer;
                    t.gameObject.hideFlags = HideFlags.HideAndDontSave;
                }

                // URP always treats the scene's sun as the main (shadow-casting) light, so for the duration of the
                // render the stage light takes that role and the scene's lights skip the stage layer. Restored below;
                // these runtime changes don't mark the scene as modified.
                sceneSun = RenderSettings.sun;
                RenderSettings.sun = light;
                // The view's own ambient light, given to each model renderer directly (not the scene's ambient
                // setting, which would mark the open scene as modified), so renders look the same in any scene.
                var ambient = new SphericalHarmonicsL2();
                ambient.AddAmbientLight(view.ambientColor.linear);
                var ambientBlock = new MaterialPropertyBlock();
                ambientBlock.CopySHCoefficientArraysFrom(new[] { ambient });
                foreach (var modelRenderer in model.GetComponentsInChildren<Renderer>())
                {
                    modelRenderer.lightProbeUsage = LightProbeUsage.CustomProvided;
                    modelRenderer.SetPropertyBlock(ambientBlock);
                }
                // Spend the whole shadow map on this small stage instead of the scene's 50 m.
                if (pipeline != null)
                {
                    pipeline.shadowDistance = view.distance + framing.size.magnitude;
                    pipeline.shadowCascadeCount = 1;
                    pipeline.mainLightShadowmapResolution = 4096;
                }
                foreach (var sceneLight in Object.FindObjectsByType<Light>())
                {
                    if (sceneLight == light)
                        continue;
                    sceneLightMasks[sceneLight] = sceneLight.cullingMask;
                    sceneLight.cullingMask &= ~(1 << StageLayer);
                }

                // No MSAA on the target: URP anti-aliases in its own buffer and resolves into this one.
                target = new RenderTexture(new RenderTextureDescriptor(width, height, RenderTextureFormat.ARGB32, 24)
                {
                    sRGB = QualitySettings.activeColorSpace == ColorSpace.Linear
                });
                camera.targetTexture = target;

                var first = onlyFrame >= 0 ? onlyFrame : 0;
                var frames = new Texture2D[onlyFrame >= 0 ? 1 : count];
                for (var i = 0; i < frames.Length; i++)
                {
                    Pose(first + i);
                    camera.Render();
                    var previous = RenderTexture.active;
                    RenderTexture.active = target;
                    frames[i] = new Texture2D(width, height, TextureFormat.RGBA32, false);
                    frames[i].ReadPixels(new Rect(0, 0, width, height), 0, 0);
                    frames[i].Apply();
                    RenderTexture.active = previous;
                }

                return frames;
            }
            finally
            {
                RenderSettings.sun = sceneSun;
                if (pipeline != null)
                {
                    pipeline.shadowDistance = shadowDistance;
                    pipeline.shadowCascadeCount = shadowCascades;
                    pipeline.mainLightShadowmapResolution = shadowResolution;
                }
                foreach (var pair in sceneLightMasks)
                    if (pair.Key != null)
                        pair.Key.cullingMask = pair.Value;
                Object.DestroyImmediate(stage); // camera, light, model and floor are its children
                if (target != null)
                    Object.DestroyImmediate(target);
                Object.DestroyImmediate(catcher);
            }
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
}
