using UnityEngine;

/// <summary>
/// Dibujo procedural de las paredes en perspectiva 3/4 (luz desde el noroeste):
/// remate (tapa) con borde iluminado al norte/oeste y sombreado al sur/este, y cara frontal con
/// junta, rejilla o luz. También sombras al pie de las paredes, placas de puerta y la línea de neón.
/// </summary>
public static class WallArt
{
    public const int Size = 16;
    public const int CapStripStart = 11;
    public const int TrimRow = 2;

    static readonly Color32 CapFill = PixelSprite.Hex("344354");
    static readonly Color32 CapDot = PixelSprite.Hex("3D4F63");
    static readonly Color32 CapRim = PixelSprite.Hex("6E8EAA");
    static readonly Color32 CapShade = PixelSprite.Hex("28333F");
    static readonly Color32 CapLip = PixelSprite.Hex("43566D");
    static readonly Color32 Outline = PixelSprite.Hex("11161D");
    static readonly Color32 FaceShadow = PixelSprite.Hex("0E1319");
    static readonly Color32 FaceTop = PixelSprite.Hex("33455A");
    static readonly Color32 FaceMid = PixelSprite.Hex("2A394B");
    static readonly Color32 FaceLow = PixelSprite.Hex("233041");
    static readonly Color32 Seam = PixelSprite.Hex("18202A");
    static readonly Color32 Groove = PixelSprite.Hex("151C25");
    static readonly Color32 Base = PixelSprite.Hex("121820");
    static readonly Color32 BaseDark = PixelSprite.Hex("0A0E13");
    static readonly Color32 JambLight = PixelSprite.Hex("5B7792");
    static readonly Color32 LightCore = PixelSprite.Hex("8FF8FF");
    static readonly Color32 LightRim = PixelSprite.Hex("2AA6B8");

    public static PixelSprite FromKey(string key)
    {
        var art = new PixelSprite(Size, Size);
        if (key.StartsWith("cap_"))
        {
            var parts = key.Split('_');
            DrawCap(art, int.Parse(parts[1]), int.Parse(parts[2]), 0);
        }
        else if (key.StartsWith("faceup_") || key.StartsWith("facelow_"))
        {
            // face(up|low)_w{F|K|O}_e{F|K|O}_d{0-3}
            var parts = key.Split('_');
            var west = WallAutotile.ParseSide(parts[1][1]);
            var east = WallAutotile.ParseSide(parts[2][1]);
            int decor = parts[3][1] - '0';
            if (key.StartsWith("faceup_")) DrawTallUpper(art, west, east, decor);
            else DrawTallLower(art, west, east, decor);
        }
        else
        {
            // face_n{0|1}_w{F|K|O}_e{F|K|O}_d{0-3}
            var parts = key.Split('_');
            DrawFace(art, parts[1][1] == '1', WallAutotile.ParseSide(parts[2][1]),
                     WallAutotile.ParseSide(parts[3][1]), parts[4][1] - '0');
        }
        return art;
    }

    /// <summary>Remate visto desde arriba. mask = vecinos que son pared; corners = esquinas cóncavas.</summary>
    public static void DrawCap(PixelSprite art, int mask, int corners, int yMin)
    {
        const int top = Size - 1;
        for (int y = yMin; y <= top; y++)
            for (int x = 0; x < Size; x++)
                art.Set(x, y, (x % 4 == 1 && y % 4 == 2) ? CapDot : CapFill);

        bool n = (mask & WallAutotile.North) == 0, e = (mask & WallAutotile.East) == 0;
        bool s = (mask & WallAutotile.South) == 0, w = (mask & WallAutotile.West) == 0;
        if (n) { art.Fill(0, top - 1, top, top - 1, CapRim); art.Fill(0, top, top, top, Outline); }
        if (w) { art.Fill(1, yMin, 1, top - (n ? 1 : 0), CapRim); art.Fill(0, yMin, 0, top, Outline); }
        if (e) { art.Fill(top - 1, yMin, top - 1, top - (n ? 2 : 0), CapShade); art.Fill(top, yMin, top, top, Outline); }
        if (s && yMin == 0) { art.Fill(0, 1, top, 1, CapShade); art.Fill(0, 0, top, 0, Outline); }
        if ((corners & 1) != 0) art.Set(top, top, Outline);
        if ((corners & 2) != 0) art.Set(top, yMin, Outline);
        if ((corners & 4) != 0) art.Set(0, yMin, Outline);
        if ((corners & 8) != 0) art.Set(0, top, Outline);
    }

