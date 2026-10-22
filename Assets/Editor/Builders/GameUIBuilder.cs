using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Construye el Canvas de GameScene: HUD, diálogo, investigación, pausa y fin.</summary>
public static class GameUIBuilder
{
    const string CanvasName = "GameUI";

    public struct Refs
    {
        public HUDController Hud;
        public DialogueController Dialogue;
        public InvestigationPanel Investigation;
        public EndScreen End;
    }

    public static Refs Build()
    {
        foreach (var old in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (old.name == CanvasName) Object.DestroyImmediate(old);
        }

        var canvas = UIFactory.Canvas(CanvasName, 10);
        var root = canvas.transform;
        var refs = new Refs
        {
            Hud = BuildHud(root),
            Investigation = InvestigationUIBuilder.Build(root),
            Dialogue = BuildDialogue(root),
            End = BuildEnd(root)
        };
        BuildPause(root);
        HideInEditor(root);
        return refs;
    }

    /// <summary>Deja ocultos los paneles modales para que la escena se vea limpia en el Editor.</summary>
    static void HideInEditor(Transform root)
    {
        foreach (var path in new[] { "Investigation/Root/Window/Feedback/Root", "Investigation/Root", "Dialogue/Box", "Pause/Root", "End/Root", "HUD/Prompt" })
        {
            var child = root.Find(path);
            if (child != null) child.gameObject.SetActive(false);
        }
    }

    static HUDController BuildHud(Transform root)
    {
        var hudRect = UIFactory.Stretch(root, "HUD");
        var hud = hudRect.gameObject.AddComponent<HUDController>();

        var bar = UIFactory.Rect(hudRect, "TopBar", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -78), Vector2.zero);
        UIFactory.Panel(bar, new Color(0.03f, 0.05f, 0.1f, 0.88f), Theme.Cyan, false);

        var title = UIFactory.Text(bar, "Title", "<color=#22E4F0>AI</color> OR <color=#FF3FD1>HUMAN?</color>", 34, Theme.Text,
                                   TextAlignmentOptions.MidlineLeft, true);
        Anchor(title.rectTransform, 0.01f, 0.2f);
        hud.caseText = HudStat(bar, "Case", "CASO 01/11", 0.21f, 0.33f);
        hud.levelText = HudStat(bar, "Level", "NIVEL 1/7", 0.33f, 0.44f);
        hud.scoreText = HudStat(bar, "Score", "PUNTOS 0000", 0.44f, 0.58f);
        hud.fragmentsText = HudStat(bar, "Fragments", "DATOS 0/8", 0.58f, 0.68f);

        var repLabel = UIFactory.Text(bar, "ReputationLabel", "REPUTACIÓN DIGITAL 75%", 20, Theme.Text, TextAlignmentOptions.BottomLeft, true);
        repLabel.rectTransform.anchorMin = new Vector2(0.69f, 0.5f);
        repLabel.rectTransform.anchorMax = new Vector2(0.99f, 1f);
        repLabel.rectTransform.offsetMin = new Vector2(0, 0);
        repLabel.rectTransform.offsetMax = new Vector2(0, -6);
        hud.reputationText = repLabel;

        var repBack = UIFactory.Rect(bar, "ReputationBar", new Vector2(0.69f, 0.18f), new Vector2(0.99f, 0.46f));
        UIFactory.Panel(repBack, new Color(1, 1, 1, 0.12f));
        var fill = UIFactory.Stretch(repBack, "Fill");
        var fillImage = UIFactory.Panel(fill, Theme.Cyan, null, false);
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillAmount = 0.75f;
        hud.reputationFill = fillImage;

        var objective = UIFactory.Rect(hudRect, "Objective", new Vector2(0, 1), new Vector2(0.6f, 1), new Vector2(16, -128), new Vector2(0, -84));
        UIFactory.Panel(objective, new Color(0.03f, 0.05f, 0.1f, 0.7f), null);
        hud.objectiveText = UIFactory.Text(objective, "Text", "Objetivo: terminal marcada", 22, Theme.Cyan, TextAlignmentOptions.MidlineLeft);
        hud.objectiveText.rectTransform.offsetMin = new Vector2(14, 0);

