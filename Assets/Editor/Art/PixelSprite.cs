using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>Lienzo de píxeles (origen abajo-izquierda) para generar pixel art desde código.</summary>
public sealed class PixelSprite
{
    public readonly int Width;
    public readonly int Height;
    readonly Color32[] pixels;

    public PixelSprite(int width, int height)
    {
        Width = width;
        Height = height;
        pixels = new Color32[width * height];
    }

    public Color32 Get(int x, int y) => pixels[y * Width + x];

    public void Set(int x, int y, Color32 color)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height) return;
        pixels[y * Width + x] = color;
    }

    /// <summary>Rellena el rectángulo [x0..x1] x [y0..y1] (inclusivo).</summary>
    public void Fill(int x0, int y0, int x1, int y1, Color32 color)
    {
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
                Set(x, y, color);
    }

    public void Blend(int x, int y, Color32 color)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height) return;
        var under = Get(x, y);
        float a = color.a / 255f;
        if (under.a == 0)
        {
            Set(x, y, color);
            return;
        }
        Set(x, y, new Color32(
            (byte)Mathf.Lerp(under.r, color.r, a), (byte)Mathf.Lerp(under.g, color.g, a),
            (byte)Mathf.Lerp(under.b, color.b, a), (byte)Mathf.Max(under.a, color.a)));
    }

    public byte[] EncodePng()
    {
        var texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
        texture.SetPixels32(pixels);
        texture.Apply();
        byte[] png = texture.EncodeToPNG();
        Object.DestroyImmediate(texture);
        return png;
    }

    public static Color32 Hex(string hex, byte alpha = 255)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out var c);
        Color32 c32 = c;
        c32.a = alpha;
        return c32;
    }
}

/// <summary>Guarda PNG generados como sprites/tiles con la configuración de pixel art del proyecto.</summary>
public static class ArtAssets
{
    public const int PixelsPerUnit = 16;

    public static Sprite SaveSprite(PixelSprite art, string path, Vector2 pivot, int ppu = PixelsPerUnit,
                                    FilterMode filter = FilterMode.Point, Vector4? border = null)
    {
        BuildPaths.EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
        File.WriteAllBytes(path, art.EncodePng());
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = ppu;
        importer.filterMode = filter;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = pivot;
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.spriteBorder = border ?? Vector4.zero;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    public static Tile SaveTile(Sprite sprite, string path, bool allowTint)
    {
        var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
        if (tile == null)
        {
            tile = ScriptableObject.CreateInstance<Tile>();
            AssetDatabase.CreateAsset(tile, path);
        }
        tile.sprite = sprite;
        tile.colliderType = Tile.ColliderType.None;
        tile.flags = allowTint ? TileFlags.None : TileFlags.LockColor;
        tile.color = Color.white;
        EditorUtility.SetDirty(tile);
        return tile;
    }

    public static Material LoadOrCreateMaterial(string path, string shaderName, Texture texture = null)
    {
        BuildPaths.EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        var shader = Shader.Find(shaderName);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }
        material.shader = shader;
        if (texture != null) material.mainTexture = texture;
        if (material.HasProperty("_TintColor")) material.SetColor("_TintColor", new Color(0.5f, 0.5f, 0.5f, 0.5f));
        EditorUtility.SetDirty(material);
        return material;
    }
}
