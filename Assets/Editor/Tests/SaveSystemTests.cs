using System;
using System.Globalization;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Pruebas del autoguardado. Usan PlayerPrefs de verdad, así que guardan el valor real
/// del jugador antes de empezar y lo devuelven al terminar.
/// </summary>
public class SaveSystemTests
{
    string backup;
    bool hadSave;

    [SetUp]
    public void SetUp()
    {
        SaveSystem.Enabled = true;
        hadSave = PlayerPrefs.HasKey(SaveSystem.Key);
        backup = PlayerPrefs.GetString(SaveSystem.Key, string.Empty);
        SaveSystem.Delete();
    }

    [TearDown]
    public void TearDown()
    {
        GameSession.StartNew(false);
        if (hadSave) PlayerPrefs.SetString(SaveSystem.Key, backup);
        else PlayerPrefs.DeleteKey(SaveSystem.Key);
        PlayerPrefs.Save();
    }

    static SaveData Sample()
    {
        var data = new SaveData
        {
            caseIndex = 3,
            lastLevel = 2,
            score = 320,
            reputation = 78,
            totalCases = 11,
            hasPlayerPosition = true,
            playerX = 12.5f,
            playerY = 7.25f
        };
        data.collectedFragments.AddRange(new[] { 0, 2, 5 });
        return data;
    }

    [Test]
    public void NoHayGuardadoAlEmpezar()
    {
        Assert.IsNull(SaveSystem.Load());
        Assert.IsFalse(SaveSystem.HasSave);
    }

    [Test]
    public void GuardarYCargarConservaElProgreso()
    {
        SaveSystem.Write(Sample());

        var loaded = SaveSystem.Load();

        Assert.IsNotNull(loaded);
        Assert.AreEqual(3, loaded.caseIndex);
        Assert.AreEqual(2, loaded.lastLevel);
        Assert.AreEqual(320, loaded.score);
        Assert.AreEqual(78, loaded.reputation);
        Assert.AreEqual(11, loaded.totalCases);
        Assert.AreEqual(new[] { 0, 2, 5 }, loaded.collectedFragments.ToArray());
        Assert.IsTrue(loaded.hasPlayerPosition);
        Assert.AreEqual(12.5f, loaded.playerX, 0.001f);
        Assert.AreEqual(7.25f, loaded.playerY, 0.001f);
    }

    [Test]
    public void GuardarSellaLaFechaEnFormatoLegible()
    {
        SaveSystem.Write(Sample());

        string stamp = SaveSystem.Load().savedAtUtc;

        Assert.IsTrue(DateTime.TryParse(stamp, CultureInfo.InvariantCulture,
                                        DateTimeStyles.RoundtripKind, out _), $"fecha ilegible: {stamp}");
    }

    [Test]
    public void UnGuardadoCorruptoSeDescarta()
    {
        PlayerPrefs.SetString(SaveSystem.Key, "{esto no es json");

        Assert.IsNull(SaveSystem.Load());
        Assert.IsFalse(PlayerPrefs.HasKey(SaveSystem.Key), "el guardado ilegible debe borrarse");
    }

    [Test]
    public void UnGuardadoDeOtraVersionSeDescarta()
    {
        PlayerPrefs.SetString(SaveSystem.Key, "{\"version\":99,\"caseIndex\":1,\"totalCases\":11}");

        Assert.IsNull(SaveSystem.Load());
    }

    [Test]
    public void UnaPartidaTerminadaNoSePuedeContinuar()
    {
        var finished = Sample();
        finished.caseIndex = finished.totalCases;
        SaveSystem.Write(finished);

        Assert.IsNull(SaveSystem.Load());
    }

    [Test]
    public void ContinuarRestauraLaSesion()
    {
        SaveSystem.Write(Sample());

        Assert.IsTrue(GameSession.Continue());
        Assert.IsFalse(GameSession.TrainingMode);
        Assert.AreEqual(3, GameSession.CaseIndex);
        Assert.AreEqual(320, GameSession.Score);
        Assert.AreEqual(78, GameSession.Reputation);
        Assert.AreEqual(3, GameSession.Fragments);
    }

