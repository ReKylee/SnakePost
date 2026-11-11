using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SnakePost.UIRendering
{
    /// <summary>
    /// Window ▸ Snake Post ▸ UI Render Studio. Browse every UI render, turn the model live in the preview,
    /// and write the sprites. Works from any open scene and never changes a scene: rendering only replaces
    /// the PNG files' pixels, so the UI that uses them updates in place.
    /// </summary>
    internal sealed class UiRenderStudioWindow : EditorWindow
    {
        private const string PresetFolder = "Assets/_Project/Art/UI/RenderPresets";
        private const float DragDegreesPerPixel = .4f;
        private const float AutoRenderDelay = .35f;
        private static readonly Color Desk = new(.78f, .5f, .33f);
        private static readonly Color Outdated = new(1f, .6f, .1f);
        private static readonly string[] Backgrounds = { "Checker", "Desk", "White" };

        private List<UiRender> _renders = new();
        private readonly HashSet<UiRender> _picked = new();
        private readonly HashSet<UiRender> _outdated = new();
        private UiRender _selected;
        private UiRenderStage _stage;
        private GameObject _stageModel;
        private RenderTexture _preview;
        private bool _previewDirty = true;
        private float _previewDetail;
        private string _stageError;
        private Vector2 _listScroll;
        private Vector2 _detailScroll;
        private string _search = string.Empty;
        private int _frame;
        private bool _playing;
        private double _lastFrameTime;
        private int _background = 1;
        private bool _showView;
        private bool _autoRender = true;
        private double _autoRenderAt = -1;
        private float _step = 15f;
        private bool _dragging;
        private Vector2 _snapCarry;
        private Rect _previewRect;

        [MenuItem("Window/Snake Post/UI Render Studio")]
        internal static void Open() => GetWindow<UiRenderStudioWindow>("UI Render Studio");

        internal static void Open(UiRender render) => GetWindow<UiRenderStudioWindow>("UI Render Studio").Select(render);

        private void OnEnable()
        {
            minSize = new Vector2(720f, 520f);
            wantsMouseMove = true;
            Undo.undoRedoPerformed += OnUndoRedo;
            EditorApplication.update += Tick;
            Reload();
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
            EditorApplication.update -= Tick;
            DisposeStage();
        }

        private void OnProjectChange() => Reload();
        private void OnFocus() => RefreshOutdated();

        private void OnSelectionChange()
        {
            if (Selection.activeObject is UiRender render && render != _selected)
                Select(render);
        }

        private void OnUndoRedo()
        {
            Invalidate(rebuild: true);
            ScheduleAutoRender();
        }

        // ---------- data ----------

        private void Reload()
        {
            _renders = UiRenderer.FindAll().OrderBy(r => r.category).ThenBy(r => r.name).ToList();
            _picked.RemoveWhere(r => r == null);
            if (_selected == null && _renders.Count > 0)
                Select(_renders[0]);
            RefreshOutdated();
        }

        private void RefreshOutdated()
        {
            _outdated.Clear();
            foreach (var render in _renders)
                if (render != null && IsOutdated(render))
                    _outdated.Add(render);
            Repaint();
        }

        // A PNG is out of date when the render, its view, or anything the model is made of (meshes, materials,
        // textures, shaders, import settings) changed after it was written.
        private static bool IsOutdated(UiRender render)
        {
            var png = render.OutputPath(0);
            if (!File.Exists(png) || EditorUtility.IsDirty(render) || (render.view != null && EditorUtility.IsDirty(render.view)))
                return true;
            var written = File.GetLastWriteTimeUtc(png);
            var sources = new List<string> { AssetDatabase.GetAssetPath(render) };
            if (render.view != null) sources.Add(AssetDatabase.GetAssetPath(render.view));
            if (render.animation != null) sources.Add(AssetDatabase.GetAssetPath(render.animation));
            if (render.model != null)
                sources.AddRange(AssetDatabase.GetDependencies(AssetDatabase.GetAssetPath(render.model), true)
                    .Where(path => !path.EndsWith(".cs") && !path.EndsWith(".dll")));
            foreach (var path in sources)
                if (File.GetLastWriteTimeUtc(path) > written || (File.Exists(path + ".meta") && File.GetLastWriteTimeUtc(path + ".meta") > written))
                    return true;
            return false;
        }

        private void Select(UiRender render)
        {
            if (render != _selected)
            {
                _selected = render;
                _frame = 0;
                _playing = false;
                Invalidate(rebuild: true);
            }

            Repaint();
        }

        private void Invalidate(bool rebuild = false)
        {
            if (rebuild || (_selected != null && _selected.model != _stageModel))
                DisposeStage();
            _stageError = null;
            if (_selected == null || _selected.model == null || _selected.view == null)
            {
                DisposeStage();
                return;
            }

            try
            {
                if (_stage == null)
                {
                    _stage = new UiRenderStage(_selected);
                    _stageModel = _selected.model;
                }

                _stage.Refit();
            }
            catch (System.Exception exception)
            {
                _stageError = exception.Message;
                DisposeStage();
            }

            _frame = Mathf.Clamp(_frame, 0, _selected.FrameCount - 1);
            _previewDirty = true;
            Repaint();
        }

        private void DisposeStage()
        {
            _stage?.Dispose();
            _stage = null;
            _stageModel = null;
            _preview = null;
        }

        private void ScheduleAutoRender()
        {
            if (_autoRender && _selected != null && _selected.FrameCount == 1)
                _autoRenderAt = EditorApplication.timeSinceStartup + AutoRenderDelay;
        }

        private void Tick()
        {
            if (_autoRenderAt > 0 && !_dragging && EditorApplication.timeSinceStartup >= _autoRenderAt)
            {
                _autoRenderAt = -1;
                if (_selected != null && _selected.model != null && _selected.view != null)
                    RenderNow(new[] { _selected });
            }

            if (_playing && _selected != null && EditorApplication.timeSinceStartup - _lastFrameTime > 1.0 / 12.0)
            {
                _lastFrameTime = EditorApplication.timeSinceStartup;
                _frame = (_frame + 1) % _selected.FrameCount;
                _previewDirty = true;
                Repaint();
            }
        }

        private void RenderNow(IList<UiRender> renders)
        {
            // Save settings first so the PNG is newer than them on disk (otherwise it reads as out of date).
            foreach (var render in renders)
            {
                AssetDatabase.SaveAssetIfDirty(render);
                if (render.view != null)
                    AssetDatabase.SaveAssetIfDirty(render.view);
            }

            UiRenderer.Save(renders, "Rendering UI sprites");
            RefreshOutdated();
        }

        // ---------- layout ----------

        private void OnGUI()
        {
            DrawToolbar();
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawList();
                DrawDetail();
            }
        }

        private void DrawToolbar()
        {
            using var _ = new EditorGUILayout.HorizontalScope(EditorStyles.toolbar);
            if (GUILayout.Button(new GUIContent("+ New", "New render for the prefab or model selected in the Project window. You can also drag models onto the list."), EditorStyles.toolbarButton, GUILayout.Width(60f)))
                foreach (var model in Selection.objects.OfType<GameObject>().Where(EditorUtility.IsPersistent).DefaultIfEmpty())
                    CreateRender(model);
            GUILayout.Space(6f);
            _autoRender = GUILayout.Toggle(_autoRender, new GUIContent("Auto Render", "Write the PNG as soon as you stop editing (single images; sequences render with the Render button)."), EditorStyles.toolbarButton);
            GUILayout.FlexibleSpace();
            using (new EditorGUI.DisabledScope(_picked.Count == 0))
                if (GUILayout.Button($"Render Selected ({_picked.Count})", EditorStyles.toolbarButton))
                    RenderNow(_picked.ToList());
            using (new EditorGUI.DisabledScope(_outdated.Count == 0))
                if (GUILayout.Button(new GUIContent($"Render Outdated ({_outdated.Count})", "Renders whose model, materials or settings changed since their PNG was written."), EditorStyles.toolbarButton))
                    RenderNow(_outdated.ToList());
            if (GUILayout.Button("Render All", EditorStyles.toolbarButton))
                RenderNow(_renders);
        }

        private void DrawList()
        {
            using var _ = new EditorGUILayout.VerticalScope(GUILayout.Width(240f));
            _search = EditorGUILayout.TextField(_search, EditorStyles.toolbarSearchField);
            var area = EditorGUILayout.BeginVertical();
            _listScroll = EditorGUILayout.BeginScrollView(_listScroll);
            string category = null;
            var visible = _renders.Where(r => r != null && (_search.Length == 0 || r.name.IndexOf(_search, System.StringComparison.OrdinalIgnoreCase) >= 0)).ToList();
            foreach (var render in visible)
            {
                if (render.category != category)
                {
                    category = render.category;
                    EditorGUILayout.LabelField(category, EditorStyles.boldLabel);
                }

                DrawRow(render, visible);
            }

            if (_renders.Count == 0)
                EditorGUILayout.HelpBox("Drag a prefab or model here to make its first render.", MessageType.Info);
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
            HandleDrop(area);
        }

        private void DrawRow(UiRender render, List<UiRender> visible)
        {
            var row = GUILayoutUtility.GetRect(0f, 44f, GUILayout.ExpandWidth(true));
            if (render == _selected)
                EditorGUI.DrawRect(row, new Color(.24f, .48f, .9f, .4f));
            else if (_picked.Contains(render))
                EditorGUI.DrawRect(row, new Color(.24f, .48f, .9f, .18f));

            var thumbnail = AssetDatabase.LoadAssetAtPath<Texture2D>(render.OutputPath(0));
            var icon = new Rect(row.x + 4f, row.y + 2f, 40f, 40f);
            if (thumbnail != null)
                GUI.DrawTexture(icon, thumbnail, ScaleMode.ScaleToFit);
            GUI.Label(new Rect(row.x + 50f, row.y + 6f, row.width - 66f, 18f), render.name);
            var status = thumbnail == null ? "not rendered yet" : render.FrameCount > 1 ? $"{render.FrameCount} frames" : "image";
            GUI.Label(new Rect(row.x + 50f, row.y + 22f, row.width - 66f, 16f), status, EditorStyles.miniLabel);
            if (_outdated.Contains(render))
            {
                var dot = new Rect(row.xMax - 14f, row.y + 16f, 8f, 8f);
                EditorGUI.DrawRect(dot, Outdated);
                GUI.Label(dot, new GUIContent(string.Empty, "Out of date: changed since its PNG was written"));
            }

            var e = Event.current;
            if (e.type != EventType.MouseDown || !row.Contains(e.mousePosition))
                return;
            if (e.button == 1)
            {
                ShowRowMenu(render);
            }
            else if (e.shift && _selected != null)
            {
                var to = visible.IndexOf(render);
                var from = visible.IndexOf(_selected);
                if (from < 0)
                    from = to;
                for (var i = Mathf.Min(from, to); i <= Mathf.Max(from, to); i++)
                    _picked.Add(visible[i]);
            }
            else if (e.control || e.command)
            {
                if (!_picked.Remove(render))
                    _picked.Add(render);
            }
            else
            {
                _picked.Clear();
                _picked.Add(render);
                Select(render);
            }

            e.Use();
            Repaint();
        }

        private void ShowRowMenu(UiRender render)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Render"), false, () => RenderNow(new[] { render }));
            menu.AddItem(new GUIContent("Duplicate"), false, () =>
            {
                var path = AssetDatabase.GenerateUniqueAssetPath(AssetDatabase.GetAssetPath(render));
                AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(render), path);
                Reload();
                Select(AssetDatabase.LoadAssetAtPath<UiRender>(path));
            });
            menu.AddItem(new GUIContent("Show Settings Asset"), false, () => EditorGUIUtility.PingObject(render));
            menu.AddItem(new GUIContent("Show PNG"), false, () => EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(render.OutputPath(0))));
            menu.AddSeparator(string.Empty);
            menu.AddItem(new GUIContent("Delete…"), false, () =>
            {
                if (!EditorUtility.DisplayDialog("Delete UI render", $"Move '{render.name}' to the trash? Its PNGs stay, so UI that uses them keeps working.", "Delete", "Cancel"))
                    return;
                if (render == _selected)
                    Select(null);
                AssetDatabase.MoveAssetToTrash(AssetDatabase.GetAssetPath(render));
                Reload();
            });
            menu.ShowAsContext();
        }

        private void HandleDrop(Rect area)
        {
            var e = Event.current;
            if ((e.type != EventType.DragUpdated && e.type != EventType.DragPerform) || !area.Contains(e.mousePosition))
                return;
            var models = DragAndDrop.objectReferences.OfType<GameObject>().Where(EditorUtility.IsPersistent).ToList();
            if (models.Count == 0)
                return;
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            if (e.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                foreach (var model in models)
                    CreateRender(model);
            }

            e.Use();
        }

        private void DrawDetail()
        {
            using var _ = new EditorGUILayout.VerticalScope();
            if (_selected == null)
            {
                EditorGUILayout.HelpBox("Pick a render on the left, press + New with a model selected in the Project window, or drag models onto the list.", MessageType.Info);
                return;
            }

            _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll);
            DrawHeader();
            DrawPreview();

            var render = new SerializedObject(_selected);
            render.Update();
            EditorGUI.BeginChangeCheck();
            DrawPose(render);

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Frames (optional)", EditorStyles.boldLabel);
            var clip = render.FindProperty("animation");
            EditorGUILayout.PropertyField(clip, new GUIContent("Animation Clip", "Render frames of this clip, e.g. the folder opening."));
            if (clip.objectReferenceValue != null)
            {
                var range = render.FindProperty("clipRange");
                var value = range.vector2Value;
                EditorGUILayout.MinMaxSlider(new GUIContent("Clip Range", "Part of the clip to render (0 = start, 1 = end)"), ref value.x, ref value.y, 0f, 1f);
                range.vector2Value = value;
            }

            EditorGUILayout.PropertyField(render.FindProperty("turntable"), new GUIContent("Turntable", "Spin once around the model's up axis over the frames."));
            EditorGUILayout.PropertyField(render.FindProperty("frames"));
            EditorGUILayout.Space(6f);
            EditorGUILayout.PropertyField(render.FindProperty("view"), new GUIContent("View", "Shared camera and light. Same view = same perspective and lighting across sprites."));

            var changed = EditorGUI.EndChangeCheck();
            render.ApplyModifiedProperties();
            if (_selected.view != null && (_showView = EditorGUILayout.Foldout(_showView, "Edit shared view", true)))
                changed |= DrawView(_selected.view);

            EditorGUILayout.EndScrollView();
            if (changed)
            {
                Invalidate();
                ScheduleAutoRender();
            }
        }

        private void DrawHeader()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginChangeCheck();
                var name = EditorGUILayout.DelayedTextField(new GUIContent("Name", "Also the PNG's name. Renaming renames the existing PNGs (they keep their IDs)."), _selected.name);
                if (EditorGUI.EndChangeCheck() && name != _selected.name && name.Trim().Length > 0)
                    MoveOutputs(name.Trim(), _selected.category);

                var ready = _stage != null;
                using (new EditorGUI.DisabledScope(!ready))
                    if (GUILayout.Button(new GUIContent("Render", "Write the PNG(s) now. Only the image pixels change; the UI that uses them updates in place."), GUILayout.Width(90f)))
                        RenderNow(new[] { _selected });
                if (GUILayout.Button("Show PNG", GUILayout.Width(80f)))
                    EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(_selected.OutputPath(0)) ?? _selected);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginChangeCheck();
                var category = EditorGUILayout.DelayedTextField(new GUIContent("Folder", "Sub-folder of Art/UI/Renders. Changing it moves the existing PNGs (they keep their IDs)."), _selected.category);
                if (EditorGUI.EndChangeCheck() && category != _selected.category && category.Trim().Length > 0)
                    MoveOutputs(_selected.name, category.Trim());
                if (_outdated.Contains(_selected))
                    GUILayout.Label(new GUIContent(" ● out of date", "Changed since its PNG was written"), new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = Outdated } }, GUILayout.Width(90f));
            }

            EditorGUILayout.LabelField(_selected.OutputPath(0), EditorStyles.miniLabel);
            if (_selected.model == null || _selected.view == null)
                EditorGUILayout.HelpBox("Assign a Model and a View.", MessageType.Warning);
            if (_stageError != null)
                EditorGUILayout.HelpBox(_stageError, MessageType.Error);
        }

        private void DrawPreview()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("Drag to turn · Shift+drag to roll · hold Ctrl to snap · arrow keys to step", EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
                _background = GUILayout.Toolbar(_background, Backgrounds, EditorStyles.miniButton, GUILayout.Width(190f));
            }

            var area = GUILayoutUtility.GetRect(0f, 340f, GUILayout.ExpandWidth(true));
            if (Event.current.type == EventType.Repaint)
            {
                _previewRect = area;
                RenderPreviewIfNeeded(area);
            }

            if (_background != 0)
                EditorGUI.DrawRect(area, _background == 1 ? Desk : Color.white);
            if (_preview != null)
            {
                // Shown turned the way the UI turns it (UI Rotation), so the shadow reads as it will in the menu.
                var matrix = GUI.matrix;
                var turned = Mathf.Abs(Mathf.Sin(_selected.uiRotation * Mathf.Deg2Rad)) > .5f;
                var fit = turned ? new Rect(area.center.x - area.height / 2f, area.center.y - area.width / 2f, area.height, area.width) : area;
                GUIUtility.RotateAroundPivot(-_selected.uiRotation, area.center);
                if (_background == 0)
                    EditorGUI.DrawTextureTransparent(fit, _preview, ScaleMode.ScaleToFit);
                else
                    GUI.DrawTexture(fit, _preview, ScaleMode.ScaleToFit, true);
                GUI.matrix = matrix;
            }

            if (_stage != null)
            {
                var size = _stage.Size(1f);
                EditorGUI.LabelField(new Rect(area.x + 6f, area.yMax - 18f, 260f, 16f), $"{size.x} × {size.y} px before trimming", EditorStyles.miniLabel);
                DrawAxes(new Vector2(area.xMax - 34f, area.yMax - 34f));
            }

            HandlePreviewInput(area);

            if (_selected.FrameCount > 1)
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(_playing ? "■ Stop" : "▶ Play", GUILayout.Width(70f)))
                        _playing = !_playing;
                    EditorGUI.BeginChangeCheck();
                    _frame = EditorGUILayout.IntSlider(_frame, 0, _selected.FrameCount - 1);
                    if (EditorGUI.EndChangeCheck())
                    {
                        _playing = false;
                        _previewDirty = true;
                    }
                }
        }

        private void RenderPreviewIfNeeded(Rect area)
        {
            if (_stage == null)
                return;
            // Render at the size it is shown (sharp, and only as much work as needed).
            var full = _stage.Size(1f);
            var detail = Mathf.Min(1f, area.width * EditorGUIUtility.pixelsPerPoint / full.x, area.height * EditorGUIUtility.pixelsPerPoint / full.y);
            if (!_previewDirty && Mathf.Abs(detail - _previewDetail) < .03f)
                return;
            _previewDetail = detail;
            _previewDirty = false;
            try
            {
                _preview = _stage.Render(_frame, detail);
            }
            catch (System.Exception exception)
            {
                _stageError = exception.Message;
                _preview = null;
            }
        }

        // The model's own X (red), Y (green) and Z (blue) axes, as seen in the preview.
        private void DrawAxes(Vector2 center)
        {
            if (Event.current.type != EventType.Repaint)
                return;
            var axes = new[] { (Vector3.right, Handles.xAxisColor, "X"), (Vector3.up, Handles.yAxisColor, "Y"), (Vector3.forward, Handles.zAxisColor, "Z") };
            foreach (var (axis, color, label) in axes)
            {
                var direction = _stage.ScreenAxis(axis);
                var end = center + new Vector2(direction.x, -direction.y) * 24f;
                Handles.color = color;
                Handles.DrawAAPolyLine(3f, center, end);
                var style = new GUIStyle(EditorStyles.miniBoldLabel) { normal = { textColor = color } };
                GUI.Label(new Rect(end.x - 4f, end.y - 8f, 14f, 14f), label, style);
            }
        }

        private void HandlePreviewInput(Rect area)
        {
            var id = GUIUtility.GetControlID(FocusType.Passive);
            if (_stage == null)
                return;
            var e = Event.current;
            EditorGUIUtility.AddCursorRect(area, MouseCursor.Orbit);
            switch (e.type)
            {
                case EventType.MouseDown when e.button == 0 && area.Contains(e.mousePosition):
                    _dragging = true;
                    _snapCarry = Vector2.zero;
                    GUIUtility.hotControl = id;
                    GUIUtility.keyboardControl = 0; // leave any text field, so arrow keys turn the model
                    Undo.RecordObject(_selected, "Turn UI Render");
                    e.Use();
                    break;
                case EventType.MouseDrag when _dragging:
                    // Turning drags follow the preview as shown (turned by UI Rotation); rolling is the same either way.
                    var delta = e.shift ? e.delta : (Vector2)(Quaternion.Euler(0f, 0f, _selected.uiRotation) * e.delta);
                    var degrees = delta * DragDegreesPerPixel;
                    if (e.control || e.command)
                    {
                        // Snap: turn in whole steps as the drag passes each step's worth of movement.
                        _snapCarry += degrees;
                        degrees = new Vector2(Mathf.Round(_snapCarry.x / _step) * _step, Mathf.Round(_snapCarry.y / _step) * _step);
                        _snapCarry -= degrees;
                    }

                    if (e.shift)
                        Turn(Vector3.forward, -degrees.x, record: false);
                    else
                    {
                        Turn(Vector3.up, -degrees.x, record: false);
                        Turn(Vector3.right, -degrees.y, record: false);
                    }

                    e.Use();
                    break;
                case EventType.MouseUp when _dragging:
                    _dragging = false;
                    GUIUtility.hotControl = 0;
                    ScheduleAutoRender();
                    e.Use();
                    break;
                case EventType.KeyDown when area.Contains(e.mousePosition) && GUIUtility.keyboardControl == 0:
                    var step = e.shift ? 90f : _step;
                    var handled = true;
                    switch (e.keyCode)
                    {
                        case KeyCode.LeftArrow: Turn(Vector3.up, step); break;
                        case KeyCode.RightArrow: Turn(Vector3.up, -step); break;
                        case KeyCode.UpArrow: Turn(Vector3.right, step); break;
                        case KeyCode.DownArrow: Turn(Vector3.right, -step); break;
                        default: handled = false; break;
                    }

                    if (handled)
                        e.Use();
                    break;
            }
        }

        private void DrawPose(SerializedObject render)
        {
            EditorGUILayout.Space(6f);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Pose", EditorStyles.boldLabel, GUILayout.Width(60f));
                GUILayout.FlexibleSpace();
                EditorGUIUtility.labelWidth = 36f;
                _step = Mathf.Clamp(EditorGUILayout.FloatField(new GUIContent("Step", "Degrees per button press, arrow key and snap"), _step, GUILayout.Width(90f)), 1f, 180f);
                EditorGUIUtility.labelWidth = 0f;
            }

            using (new EditorGUI.DisabledScope(_stage == null))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(new GUIContent("◀ Turn", "Turn left by Step, as seen in the preview"))) Turn(Vector3.up, _step);
                    if (GUILayout.Button(new GUIContent("Turn ▶", "Turn right by Step"))) Turn(Vector3.up, -_step);
                    if (GUILayout.Button(new GUIContent("Tilt ▲", "Tilt the top away by Step"))) Turn(Vector3.right, _step);
                    if (GUILayout.Button(new GUIContent("Tilt ▼", "Tilt the top toward you by Step"))) Turn(Vector3.right, -_step);
                    if (GUILayout.Button(new GUIContent("Roll ↺", "Roll counter-clockwise by Step"))) Turn(Vector3.forward, _step);
                    if (GUILayout.Button(new GUIContent("Roll ↻", "Roll clockwise by Step"))) Turn(Vector3.forward, -_step);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(new GUIContent("Flip Over", "Show the other face"))) Turn(Vector3.right, 180f);
                    if (GUILayout.Button(new GUIContent("Turn Around", "180° around the vertical"))) Turn(Vector3.up, 180f);
                    if (GUILayout.Button(new GUIContent("As Imported", "The model's own orientation, lying on the desk as authored"))) SetRotation(Quaternion.identity);
                    if (GUILayout.Button(new GUIContent("Match Scene Selection", "Copy the angle and scale of the object selected in the Hierarchy, as its scene's Main Camera sees it")))
                    {
                        UiRenderEditor.MatchSceneObject(_selected);
                        Invalidate();
                        ScheduleAutoRender();
                    }
                }
            }

            EditorGUILayout.PropertyField(render.FindProperty("model"));
            EditorGUILayout.PropertyField(render.FindProperty("modelRotation"), new GUIContent("Rotation"));
            EditorGUILayout.PropertyField(render.FindProperty("modelScale"), new GUIContent("Scale"));
            EditorGUILayout.PropertyField(render.FindProperty("groundShadow"), new GUIContent("Ground Shadow", "Shadow on the invisible ground. Off for things that hang or stand on something else."));
            EditorGUILayout.PropertyField(render.FindProperty("uiRotation"), new GUIContent("UI Rotation", "Set this to the Z rotation of the UI Image that shows the sprite. The light turns the other way, so the rotated sprite's shadow matches the rest. The preview shows it rotated."));
        }

        private bool DrawView(UiRenderView view)
        {
            var users = _renders.Count(render => render != null && render.view == view);
            EditorGUILayout.HelpBox($"'{view.name}' is shared by {users} render(s): changes mark them all out of date.", MessageType.Info);
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

        // ---------- actions ----------

        // Rotates around an axis of the preview (x right, y up, z into the screen), so turns read as seen.
        private void Turn(Vector3 screenAxis, float degrees, bool record = true)
        {
            if (_selected == null || _selected.view == null || Mathf.Approximately(degrees, 0f))
                return;
            var camera = Quaternion.Euler(_selected.view.cameraRotation);
            SetRotation(Quaternion.AngleAxis(degrees, camera * screenAxis) * Quaternion.Euler(_selected.modelRotation), record);
        }

        private void SetRotation(Quaternion rotation, bool record = true)
        {
            if (record)
                Undo.RecordObject(_selected, "Turn UI Render");
            _selected.modelRotation = rotation.eulerAngles;
            EditorUtility.SetDirty(_selected);
            Invalidate();
            if (record)
                ScheduleAutoRender();
        }

        // Renaming or changing the folder moves the existing PNGs instead of making new ones, so their asset IDs
        // (and every UI Image that uses them) stay intact.
        private void MoveOutputs(string newName, string newCategory)
        {
            var oldPaths = Enumerable.Range(0, _selected.FrameCount).Select(_selected.OutputPath).ToList();
            var renderPath = AssetDatabase.GetAssetPath(_selected);
            if (newName != _selected.name)
            {
                var error = AssetDatabase.RenameAsset(renderPath, newName);
                if (!string.IsNullOrEmpty(error))
                {
                    Debug.LogWarning($"UI Render Studio: couldn't rename: {error}", _selected);
                    return;
                }
            }

            Undo.RecordObject(_selected, "Change UI Render Folder");
            _selected.category = newCategory;
            EditorUtility.SetDirty(_selected);
            AssetDatabase.SaveAssets();

            Directory.CreateDirectory(_selected.OutputFolder);
            AssetDatabase.Refresh();
            for (var i = 0; i < oldPaths.Count; i++)
            {
                var to = _selected.OutputPath(i);
                if (File.Exists(oldPaths[i]) && oldPaths[i] != to)
                {
                    var error = AssetDatabase.MoveAsset(oldPaths[i], to);
                    if (!string.IsNullOrEmpty(error))
                        Debug.LogWarning($"UI Render Studio: couldn't move {oldPaths[i]}: {error}", _selected);
                }
            }

            Reload();
        }

        private void CreateRender(GameObject model)
        {
            var render = CreateInstance<UiRender>();
            render.model = model;
            render.view = AssetDatabase.FindAssets($"t:{nameof(UiRenderView)}")
                .Select(guid => AssetDatabase.LoadAssetAtPath<UiRenderView>(AssetDatabase.GUIDToAssetPath(guid)))
                .OrderBy(view => view.name == "DeskView" ? 0 : 1).FirstOrDefault();
            Directory.CreateDirectory(PresetFolder);
            AssetDatabase.CreateAsset(render, AssetDatabase.GenerateUniqueAssetPath($"{PresetFolder}/{(model != null ? model.name : "New Render")}.asset"));
            AssetDatabase.SaveAssets();
            Reload();
            Select(render);
        }
    }
}
