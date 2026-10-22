using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Pantalla de investigación (GDD 10 y 18.2): evidencia, herramientas, opinión de ADA,
/// clasificación, confianza, compartir y confirmación. La retroalimentación la muestra
/// <see cref="FeedbackPanel"/>.
/// </summary>
public class InvestigationPanel : MonoBehaviour
{
    static readonly string[] ToolLabels =
    {
        "ANALIZAR TEXTO", "VER FUENTE", "ANALIZAR PERFIL", "COMPROBAR ENLACE", "METADATOS"
    };

    public GameObject root;

    [Header("Evidencia")]
    public TextMeshProUGUI headerText;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI sourceText;
    public TextMeshProUGUI bodyText;
    public Image evidenceImage;

    [Header("Herramientas")]
    public Button[] toolButtons = Array.Empty<Button>();
    public Button adaButton;
    public TextMeshProUGUI toolLog;
    public ScrollRect toolLogScroll;

    [Header("Decisión")]
    public ChoiceGroup verdictGroup = new ChoiceGroup();
    public ChoiceGroup confidenceGroup = new ChoiceGroup();
    public GameObject shareRow;
    public ChoiceGroup shareGroup = new ChoiceGroup();
    public Button confirmButton;
    public TextMeshProUGUI statusText;

    public FeedbackPanel feedback;

    [Header("Escaneo")]
    public GameObject scanRoot;
    public RectTransform scanFill;
    public TextMeshProUGUI scanLabel;
    public float scanSeconds = 0.55f;

    readonly HashSet<InvestigationTool> usedTools = new HashSet<InvestigationTool>();
    bool scanning;
    readonly StringBuilder log = new StringBuilder();
    CaseData current;
    Action<CaseOutcome> onClosed;
    float openedAt;
    bool resolved;

    public bool IsOpen => root != null && root.activeSelf;

    void Awake()
    {
        if (root != null) root.SetActive(false);
        for (int i = 0; i < toolButtons.Length; i++)
        {
            var tool = (InvestigationTool)i;
            toolButtons[i].onClick.AddListener(() => UseTool(tool));
        }
        adaButton.onClick.AddListener(AskAda);
        confirmButton.onClick.AddListener(Confirm);
        verdictGroup.Bind(_ => RefreshConfirm());
        confidenceGroup.Bind(_ => RefreshConfirm());
        shareGroup.Bind(_ => RefreshConfirm());
    }

    public void Open(CaseData data, Action<CaseOutcome> closed)
    {
        StopAllCoroutines();
        current = data;
        onClosed = closed;
        openedAt = Time.unscaledTime;
        usedTools.Clear();
        log.Clear();

        headerText.text = $"{data.caseId}  ·  NIVEL {data.level}  ·  {ContentLabel(data.contentType)}";
        titleText.text = data.title;
        sourceText.text = data.sourceLabel;
        bodyText.text = data.contentBody;
        bool hasImage = data.evidenceSprite != null;
        evidenceImage.gameObject.SetActive(hasImage);
        evidenceImage.sprite = data.evidenceSprite;
        evidenceImage.preserveAspect = true;
        // Con imagen, el texto ocupa la franja inferior; sin imagen, toda la tarjeta.
        bodyText.rectTransform.anchorMax = new Vector2(1f, hasImage ? 0.29f : 0.8f);

        resolved = false;
        scanning = false;
        if (scanRoot != null) scanRoot.SetActive(false);
        SetDecisionInteractable(true);
        verdictGroup.Clear();
        confidenceGroup.Clear();
        shareGroup.Clear();
        shareRow.SetActive(data.canShare);
        AppendLog("<color=#22E4F0>» Selecciona herramientas para obtener evidencias antes de decidir.</color>");

        root.SetActive(true);
        InputGate.Lock(this);
        SfxPlayer.Play(Sfx.Scan);
        RefreshConfirm();
    }

    void SetDecisionInteractable(bool value)
    {
        foreach (var button in toolButtons) button.interactable = value;
        adaButton.interactable = value;
        verdictGroup.SetInteractable(value);
        confidenceGroup.SetInteractable(value);
        shareGroup.SetInteractable(value);
        confirmButton.interactable = false;
    }

    void UseTool(InvestigationTool tool)
    {
        if (current == null || resolved || scanning || usedTools.Contains(tool)) return;
        usedTools.Add(tool);
        toolButtons[(int)tool].interactable = false;
        StartCoroutine(ScanThenReport(tool, current));
    }

