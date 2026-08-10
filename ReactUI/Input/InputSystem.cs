using System;

namespace ReactUI.Input;

/// <summary>
/// Main input processing — polls mouse/keyboard each frame and dispatches
/// hover, click, focus, scroll, and keyboard events to the committed UI tree.
/// </summary>
public static class InputSystem
{
    static Core.UINode? _hoveredNode;
    static Core.UINode? _activeNode;
    static Core.UINode? _rightClickNode;

    // Drag state
    static bool _isDragging;
    static float _dragOffsetX, _dragOffsetY;
    static Action<float>? _dragSetX, _dragSetY;

    // Slider drag state
    static Core.UINode? _slidingNode;

    // Pointer-area drag state
    static Core.UINode? _pointerAreaNode;

    // Scrollbar drag state
    static Core.UINode? _scrollbarDragNode;
    static float _scrollbarDragOffset; // mouse Y offset from thumb top when drag started

    // Cursor position per input node (keyed by UINode reference)
    static readonly System.Collections.Generic.Dictionary<Core.UINode, int> _cursorPositions = new();

    // Selection anchor per input node — when set, text between anchor and cursor is selected
    static readonly System.Collections.Generic.Dictionary<Core.UINode, int> _selectionAnchors = new();

    /// <summary>Get the cursor position for an input node. Defaults to end of text.</summary>
    public static int GetCursorPosition(Core.UINode node)
    {
        if (_cursorPositions.TryGetValue(node, out var pos))
            return pos;
        // Default: end of text
        if (node.LastVNode?.Props.TryGetValue("value", out var val) == true && val is string s)
            return s.Length;
        return 0;
    }

    /// <summary>Set cursor position for an input node.</summary>
    public static void SetCursorPosition(Core.UINode node, int pos)
    {
        _cursorPositions[node] = pos;
    }

    /// <summary>Get the selection range for an input node, if any. Returns (start, end) where start &lt; end.</summary>
    public static (int Start, int End)? GetSelection(Core.UINode node)
    {
        if (!_selectionAnchors.TryGetValue(node, out var anchor)) return null;
        int cursor = GetCursorPosition(node);
        if (anchor == cursor) return null;
        return anchor < cursor ? (anchor, cursor) : (cursor, anchor);
    }

    /// <summary>Clear any active text selection on the node.</summary>
    public static void ClearSelection(Core.UINode node)
    {
        _selectionAnchors.Remove(node);
    }

    /// <summary>Find the next word boundary from pos in the given direction (-1 = left, +1 = right).</summary>
    static int FindWordBoundary(string text, int pos, int direction)
    {
        if (direction < 0)
        {
            if (pos <= 0) return 0;
            int i = pos - 1;
            // Skip whitespace/punctuation
            while (i > 0 && !char.IsLetterOrDigit(text[i])) i--;
            // Skip word characters
            while (i > 0 && char.IsLetterOrDigit(text[i - 1])) i--;
            return i;
        }
        else
        {
            if (pos >= text.Length) return text.Length;
            int i = pos;
            // Skip whitespace/punctuation
            while (i < text.Length && !char.IsLetterOrDigit(text[i])) i++;
            // Skip word characters
            while (i < text.Length && char.IsLetterOrDigit(text[i])) i++;
            return i;
        }
    }

    /// <summary>Get selected text string, or empty if no selection.</summary>
    static string GetSelectedText(Core.UINode node, string text)
    {
        var sel = GetSelection(node);
        if (sel == null) return "";
        return text.Substring(sel.Value.Start, sel.Value.End - sel.Value.Start);
    }

    /// <summary>Delete selected text and return new string + cursor pos. Returns null if no selection.</summary>
    static (string NewText, int Cursor)? DeleteSelection(Core.UINode node, string text)
    {
        var sel = GetSelection(node);
        if (sel == null) return null;
        string result = text.Remove(sel.Value.Start, sel.Value.End - sel.Value.Start);
        ClearSelection(node);
        return (result, sel.Value.Start);
    }

    // Registered draggable panels: componentId → callbacks
    static readonly System.Collections.Generic.Dictionary<int, DragTarget> _dragTargets = new();

    public struct DragTarget
    {
        public Func<float> GetX, GetY;
        public Action<float> SetX, SetY;
    }

