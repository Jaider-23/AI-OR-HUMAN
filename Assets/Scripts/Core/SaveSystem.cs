using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

/// <summary>
/// Foto de una partida a medias (Data/Save Manager del GDD 21). Solo guarda el progreso:
/// el banco de casos, el mapa y el arte se reconstruyen desde los assets.
/// </summary>
[Serializable]
public class SaveData
{
    /// <summary>Medio tile: un paso corto de Alex no justifica reescribir el guardado.</summary>
    const float PositionEpsilon = 0.5f;

    public int version = SaveSystem.Version;
    /// <summary>Casos ya cerrados: también es el índice del caso pendiente.</summary>
    public int caseIndex;

    /// <summary>Último nivel anunciado, para no repetir el cartel NIVEL X al continuar.</summary>
    public int lastLevel;

    public int score;
    public int reputation = ScoreRules.ReputationStart;

    /// <summary>Casos de la partida guardada (la lista de entrenamiento es más corta).</summary>
    public int totalCases;

    /// <summary>Índices de los fragmentos ya recogidos dentro de la lista del GameManager.</summary>
    public List<int> collectedFragments = new List<int>();

    public bool hasPlayerPosition;
    public float playerX;
    public float playerY;

    /// <summary>Momento de la escritura en ISO 8601 UTC (solo informativo para el menú).</summary>
    public string savedAtUtc;

    /// <summary>
    /// True si dos fotos describen la misma situación. Evita reescrituras inútiles:
    /// en WebGL cada <see cref="PlayerPrefs.Save"/> cuesta un volcado a IndexedDB.
    /// </summary>
    public static bool SameProgress(SaveData a, SaveData b)
    {
        if (a == null || b == null) return false;
        if (a.caseIndex != b.caseIndex || a.lastLevel != b.lastLevel || a.score != b.score
            || a.reputation != b.reputation || a.totalCases != b.totalCases) return false;
        if (a.collectedFragments.Count != b.collectedFragments.Count) return false;
        for (int i = 0; i < a.collectedFragments.Count; i++)
        {
            if (a.collectedFragments[i] != b.collectedFragments[i]) return false;
        }
        if (a.hasPlayerPosition != b.hasPlayerPosition) return false;
        return Mathf.Abs(a.playerX - b.playerX) < PositionEpsilon
               && Mathf.Abs(a.playerY - b.playerY) < PositionEpsilon;
    }
}

/// <summary>
/// Autoguardado de la partida en curso. Usa PlayerPrefs igual que las estadísticas
/// (<see cref="GameSession"/>) y la configuración (<see cref="GameSettings"/>): es el
/// único almacenamiento que se comporta igual en Windows y en WebGL.
/// Quién escribe y cuándo es cosa de <see cref="AutoSave"/>.
/// </summary>
public static class SaveSystem
{
    public const string Key = "aih_save_v1";

    /// <summary>Un guardado de otra versión se descarta en lugar de cargarse a medias.</summary>
    public const int Version = 1;

    /// <summary>
    /// Interruptor para las pruebas automáticas y la prueba de juego del Editor: recorren
    /// la partida entera y, sin apagarlo, sobrescribirían el guardado real del jugador.
    /// </summary>
    public static bool Enabled { get; set; } = true;

    public static bool HasSave => Load() != null;

    /// <summary>Devuelve el autoguardado, o null si no hay, está corrupto o la partida ya terminó.</summary>
    public static SaveData Load()
    {
        if (!Enabled) return null;
        string json = PlayerPrefs.GetString(Key, string.Empty);
        if (string.IsNullOrEmpty(json)) return null;

        SaveData data;
        try
        {
            data = JsonUtility.FromJson<SaveData>(json);
        }
        catch (ArgumentException e)
        {
            Debug.LogWarning($"[SaveSystem] Autoguardado corrupto, se descarta: {e.Message}");
            Delete();
            return null;
        }

        if (IsUsable(data)) return data;
        Debug.LogWarning("[SaveSystem] El autoguardado no se puede retomar (versión o progreso fuera de rango): se descarta.");
        Delete();
        return null;
    }

    public static void Write(SaveData data)
    {
        if (!Enabled || data == null) return;
        data.version = Version;
        data.savedAtUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
        PlayerPrefs.SetString(Key, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    public static void Delete()
    {
        if (!Enabled || !PlayerPrefs.HasKey(Key)) return;
        PlayerPrefs.DeleteKey(Key);
        PlayerPrefs.Save();
    }

    /// <summary>Resumen para el botón CONTINUAR: "Caso 04/11 · 320 pts · reputación 78% · hace 2 min".</summary>
    public static string Describe(SaveData data)
    {
        if (data == null) return string.Empty;
        int total = Mathf.Max(1, data.totalCases);
        int current = Mathf.Clamp(data.caseIndex + 1, 1, total);
        string summary = $"Caso {current:D2}/{total:D2} · {data.score} pts · reputación {data.reputation}%";
        string ago = TimeAgo(data.savedAtUtc);
        return string.IsNullOrEmpty(ago) ? summary : $"{summary} · {ago}";
    }

    /// <summary>Antigüedad del guardado en lenguaje natural; cadena vacía si la fecha no es legible.</summary>
    public static string TimeAgo(string savedAtUtc)
    {
        if (string.IsNullOrEmpty(savedAtUtc)) return string.Empty;
        if (!DateTime.TryParse(savedAtUtc, CultureInfo.InvariantCulture,
                               DateTimeStyles.RoundtripKind, out var when)) return string.Empty;

        var elapsed = DateTime.UtcNow - when.ToUniversalTime();
        // Un reloj del sistema que va atrás no debe producir "hace -3 min".
        if (elapsed < TimeSpan.FromMinutes(1)) return "hace unos segundos";
        if (elapsed < TimeSpan.FromHours(1)) return $"hace {(int)elapsed.TotalMinutes} min";
        if (elapsed < TimeSpan.FromDays(1)) return $"hace {(int)elapsed.TotalHours} h";
        int days = (int)elapsed.TotalDays;
        return days == 1 ? "hace 1 día" : $"hace {days} días";
    }

    static bool IsUsable(SaveData data) =>
        data != null
        && data.version == Version
        && data.totalCases > 0
        && data.caseIndex >= 0
        && data.caseIndex < data.totalCases;
}
