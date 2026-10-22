using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Pinta las capas de tiles del laboratorio a partir del mapa ASCII:
/// suelo (con variación y tema por sala), detalle (luces guía), sombras, paredes 3/4,
/// y neón de las paredes (teñido por sala). Los dinteles de las puertas los pone LabAmbience.
/// </summary>
public static class LabTilemaps
{
    const string TilesFolder = "Assets/Tilemaps/tiles";

    public sealed class Layers
    {
        public Tilemap Floor;
        public Tilemap Shadows;
        public Tilemap Walls;
        public Tilemap Neon;
    }

    public static readonly Color HubColor = Theme.Hex("5FD8FF");

    static readonly Color[] ZoneColors =
    {
        Theme.Hex("5FD8FF"), Theme.Hex("22E4F0"), Theme.Hex("3D8BFF"), Theme.Hex("46F28C"),
        Theme.Hex("FF3FD1"), Theme.Hex("FF9A3C"), Theme.Hex("9B5CFF"), Theme.Hex("FF4D6A")
    };

    static readonly string[] ZoneFloors =
    {
        "floor_concrete_clean_base", "floor_concrete_clean_steel_blue", "floor_concrete_clean_grate",
        "floor_concrete_clean_wet", "floor_concrete_clean_swap_neon", "floor_concrete_clean_grate_90",
        "floor_concrete_clean_damaged", "floor_concrete_clean_corner_blend"
    };

    public static Color ZoneColor(int zone) => ZoneColors[Mathf.Clamp(zone, 0, 7)];

    public static Layers Prepare(ArtLibrary art)
    {
        var grid = Object.FindObjectsOfType<Grid>().First();
        var layers = new Layers
        {
            Floor = Ensure(grid, "Tilemap_piso", -20),
            Shadows = Ensure(grid, "Tilemap_sombras", -18),
            Walls = Ensure(grid, "Tilemap_paredes", -10),
            Neon = Ensure(grid, "Tilemap_neon", -9),
        };
        layers.Neon.GetComponent<TilemapRenderer>().sharedMaterial = art.Additive;
        foreach (var name in new[] { "Tilemap_detalle", "Tilemap_overhead" })
        {
            var obsolete = grid.transform.Find(name);
            if (obsolete != null) Object.DestroyImmediate(obsolete.gameObject);
        }
        if (layers.Neon.GetComponent<TilemapPulse>() == null) layers.Neon.gameObject.AddComponent<TilemapPulse>();
        StripPhysics(layers.Walls.gameObject);
        foreach (var t in new[] { layers.Floor, layers.Shadows, layers.Walls, layers.Neon })
        {
            t.ClearAllTiles();
            t.color = Color.white;
        }
        layers.Floor.color = new Color(0.84f, 0.87f, 0.94f);
        return layers;
    }

    static Tilemap Ensure(Grid grid, string name, int order)
    {
        var existing = grid.transform.Find(name);
        var go = existing != null ? existing.gameObject : new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
        go.transform.SetParent(grid.transform, false);
        var renderer = go.GetComponent<TilemapRenderer>();
        renderer.sortingOrder = order;
        renderer.mode = TilemapRenderer.Mode.Chunk;
        return go.GetComponent<Tilemap>();
    }

    /// <summary>La colisión de paredes ya no sale del Tilemap (ver <see cref="WallColliderBuilder"/>).</summary>
    static void StripPhysics(GameObject walls)
    {
        var composite = walls.GetComponent<CompositeCollider2D>();
        if (composite != null) Object.DestroyImmediate(composite);
        var tileCollider = walls.GetComponent<TilemapCollider2D>();
        if (tileCollider != null) Object.DestroyImmediate(tileCollider);
        var body = walls.GetComponent<Rigidbody2D>();
        if (body != null) Object.DestroyImmediate(body);
    }

    static TileBase Tile(string name)
    {
        string path = $"{TilesFolder}/{name}.asset";
        var tile = AssetDatabase.LoadAssetAtPath<TileBase>(path);
        if (tile == null) throw new System.IO.FileNotFoundException($"Falta el tile {path} (lo usa el suelo del laboratorio)");
        return tile;
    }

    /// <summary>Variación estable por casilla en [0, 100).</summary>
    static int Noise(int x, int y) => (WallAutotile.FaceDecor(x * 7 + 3, y * 13 + 5) * 31 + ((x * 92821) ^ (y * 68917)) & 0x7fffffff) % 100;

    public static void PaintFloor(LevelLayout map, Layers layers, ArtLibrary art)
    {
        var hub = new[] { Tile("floor_concrete_clean_base"), Tile("floor_concrete_clean_damaged"), Tile("floor_concrete_clean_wet"), Tile("floor_concrete_clean_grate") };
        var zoneTiles = ZoneFloors.Select(Tile).ToArray();
        var border = Tile("floor_concrete_clean_base");
        for (int y = 0; y < map.Height; y++)
            for (int x = 0; x < map.Width; x++)
            {
                var cell = map.ToCell(x, y);
                switch (map.KindAt(x, y))
                {
                    case CellKind.Hub:
                        int n = Noise(x, y);
                        layers.Floor.SetTile(cell, n < 84 ? hub[0] : n < 91 ? hub[1] : n < 96 ? hub[2] : hub[3]);
                        break;
                    case CellKind.Zone:
                        bool edge = TouchesWall(map, x, y);
                        layers.Floor.SetTile(cell, edge ? border : zoneTiles[map.ZoneAt(x, y)]);
                        break;
                    case CellKind.Door:
                        layers.Floor.SetTile(cell, IsHorizontalDoor(map, x, y) ? art.PlateH : art.PlateV);
                        break;
                }
            }
        PaintRunways(map, layers, art);
    }

