using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>
/// Construye el laboratorio completo de GameScene desde Assets/Level/lab_layout.json:
/// tiles (suelo, paredes 3/4, sombras, neón), colisiones de pared, objetos, puertas, luces,
/// dron, partículas, jugador y cámara. Es re-ejecutable.
/// </summary>
public static class LabWorldBuilder
{
    public const string LayoutPath = "Assets/Level/lab_layout.json";
    public const string RootName = "World_Generated";

    static readonly string[] Obsolete =
    {
        RootName, "pc", "pixel-cyberpunk-interior_49", "pixel-cyberpunk-interior_60", "pixel-cyberpunk-interior_62"
    };

    public static LevelLayout LoadLayout()
    {
        var map = LevelLayout.FromJson(File.ReadAllText(LayoutPath));
        var errors = LevelLayoutValidator.Validate(map);
        if (errors.Count > 0) throw new InvalidOperationException("El mapa tiene errores:\n" + string.Join("\n", errors));
        return map;
    }

    public static (CaseTerminal[] terminals, DataFragment[] fragments) Build(CaseData[] cases)
    {
        var map = LoadLayout();
        GraphicsSettings.transparencySortMode = TransparencySortMode.CustomAxis;
        GraphicsSettings.transparencySortAxis = Vector3.up;

        var art = ArtLibrary.Create();
        var catalog = PropCatalog.Load(art.ServerRackA, art.ServerRackB);
        // Raíces de la escena (incluye las desactivadas, que FindObjectsOfType no ve).
        foreach (var go in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (Obsolete.Contains(go.name)) Object.DestroyImmediate(go);
        }
        var root = new GameObject(RootName).transform;

        var layers = LabTilemaps.Prepare(art);
        LabTilemaps.PaintFloor(map, layers, art);
        LabTilemaps.PaintWalls(map, layers, art);
        LabTilemaps.PaintShadows(map, layers, art);
        var walls = WallColliderBuilder.Build(map, root);

        var props = LabProps.Build(map, cases, art, catalog, root);
        LabAmbience.BuildDoors(map, art, Child(root, "Doors"));
        LabAmbience.BuildLights(map, art, Child(root, "Lights"));
        LabAmbience.BuildDrone(map, art, root);
        LabAmbience.BuildParticles(map, art, Child(root, "Particles"));

        var player = LabActors.SetupPlayer(map, art, root);
        LabActors.SetupCamera(map, player);
        int stale = art.DeleteUnusedWalls();
        if (stale > 0) Debug.Log($"[LabWorldBuilder] {stale} variantes de pared sin uso eliminadas");
        Debug.Log($"[LabWorldBuilder] mapa {map.Width}x{map.Height}, colisión de paredes: {walls.pathCount} contornos, " +
                  $"terminales {props.Terminals.Length}, fragmentos {props.Fragments.Length}, " +
                  $"decorado colocado {props.DecorPlaced} / descartado {props.DecorSkipped}");
        return (props.Terminals, props.Fragments);
    }

    static Transform Child(Transform parent, string name)
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent, false);
        return t;
    }
}
