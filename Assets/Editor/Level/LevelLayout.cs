using System;
using UnityEngine;

public enum CellKind
{
    Void,
    Wall,
    Hub,
    Zone,
    Door
}

[Serializable]
public class LayoutPoint
{
    public int x;
    public int y;
}

[Serializable]
public class LayoutTerminal
{
    public int zone;
    public int x;
    public int y;
}

[Serializable]
public class LayoutDecor
{
    public string kind;
    public int x;
    public int y;
    public string note;
}

/// <summary>Formato del archivo Assets/Level/lab_layout.json (editable por el equipo).</summary>
[Serializable]
public class LevelLayoutData
{
    public string concept;
    public string[] rows;
    public LayoutPoint spawn;
    public LayoutPoint ada;
    public LayoutTerminal[] terminals;
    public LayoutPoint[] fragments;
    public LayoutDecor[] decor;
}

/// <summary>
/// Mapa del laboratorio en ASCII. Coordenadas de mapa: x = columna (0 = oeste), y = fila (0 = norte).
/// '#' pared, '.' hub, '1'-'7' sala del nivel N, 'D' puerta, ' ' exterior.
/// <see cref="ToCell"/> convierte a celdas de Tilemap (y hacia arriba) con el mapa centrado en el origen.
/// </summary>
public sealed class LevelLayout
{
    public readonly int Width;
    public readonly int Height;
    readonly string[] rows;

    public LevelLayoutData Data { get; }

    LevelLayout(LevelLayoutData data)
    {
        Data = data;
        rows = data.rows;
        Height = rows.Length;
        Width = rows[0].Length;
    }

    public static LevelLayout Parse(LevelLayoutData data)
    {
        if (data == null || data.rows == null || data.rows.Length == 0)
        {
            throw new ArgumentException("El mapa no tiene filas.");
        }
        int width = data.rows[0].Length;
        for (int i = 0; i < data.rows.Length; i++)
        {
            if (data.rows[i] == null || data.rows[i].Length != width)
            {
                throw new ArgumentException($"La fila {i} mide {data.rows[i]?.Length ?? 0} y debería medir {width}.");
            }
        }
        return new LevelLayout(data);
    }

    public static LevelLayout FromJson(string json) => Parse(JsonUtility.FromJson<LevelLayoutData>(json));

    public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;

    public char CharAt(int x, int y) => InBounds(x, y) ? rows[y][x] : ' ';

    public CellKind KindAt(int x, int y)
    {
        char c = CharAt(x, y);
        if (c == '#') return CellKind.Wall;
        if (c == '.') return CellKind.Hub;
        if (c == 'D') return CellKind.Door;
        if (c >= '1' && c <= '7') return CellKind.Zone;
        return CellKind.Void;
    }

    public int ZoneAt(int x, int y)
    {
        char c = CharAt(x, y);
        return c >= '1' && c <= '7' ? c - '0' : 0;
    }

    public bool IsWalkable(int x, int y)
    {
        var kind = KindAt(x, y);
        return kind == CellKind.Hub || kind == CellKind.Zone || kind == CellKind.Door;
    }

    public Vector3Int ToCell(int x, int y) => new Vector3Int(x - Width / 2, (Height - 1 - y) - Height / 2, 0);

    public Vector2 ToWorldCenter(int x, int y)
    {
        var cell = ToCell(x, y);
        return new Vector2(cell.x + 0.5f, cell.y + 0.5f);
    }

    /// <summary>Punto medio del borde inferior de la celda (donde se apoya un objeto con pivote abajo).</summary>
    public Vector2 ToWorldBase(int x, int y) => ToWorldCenter(x, y) + Vector2.down * 0.5f;

    /// <summary>Límites del mapa completo en coordenadas de mundo.</summary>
    public Rect WorldRect
    {
        get
        {
            var min = ToCell(0, Height - 1);
            return new Rect(min.x, min.y, Width, Height);
        }
    }
}
