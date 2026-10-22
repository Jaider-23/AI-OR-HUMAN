using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Genera (o regenera) todo el arte procedural del laboratorio en Assets/Art/Generated y lo deja
/// listo para los constructores: tiles de pared por variante, sombras, umbrales, neón, efectos y materiales.
/// </summary>
public sealed class ArtLibrary
{
    public const string Root = "Assets/Art/Generated";
    const string WallsFolder = Root + "/Walls";
    const string MaterialsFolder = "Assets/Art/Materials";
    static readonly Vector2 Center = new Vector2(0.5f, 0.5f);
    static readonly Vector2 Bottom = new Vector2(0.5f, 0f);

    public Sprite Glow;
    public Sprite BlobShadow;
    public Sprite Dust;
    public Sprite Pixel;
    public Sprite FocusRing;
    public Sprite DroneA;
    public Sprite DroneB;
    public Sprite AdaHologram;
    public Sprite DoorPanelH;
    public Sprite DoorPanelV;
    public Sprite ServerRackA;
    public Sprite ServerRackB;
    public Sprite LintelSprite;

    public Tile ShadowN;
    public Tile ShadowW;
    public Tile ShadowNW;
    public Tile PlateH;
    public Tile PlateV;
    public Tile Trim;
    public Tile TrimJambW;
    public Tile TrimJambE;
    public Tile TrimJambWE;
    public Tile RunwayV;
    public Tile RunwayH;

    public Material Additive;
    public Material DustParticles;
    public Material GlowParticles;

    readonly Dictionary<string, Tile> walls = new Dictionary<string, Tile>();

    public static ArtLibrary Create()
    {
        BuildPaths.EnsureFolder(WallsFolder);
        var lib = new ArtLibrary
        {
            Glow = ArtAssets.SaveSprite(EffectArt.Glow(64), $"{Root}/glow.png", Center, 32, FilterMode.Bilinear),
            // Pivotes en píxel entero (5 px de alto -> fila 2; 9 px -> fila 4) para no dibujar a medio píxel.
            BlobShadow = SpriteOf("blob_shadow", EffectArt.BlobShadow(), new Vector2(0.5f, 2f / 5f)),
            Dust = SpriteOf("dust", EffectArt.Dust(), Center),
            Pixel = SpriteOf("pixel", EffectArt.Square(2), Center),
            FocusRing = SpriteOf("focus_ring", EffectArt.FocusRing(), new Vector2(0.5f, 4f / 9f)),
            DroneA = SpriteOf("drone_a", EffectArt.Drone(true), Center),
            DroneB = SpriteOf("drone_b", EffectArt.Drone(false), Center),
            AdaHologram = SpriteOf("ada_hologram", EffectArt.AdaHologram(BuildPaths.Aria), Bottom),
            DoorPanelH = ArtAssets.SaveSprite(EffectArt.DoorPanel(true), $"{Root}/door_panel_h.png", Bottom, border: new Vector4(4, 3, 4, 3)),
            ServerRackA = SpriteOf("server_rack_a", PropArt.ServerRack(0), Bottom),
            ServerRackB = SpriteOf("server_rack_b", PropArt.ServerRack(1), Bottom),
            LintelSprite = SpriteOf("lintel", WallArt.Lintel(), Bottom),
            DoorPanelV = ArtAssets.SaveSprite(EffectArt.DoorPanel(false), $"{Root}/door_panel_v.png", Center, border: new Vector4(3, 4, 3, 4)),
            ShadowN = TileOf("shadow_n", WallArt.Shadow(true, false), false),
            ShadowW = TileOf("shadow_w", WallArt.Shadow(false, true), false),
            ShadowNW = TileOf("shadow_nw", WallArt.Shadow(true, true), false),
            PlateH = TileOf("door_plate_h", WallArt.DoorPlate(true), false),
            PlateV = TileOf("door_plate_v", WallArt.DoorPlate(false), false),
            Trim = TileOf("neon_trim", WallArt.Trim(false, false), true),
            TrimJambW = TileOf("neon_trim_jw", WallArt.Trim(true, false), true),
            TrimJambE = TileOf("neon_trim_je", WallArt.Trim(false, true), true),
            TrimJambWE = TileOf("neon_trim_jwe", WallArt.Trim(true, true), true),
            RunwayV = TileOf("runway_v", WallArt.Runway(true), true),
            RunwayH = TileOf("runway_h", WallArt.Runway(false), true),
        };
        lib.Additive = ArtAssets.LoadOrCreateMaterial($"{MaterialsFolder}/Additive.mat", "Legacy Shaders/Particles/Additive");
        lib.DustParticles = ArtAssets.LoadOrCreateMaterial($"{MaterialsFolder}/DustParticles.mat", "Legacy Shaders/Particles/Alpha Blended", lib.Dust.texture);
        lib.GlowParticles = ArtAssets.LoadOrCreateMaterial($"{MaterialsFolder}/GlowParticles.mat", "Legacy Shaders/Particles/Additive", lib.Pixel.texture);
        return lib;
    }

    /// <summary>Tile de pared para una clave de <see cref="WallAutotile.KeyAt"/>; se genera la primera vez.</summary>
    public Tile Wall(string key)
    {
        if (walls.TryGetValue(key, out var tile)) return tile;
        var sprite = ArtAssets.SaveSprite(WallArt.FromKey(key), $"{WallsFolder}/{key}.png", Center);
        tile = ArtAssets.SaveTile(sprite, $"{WallsFolder}/{key}.asset", false);
        walls[key] = tile;
        return tile;
    }

    /// <summary>Borra las variantes de pared que el mapa actual ya no usa (quedan de mapas anteriores).</summary>
    public int DeleteUnusedWalls()
    {
        int deleted = 0;
        foreach (string guid in UnityEditor.AssetDatabase.FindAssets("t:Tile", new[] { WallsFolder }))
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            string key = System.IO.Path.GetFileNameWithoutExtension(path);
            if (walls.ContainsKey(key)) continue;
            UnityEditor.AssetDatabase.DeleteAsset(path);
            UnityEditor.AssetDatabase.DeleteAsset(System.IO.Path.ChangeExtension(path, ".png"));
            deleted++;
        }
        return deleted;
    }

    static Sprite SpriteOf(string name, PixelSprite art, Vector2 pivot) =>
        ArtAssets.SaveSprite(art, $"{Root}/{name}.png", pivot);

    static Tile TileOf(string name, PixelSprite art, bool allowTint)
    {
        var sprite = ArtAssets.SaveSprite(art, $"{Root}/{name}.png", Center);
        return ArtAssets.SaveTile(sprite, $"{Root}/{name}.asset", allowTint);
    }
}