    /// <summary>
    /// Register a component as draggable. Call during render.
    /// </summary>
    public static void RegisterDraggable(int componentId, Func<float> getX, Func<float> getY, Action<float> setX, Action<float> setY)
    {
        _dragTargets[componentId] = new DragTarget { GetX = getX, GetY = getY, SetX = setX, SetY = setY };
    }

    /// <summary>
    /// Register a specific element (by key) as a drag handle.
    /// Only mousedown on this element (or its children) will start the drag.
    /// The drag still moves the position via the provided setters.
    /// </summary>
    public static void RegisterDragHandle(string elementKey, Func<float> getX, Func<float> getY, Action<float> setX, Action<float> setY)
    {
        _dragHandles[elementKey] = new DragTarget { GetX = getX, GetY = getY, SetX = setX, SetY = setY };
    }

    static readonly System.Collections.Generic.Dictionary<string, DragTarget> _dragHandles = new();

    public static Core.UINode? HoveredNode => _hoveredNode;
    public static Core.UINode? FocusedNode => FocusManager.Focused;

    /// <summary>
    /// True when the mouse is over a ReactUI element or dragging. Other mods can check this
    /// to skip their own input processing (e.g. player movement, interactions).
    /// </summary>
    public static bool BlockGameInput => _hoveredNode != null || _isDragging;

    /// <summary>
    /// Start dragging. Call from an onMouseDown handler.
    /// Provide the current position and state setters — InputSystem will update them every frame.
    /// </summary>
    public static void StartDrag(float currentX, float currentY, Action<float> setX, Action<float> setY)
    {
        float s = Rendering.UIScale.Factor;
        float mx = UnityEngine.Input.mousePosition.x / s;
        float my = (UnityEngine.Screen.height - UnityEngine.Input.mousePosition.y) / s;
        _dragOffsetX = mx - currentX;
        _dragOffsetY = my - currentY;
        _dragSetX = setX;
        _dragSetY = setY;
        _isDragging = true;
        Plugin.ReactUIPlugin.Logger.LogInfo($"[ReactUI] StartDrag pos=({currentX},{currentY}) mouse=({mx},{my}) offset=({_dragOffsetX},{_dragOffsetY})");
    }

    /// <summary>
    /// Process input across all roots. Hit tests all roots and picks the topmost hit,
    /// so only one root receives input per frame (no cross-tree interference).
    /// </summary>
    public static void ProcessInputAll(System.Collections.Generic.List<Core.UINode> roots)
    {
        var mousePos = UnityEngine.Input.mousePosition;
        float s = Rendering.UIScale.Factor;
        float mx = mousePos.x / s;
        float my = (UnityEngine.Screen.height - mousePos.y) / s;

        // Find the topmost hit across all roots (last root = highest z-order)
        Core.UINode? hit = null;
        for (int i = roots.Count - 1; i >= 0; i--)
        {
            hit = HitTesting.HitTest(roots[i], mx, my);
            if (hit != null) break;
        }

        ProcessInputWithHit(hit, mx, my);
    }

    public static void ProcessInput(Core.UINode? root)
    {
        if (root == null) return;
        var mousePos = UnityEngine.Input.mousePosition;
        float s = Rendering.UIScale.Factor;
        float mx = mousePos.x / s;
        float my = (UnityEngine.Screen.height - mousePos.y) / s;
        var hit = HitTesting.HitTest(root, mx, my);
        ProcessInputWithHit(hit, mx, my);
    }

