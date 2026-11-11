using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SnakePost.UIRendering
{
    /// <summary>
    /// Window ▸ Snake Post ▸ UI Render Studio: browse every UI render, pose the model by dragging the live preview,
    /// and write the sprites. Works from any open scene.
    /// </summary>
    internal sealed class UiRenderStudioWindow : EditorWindow
    {
        private const string PresetFolder = "Assets/_Project/Art/UI/RenderPresets";
        private const float PreviewDetail = .8f;
        private const float DragDetail = .25f;
        private const float DragDegreesPerPixel = .4f;
        private static readonly Color Desk = new(.78f, .5f, .33f);
        private static readonly string[] Backgrounds = { "Checker", "Desk", "White" };

        private List<UiRender> _renders = new();
        private UiRender _selected;
        private Texture2D _preview;
        private Vector2 _listScroll;
        private Vector2 _detailScroll;
        private string _search = string.Empty;
        private int _frame;
        private int _background = 1;
        private bool _showView;
        private bool _dragging;

        [MenuItem("Window/Snake Post/UI Render Studio")]
        internal static void Open() => GetWindow<UiRenderStudioWindow>("UI Render Studio");

        internal static void Open(UiRender render) => GetWindow<UiRenderStudioWindow>("UI Render Studio").Select(render);

        private void OnEnable()
        {
            minSize = new Vector2(640f, 480f);
            Reload();
        }

        private void OnProjectChange() => Reload();
        private void OnDisable() => DestroyImmediate(_preview);

        private void Reload()
        {
            _renders = AssetDatabase.FindAssets($"t:{nameof(UiRender)}")
                .Select(guid => AssetDatabase.LoadAssetAtPath<UiRender>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(render => render != null)
                .OrderBy(render => render.category).ThenBy(render => render.name)
                .ToList();
            if (_selected == null && _renders.Count > 0)
                Select(_renders[0]);
            Repaint();
        }

        private void Select(UiRender render)
        {
            _selected = render;
            _frame = 0;
            RefreshPreview(PreviewDetail);
            Repaint();
        }

        private void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button(new GUIContent("New Render", "Creates a render for the prefab or model selected in the Project window."), EditorStyles.toolbarButton))
                    CreateRender();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(new GUIContent("Render All", "Re-renders every UI sprite, e.g. after changing a shared view."), EditorStyles.toolbarButton))
                    UiRenderer.RenderAll();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawList();
                DrawDetail();
            }
        }

        private void DrawList()
        {
            using var _ = new EditorGUILayout.VerticalScope(GUILayout.Width(230f));
            _search = EditorGUILayout.TextField(_search, EditorStyles.toolbarSearchField);
            _listScroll = EditorGUILayout.BeginScrollView(_listScroll);
            string category = null;
            foreach (var render in _renders)
            {
                if (_search.Length > 0 && render.name.IndexOf(_search, System.StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                if (render.category != category)
                {
                    category = render.category;
                    EditorGUILayout.LabelField(category, EditorStyles.boldLabel);
                }

                var row = GUILayoutUtility.GetRect(0f, 44f, GUILayout.ExpandWidth(true));
                if (render == _selected)
                    EditorGUI.DrawRect(row, new Color(.24f, .48f, .9f, .35f));
                var thumbnail = AssetDatabase.LoadAssetAtPath<Texture2D>(render.OutputPath(0));
                var icon = new Rect(row.x + 4f, row.y + 2f, 40f, 40f);
                if (thumbnail != null)
                    GUI.DrawTexture(icon, thumbnail, ScaleMode.ScaleToFit);
                else
                    GUI.Label(icon, "—", EditorStyles.centeredGreyMiniLabel);
                GUI.Label(new Rect(row.x + 50f, row.y + 6f, row.width - 50f, 18f), render.name);
                GUI.Label(new Rect(row.x + 50f, row.y + 22f, row.width - 50f, 16f),
                    render.FrameCount > 1 ? $"{render.FrameCount} frames" : thumbnail != null ? "rendered" : "not rendered yet",
                    EditorStyles.miniLabel);
                if (Event.current.type == EventType.MouseDown && row.Contains(Event.current.mousePosition))
                {
                    Select(render);
                    Event.current.Use();
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawDetail()
        {
            using var _ = new EditorGUILayout.VerticalScope();
            if (_selected == null)
            {
                EditorGUILayout.HelpBox("Pick a render on the left, or select a prefab or model in the Project window and press New Render.", MessageType.Info);
                return;
            }

            _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll);
            var ready = _selected.model != null && _selected.view != null;

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(_selected.name, EditorStyles.largeLabel);
                using (new EditorGUI.DisabledScope(!ready))
                    if (GUILayout.Button("Render", GUILayout.Width(90f)))
                    {
                        UiRenderer.Save(_selected);
                        Repaint();
                    }

                if (GUILayout.Button("Show PNG", GUILayout.Width(80f)))
                    EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(_selected.OutputPath(0)) ?? _selected);
            }

            EditorGUILayout.LabelField($"Writes to {_selected.OutputPath(0)}", EditorStyles.miniLabel);
            if (!ready)
                EditorGUILayout.HelpBox("Assign a Model and a View below.", MessageType.Warning);
            else
                DrawPreview();

            var render = new SerializedObject(_selected);
            render.Update();
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Pose", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(!ready))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(new GUIContent("◀ Turn", "Turn 45° to the left, as seen from the camera"))) Rotate(Vector3.up, 45f);
                    if (GUILayout.Button(new GUIContent("Turn ▶", "Turn 45° to the right"))) Rotate(Vector3.up, -45f);
                    if (GUILayout.Button(new GUIContent("Tilt ▲", "Tilt the top 15° away from the camera"))) Rotate(Vector3.right, 15f);
                    if (GUILayout.Button(new GUIContent("Tilt ▼", "Tilt the top 15° toward the camera"))) Rotate(Vector3.right, -15f);
                    if (GUILayout.Button(new GUIContent("Roll ↺", "Roll 15° counter-clockwise on screen"))) Rotate(Vector3.forward, 15f);
                    if (GUILayout.Button(new GUIContent("Roll ↻", "Roll 15° clockwise on screen"))) Rotate(Vector3.forward, -15f);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(new GUIContent("Flip Over", "Show the other face (180° around the screen's horizontal axis)"))) Rotate(Vector3.right, 180f);
                    if (GUILayout.Button(new GUIContent("Turn Around", "180° around the screen's vertical axis"))) Rotate(Vector3.up, 180f);
                    if (GUILayout.Button(new GUIContent("Reset", "No rotation"))) SetRotation(Quaternion.identity);
                    if (GUILayout.Button(new GUIContent("Match Scene Selection",
                            "Copies the angle and scale of the object selected in the Hierarchy, as the Main Camera sees it.")))
                    {
                        UiRenderEditor.MatchSceneObject(_selected);
                        RefreshPreview(PreviewDetail);
                    }
                }
            }

            EditorGUILayout.HelpBox("Drag in the preview to rotate. Each sprite keeps its real size relative to the others.", MessageType.None);
            EditorGUILayout.PropertyField(render.FindProperty("model"));
            EditorGUILayout.PropertyField(render.FindProperty("modelRotation"), new GUIContent("Rotation"));
            EditorGUILayout.PropertyField(render.FindProperty("modelScale"), new GUIContent("Scale"));
            EditorGUILayout.PropertyField(render.FindProperty("groundShadow"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(render.FindProperty("category"), new GUIContent("Folder (Category)"));

            var clip = render.FindProperty("animation"); // its own "Frames (optional)" header comes with it
            EditorGUILayout.PropertyField(clip, new GUIContent("Animation Clip"));
            if (clip.objectReferenceValue != null)
            {
                var range = render.FindProperty("clipRange");
                var value = range.vector2Value;
                EditorGUILayout.MinMaxSlider(new GUIContent("Clip Range", "Part of the clip to render"), ref value.x, ref value.y, 0f, 1f);
                range.vector2Value = value;
            }

            EditorGUILayout.PropertyField(render.FindProperty("turntable"));
            EditorGUILayout.PropertyField(render.FindProperty("frames"));

            var changed = EditorGUI.EndChangeCheck();
            render.ApplyModifiedProperties();

            EditorGUILayout.Space();
            var viewProperty = render.FindProperty("view");
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(viewProperty, new GUIContent("View (camera and light)"));
            if (EditorGUI.EndChangeCheck())
            {
                render.ApplyModifiedProperties();
                changed = true;
            }

            if (_selected.view != null && (_showView = EditorGUILayout.Foldout(_showView, "Edit shared view", true)))
                changed |= DrawView(_selected.view);

            EditorGUILayout.EndScrollView();
            if (changed && ready)
                RefreshPreview(PreviewDetail);
        }

        private void DrawPreview()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                _background = GUILayout.Toolbar(_background, Backgrounds, EditorStyles.miniButton, GUILayout.Width(200f));
            }

            var area = GUILayoutUtility.GetRect(0f, 320f, GUILayout.ExpandWidth(true));
            if (_background == 0)
            {
                if (_preview != null)
                    EditorGUI.DrawTextureTransparent(area, _preview, ScaleMode.ScaleToFit);
            }
            else
            {
                EditorGUI.DrawRect(area, _background == 1 ? Desk : Color.white);
                if (_preview != null)
                    GUI.DrawTexture(area, _preview, ScaleMode.ScaleToFit, true);
            }

            if (_preview != null)
                EditorGUI.LabelField(new Rect(area.x + 6f, area.yMax - 18f, area.width, 16f),
                    $"≈ {Mathf.RoundToInt(_preview.width / PreviewDetail)} × {Mathf.RoundToInt(_preview.height / PreviewDetail)} px",
                    EditorStyles.miniLabel);

            HandleDrag(area);

            if (_selected.FrameCount > 1)
            {
                EditorGUI.BeginChangeCheck();
                _frame = EditorGUILayout.IntSlider("Frame", _frame, 0, _selected.FrameCount - 1);
                if (EditorGUI.EndChangeCheck())
                    RefreshPreview(PreviewDetail);
            }
        }

        // Dragging turns the model as seen from the camera: left/right around the screen's vertical axis,
        // up/down around its horizontal axis. A rough preview while dragging, a sharp one on release.
        private void HandleDrag(Rect area)
        {
            var e = Event.current;
            EditorGUIUtility.AddCursorRect(area, MouseCursor.Orbit);
            switch (e.type)
            {
                case EventType.MouseDown when e.button == 0 && area.Contains(e.mousePosition):
                    _dragging = true;
                    Undo.RecordObject(_selected, "Rotate UI Render");
                    e.Use();
                    break;
                case EventType.MouseDrag when _dragging:
                    var camera = Quaternion.Euler(_selected.view.cameraRotation);
                    var delta = Quaternion.AngleAxis(-e.delta.x * DragDegreesPerPixel, camera * Vector3.up)
                                * Quaternion.AngleAxis(-e.delta.y * DragDegreesPerPixel, camera * Vector3.right);
                    _selected.modelRotation = (delta * Quaternion.Euler(_selected.modelRotation)).eulerAngles;
                    EditorUtility.SetDirty(_selected);
                    RefreshPreview(DragDetail);
                    e.Use();
                    break;
                case EventType.MouseUp when _dragging:
                    _dragging = false;
                    RefreshPreview(PreviewDetail);
                    e.Use();
                    break;
            }
        }

        private static bool DrawView(UiRenderView view)
        {
            var users = AssetDatabase.FindAssets($"t:{nameof(UiRender)}")
                .Select(guid => AssetDatabase.LoadAssetAtPath<UiRender>(AssetDatabase.GUIDToAssetPath(guid)))
                .Count(render => render != null && render.view == view);
            EditorGUILayout.HelpBox($"'{view.name}' is shared by {users} render(s). Changes apply to all of them on their next render.", MessageType.Info);

            var serialized = new SerializedObject(view);
            serialized.Update();
            EditorGUI.BeginChangeCheck();
            EditorGUI.indentLevel++;
            var property = serialized.GetIterator();
            property.NextVisible(true);
            while (property.NextVisible(false))
                if (property.name != "m_Script")
                    EditorGUILayout.PropertyField(property, true);
            EditorGUI.indentLevel--;
            var changed = EditorGUI.EndChangeCheck();
            serialized.ApplyModifiedProperties();
            return changed;
        }

        private void Rotate(Vector3 screenAxis, float degrees)
        {
            var camera = Quaternion.Euler(_selected.view.cameraRotation);
            SetRotation(Quaternion.AngleAxis(degrees, camera * screenAxis) * Quaternion.Euler(_selected.modelRotation));
        }

        private void SetRotation(Quaternion rotation)
        {
            Undo.RecordObject(_selected, "Rotate UI Render");
            _selected.modelRotation = rotation.eulerAngles;
            EditorUtility.SetDirty(_selected);
            RefreshPreview(PreviewDetail);
        }

        private void RefreshPreview(float detail)
        {
            DestroyImmediate(_preview);
            _preview = null;
            if (_selected == null || _selected.model == null || _selected.view == null)
                return;
            try
            {
                _preview = UiRenderer.Preview(_selected, detail, _frame);
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning($"UI Render Studio: couldn't preview '{_selected.name}': {exception.Message}", _selected);
            }

            Repaint();
        }

        private void CreateRender()
        {
            var model = Selection.activeObject as GameObject;
            if (model != null && !EditorUtility.IsPersistent(model))
                model = null; // a scene object, not a prefab or model asset
            var render = CreateInstance<UiRender>();
            render.model = model;
            render.view = AssetDatabase.FindAssets($"t:{nameof(UiRenderView)}")
                .Select(guid => AssetDatabase.LoadAssetAtPath<UiRenderView>(AssetDatabase.GUIDToAssetPath(guid)))
                .OrderBy(view => view.name == "DeskView" ? 0 : 1).FirstOrDefault();
            System.IO.Directory.CreateDirectory(PresetFolder);
            AssetDatabase.CreateAsset(render, AssetDatabase.GenerateUniqueAssetPath($"{PresetFolder}/{(model != null ? model.name : "New Render")}.asset"));
            AssetDatabase.SaveAssets();
            Reload();
            Select(render);
        }
    }
}
