public enum WallPiece
{
    None,
    Cap,
    Face
}

/// <summary>Estado del vecino lateral de una cara de pared (para dibujar jambas y bordes).</summary>
public enum FaceSide
{
    Continues,
    Corner,
    Open
}

/// <summary>
/// Reglas de autotile de paredes en perspectiva 3/4: una pared con suelo al sur muestra su CARA
/// (frente vertical) y una franja de remate arriba; el resto se ve como REMATE (tapa) visto desde arriba.
/// Las claves de variante se usan como nombre de archivo de cada tile generado.
/// </summary>
public static class WallAutotile
{
    public const int North = 1;
    public const int East = 2;
    public const int South = 4;
    public const int West = 8;

    public static bool IsWall(LevelLayout map, int x, int y) => map.KindAt(x, y) == CellKind.Wall;

    public static WallPiece PieceAt(LevelLayout map, int x, int y)
    {
        if (!IsWall(map, x, y)) return WallPiece.None;
        return map.IsWalkable(x, y + 1) ? WallPiece.Face : WallPiece.Cap;
    }

    /// <summary>Bits de los vecinos que también son pared (N, E, S, O).</summary>
    public static int CapMask(LevelLayout map, int x, int y)
    {
        int mask = 0;
        if (IsWall(map, x, y - 1)) mask |= North;
        if (IsWall(map, x + 1, y)) mask |= East;
        if (IsWall(map, x, y + 1)) mask |= South;
        if (IsWall(map, x - 1, y)) mask |= West;
        return mask;
    }

    /// <summary>Esquinas cóncavas: dos lados ortogonales son pared pero la diagonal no (NE, SE, SO, NO).</summary>
    public static int InnerCorners(LevelLayout map, int x, int y)
    {
        int mask = CapMask(map, x, y), corners = 0;
        if ((mask & (North | East)) == (North | East) && !IsWall(map, x + 1, y - 1)) corners |= 1;
        if ((mask & (South | East)) == (South | East) && !IsWall(map, x + 1, y + 1)) corners |= 2;
        if ((mask & (South | West)) == (South | West) && !IsWall(map, x - 1, y + 1)) corners |= 4;
        if ((mask & (North | West)) == (North | West) && !IsWall(map, x - 1, y - 1)) corners |= 8;
        return corners;
    }

    /// <summary>Si la cara termina hacia el oeste / este (el vecino no continúa la cara).</summary>
    public static (bool westEnd, bool eastEnd) FaceEnds(LevelLayout map, int x, int y) =>
        (PieceAt(map, x - 1, y) != WallPiece.Face, PieceAt(map, x + 1, y) != WallPiece.Face);

    public static FaceSide SideOf(LevelLayout map, int x, int y)
    {
        var piece = PieceAt(map, x, y);
        if (piece == WallPiece.Face) return FaceSide.Continues;
        return piece == WallPiece.Cap ? FaceSide.Corner : FaceSide.Open;
    }

    /// <summary>Decoración pseudoaleatoria pero estable de la cara: 0 lisa, 1 junta, 2 rejilla, 3 luz.</summary>
    public static int FaceDecor(int x, int y)
    {
        unchecked
        {
            int h = x * 73856093 ^ y * 19349663;
            h ^= h >> 13;
            h *= 0x5bd1e995;
            h ^= h >> 15;
            int v = (h & 0x7fffffff) % 100;
            return v < 58 ? 0 : v < 82 ? 1 : v < 91 ? 2 : 3;
        }
    }

    /// <summary>Cara exterior con el vacío encima: se dibuja con dos casillas de alto.</summary>
    public static bool IsTallFace(LevelLayout map, int x, int y) =>
        PieceAt(map, x, y) == WallPiece.Face && map.KindAt(x, y - 1) == CellKind.Void;

    static FaceSide TallSideOf(LevelLayout map, int x, int y)
    {
        if (IsTallFace(map, x, y)) return FaceSide.Continues;
        return IsWall(map, x, y) ? FaceSide.Corner : FaceSide.Open;
    }

    /// <summary>Clave del tile de la parte alta (en la casilla vacía de encima) o null si la pared no es alta.</summary>
    public static string UpperKeyAt(LevelLayout map, int x, int y)
    {
        if (!IsTallFace(map, x, y)) return null;
        return $"faceup_w{SideChar(TallSideOf(map, x - 1, y))}_e{SideChar(TallSideOf(map, x + 1, y))}_d{FaceDecor(x, y)}";
    }

    public static string KeyAt(LevelLayout map, int x, int y)
    {
        var piece = PieceAt(map, x, y);
        if (piece == WallPiece.Cap)
        {
            return $"cap_{CapMask(map, x, y):D2}_{InnerCorners(map, x, y):D2}";
        }
        if (piece != WallPiece.Face) return null;
        if (IsTallFace(map, x, y))
        {
            return $"facelow_w{SideChar(TallSideOf(map, x - 1, y))}_e{SideChar(TallSideOf(map, x + 1, y))}_d{FaceDecor(x, y)}";
        }
        int north = IsWall(map, x, y - 1) ? 0 : 1;
        char w = SideChar(SideOf(map, x - 1, y));
        char e = SideChar(SideOf(map, x + 1, y));
        return $"face_n{north}_w{w}_e{e}_d{FaceDecor(x, y)}";
    }

    public static char SideChar(FaceSide side) => side == FaceSide.Continues ? 'F' : side == FaceSide.Corner ? 'K' : 'O';

    public static FaceSide ParseSide(char c) => c == 'F' ? FaceSide.Continues : c == 'K' ? FaceSide.Corner : FaceSide.Open;
}
