using System;
using System.Text.RegularExpressions;
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
    private const string ModelingGuide = "guidance/mcp/guides/processes/process-modeling.md";
    private const string DataElementsGuide = "guidance/mcp/guides/processes/data-elements.md";

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
        ("[#[Element:{",
            "the ELEMENT-scoped form, which occurs ONCE in this article and was pinned by nothing: an agent "
            + "who has the process-parameter form still cannot derive this one, and losing it costs the "
            + "commonest mapping there is - one element's output into another element's input"),
        ("never `1.2m`",
            "a blind run measured an agent writing the C# decimal suffix after READING this article: the "
            + "converter appends its own unconditionally, so the literal arrives as 1.2mm and is refused - "
            + "the suffix was documented only as something refusals SHOW you, which is too late"),
    ];

    /// <summary>Clauses in the branch article, including both silent-when-wrong hazards.</summary>
    private static readonly (string Fragment, string Because)[] BranchClauses =
    [
        ("Do not leave a branching element with only plain flows",
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

    [Test]
    [Description("Two claims this work introduced are stated as platform absolutes and the platform disagrees. A sub-process does NOT hold its children in the schema's own FlowElements - ProcessSchemaSubProcess implements IProcessSchemaFlowElementsContainer and owns its own collection - so describe-business-process, which iterates schema.FlowElements, does not see them; the delete guards do, because they walk GetBaseElements/GetParametrizedElements recursively. And a read record's column IS reachable in three segments: on the flow-condition path TryGetParameterMapPath puts an EntityColumn segment into SubParameterMetaPath and carries it. Both absolutes read as CLOSED questions, which is exactly how a reader stops looking - the practical limit is that describe hands out no column UIds, not that the platform refuses.")]
    public void ProcessGuides_ShouldNotOverstateTwoPlatformLimits() {
        // Arrange
        string modeling = ReadGuide(ModelingGuide);
        string dataElements = ReadGuide(DataElementsGuide);

        // Act & Assert
        modeling.Should().NotContain("describe-business-process` and the delete guards do see them",
            because: "a sub-process owns its own FlowElements collection and ProcessDescriber iterates only "
                + "schema.FlowElements, so describe does NOT see them - and a caller told otherwise reads a "
                + "delete refusal naming a flow no read API will show, which is the dead end this sentence "
                + "claims to prevent");
        modeling.Should().Contain("the delete guards see them",
            because: "the true half has to survive the correction: the guards walk the recursive accessors, "
                + "so a reference from inside a sub-process really does block a delete");
        dataElements.Should().NotContain("referenceable from NOWHERE",
            because: "the flow-condition path parses a third segment - FillMatchedData routes EntityColumn "
                + "into SubParameterMetaPath and TryGetParameterMapPath carries it - so the platform does not "
                + "refuse what this calls impossible");
        dataElements.Should().Contain("describe reports no column UIds",
            because: "the real limit is discoverability, and it is the half a reader can act on: you cannot "
                + "author a segment whose UId no read API hands you");
    }

    [Test]
    [Description("A bullet inserted into this list does not inherit the tail of the bullet it split. Three cuts happened here across two commits, and each left the previous bullet's closing qualifier attached to the NEW bullet - so the absolute prohibition 'never 1.2m' came to end in 'Stay inside the guided set unless you have a reason not to', which is an escape hatch on the one control that exists: the converter is Terrasoft.Core, so the sentence is the only place this can be stopped. The same cut left the registry bullet asserting nothing about the reachable surface being wider than the documented one - the premise the SAFETY bullet rests on.")]
    public void FormulaGuide_ShouldNotLetABulletInheritTheTailOfTheOneItSplit() {
        // Arrange
        string[] bullets = Regex.Split(ReadGuide(FormulaGuide), @"^- ", RegexOptions.Multiline);

        // Act
        string prohibition = bullets.Single(bullet => bullet.Contains("never `1.2m`"));
        string registry = bullets.Single(bullet => bullet.Contains("`Convert`, `TimeSpan`"));

        // Assert
        prohibition.Should().NotContain("unless you have a reason not to",
            because: "an absolute prohibition that ends in an escape hatch is not one - and this is the "
                + "prohibition a blind run measured an agent breaking after READING the article, on the "
                + "one control available, the converter being platform-owned");
        registry.Should().Contain("GUIDED set",
            because: "the scope qualifier belongs to the bullet that lists the registry: detached from it, "
                + "nothing states that the reachable surface is wider than the documented one, which is the "
                + "premise the SAFETY bullet argues from");
        registry.Should().Contain("IS refused",
            because: "the sentence about an absent identifier being refused by name is the second half of "
                + "the registry bullet's own thought, and it was left orphaned as a bullet starting "
                + "mid-sentence by the same splice");
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
