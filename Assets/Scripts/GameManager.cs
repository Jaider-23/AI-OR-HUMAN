using System.Linq;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Control global del flujo de la partida (Game Manager / Case Manager del GDD 21):
/// activa terminales, abre investigaciones, aplica resultados y dispara la narrativa.
/// </summary>
public class GameManager : MonoBehaviour
{
    public const int MaxLevel = 7;
    const int TrainingCaseCount = 2;
    const int AllFragmentsBonus = 100;
    static readonly Color PositiveColor = new Color(0.27f, 0.95f, 0.55f);
    static readonly Color NegativeColor = new Color(1f, 0.3f, 0.42f);

    public static GameManager Instance { get; private set; }

    [Header("Banco de casos (ordenados por nivel)")]
    public CaseData[] cases;

    [Header("Mundo")]
    public CaseTerminal[] terminals;
    public DataFragment[] fragments;

    [Header("UI")]
    public HUDController hud;
    public DialogueController dialogue;
    public InvestigationPanel investigation;
    public EndScreen endScreen;

    readonly List<CaseData> playlist = new List<CaseData>();
    int lastLevel;
    AutoSave autoSave;
    PlayerController player;

    CaseData CurrentCase => GameSession.CaseIndex < playlist.Count ? playlist[GameSession.CaseIndex] : null;

    /// <summary>True cuando ya se jugaron todos los casos (no hay nada que autoguardar).</summary>
    public bool RunFinished { get; private set; }

    /// <summary>
    /// True si el mundo está en un estado coherente para escribir el autoguardado: sin casos
    /// ni diálogos abiertos. Con la investigación a medias los puntos ya están aplicados pero
    /// el caso aún no se cerró, así que guardarlo dejaría la partida descuadrada.
    /// </summary>
    public bool CanCheckpoint => !RunFinished && !dialogue.IsOpen && !investigation.IsOpen;

    void Awake()
    {
        Instance = this;
        InputGate.Clear();
        Time.timeScale = 1f;
        autoSave = GetComponent<AutoSave>();
        if (autoSave == null) autoSave = gameObject.AddComponent<AutoSave>();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        if (!GameSessionBootstrap.Started) GameSession.StartNew(false);
        GameSessionBootstrap.Started = false;

        BuildPlaylist();
        player = FindObjectOfType<PlayerController>();
        hud.Configure(playlist.Count, fragments != null ? fragments.Length : 0);
        bool resumed = ApplySave(GameSession.ConsumePendingRestore());
        RefreshWorld();
        autoSave.Begin(this);

        var intro = resumed ? NarrativeScript.Resume(GameSession.CaseIndex + 1, playlist.Count)
                  : GameSession.TrainingMode ? NarrativeScript.TrainingIntro()
                  : NarrativeScript.Intro();
        dialogue.Show(intro);
    }

    /// <summary>
    /// Devuelve el laboratorio al estado del autoguardado: fragmentos ya recogidos y
    /// Alex donde lo dejó el jugador. Devuelve true si se retomó una partida.
    /// </summary>
    bool ApplySave(SaveData data)
    {
        if (data == null) return false;
        lastLevel = data.lastLevel;

        int restored = 0;
        if (fragments != null && data.collectedFragments != null)
        {
            foreach (int index in data.collectedFragments)
            {
                if (index < 0 || index >= fragments.Length || fragments[index] == null) continue;
                fragments[index].MarkAlreadyCollected();
                restored++;
            }
        }
        // El mapa pudo cambiar desde el guardado: el contador sigue a los fragmentos que existen.
        if (restored != GameSession.Fragments) GameSession.SetFragments(restored);

        if (data.hasPlayerPosition) MovePlayer(new Vector2(data.playerX, data.playerY));
        return true;
    }

    /// <summary>El Rigidbody2D manda sobre el transform: hay que mover los dos.</summary>
    void MovePlayer(Vector2 position)
    {
        if (player == null) return;
        player.transform.position = position;
        var body = player.GetComponent<Rigidbody2D>();
        if (body != null) body.position = position;
    }

    /// <summary>Foto del progreso actual para <see cref="AutoSave"/>.</summary>
    public SaveData CaptureSave()
    {
        var data = new SaveData
        {
            caseIndex = GameSession.CaseIndex,
            lastLevel = lastLevel,
            score = GameSession.Score,
            reputation = GameSession.Reputation,
            totalCases = playlist.Count
        };
        if (fragments != null)
        {
            for (int i = 0; i < fragments.Length; i++)
            {
                if (fragments[i] != null && fragments[i].Collected) data.collectedFragments.Add(i);
            }
        }
        if (player != null)
        {
            data.hasPlayerPosition = true;
            data.playerX = player.transform.position.x;
            data.playerY = player.transform.position.y;
        }
        return data;
    }

