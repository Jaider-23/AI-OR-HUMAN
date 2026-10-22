using NUnit.Framework;

public class ScoreRulesTests
{
    [Test]
    public void CorrectAnswerWithHighConfidenceGives150()
    {
        var outcome = ScoreRules.Evaluate(Verdict.AI, Verdict.AI, ConfidenceLevel.High, 0, false, false);

        Assert.IsTrue(outcome.IsCorrect);
        Assert.AreEqual(150, outcome.Points);
    }

    [Test]
    public void WrongAnswerWithHighConfidenceCosts100()
    {
        var outcome = ScoreRules.Evaluate(Verdict.AI, Verdict.Human, ConfidenceLevel.High, 0, false, false);

        Assert.IsFalse(outcome.IsCorrect);
        Assert.AreEqual(-100, outcome.Points);
    }

    [Test]
    public void MediumConfidenceMatchesBaseRules()
    {
        Assert.AreEqual(100, ScoreRules.Evaluate(Verdict.Human, Verdict.Human, ConfidenceLevel.Medium, 0, false, false).Points);
        Assert.AreEqual(-50, ScoreRules.Evaluate(Verdict.Human, Verdict.AI, ConfidenceLevel.Medium, 0, false, false).Points);
    }

    [Test]
    public void EachRelevantToolAdds25()
    {
        var outcome = ScoreRules.Evaluate(Verdict.Human, Verdict.Human, ConfidenceLevel.Medium, 2, false, false);

        Assert.AreEqual(150, outcome.Points);
    }

    [Test]
    public void SharingFakeContentCosts100AndReputation()
    {
        var outcome = ScoreRules.Evaluate(Verdict.Manipulated, Verdict.Manipulated, ConfidenceLevel.Medium, 0, true, true);

        Assert.IsTrue(outcome.SharedFakeContent);
        Assert.AreEqual(0, outcome.Points);
        Assert.Less(outcome.ReputationDelta, 0);
    }

    [Test]
    public void SharingGenuineHumanContentIsNotPenalized()
    {
        var outcome = ScoreRules.Evaluate(Verdict.Human, Verdict.Human, ConfidenceLevel.Medium, 0, true, true);

        Assert.IsFalse(outcome.SharedFakeContent);
        Assert.AreEqual(100, outcome.Points);
    }

    [Test]
    public void NotSharingFakeContentImprovesReputation()
    {
        var shared = ScoreRules.Evaluate(Verdict.AI, Verdict.AI, ConfidenceLevel.Medium, 0, true, true);
        var withheld = ScoreRules.Evaluate(Verdict.AI, Verdict.AI, ConfidenceLevel.Medium, 0, true, false);

        Assert.Greater(withheld.ReputationDelta, shared.ReputationDelta);
    }

    [Test]
    public void ShareChoiceIgnoredWhenCaseDoesNotAllowSharing()
    {
        var outcome = ScoreRules.Evaluate(Verdict.AI, Verdict.AI, ConfidenceLevel.Medium, 0, false, true);

        Assert.IsFalse(outcome.SharedFakeContent);
        Assert.AreEqual(100, outcome.Points);
    }

    [Test]
    public void ReputationIsClampedBetween0And100()
    {
        Assert.AreEqual(0, ScoreRules.ClampReputation(-30));
        Assert.AreEqual(100, ScoreRules.ClampReputation(130));
        Assert.AreEqual(55, ScoreRules.ClampReputation(55));
    }

    [Test]
    public void RankRewardsHighAverageAndReputation()
    {
        Assert.AreEqual("ANALISTA EXPERTO", EndScreen.RankFor(1600, 90, 11));
        Assert.AreEqual("EN FORMACIÓN", EndScreen.RankFor(0, 40, 11));
    }
}
