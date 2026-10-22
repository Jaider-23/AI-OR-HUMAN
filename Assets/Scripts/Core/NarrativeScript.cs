using System.Collections.Generic;

/// <summary>
/// Diálogos fijos de la historia (GDD 7.1): alerta, reclutamiento, entrenamiento y resolución.
/// Los diálogos de cada caso viven en su <see cref="CaseData"/>.
/// </summary>
public static class NarrativeScript
{
    public static IEnumerable<DialogueLine> Intro()
    {
        yield return new DialogueLine(Speaker.System,
            "ALERTA DTA: se detecta una ola inusual de contenido sospechoso circulando por la red.");
        yield return new DialogueLine(Speaker.Ada,
            "Bienvenido a la Digital Truth Agency, Alex. Soy ADA, tu asistente de análisis.");
        yield return new DialogueLine(Speaker.Ada,
            "Tu trabajo: decidir si cada contenido es HUMANO, generado por IA o MANIPULADO.");
        yield return new DialogueLine(Speaker.Ada,
            "Muévete con WASD o las flechas. Acércate a la terminal marcada y pulsa E para investigar.");
    }

    public static IEnumerable<DialogueLine> TrainingIntro()
    {
        yield return new DialogueLine(Speaker.Ada,
            "Modo entrenamiento. Aquí nada cuenta para tus estadísticas: experimenta sin miedo.");
        yield return new DialogueLine(Speaker.Ada,
            "En cada caso usa las herramientas: analizar texto, ver fuente, perfil, enlace y metadatos.");
        yield return new DialogueLine(Speaker.Ada,
            "Luego elige HUMANO, IA o MANIPULADO e indica tu confianza. Más confianza = más puntos... o más penalización.");
        yield return new DialogueLine(Speaker.Ada,
            "Recoge los fragmentos de datos brillantes del laboratorio. Ve a la terminal marcada cuando quieras.");
    }

    /// <summary>Al retomar una partida autoguardada en lugar de empezar de cero.</summary>
    public static IEnumerable<DialogueLine> Resume(int caseNumber, int totalCases)
    {
        yield return new DialogueLine(Speaker.System,
            $"DTA · SESIÓN RESTAURADA — expediente {caseNumber} de {totalCases}.");
        yield return new DialogueLine(Speaker.Ada,
            "Retomamos donde lo dejamos, Alex. Tu progreso se guarda solo: cada caso cerrado " +
            "queda registrado aunque cierres el juego.");
    }

    public static IEnumerable<DialogueLine> Ending(int score, int reputation)
    {
        yield return new DialogueLine(Speaker.Phantom,
            "Hmm... Has desmontado mi operación pieza por pieza. Esta vez.");
        yield return new DialogueLine(Speaker.Ada,
            $"Lo lograste, Alex. Puntuación final: {score}. Reputación digital: {reputation}%.");
        yield return new DialogueLine(Speaker.Ada,
            "Y recuerda: ni siquiera yo soy infalible. Observa, investiga, analiza y luego decide.");
    }

    public static IEnumerable<DialogueLine> TrainingEnding()
    {
        yield return new DialogueLine(Speaker.Ada,
            "Entrenamiento completado. Ya conoces las herramientas: es hora de los casos reales.");
    }

    public static string LevelName(int level)
    {
        switch (level)
        {
            case 1: return "Textos";
            case 2: return "Correos";
            case 3: return "Imágenes";
            case 4: return "Redes sociales";
            case 5: return "Audio";
            case 6: return "Deepfake";
            default: return "Caso final";
        }
    }
}
