using System;
using UnityEngine;

/// <summary>
/// Datos de un caso de investigación (GDD 21.1). Cada caso es un asset independiente
/// para poder ampliar el banco de casos sin tocar código (RF-14).
/// </summary>
[CreateAssetMenu(fileName = "NewCaseData", menuName = "AI Game/Case Data")]
public class CaseData : ScriptableObject
{
    [Header("Identificación")]
    public string caseId = "C-000";
    [Range(1, 7)] public int level = 1;
    public ContentType contentType = ContentType.Text;
    public string title = "Caso sin título";

    [Header("Contenido a analizar")]
    public string sourceLabel = "Remitente / fuente";
    [TextArea(4, 12)] public string contentBody;
    public Sprite evidenceSprite;

    [Header("Herramientas de investigación")]
    public ToolFinding[] findings = Array.Empty<ToolFinding>();

    [Header("Veredicto")]
    public Verdict correctVerdict = Verdict.Human;
    [Tooltip("Si es true, el jugador decide si comparte el contenido (GDD 11.4).")]
    public bool canShare;

    [Header("ADA")]
    [Tooltip("Lo que ADA recomienda. Si difiere del veredicto correcto, ADA se equivoca a propósito.")]
    public Verdict adaSuggestion = Verdict.Human;
    [TextArea(2, 5)] public string adaHint;

    [Header("Narrativa")]
    public Speaker introSpeaker = Speaker.Ada;
    [TextArea(2, 6)] public string introDialogue;

    [Header("Retroalimentación pedagógica")]
    [TextArea(3, 10)] public string feedbackExplanation;

    public bool IsAdaWrong => adaSuggestion != correctVerdict;

    public ToolFinding FindingFor(InvestigationTool tool)
    {
        foreach (var finding in findings)
        {
            if (finding != null && finding.tool == tool) return finding;
        }
        return null;
    }
}

[Serializable]
public class ToolFinding
{
    public InvestigationTool tool;
    [TextArea(2, 6)] public string result;
    [Tooltip("Si la herramienta aporta una pista útil para este caso (+25 puntos).")]
    public bool isRelevant;
}
