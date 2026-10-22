using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Retroalimentación tras cada decisión (GDD 11.5, RF-10).</summary>
public class FeedbackPanel : MonoBehaviour
{
    public GameObject root;
    public Image banner;
    public TextMeshProUGUI resultTitle;
    public TextMeshProUGUI resultDetail;
    public TextMeshProUGUI explanationText;
    public Button continueButton;

    public Color correctColor = new Color(0.27f, 0.95f, 0.55f);
    public Color wrongColor = new Color(1f, 0.3f, 0.42f);

    Action onContinue;

    void Awake()
    {
        if (root != null) root.SetActive(false);
        continueButton.onClick.AddListener(Continue);
    }

    public void Show(CaseData data, Verdict chosen, ConfidenceLevel confidence, int relevantTools,
                     CaseOutcome outcome, Action continued)
    {
        onContinue = continued;
        Color color = outcome.IsCorrect ? correctColor : wrongColor;
        banner.color = new Color(color.r, color.g, color.b, 0.9f);
        resultTitle.text = outcome.IsCorrect ? "VEREDICTO CORRECTO" : "VEREDICTO INCORRECTO";

        string sign = outcome.Points >= 0 ? "+" : string.Empty;
        string repSign = outcome.ReputationDelta >= 0 ? "+" : string.Empty;
        string detail =
            $"Tu respuesta: <b>{ScoreRules.VerdictLabel(chosen)}</b> ({ScoreRules.ConfidenceLabel(confidence)} de confianza)\n" +
            $"Respuesta correcta: <b>{ScoreRules.VerdictLabel(data.correctVerdict)}</b>\n" +
            $"Herramientas útiles: {relevantTools} (+{relevantTools * ScoreRules.ToolBonus})\n" +
            $"Puntos: <b>{sign}{outcome.Points}</b>   ·   Reputación: <b>{repSign}{outcome.ReputationDelta}%</b>";
        if (outcome.SharedFakeContent)
        {
            detail += "\n<color=#FF4D6A>Compartiste contenido falso: la desinformación se propagó (-100).</color>";
        }
        if (data.IsAdaWrong)
        {
            detail += "\n<color=#FF9A3C>ADA se equivocó en este caso. Verificar siempre vale la pena.</color>";
        }
        resultDetail.text = detail;
        explanationText.text = data.feedbackExplanation;
        root.SetActive(true);
    }

    void Continue()
    {
        SfxPlayer.Play(Sfx.Click);
        root.SetActive(false);
        var callback = onContinue;
        onContinue = null;
        callback?.Invoke();
    }
}