    [Test]
    public void ElGuardadoPendienteSeEntregaUnaSolaVez()
    {
        SaveSystem.Write(Sample());
        GameSession.Continue();

        Assert.IsNotNull(GameSession.ConsumePendingRestore());
        Assert.IsNull(GameSession.ConsumePendingRestore(), "no debe aplicarse dos veces");
    }

    [Test]
    public void ContinuarSinGuardadoNoCambiaNada()
    {
        GameSession.StartNew(false);
        GameSession.AddPoints(50);

        Assert.IsFalse(GameSession.Continue());
        Assert.AreEqual(50, GameSession.Score);
    }

    [Test]
    public void UnaPartidaNuevaBorraElGuardado()
    {
        SaveSystem.Write(Sample());

        GameSession.StartNew(false);

        Assert.IsNull(SaveSystem.Load());
    }

    [Test]
    public void ElEntrenamientoNoBorraElGuardado()
    {
        SaveSystem.Write(Sample());

        GameSession.StartNew(true);

        Assert.IsNotNull(SaveSystem.Load(), "entrenar no debe costarle la partida al jugador");
    }

    [Test]
    public void TerminarLaPartidaBorraElGuardado()
    {
        SaveSystem.Write(Sample());
        GameSession.StartNew(true);   // entrenamiento: no escribe estadísticas reales

        GameSession.FinishGame();

        Assert.IsNull(SaveSystem.Load());
    }

    [Test]
    public void LosFragmentosSeAjustanAlMapaReal()
    {
        SaveSystem.Write(Sample());
        GameSession.Continue();

        GameSession.SetFragments(2);

        Assert.AreEqual(2, GameSession.Fragments);
    }

    [Test]
    public void ElResumenMuestraCasoPuntosYReputacion()
    {
        var text = SaveSystem.Describe(Sample());

        Assert.IsTrue(text.Contains("Caso 04/11"), text);
        Assert.IsTrue(text.Contains("320 pts"), text);
        Assert.IsTrue(text.Contains("78%"), text);
    }

    [Test]
    public void UnPasoCortoDeAlexNoJustificaReescribir()
    {
        var before = Sample();
        var after = Sample();
        after.playerX += 0.1f;

        Assert.IsTrue(SaveData.SameProgress(before, after));
    }

    [Test]
    public void CualquierCambioDeProgresoSeDetecta()
    {
        var before = Sample();

        var moved = Sample();
        moved.playerY += 3f;
        var scored = Sample();
        scored.score += 10;
        var advanced = Sample();
        advanced.caseIndex++;
        var collected = Sample();
        collected.collectedFragments.Add(7);

        Assert.IsFalse(SaveData.SameProgress(before, moved), "posición");
        Assert.IsFalse(SaveData.SameProgress(before, scored), "puntos");
        Assert.IsFalse(SaveData.SameProgress(before, advanced), "caso");
        Assert.IsFalse(SaveData.SameProgress(before, collected), "fragmentos");
        Assert.IsFalse(SaveData.SameProgress(before, null), "sin guardado previo");
    }

    [Test]
    public void LaAntiguedadSeCuentaEnLenguajeNatural()
    {
        string Stamp(TimeSpan ago) =>
            DateTime.UtcNow.Subtract(ago).ToString("o", CultureInfo.InvariantCulture);

        Assert.AreEqual("hace unos segundos", SaveSystem.TimeAgo(Stamp(TimeSpan.FromSeconds(5))));
        Assert.AreEqual("hace 7 min", SaveSystem.TimeAgo(Stamp(TimeSpan.FromMinutes(7))));
        Assert.AreEqual("hace 3 h", SaveSystem.TimeAgo(Stamp(TimeSpan.FromHours(3))));
        Assert.AreEqual("hace 1 día", SaveSystem.TimeAgo(Stamp(TimeSpan.FromDays(1))));
        Assert.AreEqual("hace 4 días", SaveSystem.TimeAgo(Stamp(TimeSpan.FromDays(4))));
        // Un reloj del sistema adelantado no debe producir "hace -2 min".
        Assert.AreEqual("hace unos segundos", SaveSystem.TimeAgo(Stamp(TimeSpan.FromMinutes(-2))));
        Assert.AreEqual(string.Empty, SaveSystem.TimeAgo("fecha inventada"));
        Assert.AreEqual(string.Empty, SaveSystem.TimeAgo(null));
    }
}
