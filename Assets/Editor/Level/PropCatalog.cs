using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

[Serializable]
public class PropFootprint
{
    public int x;
    public int y;
    public int w;
    public int h;
}

[Serializable]
public class PropEntry
{
    public int id;
    public string label;
    public string category;
    public string placement;
    public bool solid;
    public PropFootprint footprint;
    public string interactive;
    public string zoneUse;
    public string notes;
}

[Serializable]
public class PropCatalogData
{
    public PropEntry[] entries;
}

/// <summary>
/// Catálogo de muebles (Assets/Level/prop_catalog.json, revisado sprite a sprite): dónde va cada
/// pieza (suelo, pared, encima de una mesa) y la huella exacta de su base, que se usa como collider.
/// Los ids negativos son muebles generados (armarios de servidores).
/// </summary>
public sealed class PropCatalog
{
    public const string JsonPath = "Assets/Level/prop_catalog.json";
    public const int RackA = -1;
    public const int RackB = -2;

    /// <summary>Tipos de decorado del mapa → sprites candidatos (de más estrecho a más ancho).</summary>
    public static readonly Dictionary<string, int[]> KindSprites = new Dictionary<string, int[]>
    {
        { "desk", new[] { 13, 14, 6, 35 } },
        { "chair", new[] { 5, 46, 47 } },
        { "server_rack", new[] { RackA, RackB } },
        { "counter", new[] { 35, 42, 49, 55 } },
        { "cabinet", new[] { 43, 42, 49 } },
        { "pod", new[] { 23, 24 } },
        { "sofa", new[] { 46, 47, 33, 34 } },
        { "bed", new[] { 68 } },
        { "vending", new[] { 43, 44, 45 } },
        { "table", new[] { 6, 61, 62 } },
        { "console", new[] { 13, 14 } },
        { "crate", new[] { 59, 60 } },
        { "lamp", new[] { 50 } },
        { "beanbag", new[] { 23, 24 } },
        { "wall_screen", new[] { 12, 11, 36, 15, 4 } },
        { "neon_sign", new[] { 51, 52, 53, 54, 71, 72, 7 } },
        { "wall_panel", new[] { 1, 10, 15, 4 } },
        { "window_panel", new[] { 9, 27, 19, 8, 26, 18 } },
    };

    /// <summary>Muebles sobre los que se colocan objetos pequeños (monitor, teclado, taza...).</summary>
    public static readonly HashSet<int> DeskLike = new HashSet<int> { 13, 14, 6, 35, 48, 61, 62 };
    public static readonly int[] DeskItems = { 21, 31, 28, 66, 67, 63 };

    readonly Dictionary<int, PropEntry> entries;
    readonly Dictionary<int, Sprite> sprites;
    readonly Sprite[] racks;

    PropCatalog(Dictionary<int, PropEntry> entries, Dictionary<int, Sprite> sprites, Sprite[] racks)
    {
        this.entries = entries;
        this.sprites = sprites;
        this.racks = racks;
    }

    public static PropCatalog Load(Sprite rackA, Sprite rackB)
    {
        if (!File.Exists(JsonPath)) throw new FileNotFoundException($"Falta el catálogo de muebles {JsonPath}");
        var data = JsonUtility.FromJson<PropCatalogData>(File.ReadAllText(JsonPath));
        var entries = data.entries.ToDictionary(e => e.id);
        var sprites = AssetDatabase.LoadAllAssetsAtPath(BuildPaths.Interior).OfType<Sprite>()
            .ToDictionary(s => int.Parse(s.name.Substring(s.name.LastIndexOf('_') + 1)));
        entries[RackA] = RackEntry(RackA);
        entries[RackB] = RackEntry(RackB);
        return new PropCatalog(entries, sprites, new[] { rackA, rackB });
    }

    static PropEntry RackEntry(int id) => new PropEntry
    {
        id = id, label = "Server rack", category = "furniture", placement = "floor", solid = true,
        footprint = new PropFootprint { x = 0, y = 0, w = PropArt.RackWidth, h = PropArt.RackDepth }, interactive = "none"
    };

    public PropEntry Entry(int id) => entries.TryGetValue(id, out var e) ? e : null;

    public Sprite SpriteOf(int id) => id < 0 ? racks[-id - 1] : sprites.TryGetValue(id, out var s) ? s : null;

    public bool IsWallKind(string kind) => LevelLayoutValidator.WallDecor.Contains(kind);

    /// <summary>Ancho del sprite en casillas (redondeado hacia arriba).</summary>
    public int WidthInCells(int id)
    {
        var sprite = SpriteOf(id);
        return sprite == null ? 1 : Mathf.CeilToInt(sprite.rect.width / 16f);
    }

    /// <summary>Fila (en píxeles) donde la base toca el suelo: 0 salvo muebles con patas/puertas abajo.</summary>
    public int BaseRow(int id)
    {
        var entry = Entry(id);
        return entry != null && entry.solid && entry.footprint != null && entry.footprint.w > 0 ? entry.footprint.y : 0;
    }

    /// <summary>
    /// Rectángulo de la huella en unidades, relativo al pivote del sprite (abajo, en un píxel entero del
    /// centro). null si el mueble no es sólido. Es la única definición de la huella: colisión y ocupación la usan.
    /// </summary>
    public Rect? FootprintRect(int id)
    {
        var entry = Entry(id);
        var sprite = SpriteOf(id);
        if (entry == null || sprite == null || !entry.solid || entry.footprint == null || entry.footprint.w <= 0) return null;
        var f = entry.footprint;
        int depth = Mathf.Clamp(f.h, MinDepthPx, MaxDepthPx);
        return new Rect((f.x - sprite.pivot.x) / 16f, f.y / 16f, f.w / 16f, depth / 16f);
    }

    /// <summary>
    /// Añade un BoxCollider2D con la huella. <paramref name="originY"/> es la altura (en unidades) del objeto
    /// respecto al pivote del sprite (los muebles se colocan con su origen en la fila de apoyo).
    /// </summary>
    public bool AddFootprint(GameObject go, int id, float originY = 0f)
    {
        var rect = FootprintRect(id);
        if (rect == null) return false;
        var box = go.AddComponent<BoxCollider2D>();
        box.size = rect.Value.size;
        box.offset = new Vector2(rect.Value.center.x, rect.Value.center.y - originY);
        return true;
    }

    const int MinDepthPx = 3;
    const int MaxDepthPx = 16;
}
