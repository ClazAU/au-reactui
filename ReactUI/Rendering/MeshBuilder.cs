using System.Collections.Generic;

namespace ReactUI.Rendering;

using UnityEngine;

public class MeshBuilder
{
    private readonly List<Vector3> _vertices = new();
    private readonly List<Vector2> _uvs = new();
    private readonly List<Color> _colors = new();
    private readonly List<int> _indices = new();

    public void Clear()
    {
        _vertices.Clear();
        _uvs.Clear();
        _colors.Clear();
        _indices.Clear();
    }

    /// <summary>
    /// Add a screen-space quad with uniform color and default UVs (0-1).
    /// </summary>
    public void AddQuad(Core.Rect rect, Color color)
    {
        int baseIdx = _vertices.Count;

        // TL, TR, BR, BL
        _vertices.Add(new Vector3(rect.X, rect.Y, 0));
        _vertices.Add(new Vector3(rect.Right, rect.Y, 0));
        _vertices.Add(new Vector3(rect.Right, rect.Bottom, 0));
        _vertices.Add(new Vector3(rect.X, rect.Bottom, 0));

        _uvs.Add(new Vector2(0, 1));
        _uvs.Add(new Vector2(1, 1));
        _uvs.Add(new Vector2(1, 0));
        _uvs.Add(new Vector2(0, 0));

        _colors.Add(color);
        _colors.Add(color);
        _colors.Add(color);
        _colors.Add(color);

        _indices.Add(baseIdx);
        _indices.Add(baseIdx + 1);
        _indices.Add(baseIdx + 2);
        _indices.Add(baseIdx);
        _indices.Add(baseIdx + 2);
        _indices.Add(baseIdx + 3);
    }

    /// <summary>
    /// Add a screen-space quad with per-vertex colors (for gradient support).
    /// </summary>
    public void AddQuad(Core.Rect rect, Color colorTL, Color colorTR, Color colorBR, Color colorBL)
    {
        int baseIdx = _vertices.Count;

        _vertices.Add(new Vector3(rect.X, rect.Y, 0));
        _vertices.Add(new Vector3(rect.Right, rect.Y, 0));
        _vertices.Add(new Vector3(rect.Right, rect.Bottom, 0));
        _vertices.Add(new Vector3(rect.X, rect.Bottom, 0));

        _uvs.Add(new Vector2(0, 1));
        _uvs.Add(new Vector2(1, 1));
        _uvs.Add(new Vector2(1, 0));
        _uvs.Add(new Vector2(0, 0));

        _colors.Add(colorTL);
        _colors.Add(colorTR);
        _colors.Add(colorBR);
        _colors.Add(colorBL);

        _indices.Add(baseIdx);
        _indices.Add(baseIdx + 1);
        _indices.Add(baseIdx + 2);
        _indices.Add(baseIdx);
        _indices.Add(baseIdx + 2);
        _indices.Add(baseIdx + 3);
    }

    /// <summary>
    /// Add a screen-space quad with custom UV coordinates (for atlas/glyph rendering).
    /// </summary>
    public void AddQuad(Core.Rect rect, Core.Rect uvRect, Color color)
    {
        int baseIdx = _vertices.Count;

        _vertices.Add(new Vector3(rect.X, rect.Y, 0));
        _vertices.Add(new Vector3(rect.Right, rect.Y, 0));
        _vertices.Add(new Vector3(rect.Right, rect.Bottom, 0));
        _vertices.Add(new Vector3(rect.X, rect.Bottom, 0));

        _uvs.Add(new Vector2(uvRect.X, uvRect.Y + uvRect.Height));
        _uvs.Add(new Vector2(uvRect.Right, uvRect.Y + uvRect.Height));
        _uvs.Add(new Vector2(uvRect.Right, uvRect.Y));
        _uvs.Add(new Vector2(uvRect.X, uvRect.Y));

        _colors.Add(color);
        _colors.Add(color);
        _colors.Add(color);
        _colors.Add(color);

        _indices.Add(baseIdx);
        _indices.Add(baseIdx + 1);
        _indices.Add(baseIdx + 2);
        _indices.Add(baseIdx);
        _indices.Add(baseIdx + 2);
        _indices.Add(baseIdx + 3);
    }

    public Mesh Build()
    {
        var mesh = new Mesh();
        mesh.SetVertices(_vertices.ToArray());
        mesh.SetUVs(0, _uvs.ToArray());
        mesh.SetColors(_colors.ToArray());
        mesh.SetTriangles(_indices.ToArray(), 0);
        return mesh;
    }

    public int VertexCount => _vertices.Count;
    public int TriangleCount => _indices.Count / 3;
}