        var prompt = UIFactory.Rect(hudRect, "Prompt", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-260, 340), new Vector2(260, 400));
        UIFactory.Panel(prompt, new Color(0.03f, 0.05f, 0.1f, 0.9f), Theme.Magenta);
        prompt.gameObject.AddComponent<PanelAppear>();
        hud.promptRoot = prompt.gameObject;
        hud.promptText = UIFactory.Text(prompt, "Text", "[E]  Analizar evidencia", 28, Theme.Text, TextAlignmentOptions.Center, true);

        var floating = UIFactory.Text(hudRect, "FloatingPoints", "+100", 54, Theme.Cyan, TextAlignmentOptions.Center, true);
        floating.rectTransform.anchorMin = floating.rectTransform.anchorMax = new Vector2(0.51f, 0.82f);
        floating.rectTransform.sizeDelta = new Vector2(400, 80);
        hud.floatingText = floating;

        // Aviso de autoguardado: bajo la barra superior, a la derecha (el panel de objetivo ocupa la izquierda).
        var autosave = UIFactory.Text(hudRect, "Autosave", "PARTIDA GUARDADA", 20, hud.goodColor,
                                      TextAlignmentOptions.MidlineRight, true);
        autosave.rectTransform.anchorMin = new Vector2(0.6f, 1f);
        autosave.rectTransform.anchorMax = new Vector2(1f, 1f);
        autosave.rectTransform.offsetMin = new Vector2(0, -124);
        autosave.rectTransform.offsetMax = new Vector2(-16, -84);
        autosave.gameObject.SetActive(false);
        hud.autosaveText = autosave;
        return hud;
    }

    static TextMeshProUGUI HudStat(Transform bar, string name, string text, float x0, float x1)
    {
        var tmp = UIFactory.Text(bar, name, text, 24, Theme.Text, TextAlignmentOptions.Center, true);
        Anchor(tmp.rectTransform, x0, x1);
        return tmp;
    }

    static void Anchor(RectTransform rect, float x0, float x1)
    {
        rect.anchorMin = new Vector2(x0, 0);
        rect.anchorMax = new Vector2(x1, 1);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    static DialogueController BuildDialogue(Transform root)
    {
        var holder = UIFactory.Stretch(root, "Dialogue");
        var dialogue = holder.gameObject.AddComponent<DialogueController>();
        var box = UIFactory.Rect(holder, "Box", new Vector2(0.04f, 0.03f), new Vector2(0.96f, 0.3f));
        UIFactory.Panel(box, Theme.Panel, Theme.Cyan);
        box.gameObject.AddComponent<PanelAppear>();
        dialogue.root = box.gameObject;

        var frame = UIFactory.Rect(box, "PortraitFrame", new Vector2(0, 0), new Vector2(0, 1), new Vector2(18, 18), new Vector2(278, -18));
        UIFactory.Panel(frame, Color.black, Theme.Magenta);
        frame.gameObject.AddComponent<RectMask2D>();
        var portraitRect = UIFactory.Rect(frame, "Portrait", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        portraitRect.sizeDelta = new Vector2(760, 414);
        portraitRect.anchoredPosition = new Vector2(20, -50);
        var portrait = portraitRect.gameObject.AddComponent<Image>();
        portrait.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BuildPaths.Aria);
        portrait.raycastTarget = false;
        dialogue.portrait = portrait;
        dialogue.adaPortrait = portrait.sprite;

        var mask = UIFactory.Stretch(frame, "PhantomMask");
        UIFactory.Panel(mask, new Color(0.06f, 0.02f, 0.08f, 1f), null, false);
        UIFactory.Text(mask, "Glyph", "?", 150, Theme.Magenta, TextAlignmentOptions.Center, true);
        mask.gameObject.SetActive(false);
        dialogue.phantomMask = mask.gameObject;

        dialogue.nameText = UIFactory.Text(box, "Name", "ADA · ASISTENTE IA", 28, Theme.Cyan, TextAlignmentOptions.TopLeft, true);
        dialogue.nameText.rectTransform.offsetMin = new Vector2(300, 0);
        dialogue.nameText.rectTransform.offsetMax = new Vector2(-30, -18);

        dialogue.bodyText = UIFactory.Text(box, "Body", "...", 30, Theme.Text);
        dialogue.bodyText.rectTransform.offsetMin = new Vector2(300, 50);
        dialogue.bodyText.rectTransform.offsetMax = new Vector2(-30, -64);

        dialogue.continueHint = UIFactory.Text(box, "Continue", "E / CLIC PARA CONTINUAR  >>", 18, Theme.Muted, TextAlignmentOptions.BottomRight, true);
        dialogue.continueHint.rectTransform.offsetMax = new Vector2(-24, 0);
        dialogue.continueHint.rectTransform.offsetMin = new Vector2(0, 14);
        return dialogue;
    }

    static void BuildPause(Transform root)
    {
        var holder = UIFactory.Stretch(root, "Pause");
        var pause = holder.gameObject.AddComponent<PauseMenu>();
        var (modal, window) = UIFactory.Modal(holder, "Root", new Vector2(0.36f, 0.32f), new Vector2(0.64f, 0.68f));
        pause.root = modal;
        var title = UIFactory.Text(window, "Title", "PAUSA", 48, Theme.Cyan, TextAlignmentOptions.Top, true);
        title.rectTransform.offsetMax = new Vector2(0, -30);
        pause.resumeButton = PlacedButton(window, "Resume", "CONTINUAR", 0.48f, 0.66f);
        pause.menuButton = PlacedButton(window, "Menu", "MENÚ PRINCIPAL", 0.2f, 0.38f);
    }

    static EndScreen BuildEnd(Transform root)
    {
        var holder = UIFactory.Stretch(root, "End");
        var end = holder.gameObject.AddComponent<EndScreen>();
        var (modal, window) = UIFactory.Modal(holder, "Root", new Vector2(0.27f, 0.12f), new Vector2(0.73f, 0.88f));
        end.root = modal;
        end.titleText = UIFactory.Text(window, "Title", "INVESTIGACIÓN COMPLETADA", 40, Theme.Magenta, TextAlignmentOptions.Top, true);
        end.titleText.rectTransform.offsetMax = new Vector2(-20, -36);
        end.summaryText = UIFactory.Text(window, "Summary", "...", 32, Theme.Text, TextAlignmentOptions.Center);
        end.summaryText.rectTransform.anchorMin = new Vector2(0.06f, 0.26f);
        end.summaryText.rectTransform.anchorMax = new Vector2(0.94f, 0.86f);
        end.retryButton = PlacedButton(window, "Retry", "REINTENTAR", 0.06f, 0.17f, 0.08f, 0.48f);
        end.menuButton = PlacedButton(window, "Menu", "MENÚ PRINCIPAL", 0.06f, 0.17f, 0.52f, 0.92f);
        return end;
    }

    public static Button PlacedButton(Transform parent, string name, string label, float y0, float y1,
                                      float x0 = 0.15f, float x1 = 0.85f)
    {
        var button = UIFactory.Button(parent, name, label, 30);
        var rect = (RectTransform)button.transform;
        rect.anchorMin = new Vector2(x0, y0);
        rect.anchorMax = new Vector2(x1, y1);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return button;
    }
}
