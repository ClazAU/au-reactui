using System;
using System.Collections.Generic;

namespace ReactUI.Rendering;

public class ClipStack
{
    private readonly Stack<Core.Rect> _stack = new();

    public void Push(Core.Rect rect)
    {
        if (_stack.Count > 0)
            rect = Intersect(_stack.Peek(), rect);
        _stack.Push(rect);
    }

    public void Pop()
    {
        if (_stack.Count > 0)
            _stack.Pop();
    }

    public Core.Rect Current => _stack.Count > 0
        ? _stack.Peek()
        : new Core.Rect(0, 0, float.MaxValue, float.MaxValue);

    public int Depth => _stack.Count;

    public void Clear() => _stack.Clear();

    private static Core.Rect Intersect(Core.Rect a, Core.Rect b)
    {
        float x = Math.Max(a.X, b.X);
        float y = Math.Max(a.Y, b.Y);
        float r = Math.Min(a.Right, b.Right);
        float bot = Math.Min(a.Bottom, b.Bottom);
        return new Core.Rect(x, y, Math.Max(0, r - x), Math.Max(0, bot - y));
    }
}
