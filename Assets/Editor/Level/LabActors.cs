using System;
using System.Linq;
using Cinemachine;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;
using Object = UnityEngine.Object;

/// <summary>
/// Configura a Alex (física de pies, material sin fricción, parte visual en un hijo, sombra, polvo,
/// anillo de foco) y la cámara pixel-perfect que lo sigue dentro de los límites del mapa.
/// </summary>
public static class LabActors
{
    const string MaterialPath = "Assets/Art/Materials/PlayerSlide.physicsMaterial2D";
    public static readonly Vector2 FeetSize = new Vector2(0.5f, 0.16f);
    public const float FeetEdgeRadius = 0.06f;
    public const float FeetOffsetY = 0.14f;

    public static GameObject SetupPlayer(LevelLayout map, ArtLibrary art, Transform worldRoot)
    {
        var player = Object.FindObjectsOfType<PlayerController>().First().gameObject;
        var spawn = map.Data.spawn;
        player.transform.position = map.ToWorldCenter(spawn.x, spawn.y);

        var visual = EnsureVisual(player);
        SetupPhysics(player);
        var visuals = GetOrAdd<PlayerVisuals>(player);
        visuals.visual = visual.transform;
        visuals.dust = CreateDust(player.transform, art);

        var shadow = Child(player.transform, "Shadow");
        var shadowRenderer = GetOrAdd<SpriteRenderer>(shadow);
        shadowRenderer.sprite = art.BlobShadow;
        shadowRenderer.sortingOrder = -6;
        shadow.transform.localPosition = Vector3.zero;

        var ring = new GameObject("FocusRing");
        ring.transform.SetParent(worldRoot, false);
        var ringRenderer = ring.AddComponent<SpriteRenderer>();
        ringRenderer.sprite = art.FocusRing;
        ringRenderer.sortingOrder = -5;
        ringRenderer.enabled = false;
        var presenter = GetOrAdd<FocusRingPresenter>(player);
        presenter.ring = ringRenderer;

        if (player.GetComponent<PlayerInteractor>() == null) player.AddComponent<PlayerInteractor>();
        EditorUtility.SetDirty(player);
        return player;
    }

    /// <summary>Mueve SpriteRenderer y Animator a un hijo "Visual" (la respiración no mueve la física).</summary>
    static GameObject EnsureVisual(GameObject player)
    {
        var visual = Child(player.transform, "Visual");
        var controller = player.GetComponent<Animator>() != null ? player.GetComponent<Animator>().runtimeAnimatorController : null;
        var rootAnimator = player.GetComponent<Animator>();
        if (rootAnimator != null) Object.DestroyImmediate(rootAnimator);
        var rootRenderer = player.GetComponent<SpriteRenderer>();
        if (rootRenderer != null) Object.DestroyImmediate(rootRenderer);

        var renderer = GetOrAdd<SpriteRenderer>(visual);
        renderer.sprite = AnimationBuilder.LoadFrames(BuildPaths.RunDown)[0];
        renderer.sortingOrder = 0;
        renderer.spriteSortPoint = SpriteSortPoint.Pivot;
        renderer.color = Color.white;
        var animator = GetOrAdd<Animator>(visual);
        if (controller == null) controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(BuildPaths.Animation + "/player.controller");
        animator.runtimeAnimatorController = controller;
        visual.transform.localPosition = Vector3.zero;
        return visual;
    }

    static void SetupPhysics(GameObject player)
    {
        var body = player.GetComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Dynamic;
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.drag = 0f;

        foreach (var c in player.GetComponents<Collider2D>()) Object.DestroyImmediate(c);
        var feet = player.AddComponent<BoxCollider2D>();
        feet.size = FeetSize;
        feet.edgeRadius = FeetEdgeRadius;
        feet.offset = new Vector2(0f, FeetOffsetY);
        feet.sharedMaterial = SlideMaterial();
    }

    /// <summary>Sin fricción: al chocar en diagonal con una pared Alex se desliza en vez de "pegarse".</summary>
    static PhysicsMaterial2D SlideMaterial()
    {
        var material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(MaterialPath);
        if (material == null)
        {
            BuildPaths.EnsureFolder("Assets/Art/Materials");
            material = new PhysicsMaterial2D("PlayerSlide");
            AssetDatabase.CreateAsset(material, MaterialPath);
        }
        material.friction = 0f;
        material.bounciness = 0f;
        EditorUtility.SetDirty(material);
        return material;
    }

