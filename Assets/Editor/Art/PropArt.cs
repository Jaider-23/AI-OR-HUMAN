using UnityEngine;

/// <summary>Muebles que no existen en el pack y se generan: armario de servidores en perspectiva 3/4.</summary>
public static class PropArt
{
    public const int RackWidth = 16;
    public const int RackHeight = 30;
    public const int RackDepth = 8;

    /// <summary>Posiciones (en píxeles, origen abajo-izquierda) de los LED que parpadean.</summary>
    public static readonly Vector2Int[] RackLeds = { new Vector2Int(3, 18), new Vector2Int(3, 13), new Vector2Int(3, 8) };

    public static PixelSprite ServerRack(int variant)
    {
        var art = new PixelSprite(RackWidth, RackHeight);
        var outline = PixelSprite.Hex("0C1016");
        var top = PixelSprite.Hex("4A5C70");
        var topEdge = PixelSprite.Hex("6C87A0");
        var body = PixelSprite.Hex("222B37");
        var bay = PixelSprite.Hex("161D27");
        var slat = PixelSprite.Hex("2E3A48");
        Color32[] leds = variant == 0
            ? new[] { PixelSprite.Hex("46F28C"), PixelSprite.Hex("22E4F0"), PixelSprite.Hex("46F28C") }
            : new[] { PixelSprite.Hex("FF9A3C"), PixelSprite.Hex("22E4F0"), PixelSprite.Hex("FF4D6A") };

        art.Fill(0, 0, RackWidth - 1, RackHeight - 1, outline);
        // Cara superior visible (profundidad) y frente del armario.
        art.Fill(1, 23, RackWidth - 2, RackHeight - 2, top);
        art.Fill(1, RackHeight - 2, RackWidth - 2, RackHeight - 2, topEdge);
        art.Fill(1, 1, RackWidth - 2, 22, body);
        art.Fill(1, 22, RackWidth - 2, 22, outline);
        for (int unit = 0; unit < 4; unit++)
        {
            int y0 = 3 + unit * 5;
            art.Fill(2, y0, RackWidth - 3, y0 + 3, bay);
            for (int x = 7; x <= RackWidth - 4; x += 2) art.Fill(x, y0 + 1, x, y0 + 2, slat);
        }
        for (int i = 0; i < RackLeds.Length; i++)
        {
            art.Set(RackLeds[i].x, RackLeds[i].y, leds[i]);
            art.Set(RackLeds[i].x + 1, RackLeds[i].y, leds[(i + 1) % leds.Length]);
        }
        art.Fill(1, 0, RackWidth - 2, 0, PixelSprite.Hex("05070A"));
        return art;
    }
}
