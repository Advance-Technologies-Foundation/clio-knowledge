using FluentAssertions;
using NUnit.Framework;

namespace Clio.Knowledge.Bundle.Tests;

/// <summary>
/// Keeps the stepper active-step condition rules in the business-rules guide. A rule on the generated
/// <c>&lt;StepperName&gt;_ActiveStep</c> of a stepper whose <c>activeStep</c> is bound saves without error and never
/// fires, so the guide must name both forms and forbid the generated name on a bound stepper.
/// </summary>
[TestFixture]
public sealed class StepperActiveStepGuidanceTests
{
    private const string GuidePath = "guidance/mcp/guides/business-rules/business-rules.md";

    private static string Root()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "bundle-source.json")))
        {
            directory = directory.Parent;
        }
        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }

    private static string Normalized(string text) =>
        string.Join(" ", text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private static string Guide() => Normalized(File.ReadAllText(Path.Combine(Root(), GuidePath)));

    [Test]
    public void Guide_Names_Bound_And_Generated_Active_Step_Attributes()
    {
        string guide = Guide();

        guide.Should().Contain("bound to a plain `$Attr` (`\"activeStep\": \"$RentalStep\"`) that the page declares in `viewModelConfig.attributes` without a data source: the bound attribute",
            because: "a bound stepper keeps its open step in the bound attribute, and only a declared attribute lets it change step");
        guide.Should().Contain("unbound: the generated `<StepperName>_ActiveStep`",
            because: "an unbound stepper exposes the generated attribute");
    }

    [Test]
    public void Guide_Forbids_Generated_Name_On_Bound_Stepper()
    {
        Guide().Should().Contain(
            "Never use the generated name on a bound stepper.",
            because: "on a clio that accepts it, the rule saves and silently never fires");
    }
}