    /// <summary>Cara frontal (filas 0-10) con franja de remate encima (filas 11-15).</summary>
    public static void DrawFace(PixelSprite art, bool northOpen, FaceSide west, FaceSide east, int decor)
    {
        int mask = WallAutotile.South;
        if (!northOpen) mask |= WallAutotile.North;
        if (west != FaceSide.Open) mask |= WallAutotile.West;
        if (east != FaceSide.Open) mask |= WallAutotile.East;
        DrawCap(art, mask, 0, CapStripStart);
        art.Fill(0, CapStripStart, Size - 1, CapStripStart, CapLip);
        if (west == FaceSide.Open) art.Set(0, CapStripStart, Outline);
        if (east == FaceSide.Open) art.Set(Size - 1, CapStripStart, Outline);

        art.Fill(0, 10, Size - 1, 10, FaceShadow);
        art.Fill(0, 9, Size - 1, 9, FaceTop);
        art.Fill(0, 6, Size - 1, 8, FaceMid);
        art.Fill(0, 3, Size - 1, 5, FaceLow);
        art.Fill(0, TrimRow, Size - 1, TrimRow, Groove);
        art.Fill(0, 1, Size - 1, 1, Base);
        art.Fill(0, 0, Size - 1, 0, BaseDark);
        DrawDecor(art, decor);

        DrawJambs(art, west, east, 0, 10);
    }

    /// <summary>Parte alta de una pared exterior de dos casillas: remate arriba y cara con su decoración.</summary>
    public static void DrawTallUpper(PixelSprite art, FaceSide west, FaceSide east, int decor)
    {
        int mask = WallAutotile.South;
        if (west == FaceSide.Continues) mask |= WallAutotile.West;
        if (east == FaceSide.Continues) mask |= WallAutotile.East;
        DrawCap(art, mask, 0, CapStripStart);
        art.Fill(0, CapStripStart, Size - 1, CapStripStart, CapLip);
        art.Fill(0, 10, Size - 1, 10, FaceShadow);
        art.Fill(0, 7, Size - 1, 9, FaceTop);
        art.Fill(0, 0, Size - 1, 6, FaceMid);
        DrawDecor(art, decor);
        DrawJambs(art, west, east, 0, 10);
    }

    /// <summary>Parte baja de una pared exterior de dos casillas: cara, surco del neón y zócalo.</summary>
    public static void DrawTallLower(PixelSprite art, FaceSide west, FaceSide east, int decor)
    {
        art.Fill(0, 9, Size - 1, Size - 1, FaceMid);
        art.Fill(0, 3, Size - 1, 8, FaceLow);
        art.Fill(0, TrimRow, Size - 1, TrimRow, Groove);
        art.Fill(0, 1, Size - 1, 1, Base);
        art.Fill(0, 0, Size - 1, 0, BaseDark);
        if (decor == 1)
        {
            art.Fill(7, 3, 7, Size - 1, Seam);
            art.Fill(8, 3, 8, Size - 1, FaceTop);
        }
        else if (decor == 2)
        {
            for (int y = 5; y <= 11; y += 2) art.Fill(4, y, 11, y, Base);
        }
        DrawJambs(art, west, east, 0, Size - 1);
    }

    static void DrawJambs(PixelSprite art, FaceSide west, FaceSide east, int y0, int y1)
    {
        if (west != FaceSide.Continues)
        {
            art.Fill(0, y0, 0, y1, west == FaceSide.Open ? JambLight : FaceShadow);
            art.Fill(1, y0, 1, y1, Seam);
        }
        if (east != FaceSide.Continues)
        {
            art.Fill(Size - 1, y0, Size - 1, y1, BaseDark);
            art.Fill(Size - 2, y0, Size - 2, y1, Seam);
        }
    }

