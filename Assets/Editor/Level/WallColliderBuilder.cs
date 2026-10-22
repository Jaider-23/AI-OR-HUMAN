using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Colisión de las paredes generada directamente desde el mapa: las casillas de pared se agrupan en
/// rectángulos (BoxCollider2D) y un CompositeCollider2D los fusiona en contornos continuos.
/// Se evita así el TilemapCollider2D, cuya geometría quedaba desactualizada al guardar la escena
/// (causa de las "paredes invisibles"), y los contornos continuos dejan deslizar al jugador sin engancharse.
/// </summary>
public static class WallColliderBuilder
{
    public const string ObjectName = "WallColliders";

    /// <summary>Agrupa las paredes en rectángulos (coordenadas de mapa: x columna, y fila desde el norte).</summary>
    public static List<RectInt> MergeWallCells(LevelLayout map)
    {
        var used = new bool[map.Width, map.Height];
        var rects = new List<RectInt>();
        for (int y = 0; y < map.Height; y++)
        {
            for (int x = 0; x < map.Width; x++)
            {
                if (used[x, y] || map.KindAt(x, y) != CellKind.Wall) continue;
                int width = 1;
                while (x + width < map.Width && !used[x + width, y] && map.KindAt(x + width, y) == CellKind.Wall) width++;
                int height = 1;
                while (y + height < map.Height && RowFree(map, used, x, y + height, width)) height++;
                for (int dy = 0; dy < height; dy++)
                    for (int dx = 0; dx < width; dx++)
                        used[x + dx, y + dy] = true;
                rects.Add(new RectInt(x, y, width, height));
            }
        }
        return rects;
    }

    static bool RowFree(LevelLayout map, bool[,] used, int x, int y, int width)
    {
        for (int dx = 0; dx < width; dx++)
        {
            if (used[x + dx, y] || map.KindAt(x + dx, y) != CellKind.Wall) return false;
        }
        return true;
    }

    public static CompositeCollider2D Build(LevelLayout map, Transform parent)
    {
        var root = new GameObject(ObjectName);
        root.transform.SetParent(parent, false);
        var body = root.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Static;
        var composite = root.AddComponent<CompositeCollider2D>();
        composite.geometryType = CompositeCollider2D.GeometryType.Outlines;
        composite.generationType = CompositeCollider2D.GenerationType.Synchronous;

        foreach (var r in MergeWallCells(map))
        {
            var bottomLeft = map.ToCell(r.x, r.y + r.height - 1);
            var box = root.AddComponent<BoxCollider2D>();
            box.size = new Vector2(r.width, r.height);
            box.offset = new Vector2(bottomLeft.x + r.width * 0.5f, bottomLeft.y + r.height * 0.5f);
            box.usedByComposite = true;
        }
        composite.GenerateGeometry();
        return composite;
    }
}
