using System.Collections.Generic;
using UnityEngine;

/// <summary>Convierte cualquier Collider2D en contornos (polígonos) en coordenadas de mundo.</summary>
public static class ColliderShapes
{
    const int CircleSegments = 20;

    public static List<Vector2[]> Outlines(Collider2D collider)
    {
        var result = new List<Vector2[]>();
        var t = collider.transform;
        switch (collider)
        {
            case CompositeCollider2D composite:
                for (int i = 0; i < composite.pathCount; i++)
                {
                    var points = new Vector2[composite.GetPathPointCount(i)];
                    composite.GetPath(i, points);
                    result.Add(ToWorld(t, points));
                }
                break;
            case PolygonCollider2D polygon:
                for (int i = 0; i < polygon.pathCount; i++)
                {
                    var points = polygon.GetPath(i);
                    for (int p = 0; p < points.Length; p++) points[p] += polygon.offset;
                    result.Add(ToWorld(t, points));
                }
                break;
            case BoxCollider2D box:
                result.Add(ToWorld(t, BoxCorners(box.offset, box.size + Vector2.one * (2f * box.edgeRadius))));
                break;
            case CircleCollider2D circle:
                result.Add(ToWorld(t, CirclePoints(circle.offset, circle.radius)));
                break;
            case CapsuleCollider2D capsule:
                result.Add(ToWorld(t, BoxCorners(capsule.offset, capsule.size)));
                break;
            case EdgeCollider2D edge:
                var edgePoints = edge.points;
                for (int p = 0; p < edgePoints.Length; p++) edgePoints[p] += edge.offset;
                result.Add(ToWorld(t, edgePoints));
                break;
        }
        return result;
    }

    static Vector2[] BoxCorners(Vector2 center, Vector2 size)
    {
        var h = size * 0.5f;
        return new[]
        {
            center + new Vector2(-h.x, -h.y), center + new Vector2(h.x, -h.y),
            center + new Vector2(h.x, h.y), center + new Vector2(-h.x, h.y)
        };
    }

    static Vector2[] CirclePoints(Vector2 center, float radius)
    {
        var points = new Vector2[CircleSegments];
        for (int i = 0; i < CircleSegments; i++)
        {
            float a = i * Mathf.PI * 2f / CircleSegments;
            points[i] = center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius;
        }
        return points;
    }

    static Vector2[] ToWorld(Transform t, Vector2[] local)
    {
        var world = new Vector2[local.Length];
        for (int i = 0; i < local.Length; i++) world[i] = t.TransformPoint(local[i]);
        return world;
    }
}
