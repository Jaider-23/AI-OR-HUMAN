using UnityEngine;

/// <summary>Resultado inmutable de evaluar la decisión del jugador en un caso.</summary>
public readonly struct CaseOutcome
{
    public readonly bool IsCorrect;
    public readonly int Points;
    public readonly int ReputationDelta;
    public readonly bool SharedFakeContent;

    public CaseOutcome(bool isCorrect, int points, int reputationDelta, bool sharedFakeContent)
    {
        IsCorrect = isCorrect;
        Points = points;
        ReputationDelta = reputationDelta;
        SharedFakeContent = sharedFakeContent;
    }
}

/// <summary>
/// Reglas de puntuación y reputación del GDD (secciones 12, 13 y 14).
/// Lógica pura y sin estado para poder probarla de forma aislada.
/// </summary>
public static class ScoreRules
{
    public const int ToolBonus = 25;
    public const int ShareFakePenalty = -100;
    public const int ReputationMin = 0;
    public const int ReputationMax = 100;
    public const int ReputationStart = 75;

    const int RepShareFake = -20;
    const int RepVerifiedNotShared = 3;

    /// <summary>Puntos por acierto según confianza (90% acierto = +150, GDD 14).</summary>
    public static int CorrectPoints(ConfidenceLevel confidence)
    {
        switch (confidence)
        {
            case ConfidenceLevel.Low: return 75;
            case ConfidenceLevel.High: return 150;
            default: return 100;
        }
    }

    /// <summary>Penalización por error según confianza (90% error = -100, GDD 14).</summary>
    public static int WrongPoints(ConfidenceLevel confidence)
    {
        switch (confidence)
        {
            case ConfidenceLevel.Low: return -25;
            case ConfidenceLevel.High: return -100;
            default: return -50;
        }
    }

    static int DecisionReputation(bool isCorrect, ConfidenceLevel confidence)
    {
        if (isCorrect) return confidence == ConfidenceLevel.High ? 8 : 5;
        return confidence == ConfidenceLevel.High ? -10 : -5;
    }

    /// <summary>Calcula puntos y reputación de un caso resuelto.</summary>
    /// <param name="relevantToolsUsed">Herramientas útiles distintas usadas antes de decidir.</param>
    /// <param name="shared">Si el jugador decidió compartir (solo cuenta cuando canShare es true).</param>
    public static CaseOutcome Evaluate(
        Verdict correct,
        Verdict chosen,
        ConfidenceLevel confidence,
        int relevantToolsUsed,
        bool canShare,
        bool shared)
    {
        bool isCorrect = correct == chosen;
        int points = (isCorrect ? CorrectPoints(confidence) : WrongPoints(confidence))
                     + Mathf.Max(0, relevantToolsUsed) * ToolBonus;
        int reputation = DecisionReputation(isCorrect, confidence);

        bool isFake = correct != Verdict.Human;
        bool sharedFake = canShare && shared && isFake;
        if (sharedFake)
        {
            points += ShareFakePenalty;
            reputation += RepShareFake;
        }
        else if (canShare && !shared && isFake)
        {
            reputation += RepVerifiedNotShared;
        }

        return new CaseOutcome(isCorrect, points, reputation, sharedFake);
    }

    public static int ClampReputation(int value) => Mathf.Clamp(value, ReputationMin, ReputationMax);

    public static string VerdictLabel(Verdict verdict)
    {
        switch (verdict)
        {
            case Verdict.AI: return "IA";
            case Verdict.Manipulated: return "MANIPULADO";
            default: return "HUMANO";
        }
    }

    public static string ConfidenceLabel(ConfidenceLevel confidence)
    {
        switch (confidence)
        {
            case ConfidenceLevel.Low: return "50%";
            case ConfidenceLevel.High: return "90%";
            default: return "70%";
        }
    }
}
