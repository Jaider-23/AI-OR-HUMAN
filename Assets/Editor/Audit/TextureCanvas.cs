using UnityEngine;

/// <summary>
/// Dibujo sencillo sobre una Texture2D en coordenadas de mundo (líneas, puntos, polígonos).
/// Lo usa la auditoría de colisiones para superponer colliders sobre la captura del nivel.
/// </summary>
public sealed class TextureCanvas
{
    readonly Texture2D texture;
    readonly Vector2 worldMin;
    readonly float pixelsPerUnit;

    public TextureCanvas(Texture2D texture, Vector2 worldMin, float pixelsPerUnit)
    {
        this.texture = texture;
        this.worldMin = worldMin;
        this.pixelsPerUnit = pixelsPerUnit;
    }

    public Vector2Int ToPixel(Vector2 world) =>
        new Vector2Int(Mathf.RoundToInt((world.x - worldMin.x) * pixelsPerUnit),
                       Mathf.RoundToInt((world.y - worldMin.y) * pixelsPerUnit));

    public void Blend(int x, int y, Color color)
    {
        if (x < 0 || y < 0 || x >= texture.width || y >= texture.height) return;
        var under = texture.GetPixel(x, y);
        texture.SetPixel(x, y, Color.Lerp(under, new Color(color.r, color.g, color.b, 1f), color.a));
    }

    public void Dot(Vector2 world, int radius, Color color)
    {
        var p = ToPixel(world);
        for (int dx = -radius; dx <= radius; dx++)
            for (int dy = -radius; dy <= radius; dy++)
                if (dx * dx + dy * dy <= radius * radius) Blend(p.x + dx, p.y + dy, color);
    }

    public void Line(Vector2 a, Vector2 b, Color color, int thickness = 1)
    {
        var p0 = ToPixel(a);
        var p1 = ToPixel(b);
        int steps = Mathf.Max(Mathf.Abs(p1.x - p0.x), Mathf.Abs(p1.y - p0.y), 1);
        for (int i = 0; i <= steps; i++)
        {
            float t = (float)i / steps;
            int x = Mathf.RoundToInt(Mathf.Lerp(p0.x, p1.x, t));
            int y = Mathf.RoundToInt(Mathf.Lerp(p0.y, p1.y, t));
            for (int dx = -thickness / 2; dx <= thickness / 2; dx++)
                for (int dy = -thickness / 2; dy <= thickness / 2; dy++)
                    Blend(x + dx, y + dy, color);
        }
    }

    public void Polygon(Vector2[] points, Color color, int thickness = 2)
    {
        for (int i = 0; i < points.Length; i++) Line(points[i], points[(i + 1) % points.Length], color, thickness);
    }

    public void Apply() => texture.Apply();
}
