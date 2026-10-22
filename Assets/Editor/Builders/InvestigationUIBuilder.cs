using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Construye la pantalla de investigación y su panel de retroalimentación.</summary>
public static class InvestigationUIBuilder
{
    static readonly string[] ToolNames =
    {
        "ANALIZAR TEXTO", "VER FUENTE", "ANALIZAR PERFIL", "COMPROBAR ENLACE", "ANALIZAR METADATOS"
    };

    public static InvestigationPanel Build(Transform canvas)
    {
        var holder = UIFactory.Stretch(canvas, "Investigation");
        var panel = holder.gameObject.AddComponent<InvestigationPanel>();
        var (modal, window) = UIFactory.Modal(holder, "Root", new Vector2(0.025f, 0.03f), new Vector2(0.975f, 0.97f));
        panel.root = modal;

        BuildHeader(panel, window);
        BuildEvidence(panel, window);
        BuildTools(panel, window);
        BuildDecision(panel, window);
        panel.feedback = BuildFeedback(window);
        return panel;
    }

    static RectTransform Area(Transform parent, string name, float x0, float y0, float x1, float y1, bool framed = true)
    {
        var rect = UIFactory.Rect(parent, name, new Vector2(x0, y0), new Vector2(x1, y1));
        if (framed) UIFactory.Panel(rect, new Color(0.06f, 0.09f, 0.17f, 0.95f), new Color(0.13f, 0.89f, 0.94f, 0.35f));
        return rect;
    }

    static TextMeshProUGUI Label(Transform parent, string name, string text, float y0, float y1, float size = 22f)
    {
        var tmp = UIFactory.Text(parent, name, text, size, Theme.Magenta, TextAlignmentOptions.MidlineLeft, true);
        tmp.rectTransform.anchorMin = new Vector2(0, y0);
        tmp.rectTransform.anchorMax = new Vector2(1, y1);
        tmp.rectTransform.offsetMin = new Vector2(14, 0);
        tmp.rectTransform.offsetMax = new Vector2(-14, 0);
        return tmp;
    }

    static void BuildHeader(InvestigationPanel panel, RectTransform window)
    {
        var header = Area(window, "Header", 0.015f, 0.885f, 0.985f, 0.98f, false);
        panel.headerText = UIFactory.Text(header, "Meta", "C-101 · NIVEL 1 · TEXTO", 22, Theme.Cyan, TextAlignmentOptions.TopLeft, true);
        panel.titleText = UIFactory.Text(header, "Title", "Título del caso", 40, Theme.Text, TextAlignmentOptions.BottomLeft, true);
        var hint = UIFactory.Text(header, "Method", "OBSERVAR · INVESTIGAR · ANALIZAR · DECIDIR", 18, Theme.Muted, TextAlignmentOptions.TopRight, true);
        hint.rectTransform.offsetMax = new Vector2(-6, 0);
    }

    static void BuildEvidence(InvestigationPanel panel, RectTransform window)
    {
        var card = Area(window, "Evidence", 0.015f, 0.33f, 0.575f, 0.87f);
        Label(card, "Caption", "EVIDENCIA", 0.9f, 0.99f);
        panel.sourceText = UIFactory.Text(card, "Source", "Fuente", 22, Theme.Orange, TextAlignmentOptions.TopLeft);
        panel.sourceText.rectTransform.anchorMin = new Vector2(0, 0.8f);
        panel.sourceText.rectTransform.anchorMax = new Vector2(1, 0.9f);
        panel.sourceText.rectTransform.offsetMin = new Vector2(16, 0);
        panel.sourceText.rectTransform.offsetMax = new Vector2(-16, 0);

        var imageRect = UIFactory.Rect(card, "Image", new Vector2(0.03f, 0.3f), new Vector2(0.97f, 0.8f));
        var image = imageRect.gameObject.AddComponent<Image>();
        image.preserveAspect = true;
        image.raycastTarget = false;
        panel.evidenceImage = image;

        panel.bodyText = UIFactory.Text(card, "Body", "Contenido", 26, Theme.Text, TextAlignmentOptions.TopLeft);
        panel.bodyText.rectTransform.anchorMin = new Vector2(0, 0.02f);
        panel.bodyText.rectTransform.anchorMax = new Vector2(1, 0.8f);
        panel.bodyText.rectTransform.offsetMin = new Vector2(20, 0);
        panel.bodyText.rectTransform.offsetMax = new Vector2(-20, 0);
        panel.bodyText.enableAutoSizing = true;
        panel.bodyText.fontSizeMin = 16;
        panel.bodyText.fontSizeMax = 28;
    }