    static bool TouchesWall(LevelLayout map, int x, int y)
    {
        for (int dy = -1; dy <= 1; dy++)
            for (int dx = -1; dx <= 1; dx++)
                if ((dx != 0 || dy != 0) && map.KindAt(x + dx, y + dy) == CellKind.Wall) return true;
        return false;
    }

    /// <summary>Una puerta es horizontal si está en una pared horizontal (se cruza de norte a sur).</summary>
    public static bool IsHorizontalDoor(LevelLayout map, int x, int y)
    {
        bool vertical = map.IsWalkable(x - 1, y) && map.KindAt(x - 1, y) != CellKind.Door
                        || map.IsWalkable(x + 1, y) && map.KindAt(x + 1, y) != CellKind.Door;
        return !vertical;
    }

    /// <summary>Grupos de casillas de puerta contiguas (cada grupo es una puerta).</summary>
    public static List<List<Vector2Int>> DoorGroups(LevelLayout map)
    {
        var groups = new List<List<Vector2Int>>();
        var seen = new HashSet<Vector2Int>();
        for (int y = 0; y < map.Height; y++)
            for (int x = 0; x < map.Width; x++)
            {
                var start = new Vector2Int(x, y);
                if (map.KindAt(x, y) != CellKind.Door || seen.Contains(start)) continue;
                var group = new List<Vector2Int>();
                var queue = new Queue<Vector2Int>();
                queue.Enqueue(start);
                seen.Add(start);
                while (queue.Count > 0)
                {
                    var p = queue.Dequeue();
                    group.Add(p);
                    foreach (var d in new[] { Vector2Int.left, Vector2Int.right, Vector2Int.up, Vector2Int.down })
                    {
                        var q = p + d;
                        if (map.KindAt(q.x, q.y) != CellKind.Door || seen.Contains(q)) continue;
                        seen.Add(q);
                        queue.Enqueue(q);
                    }
                }
                groups.Add(group);
            }
        return groups;
    }

    /// <summary>Zona a la que da una puerta (la sala numerada que toca).</summary>
    public static int ZoneOfDoor(LevelLayout map, List<Vector2Int> group)
    {
        foreach (var p in group)
            foreach (var d in new[] { Vector2Int.left, Vector2Int.right, Vector2Int.up, Vector2Int.down })
            {
                int zone = map.ZoneAt(p.x + d.x, p.y + d.y);
                if (zone > 0) return zone;
            }
        return 0;
    }

    /// <summary>
    /// Luces guía en el suelo del hub que salen de cada puerta hacia el pasillo, en la capa de neón
    /// (aditiva) y teñidas con el color de la sala a la que llevan.
    /// </summary>
    static void PaintRunways(LevelLayout map, Layers layers, ArtLibrary art)
    {
        foreach (var group in DoorGroups(map))
        {
            var color = ZoneColor(ZoneOfDoor(map, group));
            var center = group[group.Count / 2];
            bool horizontal = IsHorizontalDoor(map, center.x, center.y);
            var away = horizontal
                ? (map.KindAt(center.x, center.y + 1) == CellKind.Hub ? Vector2Int.up : Vector2Int.down)
                : (map.KindAt(center.x + 1, center.y) == CellKind.Hub ? Vector2Int.right : Vector2Int.left);
            for (int step = 1; step <= 3; step++)
            {
                var p = center + away * step;
                if (map.KindAt(p.x, p.y) != CellKind.Hub) break;
                var cell = map.ToCell(p.x, p.y);
                layers.Neon.SetTile(cell, horizontal ? art.RunwayV : art.RunwayH);
                layers.Neon.SetTileFlags(cell, TileFlags.None);
                layers.Neon.SetColor(cell, color);
            }
        }
    }

    public static void PaintWalls(LevelLayout map, Layers layers, ArtLibrary art)
    {
        for (int y = 0; y < map.Height; y++)
            for (int x = 0; x < map.Width; x++)
            {
                string key = WallAutotile.KeyAt(map, x, y);
                if (key == null) continue;
                var cell = map.ToCell(x, y);
                layers.Walls.SetTile(cell, art.Wall(key));
                if (WallAutotile.PieceAt(map, x, y) == WallPiece.Face) PaintTrim(map, layers, art, x, y, cell);
                string upper = WallAutotile.UpperKeyAt(map, x, y);
                if (upper != null) layers.Walls.SetTile(map.ToCell(x, y - 1), art.Wall(upper));
            }

    }

    static void PaintTrim(LevelLayout map, Layers layers, ArtLibrary art, int x, int y, Vector3Int cell)
    {
        bool westOpen = WallAutotile.SideOf(map, x - 1, y) == FaceSide.Open;
        bool eastOpen = WallAutotile.SideOf(map, x + 1, y) == FaceSide.Open;
        var tile = westOpen && eastOpen ? art.TrimJambWE : westOpen ? art.TrimJambW : eastOpen ? art.TrimJambE : art.Trim;
        layers.Neon.SetTile(cell, tile);
        layers.Neon.SetTileFlags(cell, TileFlags.None);
        int zone = map.ZoneAt(x, y + 1);
        layers.Neon.SetColor(cell, ZoneColor(zone));
    }

    public static void PaintShadows(LevelLayout map, Layers layers, ArtLibrary art)
    {
        for (int y = 0; y < map.Height; y++)
            for (int x = 0; x < map.Width; x++)
            {
                if (!map.IsWalkable(x, y)) continue;
                bool north = map.KindAt(x, y - 1) == CellKind.Wall;
                bool west = map.KindAt(x - 1, y) == CellKind.Wall;
                if (!north && !west) continue;
                layers.Shadows.SetTile(map.ToCell(x, y), north && west ? art.ShadowNW : north ? art.ShadowN : art.ShadowW);
            }
    }
}