    private static void ProcessInputWithHit(Core.UINode? hit, float mx, float my)
    {
        // Handle hover transitions — set IsHovered on hit node AND all ancestors
        if (hit != _hoveredNode)
        {
            if (_hoveredNode != null)
            {
                var n = _hoveredNode;
                while (n != null) { n.IsHovered = false; n = n.Parent; }
                FireEvent(_hoveredNode, "onMouseLeave");
            }
            _hoveredNode = hit;
            if (_hoveredNode != null)
            {
                var n = _hoveredNode;
                while (n != null) { n.IsHovered = true; n = n.Parent; }
                FireEvent(_hoveredNode, "onMouseEnter");
            }
        }

        // Mouse down (left)
        if (UnityEngine.Input.GetMouseButtonDown(0))
        {
            // Check scrollbar hit first — scrollbar takes priority over content clicks
            Core.UINode? scrollbarHit = hit != null ? FindScrollbarHit(hit, mx, my) : null;
            if (scrollbarHit != null)
            {
                var geo = GetScrollbarGeometry(scrollbarHit)!.Value;
                if (my >= geo.ThumbY && my <= geo.ThumbY + geo.ThumbH)
                {
                    // Clicked on thumb — start dragging
                    _scrollbarDragNode = scrollbarHit;
                    _scrollbarDragOffset = my - geo.ThumbY;
                }
                else
                {
                    // Clicked on track — jump scroll to that position
                    float clickRatio = (my - geo.TrackY) / geo.TrackH;
                    scrollbarHit.ScrollOffsetY = clickRatio * geo.MaxScroll;
                    if (scrollbarHit.ScrollOffsetY < 0) scrollbarHit.ScrollOffsetY = 0;
                    if (scrollbarHit.ScrollOffsetY > geo.MaxScroll) scrollbarHit.ScrollOffsetY = geo.MaxScroll;

                    // Start dragging from the new thumb position so user can keep dragging
                    _scrollbarDragNode = scrollbarHit;
                    var newGeo = GetScrollbarGeometry(scrollbarHit)!.Value;
                    _scrollbarDragOffset = newGeo.ThumbH / 2; // center thumb on click
                }
                // Don't process normal click logic
            }
            else
            {
            _activeNode = hit;
            if (hit != null)
            {
                var hitKey = hit.Key ?? "null";
                var hitType = hit.Type;
                var hasClick = HasEventHandler(hit, "onClick");
                Plugin.ReactUIPlugin.Logger.LogInfo($"[ReactUI Input] MouseDown on type={hitType} key={hitKey} hasOnClick={hasClick}");

                hit.IsActive = true;
                FireEvent(hit, "onMouseDown");

                // Priority: slider > pointer area > button/onClick > panel drag
                var sliderNode = FindAncestorOfType(hit, "slider");
                var pointerAreaNode = sliderNode == null ? FindAncestorOfType(hit, "pointerarea") : null;
                if (sliderNode != null)
                {
                    _slidingNode = sliderNode;
                    UpdateSliderValue(sliderNode, mx);
                }
                else if (pointerAreaNode != null)
                {
                    _pointerAreaNode = pointerAreaNode;
                    UpdatePointerArea(pointerAreaNode, mx, my);
                }
                else if (!HasEventHandler(hit, "onClick") && !HasEventHandler(hit, "onRightClick") && !_isDragging)
                {
                    // Check drag handles first (element-level, by key)
                    bool foundHandle = false;
                    var handleCheck = hit;
                    while (handleCheck != null)
                    {
                        if (handleCheck.Key != null && _dragHandles.TryGetValue(handleCheck.Key, out var handleTarget))
                        {
                            _dragOffsetX = mx - handleTarget.GetX();
                            _dragOffsetY = my - handleTarget.GetY();
                            _dragSetX = handleTarget.SetX;
                            _dragSetY = handleTarget.SetY;
                            _isDragging = true;
                            foundHandle = true;
                            break;
                        }
                        handleCheck = handleCheck.Parent;
                    }

                    // Fall back to component-level drag targets
                    if (!foundHandle)
                    {
                        var ancestor = hit;
                        while (ancestor != null)
                        {
                            if (ancestor.Type == "__component" && _dragTargets.TryGetValue(ancestor.ComponentId, out var target))
                            {
                                _dragOffsetX = mx - target.GetX();
                                _dragOffsetY = my - target.GetY();
                                _dragSetX = target.SetX;
                                _dragSetY = target.SetY;
                                _isDragging = true;
                                break;
                            }
                            ancestor = ancestor.Parent;
                        }
                    }
                }
            }

            FocusManager.SetFocus(hit);

            // Click-to-position cursor in input elements
            if (hit != null && hit.Type == "input")
            {
                ClearSelection(hit);
                PositionCursorFromClick(hit, mx);
            }
            } // end else (not scrollbar)
        }

        // Mouse up (left)
        if (UnityEngine.Input.GetMouseButtonUp(0))
        {
            if (_activeNode != null)
            {
                _activeNode.IsActive = false;
                FireEvent(_activeNode, "onMouseUp");
                if (_activeNode == hit && !_isDragging && _slidingNode == null && _pointerAreaNode == null)
                {
                    Plugin.ReactUIPlugin.Logger.LogInfo($"[ReactUI Input] onClick fired on type={_activeNode.Type} key={_activeNode.Key ?? "null"}");
                    FireEvent(_activeNode, "onClick");
                }
                _activeNode = null;
            }
            _isDragging = false;
            _dragSetX = null;
            _dragSetY = null;
            _slidingNode = null;
            _pointerAreaNode = null;
            _scrollbarDragNode = null;
        }

        // Right-click
        if (UnityEngine.Input.GetMouseButtonDown(1))
        {
            _rightClickNode = hit;
            if (hit != null)
                FireEvent(hit, "onRightMouseDown");
        }
        if (UnityEngine.Input.GetMouseButtonUp(1))
        {
            if (_rightClickNode != null)
            {
                FireEvent(_rightClickNode, "onRightMouseUp");
                if (_rightClickNode == hit)
                    FireEvent(_rightClickNode, "onRightClick");
                _rightClickNode = null;
            }
        }

        // Drag tracking
        if (_isDragging && _dragSetX != null && _dragSetY != null)
        {
            _dragSetX(mx - _dragOffsetX);
            _dragSetY(my - _dragOffsetY);
        }

        // Slider drag tracking
        if (_slidingNode != null && UnityEngine.Input.GetMouseButton(0))
        {
            UpdateSliderValue(_slidingNode, mx);
        }

        // Pointer-area drag tracking
        if (_pointerAreaNode != null && UnityEngine.Input.GetMouseButton(0))
        {
            UpdatePointerArea(_pointerAreaNode, mx, my);
        }

        // Scrollbar thumb drag tracking
        if (_scrollbarDragNode != null && UnityEngine.Input.GetMouseButton(0))
        {
            var geo = GetScrollbarGeometry(_scrollbarDragNode);
            if (geo != null)
            {
                var g = geo.Value;
                // Convert mouse Y to scroll position
                float thumbTop = my - _scrollbarDragOffset;
                float scrollRatio = (thumbTop - g.TrackY) / (g.TrackH - g.ThumbH);
                scrollRatio = System.Math.Max(0, System.Math.Min(1, scrollRatio));
                _scrollbarDragNode.ScrollOffsetY = scrollRatio * g.MaxScroll;
            }
        }

        // Scroll
        float scroll = UnityEngine.Input.mouseScrollDelta.y;
        if (scroll != 0 && hit != null)
        {
            var scrollable = FindScrollableAncestor(hit);
            if (scrollable != null)
            {
                // a row container scrolls along its own axis, so the wheel drives X there
                if (scrollable.ComputedStyle?.FlexDirection == Style.FlexDirection.Row)
                {
                    scrollable.ScrollOffsetX -= scroll * 40f;
                    var maxScrollX = GetMaxScrollX(scrollable);
                    if (scrollable.ScrollOffsetX < 0) scrollable.ScrollOffsetX = 0;
                    if (scrollable.ScrollOffsetX > maxScrollX) scrollable.ScrollOffsetX = maxScrollX;
                }
                else
                {
                    scrollable.ScrollOffsetY -= scroll * 40f;
                    if (scrollable.ScrollOffsetY < 0)
                        scrollable.ScrollOffsetY = 0;
                }

                FireEvent(scrollable, "onScroll");
            }
        }

        if (FocusManager.Focused != null)
            ProcessKeyboard(FocusManager.Focused);

        CursorManager.Update(_hoveredNode);
    }

