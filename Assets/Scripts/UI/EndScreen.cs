using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Resumen al terminar todos los casos: puntuación, reputación y rango del analista.</summary>
public class EndScreen : MonoBehaviour
{
    public const string MenuScene = "MainMenuScene";
    public const string GameScene = "GameScene";

    public GameObject root;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI summaryText;
    public Button retryButton;
    public Button menuButton;

    void Awake()
    {
        if (root != null) root.SetActive(false);
        retryButton.onClick.AddListener(Retry);
        menuButton.onClick.AddListener(GoToMenu);
    }

    public void Show(int totalCases)
    {
        InputGate.Lock(this);
        int score = GameSession.Score;
        int reputation = GameSession.Reputation;
        titleText.text = GameSession.TrainingMode ? "ENTRENAMIENTO COMPLETADO" : "INVESTIGACIÓN COMPLETADA";
        summaryText.text =
            $"Casos analizados: <b>{totalCases}</b>\n" +
            $"Puntuación final: <b>{score}</b>\n" +
            $"Reputación digital: <b>{reputation}%</b>\n" +
            $"Fragmentos de datos: <b>{GameSession.Fragments}</b>\n\n" +
            $"Rango: <color=#22E4F0><b>{RankFor(score, reputation, totalCases)}</b></color>";
        root.SetActive(true);
    }

    /// <summary>Rango según el promedio de puntos por caso y la reputación final.</summary>
    public static string RankFor(int score, int reputation, int totalCases)
    {
        float average = totalCases > 0 ? (float)score / totalCases : 0f;
        if (average >= 140f && reputation >= 85) return "ANALISTA EXPERTO";
        if (average >= 90f && reputation >= 65) return "ANALISTA SENIOR";
        if (average >= 40f) return "ANALISTA JUNIOR";
        return "EN FORMACIÓN";
    }

    void Retry()
    {
        SfxPlayer.Play(Sfx.Click);
        GameSession.StartNew(GameSession.TrainingMode);
        GameSessionBootstrap.Started = true;
        SceneFader.LoadScene(GameScene);
    }

    void GoToMenu()
    {
        SfxPlayer.Play(Sfx.Click);
        SceneFader.LoadScene(MenuScene);
    }
}
