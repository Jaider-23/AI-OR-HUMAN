using System.IO;
using UnityEngine;

/// <summary>Sprites de efectos generados por código: luces, sombras, partículas, dron, holograma de ADA, puertas.</summary>
public static class EffectArt
{
    public static PixelSprite Glow(int size)
    {
        var art = new PixelSprite(size, size);
        float r = size * 0.5f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r)) / r;
                if (d >= 1f) continue;
                art.Set(x, y, new Color32(255, 255, 255, (byte)(Mathf.Pow(1f - d, 2.2f) * 255f)));
            }
        return art;
    }

    public static PixelSprite BlobShadow()
    {
        var art = new PixelSprite(12, 5);
        for (int y = 0; y < 5; y++)
            for (int x = 0; x < 12; x++)
            {
                float dx = (x + 0.5f - 6f) / 6f, dy = (y + 0.5f - 2.5f) / 2.5f;
                float d = dx * dx + dy * dy;
                if (d < 0.45f) art.Set(x, y, new Color32(0, 0, 0, 95));
                else if (d < 1f) art.Set(x, y, new Color32(0, 0, 0, 55));
            }
        return art;
    }

    public static PixelSprite Dust()
    {
        var art = new PixelSprite(4, 4);
        var edge = new Color32(205, 215, 225, 110);
        var core = new Color32(225, 232, 240, 220);
        art.Fill(1, 0, 2, 3, edge);
        art.Fill(0, 1, 3, 2, edge);
        art.Fill(1, 1, 2, 2, core);
        return art;
    }

    public static PixelSprite Square(int size)
    {
        var art = new PixelSprite(size, size);
        art.Fill(0, 0, size - 1, size - 1, new Color32(255, 255, 255, 255));
        return art;
    }

    /// <summary>Anillo elíptico que marca el objeto con el que se puede interactuar.</summary>
    public static PixelSprite FocusRing()
    {
        var art = new PixelSprite(24, 9);
        for (int y = 0; y < 9; y++)
            for (int x = 0; x < 24; x++)
            {
                float dx = (x + 0.5f - 12f) / 12f, dy = (y + 0.5f - 4.5f) / 4.5f;
                float d = dx * dx + dy * dy;
                if (d <= 1f && d >= 0.62f) art.Set(x, y, new Color32(255, 255, 255, 255));
            }
        return art;
    }

    public static PixelSprite Drone(bool bladesOpen)
    {
        var art = new PixelSprite(16, 10);
        var body = PixelSprite.Hex("2C3644");
        var top = PixelSprite.Hex("5A7088");
        var shade = PixelSprite.Hex("1A2029");
        var arm = PixelSprite.Hex("46505F");
        var blade = new Color32(190, 200, 212, 210);
        var blur = new Color32(190, 200, 212, 90);
        for (int y = 1; y <= 6; y++)
            for (int x = 3; x <= 12; x++)
            {
                float dx = (x + 0.5f - 8f) / 5f, dy = (y + 0.5f - 4f) / 3f;
                if (dx * dx + dy * dy > 1f) continue;
                art.Set(x, y, y >= 5 ? top : y <= 2 ? shade : body);
            }
        art.Fill(2, 7, 13, 7, arm);
        art.Fill(7, 4, 9, 4, PixelSprite.Hex("5FF2FF"));
        art.Set(8, 4, PixelSprite.Hex("D8FFFF"));
        art.Set(11, 3, PixelSprite.Hex("FF4D6A"));
        if (bladesOpen)
        {
            art.Fill(0, 8, 5, 8, blade);
            art.Fill(10, 8, 15, 8, blade);
        }
        else
        {
            art.Fill(1, 8, 4, 8, blur);
            art.Fill(11, 8, 14, 8, blur);
            art.Fill(2, 9, 3, 9, blade);
            art.Fill(12, 9, 13, 9, blade);
        }
        return art;
    }

    /// <summary>Panel de puerta corrediza (se estira con 9-slice). horizontal = puerta en pared horizontal.</summary>
    public static PixelSprite DoorPanel(bool horizontal)
    {
        int w = 16, h = horizontal ? 11 : 16;
        var art = new PixelSprite(w, h);
        var frame = PixelSprite.Hex("141A22");
        var metal = horizontal ? PixelSprite.Hex("4A5D73") : PixelSprite.Hex("3C4D61");
        var hi = PixelSprite.Hex("7C98B4");
        var seam = PixelSprite.Hex("26313E");
        art.Fill(0, 0, w - 1, h - 1, frame);
        art.Fill(1, 1, w - 2, h - 2, metal);
        art.Fill(1, h - 2, w - 2, h - 2, hi);
        if (horizontal)
        {
            art.Fill(2, 5, w - 3, 5, seam);
            art.Fill(3, 2, w - 4, 2, PixelSprite.Hex("2E3B4A"));
        }
        else
        {
            art.Fill(5, 2, 5, h - 3, seam);
            art.Fill(10, 2, 10, h - 3, seam);
        }
        return art;
    }

    /// <summary>
    /// Holograma pixelado de ADA a partir de su retrato: recorte del rostro, reducción por promedio,
    /// paleta cian de 4 tonos, viñeta elíptica y líneas de barrido.
    /// </summary>
    public static PixelSprite AdaHologram(string portraitPath, int width = 22, int height = 26)
    {
        var art = new PixelSprite(width, height);
        if (!File.Exists(portraitPath)) return art;
        var source = new Texture2D(2, 2);
        source.LoadImage(File.ReadAllBytes(portraitPath));
        // Región del rostro y hombros (fracciones medidas desde arriba-izquierda del retrato).
        float x0 = 0.33f * source.width, x1 = 0.67f * source.width;
        float yTop = 0.0f * source.height, yBottom = 0.66f * source.height;
        Color32[] ramp =
        {
            PixelSprite.Hex("0F5C6E", 170), PixelSprite.Hex("1FA6B8", 220),
            PixelSprite.Hex("5FF2FF", 255), PixelSprite.Hex("D9FFFF", 255)
        };
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float lum = AverageLuminance(source, Mathf.Lerp(x0, x1, x / (float)width), Mathf.Lerp(x0, x1, (x + 1) / (float)width),
                                             Mathf.Lerp(yBottom, yTop, (y + 1) / (float)height), Mathf.Lerp(yBottom, yTop, y / (float)height));
                float vx = (x + 0.5f) / width - 0.5f, vy = (y + 0.5f) / height - 0.55f;
                float vignette = 1f - Mathf.Clamp01((vx * vx / 0.2f + vy * vy / 0.26f) - 0.55f) * 2f;
                lum *= Mathf.Clamp01(vignette);
                if (lum < 0.2f) continue;
                int level = lum < 0.36f ? 0 : lum < 0.55f ? 1 : lum < 0.74f ? 2 : 3;
                var c = ramp[level];
                if (y % 2 == 1) c.a = (byte)(c.a * 0.62f);
                art.Set(x, y, c);
            }
        Object.DestroyImmediate(source);
        return art;
    }

    /// <summary>Luminancia media de un rectángulo dado en píxeles medidos desde arriba-izquierda.</summary>
    static float AverageLuminance(Texture2D tex, float xa, float xb, float ya, float yb)
    {
        float sum = 0f;
        int n = 0;
        int step = Mathf.Max(1, Mathf.RoundToInt((xb - xa) / 6f));
        for (int y = Mathf.FloorToInt(ya); y < Mathf.CeilToInt(yb); y += step)
            for (int x = Mathf.FloorToInt(xa); x < Mathf.CeilToInt(xb); x += step)
            {
                var c = tex.GetPixel(x, tex.height - 1 - y);
                sum += c.r * 0.3f + c.g * 0.59f + c.b * 0.11f;
                n++;
            }
        return n == 0 ? 0f : sum / n;
    }
}
