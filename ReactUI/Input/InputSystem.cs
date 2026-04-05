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

    // Drag state
    static bool _isDragging;
    static float _dragOffsetX, _dragOffsetY;
    static Action<float>? _dragSetX, _dragSetY;

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
        float mx = UnityEngine.Input.mousePosition.x;
        float my = UnityEngine.Screen.height - UnityEngine.Input.mousePosition.y;
        _dragOffsetX = mx - currentX;
        _dragOffsetY = my - currentY;
        _dragSetX = setX;
        _dragSetY = setY;
        _isDragging = true;
        Plugin.ReactUIPlugin.Logger.LogInfo($"[ReactUI] StartDrag pos=({currentX},{currentY}) mouse=({mx},{my}) offset=({_dragOffsetX},{_dragOffsetY})");
    }

    public static void ProcessInput(Core.UINode? root)
    {
        if (root == null) return;

        var mousePos = UnityEngine.Input.mousePosition;
        // Unity mouse Y is bottom-up, convert to top-down
        float mx = mousePos.x;
        float my = UnityEngine.Screen.height - mousePos.y;

        var hit = HitTesting.HitTest(root, mx, my);

        // Log hit testing periodically for diagnostics
        if (UnityEngine.Input.GetMouseButtonDown(0))
            Plugin.ReactUIPlugin.Logger.LogInfo($"[ReactUI] HitTest at ({mx},{my}) = {(hit != null ? hit.Type + " rect=" + hit.ScreenRect : "null")}");


        // Handle hover transitions
        if (hit != _hoveredNode)
        {
            if (_hoveredNode != null)
            {
                _hoveredNode.IsHovered = false;
                FireEvent(_hoveredNode, "onMouseLeave");
            }
            _hoveredNode = hit;
            if (_hoveredNode != null)
            {
                _hoveredNode.IsHovered = true;
                FireEvent(_hoveredNode, "onMouseEnter");
            }
        }

        // Mouse down
        if (UnityEngine.Input.GetMouseButtonDown(0))
        {
            _activeNode = hit;
            if (hit != null)
            {
                hit.IsActive = true;
                FireEvent(hit, "onMouseDown");

                // Check if we should start dragging: walk up from hit to find a registered draggable.
                // Skip if the hit node has an onClick handler (button/interactive element).
                if (!_isDragging)
                {
                    bool hasClickHandler = HasEventHandler(hit, "onClick");
                    if (!hasClickHandler)
                    {
                        var ancestor = hit;
                        while (ancestor != null)
                        {
                            if (ancestor.Type == "__component" && _dragTargets.TryGetValue(ancestor.ComponentId, out var target))
                            {
                                float curX = target.GetX();
                                float curY = target.GetY();
                                _dragOffsetX = mx - curX;
                                _dragOffsetY = my - curY;
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
            // Focus management
            FocusManager.SetFocus(hit);
        }

        // Mouse up
        if (UnityEngine.Input.GetMouseButtonUp(0))
        {
            if (_activeNode != null)
            {
                _activeNode.IsActive = false;
                FireEvent(_activeNode, "onMouseUp");
                if (_activeNode == hit && !_isDragging)
                    FireEvent(_activeNode, "onClick");
                _activeNode = null;
            }
            _isDragging = false;
            _dragSetX = null;
            _dragSetY = null;
        }

        // Drag tracking — runs every frame while dragging
        if (_isDragging && _dragSetX != null && _dragSetY != null)
        {
            float newX = mx - _dragOffsetX;
            float newY = my - _dragOffsetY;
            _dragSetX(newX);
            _dragSetY(newY);
        }

        // Scroll
        float scroll = UnityEngine.Input.mouseScrollDelta.y;
        if (scroll != 0 && hit != null)
        {
            var scrollable = FindScrollableAncestor(hit);
            if (scrollable != null)
            {
                scrollable.ScrollOffsetY -= scroll * 40f;
                // Clamp scroll offset to non-negative (the renderer will handle max clamp
                // based on content height minus viewport height)
                if (scrollable.ScrollOffsetY < 0)
                    scrollable.ScrollOffsetY = 0;
                FireEvent(scrollable, "onScroll");
            }
        }

        // Keyboard input to focused element
        if (FocusManager.Focused != null)
        {
            ProcessKeyboard(FocusManager.Focused);
        }

        // Cursor
        CursorManager.Update(_hoveredNode);
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

            // Handle backspace
            if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.Backspace))
            {
                if (currentValue.Length > 0)
                {
                    try { onChange(currentValue.Substring(0, currentValue.Length - 1)); }
                    catch (System.Exception) { }
                }
                return;
            }

            // Handle typed characters
            string inputString = UnityEngine.Input.inputString;
            if (!string.IsNullOrEmpty(inputString))
            {
                // Filter out control characters
                var sb = new System.Text.StringBuilder(currentValue);
                foreach (char c in inputString)
                {
                    if (c == '\b') continue; // backspace already handled
                    if (c == '\n' || c == '\r') continue; // ignore enter
                    if (c < 32) continue; // ignore other control chars
                    sb.Append(c);
                }
                string newValue = sb.ToString();
                if (newValue != currentValue)
                {
                    try { onChange(newValue); }
                    catch (System.Exception) { }
                }
            }

            // Fire onKeyDown for any key
            foreach (UnityEngine.KeyCode kc in System.Enum.GetValues(typeof(UnityEngine.KeyCode)))
            {
                if (kc == UnityEngine.KeyCode.None) continue;
                if (UnityEngine.Input.GetKeyDown(kc))
                {
                    FireEvent(node, "onKeyDown");
                    break;
                }
            }
        }
    }
}
