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
        guide.Should().Contain("A constant step name (`\"activeStep\": \"DeliveryStep\"`) only sets the opening step and uses the generated attribute too.",
            because: "a constant activeStep is not a binding and exposes the generated attribute");
        guide.Should().Contain("a property binding (`$Attr:prop`)",
            because: "a property binding leaves the stepper unprepared, like a nested path");
    }

    [Test]
    public void Guide_States_Version_Boundaries()
    {
        string guide = Guide();

        guide.Should().Contain("`RentalStep`); requires clio 8.1.0.150 or later.",
            because: "the bound attribute is offered from clio 8.1.0.150");
        guide.Should().Contain("`RentalStepper_ActiveStep`); requires clio 8.1.0.148 or later.",
            because: "the generated attribute is offered from clio 8.1.0.148");
        guide.Should().Contain("a dotted stepper name requires clio 8.1.0.150 or later.",
            because: "clio 8.1.0.148-8.1.0.149 keep the dots and the rule never fires");
        guide.Should().Contain("on clio below 8.1.0.150 a bound stepper has no working active-step condition",
            because: "clio 8.1.0.148-8.1.0.149 save a generated-name rule on a bound stepper that never fires");
    }

    [Test]
    public void Guide_Requires_Not_Filled_In_Clause_For_First_Step()
    {
        Guide().Should().Contain(
            "a rule for the first step also needs an `is-not-filled-in` condition on the attribute, joined with OR (`RentalStepper_ActiveStep = <FirstStepName> OR RentalStepper_ActiveStep is not filled in`)",
            because: "the attribute stays empty until the first step change, so an equal-only rule misses the first step");
    }

    [Test]
    public void Guide_Forbids_Generated_Name_On_Bound_Stepper()
    {
        Guide().Should().Contain(
            "Never use the generated name on a bound stepper.",
            because: "on a clio that accepts it, the rule saves and silently never fires");
    }
}
