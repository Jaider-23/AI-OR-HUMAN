using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Registro de desempeño de un caso (GDD 23).</summary>
[Serializable]
public class CaseRecord
{
    public string caseId;
    public int level;
    public string answer;
    public string correctAnswer;
    public string confidence;
    public List<string> toolsUsed = new List<string>();
    public int points;
    public int reputationBefore;
    public int reputationAfter;
    public bool shared;
    public float seconds;
    public bool correct;
}

/// <summary>Estadísticas persistentes del jugador.</summary>
[Serializable]
public class PlayerStats
{
    public int gamesPlayed;
    public int bestScore;
    public int lastScore;
    public int lastReputation = ScoreRules.ReputationStart;
    public List<CaseRecord> history = new List<CaseRecord>();
}

/// <summary>
/// Estado de la partida que sobrevive al cambio de escena y persistencia de estadísticas
/// en PlayerPrefs (Data/Save Manager del GDD 21). El autoguardado de la partida en curso
/// vive en <see cref="SaveSystem"/> y lo escribe <see cref="AutoSave"/>.
/// </summary>
public static class GameSession
{
    const string StatsKey = "aih_stats_v1";
    const int MaxHistory = 200;

    public static bool TrainingMode { get; private set; }
    public static int Score { get; private set; }
    public static int Reputation { get; private set; } = ScoreRules.ReputationStart;
    public static int Fragments { get; private set; }
    public static int CaseIndex { get; private set; }

    /// <summary>
    /// Autoguardado que la escena de juego debe aplicar al cargar (fragmentos recogidos y
    /// posición de Alex). Null cuando la partida empieza de cero.
    /// </summary>
    public static SaveData PendingRestore { get; private set; }

    public static event Action Changed;

    public static void StartNew(bool training)
    {
        TrainingMode = training;
        Score = 0;
        Reputation = ScoreRules.ReputationStart;
        Fragments = 0;
        CaseIndex = 0;
        PendingRestore = null;
        // Una partida nueva sustituye a la guardada; el entrenamiento no toca el autoguardado.
        if (!training) SaveSystem.Delete();
        Changed?.Invoke();
    }

    /// <summary>
    /// Retoma la partida autoguardada. Devuelve false si no hay nada que continuar
    /// (sin guardado, corrupto o de otra versión).
    /// </summary>
    public static bool Continue()
    {
        var data = SaveSystem.Load();
        if (data == null) return false;

        TrainingMode = false;
        Score = data.score;
        Reputation = ScoreRules.ClampReputation(data.reputation);
        Fragments = data.collectedFragments != null ? data.collectedFragments.Count : 0;
        CaseIndex = Mathf.Max(0, data.caseIndex);
        PendingRestore = data;
        Changed?.Invoke();
        return true;
    }

    /// <summary>Entrega el autoguardado pendiente una sola vez (lo consume el GameManager).</summary>
    public static SaveData ConsumePendingRestore()
    {
        var data = PendingRestore;
        PendingRestore = null;
        return data;
    }

    /// <summary>
    /// Ajusta el contador de fragmentos a los que de verdad existen en el mapa restaurado:
    /// el nivel pudo cambiar desde que se guardó la partida.
    /// </summary>
    public static void SetFragments(int count)
    {
        Fragments = Mathf.Max(0, count);
        Changed?.Invoke();
    }

    public static void ApplyOutcome(CaseOutcome outcome)
    {
        Score += outcome.Points;
        Reputation = ScoreRules.ClampReputation(Reputation + outcome.ReputationDelta);
        Changed?.Invoke();
    }

    public static void AddPoints(int points)
    {
        Score += points;
        Changed?.Invoke();
    }

    public static void AddFragment()
    {
        Fragments++;
        Changed?.Invoke();
    }

    public static void AdvanceCase()
    {
        CaseIndex++;
        Changed?.Invoke();
    }

    public static void RecordCase(CaseRecord record)
    {
        if (TrainingMode || record == null) return;
        var stats = LoadStats();
        stats.history.Add(record);
        if (stats.history.Count > MaxHistory) stats.history.RemoveRange(0, stats.history.Count - MaxHistory);
        SaveStats(stats);
    }

    public static void FinishGame()
    {
        // La investigación terminó: ya no hay partida a medias que retomar.
        SaveSystem.Delete();
        if (TrainingMode) return;
        var stats = LoadStats();
        stats.gamesPlayed++;
        stats.lastScore = Score;
        stats.lastReputation = Reputation;
        stats.bestScore = Mathf.Max(stats.bestScore, Score);
        SaveStats(stats);
    }

    public static PlayerStats LoadStats()
    {
        string json = PlayerPrefs.GetString(StatsKey, string.Empty);
        if (string.IsNullOrEmpty(json)) return new PlayerStats();
        try
        {
            return JsonUtility.FromJson<PlayerStats>(json) ?? new PlayerStats();
        }
        catch (ArgumentException e)
        {
            Debug.LogWarning($"[GameSession] Estadísticas corruptas, se reinician: {e.Message}");
            return new PlayerStats();
        }
    }

    public static void ResetStats()
    {
        PlayerPrefs.DeleteKey(StatsKey);
        PlayerPrefs.Save();
        SaveSystem.Delete();
    }

    static void SaveStats(PlayerStats stats)
    {
        PlayerPrefs.SetString(StatsKey, JsonUtility.ToJson(stats));
        PlayerPrefs.Save();
    }
}
