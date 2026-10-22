using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Lee la opacidad real de un sprite en un punto del mundo (carga una copia legible del PNG/JPG
/// original, sin tocar la configuración de importación). Sirve para saber si un collider cubre
/// píxeles visibles o "aire".
/// </summary>
public static class SpritePixelSampler
{
    static readonly Dictionary<Texture2D, Texture2D> cache = new Dictionary<Texture2D, Texture2D>();

    public static bool IsOpaqueAt(SpriteRenderer renderer, Vector2 world, float alphaThreshold = 0.5f)
    {
        var sprite = renderer.sprite;
        if (sprite == null || !renderer.enabled) return false;
        var readable = Readable(sprite.texture);
        if (readable == null) return false;

        Vector2 local = renderer.transform.InverseTransformPoint(world);
        if (renderer.flipX) local.x = -local.x;
        if (renderer.flipY) local.y = -local.y;
        float px = sprite.rect.x + sprite.pivot.x + local.x * sprite.pixelsPerUnit;
        float py = sprite.rect.y + sprite.pivot.y + local.y * sprite.pixelsPerUnit;
        if (px < sprite.rect.xMin || px >= sprite.rect.xMax || py < sprite.rect.yMin || py >= sprite.rect.yMax) return false;

        float scale = (float)readable.width / sprite.texture.width;
        return readable.GetPixel(Mathf.FloorToInt(px * scale), Mathf.FloorToInt(py * scale)).a >= alphaThreshold;
    }

    static Texture2D Readable(Texture2D source)
    {
        if (cache.TryGetValue(source, out var cached)) return cached;
        Texture2D copy = null;
        string path = AssetDatabase.GetAssetPath(source);
        if (!string.IsNullOrEmpty(path) && File.Exists(path))
        {
            copy = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!copy.LoadImage(File.ReadAllBytes(path))) copy = null;
        }
        cache[source] = copy;
        return copy;
    }

    public static void ClearCache() => cache.Clear();
}
