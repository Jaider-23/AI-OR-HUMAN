using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Menú principal (GDD 18.1, RF-01): Jugar, Entrenamiento, Casos, Estadísticas,
/// Configuración, Créditos y Salir.
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    [Header("Paneles de Interfaz")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private GameObject creditsPanel;
    [SerializeField] private GameObject casesPanel;
    [SerializeField] private GameObject statsPanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("Contenido")]
    [SerializeField] private CaseData[] cases;
    [SerializeField] private TextMeshProUGUI casesBody;
    [SerializeField] private TextMeshProUGUI statsBody;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Slider textSpeedSlider;
    [SerializeField] private Toggle fullscreenToggle;
    [Tooltip("Botón SALIR: se oculta en la versión web (Application.Quit congela el navegador).")]
    [SerializeField] private GameObject quitButton;

    [Header("Continuar partida")]
    [Tooltip("Grupo CONTINUAR (botón + resumen): se oculta cuando no hay partida autoguardada.")]
    [SerializeField] private GameObject continueGroup;
    [Tooltip("Resumen de la partida guardada que se muestra bajo el botón CONTINUAR.")]
    [SerializeField] private TextMeshProUGUI continueCaption;

    private void Start()
    {
        Time.timeScale = 1f;
        InputGate.Clear();
        ShowOnly(menuPanel);
        RefreshContinue();
        if (Application.platform == RuntimePlatform.WebGLPlayer && quitButton != null) quitButton.SetActive(false);

        if (volumeSlider != null)
        {
            volumeSlider.value = GameSettings.Volume;
            volumeSlider.onValueChanged.AddListener(GameSettings.SetVolume);
        }
        if (textSpeedSlider != null)
        {
            textSpeedSlider.value = GameSettings.TextSpeed;
            textSpeedSlider.onValueChanged.AddListener(GameSettings.SetTextSpeed);
        }
        if (fullscreenToggle != null)
        {
            fullscreenToggle.isOn = Screen.fullScreen;
            fullscreenToggle.onValueChanged.AddListener(GameSettings.SetFullscreen);
        }
    }

    // Carga la escena principal del juego (partida nueva: reemplaza el autoguardado)
    public void PlayGame() => StartSession(false);

    /// <summary>Retoma la partida autoguardada (GDD 21, Data/Save Manager).</summary>
    public void ContinueGame()
    {
        SfxPlayer.Play(Sfx.Click);
        if (!GameSession.Continue())
        {
            // El guardado desapareció o era de otra versión: se actualiza el menú y no se carga nada.
            RefreshContinue();
            return;
        }
        GameSessionBootstrap.Started = true;
        SceneFader.LoadScene(EndScreen.GameScene);
    }

    /// <summary>Muestra CONTINUAR solo si hay una partida a medias, con su resumen.</summary>
    private void RefreshContinue()
    {
        var save = SaveSystem.Load();
        if (continueGroup != null) continueGroup.SetActive(save != null);
        if (continueCaption == null || save == null) return;
        continueCaption.text =
            $"<color=#22E4F0>{SaveSystem.Describe(save)}</color>\n" +
            "<size=88%>JUGAR empieza una partida nueva y borra este guardado.</size>";
    }

    // Modo entrenamiento: tutorial guiado sin guardar estadísticas
    public void StartTraining() => StartSession(true);

    private void StartSession(bool training)
    {
        SfxPlayer.Play(Sfx.Click);
        GameSession.StartNew(training);
        GameSessionBootstrap.Started = true;
        SceneFader.LoadScene(EndScreen.GameScene);
    }

    public void OpenCredits() => ShowOnly(creditsPanel);

    public void OpenCases()
    {
        if (casesBody != null) casesBody.text = BuildCasesText();
        ShowOnly(casesPanel);
    }

    public void OpenStats()
    {
        if (statsBody != null) statsBody.text = BuildStatsText();
        ShowOnly(statsPanel);
    }

    public void OpenSettings()
    {
        // La pantalla completa puede cambiar fuera del menú (F11, Esc en el navegador): se resincroniza.
        if (fullscreenToggle != null) fullscreenToggle.SetIsOnWithoutNotify(Screen.fullScreen);
        ShowOnly(settingsPanel);
    }

    // Vuelve al menú principal desde cualquier panel
    public void CloseCredits() => ShowOnly(menuPanel);

    public void BackToMenu() => ShowOnly(menuPanel);

    public void ResetProgress()
    {
        // Borra estadísticas y autoguardado: el menú deja de ofrecer CONTINUAR.
        GameSession.ResetStats();
        RefreshContinue();
        OpenStats();
    }

    private void ShowOnly(GameObject panel)
    {
        SfxPlayer.Play(Sfx.Click);
        GameSettings.Persist();
        foreach (var candidate in new[] { menuPanel, creditsPanel, casesPanel, statsPanel, settingsPanel })
        {
            if (candidate != null) candidate.SetActive(candidate == panel);
        }
    }

    private string BuildCasesText()
    {
        var stats = GameSession.LoadStats();
        var builder = new StringBuilder();
        foreach (var data in cases)
        {
            if (data == null) continue;
            var best = stats.history.FindLast(r => r.caseId == data.caseId);
            string status = best == null ? "<color=#8FA3BF>PENDIENTE</color>"
                          : best.correct ? "<color=#46F28C>RESUELTO</color>"
                          : "<color=#FF4D6A>FALLADO</color>";
            builder.AppendLine($"<b>{data.caseId}</b>  Nivel {data.level} · {NarrativeScript.LevelName(data.level)} — {data.title}   {status}");
        }
        return builder.ToString();
    }

    private static string BuildStatsText()
    {
        var stats = GameSession.LoadStats();
        int total = stats.history.Count;
        if (total == 0) return "Aún no hay partidas registradas.\nJuega una investigación para ver tus estadísticas.";

        int correct = 0, ai = 0, aiOk = 0, manip = 0, manipOk = 0, sharedFake = 0;
        float seconds = 0f;
        foreach (var r in stats.history)
        {
            if (r.correct) correct++;
            seconds += r.seconds;
            if (r.correctAnswer == nameof(Verdict.AI)) { ai++; if (r.correct) aiOk++; }
            if (r.correctAnswer == nameof(Verdict.Manipulated)) { manip++; if (r.correct) manipOk++; }
            if (r.shared && r.correctAnswer != nameof(Verdict.Human)) sharedFake++;
        }

        return $"Partidas completadas: <b>{stats.gamesPlayed}</b>\n" +
               $"Mejor puntuación: <b>{stats.bestScore}</b>   ·   Última: <b>{stats.lastScore}</b>\n" +
               $"Reputación final más reciente: <b>{stats.lastReputation}%</b>\n\n" +
               $"Precisión global: <b>{Percent(correct, total)}</b> ({correct}/{total})\n" +
               $"Contenido IA identificado: <b>{Percent(aiOk, ai)}</b>\n" +
               $"Manipulaciones / deepfakes detectados: <b>{Percent(manipOk, manip)}</b>\n" +
               $"Contenido falso compartido: <b>{sharedFake}</b> veces\n" +
               $"Tiempo medio por caso: <b>{seconds / total:F0} s</b>\n\n" +
               $"Pensamiento crítico: <color=#22E4F0><b>{CriticalLevel(correct, total)}</b></color>";
    }

    private static string Percent(int part, int whole) => whole == 0 ? "—" : $"{100f * part / whole:F0}%";

    private static string CriticalLevel(int correct, int total)
    {
        float ratio = total == 0 ? 0f : (float)correct / total;
        return ratio >= 0.8f ? "ALTO" : ratio >= 0.5f ? "MEDIO" : "EN DESARROLLO";
    }

    // Cierra el juego (soporta tanto la ejecución en el editor como en la compilación final)
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
