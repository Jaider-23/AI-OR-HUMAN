using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// Prueba de extremo a extremo del autoguardado en la escena real: Alex recoge un fragmento,
/// el juego guarda solo y, al continuar, el laboratorio vuelve a estar como lo dejó
/// (fragmento retirado, puntos intactos y Alex en su sitio).
/// </summary>
public class AutoSaveTests
{
    const float WriteWaitSeconds = 4f;
    const float PositionTolerance = 0.6f;

    string backup;
    bool hadSave;

    [SetUp]
    public void SetUp()
    {
        hadSave = PlayerPrefs.HasKey(SaveSystem.Key);
        backup = PlayerPrefs.GetString(SaveSystem.Key, string.Empty);
        SaveSystem.Enabled = true;
        SaveSystem.Delete();
    }

    [TearDown]
    public void TearDown()
    {
        // La escena sigue viva hasta que el Test Runner sale del modo juego: si su AutoSave
        // no se detiene aquí, al descargarse volvería a escribir sobre la partida del jugador.
        var gm = GameManager.Instance;
        var autoSave = gm != null ? gm.GetComponent<AutoSave>() : null;
        if (autoSave != null) autoSave.Stop();

        SaveSystem.Enabled = false;
        if (hadSave) PlayerPrefs.SetString(SaveSystem.Key, backup);
        else PlayerPrefs.DeleteKey(SaveSystem.Key);
        PlayerPrefs.Save();
        SaveSystem.Enabled = true;
    }

    [UnityTest, Timeout(180000)]
    public IEnumerator LaPartidaSeGuardaSolaYSeRetomaDondeSeDejo()
    {
        GameSession.StartNew(false);
        GameSessionBootstrap.Started = true;
        yield return SceneManager.LoadSceneAsync("GameScene");
        yield return null;

        var gm = GameManager.Instance;
        Assert.IsNotNull(gm, "GameManager no encontrado");
        Assert.Greater(gm.fragments.Length, 0, "La escena no tiene fragmentos de datos");
        // La introducción bloquea el autoguardado: con un caso o un diálogo abierto no se escribe.
        Assert.IsNull(SaveSystem.Load(), "No debe guardarse durante la introducción");
        yield return CloseDialogue(gm.dialogue);

        var player = Object.FindObjectOfType<PlayerController>();
        var fragment = gm.fragments[0];
        player.GetComponent<Rigidbody2D>().position = fragment.transform.position;
        yield return new WaitForSeconds(0.5f);
        Assert.IsTrue(fragment.Collected, "Alex pasó por el fragmento y no lo recogió");

        var saved = SaveSystem.Load();
        for (float t = 0f; t < WriteWaitSeconds && (saved == null || saved.collectedFragments.Count == 0); t += 0.1f)
        {
            yield return new WaitForSecondsRealtime(0.1f);
            saved = SaveSystem.Load();
        }

        Assert.IsNotNull(saved, "El juego no guardó la partida por su cuenta");
        Assert.AreEqual(new[] { 0 }, saved.collectedFragments.ToArray(), "No recordó qué fragmento se recogió");
        Assert.IsTrue(saved.hasPlayerPosition, "No guardó dónde está Alex");
        Assert.AreEqual(0, saved.caseIndex, "Ningún caso estaba cerrado todavía");

        int score = GameSession.Score;
        var spot = new Vector2(saved.playerX, saved.playerY);

        Assert.IsTrue(GameSession.Continue(), "El menú no pudo retomar la partida");
        GameSessionBootstrap.Started = true;
        yield return SceneManager.LoadSceneAsync("GameScene");
        yield return null;

        var resumed = GameManager.Instance;
        Assert.IsNotNull(resumed, "GameManager no encontrado al continuar");
        Assert.AreEqual(score, GameSession.Score, "Los puntos no se conservaron");
        Assert.AreEqual(1, GameSession.Fragments, "El contador de fragmentos no se restauró");
        Assert.IsFalse(resumed.fragments[0].gameObject.activeSelf,
                       "El fragmento ya recogido volvió a aparecer en el mapa");

        var reborn = Object.FindObjectOfType<PlayerController>();
        Assert.Less(Vector2.Distance(reborn.transform.position, spot), PositionTolerance,
                    $"Alex apareció en {reborn.transform.position} y no en {spot}");
    }

    static IEnumerator CloseDialogue(DialogueController dialogue)
    {
        for (int guard = 0; dialogue.IsOpen && guard < 60; guard++)
        {
            dialogue.Advance();
            yield return new WaitForSecondsRealtime(0.05f);
        }
    }
}
