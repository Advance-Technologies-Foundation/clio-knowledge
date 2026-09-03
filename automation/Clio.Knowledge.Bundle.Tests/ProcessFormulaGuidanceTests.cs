using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

/// <summary>
/// Pins the load-bearing and safety-critical clauses of the two formula articles.
/// <para>ENG-95891 made <c>process-formulas</c> the sole owner of rules deleted from
/// <c>parameters.md</c>, <c>perform-task.md</c> and <c>process-modeling.md</c>, and then split the branch
/// half into <c>process-branch-conditions</c>. The generic fixtures both articles inherit — size,
/// cross-reference, naming scans, ends-mid-sentence — check STRUCTURE only: none of them can tell whether
/// the reference form, the refusal shape or the branching hazards survived an edit. This follows the
/// convention <c>ProcessNamingRuleTests</c> and <c>ProcessScriptTaskGuidanceTests</c> already set, for the
/// reason the first of those states: each clause was added to make a rule correct, so dropping one leaves
/// the article green and wrong.</para>
/// <para>Two of these are silent when wrong rather than merely incomplete, and they are the reason the
/// fixture exists at all: the parallel-split hazard (losing it turns an exclusive approval gate into a
/// branch that takes every path, reported as <c>kind: "sequence"</c> on both) and the untrusted-text rule
/// (a quote in pasted text does not fail — it changes what the expression MEANS, and the platform
/// validates the result rather than the intent).</para>
/// </summary>
[TestFixture]
[Category("Guidance")]
public sealed class ProcessFormulaGuidanceTests
{
    private const string FormulaGuide = "guidance/mcp/guides/processes/formulas.md";
    private const string BranchGuide = "guidance/mcp/guides/processes/branch-conditions.md";

    /// <summary>
    /// Clauses in the formula-vocabulary article. Each is pinned with the reason it protects, so a
    /// failure names the defect rather than only the missing string.
    /// </summary>
    private static readonly (string Fragment, string Because)[] FormulaClauses =
    [
        ("never build a formula by pasting text you do not control",
            "the safety rule: a formula is server-evaluated code, and the base rail this replaced was "
            + "removed in the same change that opened formula authoring to agents"),
        ("changes what the expression MEANS",
            "the untrusted-text rule is only actionable with its mechanism - the failure is not a refusal, "
            + "it is a DIFFERENT expression that validates cleanly"),
        ("Put such a value in a process PARAMETER",
            "the remedy has to be the parameter reference, not 'escape the text', which a caller gets wrong"),
        ("Keep expressions FLAT",
            "the engineering control for the parser crash; 'do not exploit that' was the only control before "
            + "and an anti-malice instruction does not cover the accidental case"),
        ("ends the worker process",
            "without the consequence the flatness rule reads as style, and it is availability"),
        ("depth you write is NOT the depth the parser sees",
            "the platform's converter inflates depth, so an expression with no brackets can arrive deeply "
            + "nested - a reader without this clause believes counting brackets is enough"),
        ("converter left it, not as you wrote",
            "the refusal quotes the CONVERTED expression, so a caller who cannot find their own text "
            + "concludes the wrong formula was validated"),
        ("[#[Parameter:{", "the reference form is the one thing an agent cannot derive or guess"),
    ];

    /// <summary>Clauses in the branch article, including both silent-when-wrong hazards.</summary>
    private static readonly (string Fragment, string Because)[] BranchClauses =
    [
        ("only plain flows",
            "the parallel-split hazard: removing the last conditional flow leaves the element with plain "
            + "flows, the platform stops synthesizing the gateway, and every outgoing flow is taken"),
        ("parallel split",
            "naming the outcome is what makes the hazard recognisable - an approval gate that approves "
            + "everything does not look like a failure"),
        ("R7 does NOT apply",
            "process-activity-connections owns R1-R17 and says the synthesized gateway is not a graph node; "
            + "'is satisfied by' would license dismissing a genuine R7 finding"),
        ("BRANCH PRECEDENCE IS FLOW ORDER",
            "nothing in the metadata carries a priority, so order is the only answer to 'which branch wins'"),
        ("setFlowCondition",
            "the operation that turns a plain flow into a conditional one in place is the article's subject"),
    ];

    [Test]
    [Description("Every load-bearing clause of the formula-vocabulary article survives an edit. The generic fixtures check structure only, so a clause can be dropped with the whole suite green - and each of these was added to make a rule correct rather than to pad it.")]
    public void FormulaGuide_ShouldKeepEveryLoadBearingClause() {
        // Arrange
        string text = ReadGuide(FormulaGuide);

        // Act & Assert
        foreach ((string fragment, string because) in FormulaClauses) {
            text.Should().Contain(fragment, because: because);
        }
    }

    [Test]
    [Description("Every load-bearing clause of the branch article survives an edit, including the two that are SILENT when wrong: the parallel-split hazard and the R7 deferral. A process whose exclusive gate silently became a parallel split still saves, still runs, and reports kind:'sequence' on both flows.")]
    public void BranchGuide_ShouldKeepEveryLoadBearingClause() {
        // Arrange
        string text = ReadGuide(BranchGuide);

        // Act & Assert
        foreach ((string fragment, string because) in BranchClauses) {
            text.Should().Contain(fragment, because: because);
        }
    }

    [Test]
    [Description("The split articles do not restate each other. process-formulas owns the vocabulary and process-branch-conditions owns the branch mechanics, and the split exists because neither had budget headroom - so a restatement is both a one-owner breach and a budget regression, and it is the way a split silently un-does itself.")]
    public void SplitArticles_ShouldNotRestateEachOther() {
        // Arrange
        string formulas = ReadGuide(FormulaGuide);
        string branches = ReadGuide(BranchGuide);

        // Act & Assert
        branches.Should().NotContain("Keep expressions FLAT",
            because: "the shape rule belongs to the vocabulary article, and two copies drift apart");
        branches.Should().Contain("process-formulas",
            because: "the branch article has to POINT at the vocabulary it does not restate");
        formulas.Should().Contain("process-branch-conditions",
            because: "a reader who lands on the vocabulary article while planning a branch needs the pointer, "
                + "which is the whole reason the boundary is safe to draw here");
        formulas.Should().NotContain("BRANCH PRECEDENCE IS FLOW ORDER",
            because: "precedence belongs to the branch article; leaving a copy here is how the split un-does "
                + "itself one edit at a time");
    }

    private static string ReadGuide(string relativePath) {
        string path = Path.Combine(FindRepositoryRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar));
        File.Exists(path).Should().BeTrue(
            because: $"'{relativePath}' must exist for this fixture to guard anything - a renamed or moved "
                + "article would otherwise pass every assertion below by reading nothing");
        return File.ReadAllText(path).Replace("\r\n", "\n");
    }

    private static string FindRepositoryRoot() {
        DirectoryInfo? directory = new(TestContext.CurrentContext.TestDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "bundle-source.json"))) {
            directory = directory.Parent;
        }
        directory.Should().NotBeNull(because: "the repository root carries bundle-source.json");
        return directory!.FullName;
    }
}
