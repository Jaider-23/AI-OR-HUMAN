using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

/// <summary>Paleta del estilo cyberpunk del juego (GDD 19).</summary>
public static class Theme
{
    public static readonly Color Cyan = Hex("22E4F0");
    public static readonly Color Magenta = Hex("FF3FD1");
    public static readonly Color Text = Hex("E6F7FF");
    public static readonly Color Muted = Hex("8FA3BF");
    public static readonly Color Orange = Hex("FF9A3C");
    public static readonly Color Panel = new Color(0.04f, 0.06f, 0.12f, 0.94f);
    public static readonly Color PanelLight = new Color(0.08f, 0.13f, 0.22f, 0.95f);
    public static readonly Color Dim = new Color(0f, 0f, 0.02f, 0.72f);

    public static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out var color);
        return color;
    }
}

/// <summary>Atajos para construir jerarquías de UI desde los constructores del Editor.</summary>
public static class UIFactory
{
    static TMP_FontAsset heading;
    static TMP_FontAsset body;
    static Sprite rounded;

    public static TMP_FontAsset Heading => heading != null ? heading : heading = LoadOrCreateHeadingFont();
    public static TMP_FontAsset Body => body != null ? body : body = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BuildPaths.BodyFont);
    static Sprite Rounded => rounded != null ? rounded : rounded = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

    /// <summary>
    /// Orbitron estático solo trae ASCII; se crea una versión dinámica que añade
    /// tildes, ñ y signos de apertura en tiempo de ejecución.
    /// </summary>
    static TMP_FontAsset LoadOrCreateHeadingFont()
    {
        var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BuildPaths.OrbitronDynamic);
        if (existing != null) return existing;

        BuildPaths.EnsureFolder(BuildPaths.Fonts);
        var font = AssetDatabase.LoadAssetAtPath<Font>(BuildPaths.OrbitronBoldTtf);
        var asset = TMP_FontAsset.CreateFontAsset(font, 72, 8, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic);
        asset.name = "Orbitron-Bold Dynamic SDF";
        AssetDatabase.CreateAsset(asset, BuildPaths.OrbitronDynamic);
        asset.atlasTextures[0].name = "Orbitron Atlas";
        asset.material.name = "Orbitron Material";
        AssetDatabase.AddObjectToAsset(asset.atlasTextures[0], asset);
        AssetDatabase.AddObjectToAsset(asset.material, asset);
        asset.TryAddCharacters("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789ÁÉÍÓÚÜÑáéíóúüñ¿¡?!.,:;·%+-/()[]«»\"'·_<>=#");
        var fallback = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BuildPaths.BodyFont);
        if (asset.fallbackFontAssetTable == null) asset.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset>();
        if (fallback != null) asset.fallbackFontAssetTable.Add(fallback);
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
        return asset;
    }

    public static RectTransform Rect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
                                     Vector2? offsetMin = null, Vector2? offsetMax = null)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin ?? Vector2.zero;
        rect.offsetMax = offsetMax ?? Vector2.zero;
        return rect;
    }

    public static RectTransform Stretch(Transform parent, string name) => Rect(parent, name, Vector2.zero, Vector2.one);

    public static Canvas Canvas(string name, int sortingOrder)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        go.layer = LayerMask.NameToLayer("UI");
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    public static Image Panel(RectTransform rect, Color color, Color? outline = null, bool roundedSprite = true)
    {
        var image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        if (roundedSprite)
        {
            image.sprite = Rounded;
            image.type = Image.Type.Sliced;
        }
        if (outline.HasValue)
        {
            var o = rect.gameObject.AddComponent<Outline>();
            o.effectColor = outline.Value;
            o.effectDistance = new Vector2(2f, -2f);
        }
        return image;
    }

    public static TextMeshProUGUI Text(Transform parent, string name, string text, float size, Color color,
                                       TextAlignmentOptions align = TextAlignmentOptions.TopLeft,
                                       bool headingFont = false)
    {
        var rect = Stretch(parent, name);
        var tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.font = headingFont ? Heading : Body;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = align;
        tmp.enableWordWrapping = true;
        tmp.richText = true;
        tmp.raycastTarget = false;
        return tmp;
    }

    public static Button Button(Transform parent, string name, string label, float fontSize = 26f,
                                Color? fill = null, Color? outline = null)
    {
        var rect = Stretch(parent, name);
        var image = Panel(rect, fill ?? Theme.PanelLight, outline ?? Theme.Cyan);
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.75f, 1f, 1f);
        colors.pressedColor = new Color(0.6f, 0.85f, 0.9f);
        colors.disabledColor = new Color(0.45f, 0.45f, 0.5f, 0.6f);
        button.colors = colors;

        var text = Text(rect, "Label", label, fontSize, Theme.Text, TextAlignmentOptions.Center, true);
        text.rectTransform.offsetMin = new Vector2(8, 4);
        text.rectTransform.offsetMax = new Vector2(-8, -4);
        text.enableAutoSizing = true;
        text.fontSizeMin = 12;
        text.fontSizeMax = fontSize;
        return button;
    }

    /// <summary>Texto con scroll vertical (registro de herramientas, listas largas).</summary>
    public static (ScrollRect scroll, TextMeshProUGUI text) ScrollText(Transform parent, string name, float fontSize)
    {
        var root = Stretch(parent, name);
        var scroll = root.gameObject.AddComponent<ScrollRect>();
        var viewport = Stretch(root, "Viewport");
        viewport.gameObject.AddComponent<RectMask2D>();
        var raycast = viewport.gameObject.AddComponent<Image>();
        raycast.color = new Color(0, 0, 0, 0.001f);

        var content = Rect(viewport, "Content", new Vector2(0, 1), new Vector2(1, 1));
        content.pivot = new Vector2(0.5f, 1f);
        var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var text = content.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = Body;
        text.fontSize = fontSize;
        text.color = Theme.Text;
        text.enableWordWrapping = true;
        text.raycastTarget = false;
        text.margin = new Vector4(12, 8, 18, 8);

        scroll.viewport = viewport;
        scroll.content = content;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 30f;
        return (scroll, text);
    }

    public static Slider Slider(Transform parent, string name, float min, float max)
    {
        var go = DefaultControls.CreateSlider(new DefaultControls.Resources { standard = Rounded, background = Rounded, knob = Rounded });
        go.name = name;
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        var slider = go.GetComponent<Slider>();
        slider.minValue = min;
        slider.maxValue = max;
        slider.fillRect.GetComponent<Image>().color = Theme.Cyan;
        var background = go.transform.Find("Background");
        if (background != null) background.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.15f);
        slider.handleRect.GetComponent<Image>().color = Theme.Magenta;
        return slider;
    }

    public static Toggle Toggle(Transform parent, string name, string label)
    {
        var go = DefaultControls.CreateToggle(new DefaultControls.Resources { standard = Rounded, background = Rounded, checkmark = Rounded });
        go.name = name;
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        Object.DestroyImmediate(go.GetComponentInChildren<Text>().gameObject);
        var text = Text(rect, "Label", label, 28, Theme.Text, TextAlignmentOptions.Left);
        text.rectTransform.offsetMin = new Vector2(40, 0);
        var toggle = go.GetComponent<Toggle>();
        toggle.graphic.color = Theme.Cyan;
        return toggle;
    }

    /// <summary>Panel modal centrado con fondo atenuado. Devuelve (raíz, ventana).</summary>
    public static (GameObject root, RectTransform window) Modal(Transform parent, string name,
                                                                 Vector2 min, Vector2 max)
    {
        var root = Stretch(parent, name);
        Panel(root, Theme.Dim, null, false);
        var window = Rect(root, "Window", min, max);
        Panel(window, Theme.Panel, Theme.Cyan);
        window.gameObject.AddComponent<PanelAppear>();
        return (root.gameObject, window);
    }
}