    static void BuildTools(InvestigationPanel panel, RectTransform window)
    {
        var tools = Area(window, "Tools", 0.59f, 0.33f, 0.985f, 0.87f);
        Label(tools, "Caption", "HERRAMIENTAS DE INVESTIGACIÓN", 0.9f, 0.99f);

        panel.toolButtons = new Button[ToolNames.Length];
        for (int i = 0; i <= ToolNames.Length; i++)
        {
            int row = i / 2, col = i % 2;
            float y1 = 0.89f - row * 0.1f, y0 = y1 - 0.085f;
            float x0 = col == 0 ? 0.03f : 0.515f, x1 = col == 0 ? 0.485f : 0.97f;
            bool isAda = i == ToolNames.Length;
            var button = UIFactory.Button(tools, isAda ? "AskAda" : $"Tool_{i}", isAda ? "PREGUNTAR A ADA" : ToolNames[i], 20,
                                          null, isAda ? Theme.Magenta : Theme.Cyan);
            var rect = (RectTransform)button.transform;
            rect.anchorMin = new Vector2(x0, y0);
            rect.anchorMax = new Vector2(x1, y1);
            if (isAda) panel.adaButton = button;
            else panel.toolButtons[i] = button;
        }

        var logArea = UIFactory.Rect(tools, "LogArea", new Vector2(0.03f, 0.03f), new Vector2(0.97f, 0.58f));
        UIFactory.Panel(logArea, new Color(0f, 0f, 0f, 0.35f), null);
        var (scroll, text) = UIFactory.ScrollText(logArea, "Log", 22);
        panel.toolLogScroll = scroll;
        panel.toolLog = text;

        // Ventana de "escaneo" que aparece mientras una herramienta analiza la evidencia.
        var scan = UIFactory.Rect(logArea, "Scan", new Vector2(0.08f, 0.36f), new Vector2(0.92f, 0.64f));
        UIFactory.Panel(scan, new Color(0.05f, 0.03f, 0.1f, 0.96f), Theme.Magenta);
        panel.scanLabel = UIFactory.Text(scan, "Label", "ANALIZANDO...", 22, Theme.Magenta, TextAlignmentOptions.Top, true);
        panel.scanLabel.rectTransform.offsetMax = new Vector2(-10, -10);
        var bar = UIFactory.Rect(scan, "Bar", new Vector2(0.06f, 0.18f), new Vector2(0.94f, 0.42f));
        UIFactory.Panel(bar, new Color(1f, 1f, 1f, 0.1f), null, false);
        var fill = UIFactory.Rect(bar, "Fill", Vector2.zero, new Vector2(0f, 1f));
        UIFactory.Panel(fill, Theme.Cyan, null, false);
        panel.scanFill = fill;
        panel.scanRoot = scan.gameObject;
        scan.gameObject.SetActive(false);
    }

    static void BuildDecision(InvestigationPanel panel, RectTransform window)
    {
        var area = Area(window, "Decision", 0.015f, 0.02f, 0.985f, 0.315f);

        panel.verdictGroup.buttons = Row(area, "Verdict", "CLASIFICACIÓN",
            new[] { "HUMANO", "IA", "MANIPULADO" }, 0.68f, 0.96f, 0.68f);
        panel.confidenceGroup.buttons = Row(area, "Confidence", "CONFIANZA",
            new[] { "BAJA · 50%", "MEDIA · 70%", "ALTA · 90%" }, 0.36f, 0.64f, 0.68f);

        var share = UIFactory.Rect(area, "ShareRow", new Vector2(0, 0.04f), new Vector2(0.68f, 0.32f));
        panel.shareRow = share.gameObject;
        panel.shareGroup.buttons = Row(share, "Share", "¿COMPARTIR?", new[] { "SÍ, COMPARTIR", "NO COMPARTIR" }, 0f, 1f, 1f);

        panel.statusText = UIFactory.Text(area, "Status", "1. Elige una clasificación.", 22, Theme.Orange, TextAlignmentOptions.Center);
        panel.statusText.rectTransform.anchorMin = new Vector2(0.7f, 0.62f);
        panel.statusText.rectTransform.anchorMax = new Vector2(0.99f, 0.96f);

        panel.confirmButton = UIFactory.Button(area, "Confirm", "CONFIRMAR VEREDICTO", 30, new Color(0.12f, 0.05f, 0.16f, 0.95f), Theme.Magenta);
        var rect = (RectTransform)panel.confirmButton.transform;
        rect.anchorMin = new Vector2(0.71f, 0.12f);
        rect.anchorMax = new Vector2(0.985f, 0.56f);
    }