    static ParticleSystem CreateDust(Transform player, ArtLibrary art)
    {
        var old = player.Find("Dust");
        if (old != null) Object.DestroyImmediate(old.gameObject);
        var go = new GameObject("Dust");
        go.transform.SetParent(player, false);
        go.transform.localPosition = new Vector3(0f, 0.05f, 0f);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 1f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.55f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.15f, 0.45f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.16f, 0.28f);
        main.startColor = new Color(0.78f, 0.84f, 0.9f, 0.7f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 40;
        var emission = ps.emission;
        emission.rateOverTime = 0f;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.12f;
        var color = ps.colorOverLifetime;
        color.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                         new[] { new GradientAlphaKey(0.8f, 0f), new GradientAlphaKey(0f, 1f) });
        color.color = gradient;
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = art.DustParticles;
        renderer.sortingOrder = -4;
        return ps;
    }

    public static void SetupCamera(LevelLayout map, GameObject player)
    {
        var cam = Camera.main;
        cam.backgroundColor = new Color(0.02f, 0.025f, 0.045f);
        var ppc = GetOrAdd<PixelPerfectCamera>(cam.gameObject);
        ppc.assetsPPU = 16;
        ppc.refResolutionX = 384;
        ppc.refResolutionY = 216;
        ppc.upscaleRT = false;
        ppc.pixelSnapping = true;
        ppc.cropFrameX = false;
        ppc.cropFrameY = false;
        ppc.stretchFill = false;

        var vcam = Object.FindObjectOfType<CinemachineVirtualCamera>();
        if (vcam == null) return;
        vcam.Follow = player.transform;
        vcam.m_Lens.OrthographicSize = 216f / 2f / 16f;
        var pixelPerfect = Type.GetType("Cinemachine.CinemachinePixelPerfect, Cinemachine");
        if (pixelPerfect != null && vcam.GetComponent(pixelPerfect) == null) vcam.gameObject.AddComponent(pixelPerfect);
        var framing = vcam.GetCinemachineComponent<CinemachineFramingTransposer>();
        if (framing != null)
        {
            framing.m_XDamping = 0.6f;
            framing.m_YDamping = 0.6f;
            framing.m_DeadZoneWidth = 0.08f;
            framing.m_DeadZoneHeight = 0.08f;
        }
        var transposer = vcam.GetCinemachineComponent<CinemachineTransposer>();
        if (transposer != null)
        {
            transposer.m_XDamping = 0.6f;
            transposer.m_YDamping = 0.6f;
        }
        SetupConfiner(map);
        EditorUtility.SetDirty(vcam);
        EditorUtility.SetDirty(cam);
    }

    static void SetupConfiner(LevelLayout map)
    {
        var confiner = Object.FindObjectOfType<CinemachineConfiner>();
        var shape = confiner != null ? confiner.m_BoundingShape2D as PolygonCollider2D : null;
        if (shape == null) return;
        var r = map.WorldRect;
        shape.isTrigger = true;
        shape.transform.position = Vector3.zero;
        shape.transform.localScale = Vector3.one;
        shape.offset = Vector2.zero;
        shape.pathCount = 1;
        // Margen arriba: las paredes exteriores del norte miden dos casillas y la barra del HUD tapa ~1 unidad;
        // sin él, las pantallas del muro norte y su holograma quedaban fuera de la vista.
        float top = r.yMax + 3f;
        shape.SetPath(0, new[] { new Vector2(r.xMin, r.yMin), new Vector2(r.xMax, r.yMin), new Vector2(r.xMax, top), new Vector2(r.xMin, top) });
        confiner.m_ConfineScreenEdges = true;
        confiner.InvalidatePathCache();
        EditorUtility.SetDirty(shape);
        EditorUtility.SetDirty(confiner);
    }

    /// <summary>GetComponent o AddComponent sin usar ?? (los objetos Unity "nulos" no son null para C#).</summary>
    public static T GetOrAdd<T>(GameObject go) where T : Component
    {
        var existing = go.GetComponent<T>();
        return existing != null ? existing : go.AddComponent<T>();
    }

    static GameObject Child(Transform parent, string name)
    {
        var existing = parent.Find(name);
        if (existing != null) return existing.gameObject;
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go;
    }
}
