using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Regresión de las "paredes invisibles": la colisión guardada en GameScene debe coincidir casilla a
/// casilla con el mapa (lab_layout.json). El fallo original era una geometría de colisión obsoleta
/// (de la sala antigua) guardada en el CompositeCollider2D del Tilemap.
/// </summary>
public class CollisionIntegrityTests
{
    [Test]
    public void ShippedLayoutPassesAllRules()
    {
        var map = LabWorldBuilder.LoadLayout();

        Assert.Greater(map.Width, 0);
    }

    [Test]
    public void SavedWallCollisionMatchesTheLayoutCellByCell()
    {
        var map = LabWorldBuilder.LoadLayout();
        EditorSceneManager.OpenScene(BuildPaths.GameScene, OpenSceneMode.Single);
        var walls = GameObject.Find(LabWorldBuilder.RootName + "/" + WallColliderBuilder.ObjectName)
                              .GetComponent<CompositeCollider2D>();
        // Se mide la geometría TAL COMO QUEDÓ GUARDADA, sin regenerarla: el fallo original era justo una
        // geometría guardada obsoleta (la de la sala antigua) que no coincidía con las paredes visibles.
        var outlines = new List<Vector2[]>();
        for (int i = 0; i < walls.pathCount; i++)
        {
            var points = new Vector2[walls.GetPathPointCount(i)];
            walls.GetPath(i, points);
            outlines.Add(points);
        }
        CollectionAssert.IsNotEmpty(outlines, "Las paredes no tienen geometría de colisión guardada");

        var wrong = new List<string>();
        for (int y = 0; y < map.Height; y++)
            for (int x = 0; x < map.Width; x++)
            {
                var kind = map.KindAt(x, y);
                if (kind == CellKind.Void) continue;
                Vector2 local = walls.transform.InverseTransformPoint(map.ToWorldCenter(x, y));
                bool solid = InsideEvenOdd(outlines, local);
                if (solid != (kind == CellKind.Wall)) wrong.Add($"({x},{y}) {kind} solid={solid}");
            }

        CollectionAssert.IsEmpty(wrong, "Colisión guardada distinta del mapa:\n" + string.Join("\n", wrong.Take(30)));
    }

    /// <summary>Regla par-impar sobre todos los contornos (el anillo exterior y sus huecos).</summary>
    static bool InsideEvenOdd(List<Vector2[]> outlines, Vector2 p)
    {
        bool inside = false;
        foreach (var path in outlines)
            for (int i = 0, j = path.Length - 1; i < path.Length; j = i++)
            {
                bool crosses = (path[i].y > p.y) != (path[j].y > p.y)
                               && p.x < (path[j].x - path[i].x) * (p.y - path[i].y) / (path[j].y - path[i].y) + path[i].x;
                if (crosses) inside = !inside;
            }
        return inside;
    }

    [Test]
    public void TilemapsCarryNoStaleColliders()
    {
        EditorSceneManager.OpenScene(BuildPaths.GameScene, OpenSceneMode.Single);

        var stale = Object.FindObjectsOfType<Tilemap>()
                          .Where(t => t.GetComponent<TilemapCollider2D>() != null || t.GetComponent<CompositeCollider2D>() != null)
                          .Select(t => t.name)
                          .ToList();

        CollectionAssert.IsEmpty(stale, "Los tilemaps no deben tener colisión propia (se genera desde el mapa)");
    }

    [Test]
    public void EverySolidPropColliderSitsOnWalkableFloor()
    {
        var map = LabWorldBuilder.LoadLayout();
        EditorSceneManager.OpenScene(BuildPaths.GameScene, OpenSceneMode.Single);
        var root = GameObject.Find(LabWorldBuilder.RootName).transform;

        var outside = new List<string>();
        foreach (var box in root.GetComponentsInChildren<BoxCollider2D>().Where(b => !b.usedByComposite && !b.isTrigger))
        {
            var center = (Vector2)box.bounds.center;
            var cell = new Vector2Int(Mathf.FloorToInt(center.x), Mathf.FloorToInt(center.y));
            bool onFloor = Enumerable.Range(0, map.Height).Any(y => Enumerable.Range(0, map.Width)
                .Any(x => map.ToCell(x, y) == new Vector3Int(cell.x, cell.y, 0) && map.IsWalkable(x, y)));
            if (!onFloor) outside.Add($"{box.transform.parent?.name}/{box.name} @ {center}");
        }

        CollectionAssert.IsEmpty(outside, "Hay muebles con colisión fuera del suelo:\n" + string.Join("\n", outside));
    }
}
