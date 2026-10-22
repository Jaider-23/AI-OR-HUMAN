/// <summary>Clasificación que el jugador puede asignar a un contenido (GDD 11.1).</summary>
public enum Verdict
{
    Human,
    AI,
    Manipulated
}

/// <summary>Nivel de seguridad del jugador antes de confirmar (GDD 14).</summary>
public enum ConfidenceLevel
{
    Low,
    Medium,
    High
}

/// <summary>Herramientas de investigación disponibles (GDD 15).</summary>
public enum InvestigationTool
{
    AnalyzeText,
    ViewSource,
    AnalyzeProfile,
    CheckLink,
    AnalyzeMetadata
}

/// <summary>Tipo de contenido de un caso (GDD 17).</summary>
public enum ContentType
{
    Text,
    Email,
    Image,
    SocialPost,
    Audio,
    Video,
    Multimodal
}

/// <summary>Personajes que hablan en el panel de diálogo.</summary>
public enum Speaker
{
    Ada,
    Phantom,
    System
}
