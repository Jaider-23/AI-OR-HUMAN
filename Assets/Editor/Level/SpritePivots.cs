using System;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

/// <summary>
/// Pivotes "en la base" para la ordenación por profundidad (eje Y): los muebles se apoyan en su borde
/// inferior y Alex en sus pies (punto (20, 8) de cada celda de 40 px, donde terminan sus zapatos).
/// El pivote horizontal cae siempre en un píxel entero (en sprites de ancho impar, el centro exacto
/// quedaría a medio píxel y el pixel art se dibujaría desalineado).
/// Usa la API del editor de sprites (ISpriteEditorDataProvider): asignar TextureImporter.spritesheet
/// no se guardaba en esta versión de Unity.
/// </summary>
public static class SpritePivots
{
    /// <summary>Cada fotograma de Alex ocupa una celda de 40x40 px; sus pies están en (20, 8) de la celda.</summary>
    const int FrameSize = 40;
    static readonly Vector2 FeetInFrame = new Vector2(20f, 8f);

    /// <summary>Pivote inferior con la x redondeada hacia abajo al píxel entero más cercano al centro.</summary>
    public static Vector2 BottomPixelPivot(Rect rect) => new Vector2(Mathf.Floor(rect.width * 0.5f) / rect.width, 0f);

    public static void Apply()
    {
        SetSheet(BuildPaths.Interior, BottomPixelPivot);
        foreach (var sheet in new[] { BuildPaths.RunDown, BuildPaths.RunUp, BuildPaths.RunLeft, BuildPaths.RunRight })
        {
            SetSheet(sheet, FeetPivot);
        }
    }

    /// <summary>
    /// Los fotogramas están recortados al contenido (p. ej. 19x24 px), así que el pivote se calcula por
    /// fotograma para que caiga siempre en el mismo punto de la celda: los pies. Así el dibujo coincide
    /// con el collider y la animación no "baila" de lado entre fotogramas.
    /// </summary>
    public static Vector2 FeetPivot(Rect rect)
    {
        float frameX = Mathf.Floor(rect.x / FrameSize) * FrameSize;
        float frameY = Mathf.Floor(rect.y / FrameSize) * FrameSize;
        return new Vector2((frameX + FeetInFrame.x - rect.x) / rect.width, (frameY + FeetInFrame.y - rect.y) / rect.height);
    }

    static void SetSheet(string path, Func<Rect, Vector2> pivotFor)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();
        var rects = provider.GetSpriteRects();
        if (rects.All(r => r.alignment == SpriteAlignment.Custom && r.pivot == pivotFor(r.rect))) return;
        foreach (var rect in rects)
        {
            rect.alignment = SpriteAlignment.Custom;
            rect.pivot = pivotFor(rect.rect);
        }
        provider.SetSpriteRects(rects);
        provider.Apply();
        importer.SaveAndReimport();
        Debug.Log($"[SpritePivots] {path}: {rects.Length} sprites con pivote en la base");
    }
}
