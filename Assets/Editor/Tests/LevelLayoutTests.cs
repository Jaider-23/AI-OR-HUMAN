using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public class LevelLayoutTests
{
    // Mapa mínimo válido: hub central, una sala por nivel en dos filas, puertas de 3.
    // (Las pruebas de reglas parten de él y lo rompen a propósito.)
    static readonly string[] ValidRows =
    {
        "##########################################",
        "#1111111#2222222#3333333#4444444##########",
        "#1111111#2222222#3333333#4444444##########",
        "#1111111#2222222#3333333#4444444##########",
        "#1111111#2222222#3333333#4444444##########",
        "#1111111#2222222#3333333#4444444##########",
        "##DDD######DDD######DDD#####DDD###########",
        "#........................................#",
        "#........................................#",
        "#........................................#",
        "#........................................#",
        "######DDD#########DDD##########DDD########",
        "#55555555555#666666666666#777777777777777#",
        "#55555555555#666666666666#777777777777777#",
        "#55555555555#666666666666#777777777777777#",
        "#55555555555#666666666666#777777777777777#",
        "#55555555555#666666666666#777777777777777#",
        "##########################################",
    };

    static LevelLayoutData Data(string[] rows = null)
    {
        return new LevelLayoutData
        {
            rows = rows ?? ValidRows,
            spawn = new LayoutPoint { x = 2, y = 8 },
            ada = new LayoutPoint { x = 5, y = 7 },
            terminals = new[]
            {
                T(1, 2, 1), T(1, 5, 1), T(2, 10, 1), T(2, 13, 1), T(3, 18, 1), T(3, 21, 1),
                T(4, 26, 1), T(4, 29, 1), T(5, 2, 12), T(6, 14, 12), T(7, 28, 12)
            },
            fragments = new[]
            {
                P(10, 9), P(20, 9), P(30, 9), P(3, 3), P(12, 3), P(20, 3), P(4, 15), P(30, 15)
            },
            decor = Array.Empty<LayoutDecor>()
        };
    }

    static LayoutTerminal T(int zone, int x, int y) => new LayoutTerminal { zone = zone, x = x, y = y };
    static LayoutPoint P(int x, int y) => new LayoutPoint { x = x, y = y };

    static string[] Replace(string[] rows, int x, int y, char c)
    {
        var copy = rows.ToArray();
        var chars = copy[y].ToCharArray();
        chars[x] = c;
        copy[y] = new string(chars);
        return copy;
    }

    [Test]
    public void ParseRejectsRaggedRows()
    {
        var rows = ValidRows.ToArray();
        rows[3] = rows[3] + "#";

        var error = Assert.Throws<ArgumentException>(() => LevelLayout.Parse(Data(rows)));

        StringAssert.Contains("3", error.Message);
    }

    [Test]
    public void KindAtClassifiesEveryCharacter()
    {
        var layout = LevelLayout.Parse(Data());

        Assert.AreEqual(CellKind.Wall, layout.KindAt(0, 0));
        Assert.AreEqual(CellKind.Zone, layout.KindAt(1, 1));
        Assert.AreEqual(1, layout.ZoneAt(1, 1));
        Assert.AreEqual(CellKind.Door, layout.KindAt(2, 6));
        Assert.AreEqual(CellKind.Hub, layout.KindAt(1, 7));
        Assert.AreEqual(CellKind.Void, layout.KindAt(-1, 4));
        Assert.IsTrue(layout.IsWalkable(2, 6));
        Assert.IsFalse(layout.IsWalkable(0, 0));
    }

    [Test]
    public void ToCellFlipsRowsAndCentersTheMap()
    {
        var layout = LevelLayout.Parse(Data());

        // Fila 0 (norte) queda arriba (y mayor) y el mapa se centra en el origen.
        Assert.AreEqual(new Vector3Int(-21, 8, 0), layout.ToCell(0, 0));
        Assert.AreEqual(new Vector3Int(20, -9, 0), layout.ToCell(41, 17));
        Assert.AreEqual(new Vector2(-20.5f, 8.5f), layout.ToWorldCenter(0, 0));
    }

    [Test]
    public void ValidLayoutHasNoErrors()
    {
        var errors = LevelLayoutValidator.Validate(LevelLayout.Parse(Data()));

        CollectionAssert.IsEmpty(errors, string.Join("\n", errors));
    }

    [Test]
    public void WalkableCellOnBorderIsRejected()
    {
        var rows = Replace(ValidRows, 0, 8, '.');

        var errors = LevelLayoutValidator.Validate(LevelLayout.Parse(Data(rows)));

        Assert.IsTrue(errors.Any(e => e.Contains("borde")), string.Join("\n", errors));
    }

    [Test]
    public void ZoneTouchingHubWithoutDoorIsRejected()
    {
        var rows = Replace(ValidRows, 4, 6, '1');

        var errors = LevelLayoutValidator.Validate(LevelLayout.Parse(Data(rows)));

        Assert.IsTrue(errors.Any(e => e.Contains("sin puerta")), string.Join("\n", errors));
    }

    [Test]
    public void DoorNarrowerThanThreeIsRejected()
    {
        var rows = Replace(ValidRows, 4, 6, '#');

        var errors = LevelLayoutValidator.Validate(LevelLayout.Parse(Data(rows)));

        Assert.IsTrue(errors.Any(e => e.Contains("puerta") && e.Contains("3")), string.Join("\n", errors));
    }

    [Test]
    public void TerminalMustStandAgainstNorthWall()
    {
        var data = Data();
        data.terminals[0] = T(1, 2, 3);

        var errors = LevelLayoutValidator.Validate(LevelLayout.Parse(data));

        Assert.IsTrue(errors.Any(e => e.Contains("pared norte")), string.Join("\n", errors));
    }

    [Test]
    public void BlockedDoorByDecorIsRejected()
    {
        var data = Data();
        data.decor = new[] { new LayoutDecor { kind = "desk", x = 2, y = 7 }, new LayoutDecor { kind = "desk", x = 3, y = 7 },
                             new LayoutDecor { kind = "desk", x = 4, y = 7 }, new LayoutDecor { kind = "desk", x = 1, y = 7 },
                             new LayoutDecor { kind = "desk", x = 5, y = 8 } };

        var errors = LevelLayoutValidator.Validate(LevelLayout.Parse(data));

        Assert.IsTrue(errors.Any(e => e.Contains("inalcanzable")), string.Join("\n", errors));
    }

    [Test]
    public void WallWithFloorBelowIsAFace()
    {
        var layout = LevelLayout.Parse(Data());

        Assert.AreEqual(WallPiece.Face, WallAutotile.PieceAt(layout, 1, 0));
        Assert.AreEqual(WallPiece.Cap, WallAutotile.PieceAt(layout, 0, 3));
    }

    [Test]
    public void CapMaskMarksWallNeighbours()
    {
        var layout = LevelLayout.Parse(Data());

        // (8,3): pared divisoria con pared al norte y al sur, sala a este y oeste.
        Assert.AreEqual(WallAutotile.North | WallAutotile.South, WallAutotile.CapMask(layout, 8, 3));
    }

    [Test]
    public void FaceEndsWhereTheFaceRunStops()
    {
        var layout = LevelLayout.Parse(Data());

        // Fila 6: "##DDD###..." -> la cara en x=5 empieza junto a la puerta (oeste) y continúa al este.
        var (westEnd, eastEnd) = WallAutotile.FaceEnds(layout, 5, 6);

        Assert.IsTrue(westEnd);
        Assert.IsFalse(eastEnd);
    }

    [Test]
    public void OuterWallWithVoidAboveIsDrawnTwoTilesTall()
    {
        var layout = LevelLayout.Parse(Data());

        // (1,0): pared exterior norte con la sala 1 al sur y el exterior encima.
        StringAssert.StartsWith("facelow_", WallAutotile.KeyAt(layout, 1, 0));
        StringAssert.StartsWith("faceup_", WallAutotile.UpperKeyAt(layout, 1, 0));
        // (5,6): pared interior (sala encima) -> cara compacta de una casilla, sin parte superior.
        StringAssert.StartsWith("face_", WallAutotile.KeyAt(layout, 5, 6));
        Assert.IsNull(WallAutotile.UpperKeyAt(layout, 5, 6));
    }

    [Test]
    public void DoorGroupsFindsEveryDoorRun()
    {
        var layout = LevelLayout.Parse(Data());

        var groups = LabTilemaps.DoorGroups(layout);

        Assert.AreEqual(7, groups.Count);
        Assert.IsTrue(groups.All(g => g.Count == 3));
        Assert.IsTrue(LabTilemaps.IsHorizontalDoor(layout, 2, 6));
        Assert.AreEqual(1, LabTilemaps.ZoneOfDoor(layout, groups.First(g => g.Contains(new Vector2Int(2, 6)))));
    }

    [Test]
    public void OccupancyKeepsDoorApproachesFree()
    {
        var occupancy = new Occupancy(LevelLayout.Parse(Data()));

        Assert.IsFalse(occupancy.TryOccupy(new[] { new Vector2Int(3, 7) }), "justo delante de la puerta de la sala 1");
    }

    [Test]
    public void OccupancyRejectsFurnitureThatCutsTheHub()
    {
        var occupancy = new Occupancy(LevelLayout.Parse(Data()));
        var barrier = new[] { new Vector2Int(15, 7), new Vector2Int(15, 8), new Vector2Int(15, 9), new Vector2Int(15, 10) };

        Assert.IsFalse(occupancy.TryOccupy(barrier), "una fila de muebles que corta el hub deja salas inalcanzables");
        Assert.IsTrue(occupancy.TryOccupy(new[] { new Vector2Int(15, 8) }), "tras rechazarla, el espacio queda libre");
    }

    [Test]
    public void OccupancyAllowsHarmlessFurniture()
    {
        var occupancy = new Occupancy(LevelLayout.Parse(Data()));

        Assert.IsTrue(occupancy.TryOccupy(new[] { new Vector2Int(40, 9) }));
        Assert.IsFalse(occupancy.TryOccupy(new[] { new Vector2Int(40, 9) }), "una casilla no se puede ocupar dos veces");
    }

    [Test]
    public void FaceDecorIsDeterministic()
    {
        Assert.AreEqual(WallAutotile.FaceDecor(7, 3), WallAutotile.FaceDecor(7, 3));
        Assert.That(Enumerable.Range(0, 200).Select(i => WallAutotile.FaceDecor(i, i * 3)).Distinct().Count(), Is.GreaterThan(1));
    }

    [Test]
    public void GreedyRectanglesCoverEveryWallCellExactlyOnce()
    {
        var layout = LevelLayout.Parse(Data());
        var rects = WallColliderBuilder.MergeWallCells(layout);

        var covered = new System.Collections.Generic.HashSet<Vector2Int>();
        foreach (var r in rects)
            for (int y = r.yMin; y < r.yMax; y++)
                for (int x = r.xMin; x < r.xMax; x++)
                {
                    Assert.AreEqual(CellKind.Wall, layout.KindAt(x, y), $"rect {r} covers non-wall ({x},{y})");
                    Assert.IsTrue(covered.Add(new Vector2Int(x, y)), $"cell ({x},{y}) covered twice");
                }

        int walls = 0;
        for (int y = 0; y < layout.Height; y++)
            for (int x = 0; x < layout.Width; x++)
                if (layout.KindAt(x, y) == CellKind.Wall) walls++;
        Assert.AreEqual(walls, covered.Count);
        Assert.Less(rects.Count, walls / 3, "cells should be merged into long strips");
    }
}