    void BuildPlaylist()
    {
        playlist.Clear();
        // Orden estable: los casos del mismo nivel conservan su orden (coincide con las terminales).
        playlist.AddRange(cases.Where(data => data != null).OrderBy(data => data.level));
        if (GameSession.TrainingMode && playlist.Count > TrainingCaseCount)
        {
            playlist.RemoveRange(TrainingCaseCount, playlist.Count - TrainingCaseCount);
        }
    }

    void RefreshWorld()
    {
        int index = GameSession.CaseIndex;
        for (int i = 0; i < terminals.Length; i++)
        {
            var terminal = terminals[i];
            if (terminal == null) continue;
            bool exists = terminal.caseIndex < playlist.Count;
            terminal.gameObject.SetActive(exists);
            if (!exists) continue;
            terminal.SetState(terminal.caseIndex < index ? TerminalState.Solved
                            : terminal.caseIndex == index ? TerminalState.Active
                            : TerminalState.Locked);
        }

        var current = CurrentCase;
        if (current == null)
        {
            hud.SetObjective("Investigación completada.");
            return;
        }
        hud.SetLevel(current.level, MaxLevel);
        hud.SetObjective($"Objetivo: terminal marcada · Nivel {current.level}: {NarrativeScript.LevelName(current.level)}");
    }

    public void OnTerminalUsed(CaseTerminal terminal)
    {
        if (dialogue.IsOpen || investigation.IsOpen) return;
        switch (terminal.State)
        {
            case TerminalState.Active:
                OpenCase(CurrentCase);
                break;
            case TerminalState.Solved:
                dialogue.Show(Speaker.Ada, "Ese caso ya está cerrado. Busca la terminal con el holograma.");
                break;
            default:
                dialogue.Show(Speaker.Ada, "Terminal cifrada. Resuelve primero el caso marcado con el holograma.");
                break;
        }
    }

    void OpenCase(CaseData data)
    {
        if (data == null) return;
        bool newLevel = data.level != lastLevel;
        lastLevel = data.level;

        var lines = new List<DialogueLine>();
        if (newLevel)
        {
            lines.Add(new DialogueLine(Speaker.System,
                $"NIVEL {data.level} · {NarrativeScript.LevelName(data.level).ToUpper()}"));
        }
        lines.Add(new DialogueLine(data.introSpeaker, data.introDialogue));
        dialogue.Show(lines, () => investigation.Open(data, OnCaseClosed));
    }

    void OnCaseClosed(CaseOutcome outcome)
    {
        string sign = outcome.Points >= 0 ? "+" : string.Empty;
        hud.Flash($"{sign}{outcome.Points}", outcome.Points >= 0 ? PositiveColor : NegativeColor);

        GameSession.AdvanceCase();
        RefreshWorld();

        if (CurrentCase == null) FinishGame();
        else autoSave.Request();
    }

    void FinishGame()
    {
        RunFinished = true;
        // Consolida las estadísticas y borra el autoguardado: la partida ya no se retoma.
        GameSession.FinishGame();
        var ending = GameSession.TrainingMode
            ? NarrativeScript.TrainingEnding()
            : NarrativeScript.Ending(GameSession.Score, GameSession.Reputation);
        dialogue.Show(ending, () => endScreen.Show(playlist.Count));
    }

    public void TalkToAda()
    {
        if (dialogue.IsOpen || investigation.IsOpen) return;
        var current = CurrentCase;
        string line = current == null
            ? "Hemos terminado por hoy, Alex. Buen trabajo."
            : $"Siguiente caso: {current.title}. Consejo: no te fíes solo de la apariencia; " +
              "contrasta la fuente y los metadatos antes de decidir.";
        dialogue.Show(Speaker.Ada, line);
    }

    public void OnFragmentCollected()
    {
        int total = fragments != null ? fragments.Length : 0;
        hud.Flash($"+{DataFragment.PointsPerFragment}", PositiveColor);
        autoSave.Request();
        if (total == 0 || GameSession.Fragments < total) return;

        GameSession.AddPoints(AllFragmentsBonus);
        dialogue.Show(new[]
        {
            new DialogueLine(Speaker.Ada, $"¡Todos los fragmentos de datos recuperados! Bonificación +{AllFragmentsBonus}."),
            new DialogueLine(Speaker.Phantom, "Recoger migajas no te hará ver el panorama completo, analista...")
        });
    }
}

/// <summary>Indica a la escena de juego que el menú ya inició una sesión (normal o entrenamiento).</summary>
public static class GameSessionBootstrap
{
    public static bool Started;
}