    static void DrawDecor(PixelSprite art, int decor)
    {
        switch (decor)
        {
            case 1:
                art.Fill(7, 3, 7, 9, Seam);
                art.Fill(8, 3, 8, 9, FaceTop);
                break;
            case 2:
                for (int y = 4; y <= 8; y += 2) art.Fill(4, y, 11, y, Base);
                for (int y = 5; y <= 7; y += 2) art.Fill(4, y, 11, y, FaceTop);
                break;
            case 3:
                art.Fill(5, 5, 10, 7, LightRim);
                art.Fill(6, 6, 9, 6, LightCore);
                break;
        }
    }

    /// <summary>Sombra al pie de las paredes: "n" (pared al norte), "w" (pared al oeste) o "nw".</summary>
    public static PixelSprite Shadow(bool north, bool west)
    {
        var art = new PixelSprite(Size, Size);
        byte[] northAlpha = { 110, 72, 40, 16 };
        byte[] westAlpha = { 78, 44, 18 };
        for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                int a = 0;
                int fromTop = Size - 1 - y;
                if (north && fromTop < northAlpha.Length) a = Mathf.Max(a, northAlpha[fromTop]);
                if (west && x < westAlpha.Length) a = Mathf.Max(a, westAlpha[x]);
                if (a > 0) art.Set(x, y, new Color32(4, 6, 10, (byte)a));
            }
        return art;
    }

    /// <summary>Placa metálica del umbral con franjas de peligro (horizontal o vertical).</summary>
    public static PixelSprite DoorPlate(bool horizontal)
    {
        var art = new PixelSprite(Size, Size);
        var metal = PixelSprite.Hex("3A4757");
        var rivet = PixelSprite.Hex("5D6E82");
        var track = PixelSprite.Hex("161C24");
        var yellow = PixelSprite.Hex("E8B930");
        var black = PixelSprite.Hex("1B1B1B");
        for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                int along = horizontal ? x : y, across = horizontal ? y : x;
                Color32 c = metal;
                if (across <= 1 || across >= Size - 2) c = ((along + across) % 4) < 2 ? yellow : black;
                else if (across == 7 || across == 8) c = track;
                else if (along % 8 == 3 && (across == 4 || across == 11)) c = rivet;
                art.Set(x, y, c);
            }
        return art;
    }

    /// <summary>Línea de neón blanca (se tiñe por sala) en el surco de la cara; con jambas iluminadas opcionales.</summary>
    public static PixelSprite Trim(bool westJamb, bool eastJamb)
    {
        var art = new PixelSprite(Size, Size);
        var full = new Color32(255, 255, 255, 255);
        var soft = new Color32(255, 255, 255, 90);
        art.Fill(0, TrimRow, Size - 1, TrimRow, full);
        art.Fill(0, TrimRow - 1, Size - 1, TrimRow - 1, soft);
        art.Fill(0, TrimRow + 1, Size - 1, TrimRow + 1, soft);
        if (westJamb) { art.Fill(0, TrimRow, 0, 10, full); art.Fill(1, TrimRow, 1, 10, soft); }
        if (eastJamb) { art.Fill(Size - 1, TrimRow, Size - 1, 10, full); art.Fill(Size - 2, TrimRow, Size - 2, 10, soft); }
        return art;
    }

    /// <summary>Línea guía de suelo (blanca, se tiñe con el color de la sala a la que lleva).</summary>
    public static PixelSprite Runway(bool vertical)
    {
        var art = new PixelSprite(Size, Size);
        var core = new Color32(255, 255, 255, 230);
        var soft = new Color32(255, 255, 255, 70);
        for (int i = 0; i < Size; i++)
        {
            bool dash = i % 8 < 6;
            if (!dash) continue;
            if (vertical)
            {
                art.Set(7, i, core);
                art.Set(8, i, core);
                art.Set(6, i, soft);
                art.Set(9, i, soft);
            }
            else
            {
                art.Set(i, 7, core);
                art.Set(i, 8, core);
                art.Set(i, 6, soft);
                art.Set(i, 9, soft);
            }
        }
        return art;
    }

    /// <summary>Dintel sobre una puerta horizontal: franja de remate que se dibuja por encima del jugador.</summary>
    public static PixelSprite Lintel()
    {
        var art = new PixelSprite(Size, Size);
        DrawCap(art, WallAutotile.South | WallAutotile.East | WallAutotile.West, 0, CapStripStart);
        art.Fill(0, CapStripStart, Size - 1, CapStripStart, CapLip);
        return art;
    }
}