    /// <summary>How far a row container can scroll before its last child is flush with the right edge.</summary>
    static float GetMaxScrollX(Core.UINode node)
    {
        float contentRight = node.ScreenRect.X;
        foreach (var child in node.Children)
        {
            var right = child.ScreenRect.X + child.ScreenRect.Width + node.ScrollOffsetX;
            if (right > contentRight) contentRight = right;
        }

        var padR = node.ComputedStyle?.Padding?.Right ?? 0;
        return System.Math.Max(0, contentRight - node.ScreenRect.X - node.ScreenRect.Width + padR);
    }

    /// <summary>
    /// Compute scrollbar geometry for a scroll container. Returns null if no scrollbar visible.
    /// Matches the constants in RenderPipeline scrollbar drawing.
    /// </summary>
    static (float TrackX, float TrackY, float TrackW, float TrackH,
            float ThumbY, float ThumbH, float ContentHeight, float MaxScroll)?
        GetScrollbarGeometry(Core.UINode node)
    {
        if (node.ComputedStyle?.Overflow != Style.Overflow.Scroll) return null;
        var rect = node.ScreenRect;
        float contentHeight = node.ContentHeight;
        if (contentHeight <= rect.Height) return null;

        float trackW = 6;
        float trackX = rect.Right - trackW - 2;
        float trackY = rect.Y + 2;
        float trackH = rect.Height - 4;

        float visibleRatio = rect.Height / contentHeight;
        float thumbH = System.Math.Max(trackH * visibleRatio, 20);
        float maxScroll = System.Math.Max(1, contentHeight - rect.Height);
        float thumbY = trackY + (trackH - thumbH) * (node.ScrollOffsetY / maxScroll);

        return (trackX, trackY, trackW, trackH, thumbY, thumbH, contentHeight, maxScroll);
    }