    /// <summary>Fila con etiqueta a la izquierda y botones repartidos en el ancho restante.</summary>
    static Button[] Row(Transform parent, string name, string caption, string[] labels, float y0, float y1, float width)
    {
        var row = UIFactory.Rect(parent, name, new Vector2(0, y0), new Vector2(width, y1));
        var label = UIFactory.Text(row, "Caption", caption, 22, Theme.Magenta, TextAlignmentOptions.MidlineLeft, true);
        label.rectTransform.anchorMax = new Vector2(0.24f, 1);
        label.rectTransform.offsetMin = new Vector2(18, 0);

        var buttons = new Button[labels.Length];
        float start = 0.25f, span = 0.74f / labels.Length;
        for (int i = 0; i < labels.Length; i++)
        {
            buttons[i] = UIFactory.Button(row, $"Option_{i}", labels[i], 24);
            var rect = (RectTransform)buttons[i].transform;
            rect.anchorMin = new Vector2(start + span * i + 0.006f, 0.08f);
            rect.anchorMax = new Vector2(start + span * (i + 1) - 0.006f, 0.92f);
        }
        return buttons;
    }

    static FeedbackPanel BuildFeedback(RectTransform window)
    {
        var holder = UIFactory.Stretch(window, "Feedback");
        var feedback = holder.gameObject.AddComponent<FeedbackPanel>();
        var (modal, box) = UIFactory.Modal(holder, "Root", new Vector2(0.14f, 0.07f), new Vector2(0.86f, 0.93f));
        feedback.root = modal;

        var banner = UIFactory.Rect(box, "Banner", new Vector2(0, 0.86f), new Vector2(1, 1));
        feedback.banner = UIFactory.Panel(banner, Theme.Cyan);
        feedback.resultTitle = UIFactory.Text(banner, "Title", "VEREDICTO CORRECTO", 46, new Color(0.02f, 0.03f, 0.07f), TextAlignmentOptions.Center, true);

        feedback.resultDetail = UIFactory.Text(box, "Detail", "...", 26, Theme.Text, TextAlignmentOptions.TopLeft);
        feedback.resultDetail.rectTransform.anchorMin = new Vector2(0, 0.55f);
        feedback.resultDetail.rectTransform.anchorMax = new Vector2(1, 0.84f);
        feedback.resultDetail.rectTransform.offsetMin = new Vector2(40, 0);
        feedback.resultDetail.rectTransform.offsetMax = new Vector2(-40, 0);

        var explanationBox = UIFactory.Rect(box, "ExplanationBox", new Vector2(0.03f, 0.17f), new Vector2(0.97f, 0.53f));
        UIFactory.Panel(explanationBox, new Color(0.06f, 0.09f, 0.17f, 0.95f), new Color(0.13f, 0.89f, 0.94f, 0.35f));
        var caption = UIFactory.Text(explanationBox, "Caption", "¿POR QUÉ?", 22, Theme.Magenta, TextAlignmentOptions.TopLeft, true);
        caption.rectTransform.offsetMin = new Vector2(20, 0);
        caption.rectTransform.offsetMax = new Vector2(-20, -12);
        feedback.explanationText = UIFactory.Text(explanationBox, "Text", "...", 26, Theme.Text, TextAlignmentOptions.TopLeft);
        feedback.explanationText.rectTransform.offsetMin = new Vector2(20, 12);
        feedback.explanationText.rectTransform.offsetMax = new Vector2(-20, -50);
        feedback.explanationText.enableAutoSizing = true;
        feedback.explanationText.fontSizeMin = 16;
        feedback.explanationText.fontSizeMax = 26;

        feedback.continueButton = GameUIBuilder.PlacedButton(box, "Continue", "CONTINUAR", 0.03f, 0.13f, 0.32f, 0.68f);
        return feedback;
    }
}
