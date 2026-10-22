using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Completa el menú principal existente (conserva el arte original: fondo, logo y botones
/// JUGAR / CRÉDITOS / SALIR) y añade Continuar, Entrenamiento, Casos, Estadísticas
/// y Configuración (GDD 18.1).
/// </summary>
public static class MenuBuilder
{
    const string SecondaryName = "SecondaryButtons";
    const string ContinueName = "ContinueGroup";

    public static void Build(CaseData[] cases)
    {
        var manager = Object.FindObjectOfType<MainMenuManager>();
        var menuPanel = FindInScene("MenuPanel");
        var canvas = menuPanel.GetComponentInParent<Canvas>().transform;

        foreach (var name in new[] { SecondaryName, ContinueName, "CasesPanel", "StatsPanel", "SettingsPanel" })
        {
            var old = FindInScene(name);
            if (old != null) Object.DestroyImmediate(old);
        }

        BuildSecondaryButtons(menuPanel.transform, manager);
        var continueGroup = BuildContinueGroup(menuPanel.transform, manager, out var continueCaption);
        var cases_ = BuildInfoPanel(canvas, "CasesPanel", "BANCO DE CASOS", manager, out var casesBody);
        var stats = BuildInfoPanel(canvas, "StatsPanel", "ESTADÍSTICAS DEL ANALISTA", manager, out var statsBody);
        var reset = GameUIBuilder.PlacedButton(stats.transform.Find("Window"), "Reset", "BORRAR PROGRESO", 0.04f, 0.12f, 0.06f, 0.36f);
        UnityEventTools.AddPersistentListener(reset.onClick, manager.ResetProgress);
        var settings = BuildSettingsPanel(canvas, manager, out var volume, out var speed, out var fullscreen);

        var so = new SerializedObject(manager);
        so.FindProperty("casesPanel").objectReferenceValue = cases_;
        so.FindProperty("statsPanel").objectReferenceValue = stats;
        so.FindProperty("settingsPanel").objectReferenceValue = settings;
        so.FindProperty("casesBody").objectReferenceValue = casesBody;
        so.FindProperty("statsBody").objectReferenceValue = statsBody;
        so.FindProperty("volumeSlider").objectReferenceValue = volume;
        so.FindProperty("textSpeedSlider").objectReferenceValue = speed;
        so.FindProperty("fullscreenToggle").objectReferenceValue = fullscreen;
        so.FindProperty("quitButton").objectReferenceValue = FindInScene("Btn_Quit");
        so.FindProperty("continueGroup").objectReferenceValue = continueGroup;
        so.FindProperty("continueCaption").objectReferenceValue = continueCaption;
        var casesProp = so.FindProperty("cases");
        casesProp.arraySize = cases.Length;
        for (int i = 0; i < cases.Length; i++) casesProp.GetArrayElementAtIndex(i).objectReferenceValue = cases[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static GameObject FindInScene(string name) =>
        Resources.FindObjectsOfTypeAll<Transform>()
            .Where(t => t.name == name && t.gameObject.scene.IsValid())
            .Select(t => t.gameObject)
            .FirstOrDefault();

    static void BuildSecondaryButtons(Transform menuPanel, MainMenuManager manager)
    {
        // Directamente dentro de MenuPanel (se oculta con el menú). Antes se reubicaba conservando la
        // posición de mundo, que dependía del tamaño de pantalla del modo batch y la dejaba medio fuera.
        var row = UIFactory.Rect(menuPanel, SecondaryName, new Vector2(0.14f, 0.035f), new Vector2(0.86f, 0.105f));
        var entries = new (string label, UnityAction action)[]
        {
            ("ENTRENAMIENTO", manager.StartTraining),
            ("CASOS", manager.OpenCases),
            ("ESTADÍSTICAS", manager.OpenStats),
            ("CONFIGURACIÓN", manager.OpenSettings)
        };
        float span = 1f / entries.Length;
        for (int i = 0; i < entries.Length; i++)
        {
            var button = UIFactory.Button(row, $"Btn_{i}", entries[i].label, 26,
                                          new Color(0.04f, 0.06f, 0.14f, 0.88f), i % 2 == 0 ? Theme.Cyan : Theme.Magenta);
            var rect = (RectTransform)button.transform;
            rect.anchorMin = new Vector2(span * i + 0.008f, 0f);
            rect.anchorMax = new Vector2(span * (i + 1) - 0.008f, 1f);
            UnityEventTools.AddPersistentListener(button.onClick, entries[i].action);
        }
    }

    /// <summary>
    /// Botón CONTINUAR con el resumen de la partida guardada. Ocupa la franja libre entre
    /// SALIR y la fila de botones secundarios; <see cref="MainMenuManager"/> lo enciende
    /// solo si hay algo que retomar.
    /// </summary>
    static GameObject BuildContinueGroup(Transform menuPanel, MainMenuManager manager,
                                         out TextMeshProUGUI caption)
    {
        var group = UIFactory.Rect(menuPanel, ContinueName, new Vector2(0.2f, 0.11f), new Vector2(0.8f, 0.235f));

        var button = UIFactory.Button(group, "Btn_Continue", "CONTINUAR", 30,
                                      new Color(0.04f, 0.08f, 0.16f, 0.92f), Theme.Cyan);
        var rect = (RectTransform)button.transform;
        // Mismo ancho que JUGAR / CRÉDITOS / SALIR (500 px sobre los 2040 del panel).
        rect.anchorMin = new Vector2(0.3f, 0.45f);
        rect.anchorMax = new Vector2(0.7f, 1f);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        UnityEventTools.AddPersistentListener(button.onClick, manager.ContinueGame);

        caption = UIFactory.Text(group, "Caption", string.Empty, 20, Theme.Muted, TextAlignmentOptions.Top);
        caption.rectTransform.anchorMin = new Vector2(0f, 0f);
        caption.rectTransform.anchorMax = new Vector2(1f, 0.42f);
        caption.lineSpacing = 6;

        group.gameObject.SetActive(false);
        return group.gameObject;
    }

    static GameObject BuildInfoPanel(Transform canvas, string name, string title, MainMenuManager manager,
                                     out TextMeshProUGUI body)
    {
        var (root, window) = UIFactory.Modal(canvas, name, new Vector2(0.14f, 0.08f), new Vector2(0.86f, 0.92f));
        var heading = UIFactory.Text(window, "Title", title, 44, Theme.Cyan, TextAlignmentOptions.Top, true);
        heading.rectTransform.offsetMax = new Vector2(0, -28);

        var area = UIFactory.Rect(window, "BodyArea", new Vector2(0.04f, 0.16f), new Vector2(0.96f, 0.86f));
        var (_, text) = UIFactory.ScrollText(area, "Body", 28);
        text.lineSpacing = 12;
        body = text;

        var back = GameUIBuilder.PlacedButton(window, "Back", "VOLVER", 0.04f, 0.12f, 0.64f, 0.94f);
        UnityEventTools.AddPersistentListener(back.onClick, manager.BackToMenu);
        root.SetActive(false);
        return root;
    }

    static GameObject BuildSettingsPanel(Transform canvas, MainMenuManager manager,
                                         out Slider volume, out Slider speed, out Toggle fullscreen)
    {
        var (root, window) = UIFactory.Modal(canvas, "SettingsPanel", new Vector2(0.3f, 0.2f), new Vector2(0.7f, 0.8f));
        var heading = UIFactory.Text(window, "Title", "CONFIGURACIÓN", 44, Theme.Cyan, TextAlignmentOptions.Top, true);
        heading.rectTransform.offsetMax = new Vector2(0, -28);

        volume = LabeledSlider(window, "Volume", "VOLUMEN", 0.62f, 0f, 1f);
        speed = LabeledSlider(window, "TextSpeed", "VELOCIDAD DEL TEXTO", 0.42f, 10f, 120f);

        var toggleArea = UIFactory.Rect(window, "Fullscreen", new Vector2(0.1f, 0.26f), new Vector2(0.9f, 0.34f));
        fullscreen = UIFactory.Toggle(toggleArea, "Toggle", "PANTALLA COMPLETA");

        var back = GameUIBuilder.PlacedButton(window, "Back", "VOLVER", 0.06f, 0.17f, 0.3f, 0.7f);
        UnityEventTools.AddPersistentListener(back.onClick, manager.BackToMenu);
        root.SetActive(false);
        return root;
    }

    static Slider LabeledSlider(Transform window, string name, string label, float y, float min, float max)
    {
        var caption = UIFactory.Text(window, name + "Label", label, 26, Theme.Magenta, TextAlignmentOptions.BottomLeft, true);
        caption.rectTransform.anchorMin = new Vector2(0.1f, y + 0.08f);
        caption.rectTransform.anchorMax = new Vector2(0.9f, y + 0.14f);
        var area = UIFactory.Rect(window, name, new Vector2(0.1f, y), new Vector2(0.9f, y + 0.06f));
        return UIFactory.Slider(area, "Slider", min, max);
    }
}