    /// <summary>
    /// Find the nearest scrollable ancestor (or self) that has a visible scrollbar at (mx, my).
    /// </summary>
    static Core.UINode? FindScrollbarHit(Core.UINode node, float mx, float my)
    {
        var current = node;
        while (current != null)
        {
            var geo = GetScrollbarGeometry(current);
            if (geo != null)
            {
                var g = geo.Value;
                if (mx >= g.TrackX && mx <= g.TrackX + g.TrackW &&
                    my >= g.TrackY && my <= g.TrackY + g.TrackH)
                    return current;
            }
            current = current.Parent;
        }
        return null;
    }

    static Core.UINode? FindAncestorOfType(Core.UINode node, string type)
    {
        var current = node;
        while (current != null)
        {
            if (current.Type == type) return current;
            current = current.Parent;
        }
        return null;
    }

    /// <summary>
    /// Compute the accumulated scroll offset for a node by walking up the tree.
    /// </summary>
    static (float x, float y) GetAccumulatedScrollOffset(Core.UINode node)
    {
        float sx = 0, sy = 0;
        var current = node.Parent;
        while (current != null)
        {
            if (current.ComputedStyle?.Overflow == Style.Overflow.Scroll)
            {
                sx += current.ScrollOffsetX;
                sy += current.ScrollOffsetY;
            }
            current = current.Parent;
        }
        return (sx, sy);
    }

    static void UpdateSliderValue(Core.UINode slider, float mouseX)
    {
        var vnode = slider.LastVNode;
        if (vnode == null) return;

        float min = 0, max = 1;
        if (vnode.Props.TryGetValue("min", out var mnObj) && mnObj is float mnf) min = mnf;
        if (vnode.Props.TryGetValue("max", out var mxObj) && mxObj is float mxf) max = mxf;

        var (scrollX, _) = GetAccumulatedScrollOffset(slider);
        var rect = slider.ScreenRect;
        float padL = slider.ComputedStyle?.Padding?.Left ?? 0;
        float padR = slider.ComputedStyle?.Padding?.Right ?? 0;
        float trackX = rect.X - scrollX + padL;
        float trackW = rect.Width - padL - padR;

        float pct = trackW > 0 ? (mouseX - trackX) / trackW : 0;
        pct = System.Math.Max(0, System.Math.Min(1, pct));
        float newVal = min + pct * (max - min);

        float step = 0f;
        if (vnode.Props.TryGetValue("step", out var stObj) && stObj is float stf) step = stf;

        if (step > 0)
            newVal = min + (float) System.Math.Round((newVal - min) / step) * step;

        if (vnode.Props.TryGetValue("onChange", out var handler) && handler is System.Action<float> onChange)
        {
            try { onChange(newVal); }
            catch (System.Exception) { }
        }
    }

    static void UpdatePointerArea(Core.UINode area, float mouseX, float mouseY)
    {
        var vnode = area.LastVNode;
        if (vnode == null) return;

        var (scrollX, scrollY) = GetAccumulatedScrollOffset(area);
        var rect = area.ScreenRect;
        if (rect.Width <= 0 || rect.Height <= 0) return;

        var normalized = new UnityEngine.Vector2(
            (mouseX - (rect.X - scrollX)) / rect.Width,
            (mouseY - (rect.Y - scrollY)) / rect.Height);

        if (vnode.Props.TryGetValue("onPointer", out var handler) && handler is System.Action<UnityEngine.Vector2> onPointer)
        {
            try { onPointer(normalized); }
            catch (System.Exception) { }
        }
    }

    static bool HasEventHandler(Core.UINode node, string eventName)
    {
        var current = node;
        while (current != null)
        {
            var vnode = current.LastVNode;
            if (vnode != null && vnode.Props.TryGetValue(eventName, out var handler) && handler is Action)
                return true;
            current = current.Parent;
        }
        return false;
    }