    /// <summary>Animación breve de escaneo (GDD 19) y después el resultado de la herramienta.</summary>
    IEnumerator ScanThenReport(InvestigationTool tool, CaseData data)
    {
        scanning = true;
        // Mientras se escanea, las demás herramientas se atenúan (antes sus clics se perdían sin aviso).
        foreach (var button in toolButtons) button.interactable = false;
        SfxPlayer.Play(Sfx.Scan);
        if (scanRoot != null && scanFill != null)
        {
            scanRoot.SetActive(true);
            if (scanLabel != null) scanLabel.text = $"ANALIZANDO · {ToolLabels[(int)tool]}";
            for (float t = 0f; t < scanSeconds; t += Time.unscaledDeltaTime)
            {
                scanFill.anchorMax = new Vector2(t / scanSeconds, 1f);
                yield return null;
            }
            scanRoot.SetActive(false);
        }
        scanning = false;
        if (current != data) yield break;
        for (int i = 0; i < toolButtons.Length; i++)
        {
            toolButtons[i].interactable = !resolved && !usedTools.Contains((InvestigationTool)i);
        }

        var finding = data.FindingFor(tool);
        string result = finding != null && !string.IsNullOrWhiteSpace(finding.result)
            ? finding.result
            : "Sin datos concluyentes para este contenido. Una herramienta no siempre aporta pistas.";
        AppendLog($"<color=#FF3FD1>► {ToolLabels[(int)tool]}</color>\n{result}");
    }

    void AskAda()
    {
        if (current == null) return;
        adaButton.interactable = false;
        SfxPlayer.Play(Sfx.Click);
        string verdict = ScoreRules.VerdictLabel(current.adaSuggestion);
        AppendLog($"<color=#22E4F0>► ADA RECOMIENDA: {verdict}</color>\n{current.adaHint}\n" +
                  "<i><color=#8FA3BF>Recuerda: ADA es una herramienta, no una autoridad. Verifica.</color></i>");
    }

    void AppendLog(string entry)
    {
        if (log.Length > 0) log.Append("\n\n");
        log.Append(entry);
        toolLog.text = log.ToString();
        Canvas.ForceUpdateCanvases();
        if (toolLogScroll != null) toolLogScroll.verticalNormalizedPosition = 0f;
    }

    void RefreshConfirm()
    {
        bool needsShare = current != null && current.canShare;
        bool ready = verdictGroup.HasSelection && confidenceGroup.HasSelection
                     && (!needsShare || shareGroup.HasSelection);
        confirmButton.interactable = ready;
        statusText.text = ready ? "Listo para confirmar el veredicto."
            : !verdictGroup.HasSelection ? "1. Elige una clasificación."
            : !confidenceGroup.HasSelection ? "2. ¿Qué tan seguro estás?"
            : "3. ¿Compartirías este contenido?";
    }

    void Confirm()
    {
        if (current == null || resolved || !confirmButton.interactable) return;
        // Bloquea la decisión para que Enter o un doble clic no cuenten el caso dos veces.
        resolved = true;
        SetDecisionInteractable(false);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        var chosen = (Verdict)verdictGroup.Selected;
        var confidence = (ConfidenceLevel)confidenceGroup.Selected;
        bool shared = current.canShare && shareGroup.Selected == 0;

        int relevantTools = 0;
        foreach (var tool in usedTools)
        {
            var finding = current.FindingFor(tool);
            if (finding != null && finding.isRelevant) relevantTools++;
        }

        int reputationBefore = GameSession.Reputation;
        var outcome = ScoreRules.Evaluate(current.correctVerdict, chosen, confidence,
                                          relevantTools, current.canShare, shared);
        GameSession.ApplyOutcome(outcome);
        GameSession.RecordCase(BuildRecord(chosen, confidence, shared, outcome, reputationBefore));

        SfxPlayer.Play(outcome.IsCorrect ? Sfx.Correct : Sfx.Wrong);
        feedback.Show(current, chosen, confidence, relevantTools, outcome, () => Close(outcome));
    }

    CaseRecord BuildRecord(Verdict chosen, ConfidenceLevel confidence, bool shared,
                           CaseOutcome outcome, int reputationBefore)
    {
        var record = new CaseRecord
        {
            caseId = current.caseId,
            level = current.level,
            answer = chosen.ToString(),
            correctAnswer = current.correctVerdict.ToString(),
            confidence = ScoreRules.ConfidenceLabel(confidence),
            points = outcome.Points,
            reputationBefore = reputationBefore,
            reputationAfter = GameSession.Reputation,
            shared = shared,
            seconds = Time.unscaledTime - openedAt,
            correct = outcome.IsCorrect
        };
        foreach (var tool in usedTools) record.toolsUsed.Add(tool.ToString());
        return record;
    }

    void Close(CaseOutcome outcome)
    {
        root.SetActive(false);
        InputGate.Unlock(this);
        var callback = onClosed;
        onClosed = null;
        current = null;
        callback?.Invoke(outcome);
    }

    static string ContentLabel(ContentType type)
    {
        switch (type)
        {
            case ContentType.Email: return "CORREO";
            case ContentType.Image: return "IMAGEN";
            case ContentType.SocialPost: return "RED SOCIAL";
            case ContentType.Audio: return "AUDIO";
            case ContentType.Video: return "VIDEO";
            case ContentType.Multimodal: return "CASO MULTIMODAL";
            default: return "TEXTO";
        }
    }
}