    static Core.UINode? FindScrollableAncestor(Core.UINode node)
    {
        var current = node;
        while (current != null)
        {
            if (current.ComputedStyle?.Overflow == Style.Overflow.Scroll)
                return current;
            current = current.Parent;
        }
        return null;
    }

    static void FireEvent(Core.UINode node, string eventName)
    {
        // Bubble up through ancestors until a handler is found (like DOM event bubbling)
        var current = node;
        while (current != null)
        {
            var vnode = current.LastVNode;
            if (vnode != null && vnode.Props.TryGetValue(eventName, out var handler))
            {
                if (handler is Action action)
                {
                    try { action(); }
                    catch (System.Exception ex)
                    {
                        Plugin.ReactUIPlugin.Logger.LogError($"[ReactUI] Event '{eventName}' handler threw: {ex}");
                    }
                    return; // handled — stop bubbling
                }
                else
                {
                    Plugin.ReactUIPlugin.Logger.LogWarning($"[ReactUI] Found '{eventName}' on {current.Type} but handler type is {handler?.GetType()?.Name ?? "null"}, not Action");
                }
            }
            current = current.Parent;
        }
    }

    /// <summary>
    /// Position the cursor in an input based on click X coordinate.
    /// Uses GUIStyle.CalcSize to measure text widths.
    /// </summary>
    static void PositionCursorFromClick(Core.UINode node, float clickX)
    {
        if (node.LastVNode == null) return;

        string text = "";
        if (node.LastVNode.Props.TryGetValue("value", out var valObj) && valObj is string s)
            text = s;

        if (text.Length == 0) { SetCursorPosition(node, 0); return; }

        var style = node.ComputedStyle;
        float padL = 0;
        if (style?.Padding != null)
            padL = style.Padding.Value.Left.Value;

        float textStartX = node.ScreenRect.X + padL;
        float relX = clickX - textStartX;

        if (relX <= 0) { SetCursorPosition(node, 0); return; }

        var guiStyle = new UnityEngine.GUIStyle();
        guiStyle.fontSize = (int)(style?.FontSize ?? 14f);
        guiStyle.fontStyle = (style?.FontWeight ?? 400) >= 700
            ? UnityEngine.FontStyle.Bold : UnityEngine.FontStyle.Normal;

        // Binary search for the character position closest to the click
        int best = text.Length;
        for (int i = 1; i <= text.Length; i++)
        {
            float w = guiStyle.CalcSize(new UnityEngine.GUIContent(text.Substring(0, i))).x;
            if (w > relX)
            {
                // Check if click is closer to i-1 or i
                float prevW = i > 1
                    ? guiStyle.CalcSize(new UnityEngine.GUIContent(text.Substring(0, i - 1))).x
                    : 0;
                best = (relX - prevW < w - relX) ? i - 1 : i;
                break;
            }
        }
        SetCursorPosition(node, best);
    }

    static void ProcessKeyboard(Core.UINode node)
    {
        // Tab key for focus navigation
        if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.Tab))
        {
            bool reverse = UnityEngine.Input.GetKey(UnityEngine.KeyCode.LeftShift) ||
                           UnityEngine.Input.GetKey(UnityEngine.KeyCode.RightShift);
            // Find the root node
            var root = node;
            while (root.Parent != null) root = root.Parent;
            FocusManager.TabNext(root, reverse);
            return;
        }

        // Key capture element handling
        if (node.Type == "keycapture")
        {
            // Check all key codes for a press
            foreach (UnityEngine.KeyCode kc in System.Enum.GetValues(typeof(UnityEngine.KeyCode)))
            {
                if (kc == UnityEngine.KeyCode.None) continue;
                if (UnityEngine.Input.GetKeyDown(kc))
                {
                    var vnode = node.LastVNode;
                    if (vnode != null && vnode.Props.TryGetValue("onCapture", out var captureObj) &&
                        captureObj is System.Action<UnityEngine.KeyCode> onCapture)
                    {
                        try { onCapture(kc); }
                        catch (System.Exception) { }
                    }
                    return;
                }
            }
            return;
        }

        // Text input handling
        if (node.Type == "input")
        {
            var vnode = node.LastVNode;
            if (vnode == null) return;

            string currentValue = "";
            if (vnode.Props.TryGetValue("value", out var valObj) && valObj is string valStr)
                currentValue = valStr;

            System.Action<string>? onChange = null;
            if (vnode.Props.TryGetValue("onChange", out var changeObj) && changeObj is System.Action<string> changeAction)
                onChange = changeAction;

            if (onChange == null) return;

            // Get/clamp cursor position
            int cursor = GetCursorPosition(node);
            if (cursor > currentValue.Length) cursor = currentValue.Length;
            if (cursor < 0) cursor = 0;

            bool ctrl = UnityEngine.Input.GetKey(UnityEngine.KeyCode.LeftControl) ||
                        UnityEngine.Input.GetKey(UnityEngine.KeyCode.RightControl);
            bool shift = UnityEngine.Input.GetKey(UnityEngine.KeyCode.LeftShift) ||
                         UnityEngine.Input.GetKey(UnityEngine.KeyCode.RightShift);
            bool hasSelection = GetSelection(node) != null;

            // Helper: start or extend selection when shift is held
            void EnsureSelectionAnchor()
            {
                if (!_selectionAnchors.ContainsKey(node))
                    _selectionAnchors[node] = cursor;
            }

            // Ctrl+A — Select all
            if (ctrl && UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.A))
            {
                _selectionAnchors[node] = 0;
                SetCursorPosition(node, currentValue.Length);
            }
            // Ctrl+C — Copy
            else if (ctrl && UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.C))
            {
                string selected = GetSelectedText(node, currentValue);
                if (selected.Length > 0)
                    UnityEngine.GUIUtility.systemCopyBuffer = selected;
            }
            // Ctrl+X — Cut
            else if (ctrl && UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.X))
            {
                string selected = GetSelectedText(node, currentValue);
                if (selected.Length > 0)
                {
                    UnityEngine.GUIUtility.systemCopyBuffer = selected;
                    var del = DeleteSelection(node, currentValue);
                    if (del != null)
                    {
                        SetCursorPosition(node, del.Value.Cursor);
                        try { onChange(del.Value.NewText); } catch { }
                    }
                }
            }
            // Ctrl+V — Paste
            else if (ctrl && UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.V))
            {
                string clipboard = UnityEngine.GUIUtility.systemCopyBuffer ?? "";
                // Strip control characters
                var sb = new System.Text.StringBuilder();
                foreach (char c in clipboard)
                {
                    if (c == '\n' || c == '\r' || c == '\t') { sb.Append(' '); continue; }
                    if (c >= 32) sb.Append(c);
                }
                string paste = sb.ToString();
                if (paste.Length > 0)
                {
                    // Delete selection first if any
                    var del = DeleteSelection(node, currentValue);
                    if (del != null)
                    {
                        currentValue = del.Value.NewText;
                        cursor = del.Value.Cursor;
                    }
                    string newValue = currentValue.Insert(cursor, paste);
                    SetCursorPosition(node, cursor + paste.Length);
                    ClearSelection(node);
                    try { onChange(newValue); } catch { }
                }
            }
            // Ctrl+Backspace — Delete word left
            else if (ctrl && UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.Backspace))
            {
                if (hasSelection)
                {
                    var del = DeleteSelection(node, currentValue);
                    if (del != null)
                    {
                        SetCursorPosition(node, del.Value.Cursor);
                        try { onChange(del.Value.NewText); } catch { }
                    }
                }
                else if (cursor > 0)
                {
                    int boundary = FindWordBoundary(currentValue, cursor, -1);
                    string newValue = currentValue.Remove(boundary, cursor - boundary);
                    SetCursorPosition(node, boundary);
                    try { onChange(newValue); } catch { }
                }
            }
            // Ctrl+Delete — Delete word right
            else if (ctrl && UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.Delete))
            {
                if (hasSelection)
                {
                    var del = DeleteSelection(node, currentValue);
                    if (del != null)
                    {
                        SetCursorPosition(node, del.Value.Cursor);
                        try { onChange(del.Value.NewText); } catch { }
                    }
                }
                else if (cursor < currentValue.Length)
                {
                    int boundary = FindWordBoundary(currentValue, cursor, 1);
                    string newValue = currentValue.Remove(cursor, boundary - cursor);
                    try { onChange(newValue); } catch { }
                }
            }
            // Ctrl+Left — Move cursor one word left
            else if (ctrl && UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.LeftArrow))
            {
                if (shift) EnsureSelectionAnchor();
                else ClearSelection(node);
                SetCursorPosition(node, FindWordBoundary(currentValue, cursor, -1));
            }
            // Ctrl+Right — Move cursor one word right
            else if (ctrl && UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.RightArrow))
            {
                if (shift) EnsureSelectionAnchor();
                else ClearSelection(node);
                SetCursorPosition(node, FindWordBoundary(currentValue, cursor, 1));
            }
            // Arrow keys (with optional Shift for selection)
            else if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.LeftArrow))
            {
                if (shift)
                {
                    EnsureSelectionAnchor();
                    if (cursor > 0) SetCursorPosition(node, cursor - 1);
                }
                else if (hasSelection)
                {
                    var sel = GetSelection(node)!.Value;
                    SetCursorPosition(node, sel.Start);
                    ClearSelection(node);
                }
                else if (cursor > 0)
                {
                    SetCursorPosition(node, cursor - 1);
                }
            }
            else if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.RightArrow))
            {
                if (shift)
                {
                    EnsureSelectionAnchor();
                    if (cursor < currentValue.Length) SetCursorPosition(node, cursor + 1);
                }
                else if (hasSelection)
                {
                    var sel = GetSelection(node)!.Value;
                    SetCursorPosition(node, sel.End);
                    ClearSelection(node);
                }
                else if (cursor < currentValue.Length)
                {
                    SetCursorPosition(node, cursor + 1);
                }
            }
            // Home/End (with optional Shift)
            else if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.Home))
            {
                if (shift) EnsureSelectionAnchor();
                else ClearSelection(node);
                SetCursorPosition(node, 0);
            }
            else if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.End))
            {
                if (shift) EnsureSelectionAnchor();
                else ClearSelection(node);
                SetCursorPosition(node, currentValue.Length);
            }
            // Delete key — delete selection or char at cursor
            else if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.Delete))
            {
                if (hasSelection)
                {
                    var del = DeleteSelection(node, currentValue);
                    if (del != null)
                    {
                        SetCursorPosition(node, del.Value.Cursor);
                        try { onChange(del.Value.NewText); } catch { }
                    }
                }
                else if (cursor < currentValue.Length)
                {
                    string newValue = currentValue.Remove(cursor, 1);
                    try { onChange(newValue); } catch { }
                }
            }
            // Backspace — delete selection or char before cursor
            else if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.Backspace))
            {
                if (hasSelection)
                {
                    var del = DeleteSelection(node, currentValue);
                    if (del != null)
                    {
                        SetCursorPosition(node, del.Value.Cursor);
                        try { onChange(del.Value.NewText); } catch { }
                    }
                }
                else if (cursor > 0)
                {
                    string newValue = currentValue.Remove(cursor - 1, 1);
                    SetCursorPosition(node, cursor - 1);
                    try { onChange(newValue); } catch { }
                }
            }
            else if (!ctrl)
            {
                // Handle typed characters — insert at cursor position (replaces selection)
                string inputString = UnityEngine.Input.inputString;
                if (!string.IsNullOrEmpty(inputString))
                {
                    var sb = new System.Text.StringBuilder();
                    foreach (char c in inputString)
                    {
                        if (c == '\b') continue;
                        if (c == '\n' || c == '\r') continue;
                        if (c < 32) continue;
                        sb.Append(c);
                    }
                    string typed = sb.ToString();
                    if (typed.Length > 0)
                    {
                        // Delete selection first if any
                        var del = DeleteSelection(node, currentValue);
                        if (del != null)
                        {
                            currentValue = del.Value.NewText;
                            cursor = del.Value.Cursor;
                        }
                        string newValue = currentValue.Insert(cursor, typed);
                        SetCursorPosition(node, cursor + typed.Length);
                        ClearSelection(node);
                        try { onChange(newValue); } catch { }
                    }
                }
            }

            // Fire onKeyDown for any key, passing the KeyCode
            foreach (UnityEngine.KeyCode kc in System.Enum.GetValues(typeof(UnityEngine.KeyCode)))
            {
                if (kc == UnityEngine.KeyCode.None) continue;
                if (UnityEngine.Input.GetKeyDown(kc))
                {
                    var lastVNode = node.LastVNode;
                    if (lastVNode != null && lastVNode.Props.TryGetValue("onKeyDown", out var handler))
                    {
                        if (handler is System.Action<UnityEngine.KeyCode> kcAction)
                            try { kcAction(kc); } catch { }
                        else if (handler is System.Action action)
                            try { action(); } catch { }
                    }
                    break;
                }
            }
        }
    }
}
