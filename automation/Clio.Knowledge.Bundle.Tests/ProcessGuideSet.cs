namespace Clio.Knowledge.Bundle.Tests;

/// <summary>
/// The one place that knows which articles ENG-96212 split <c>process-modeling</c> into.
///
/// Three fixtures assert over this set — the response-size contract, the naming-rule scans, and the
/// feature-gate decision — and each of them used to carry its own hand-maintained copy of the list.
/// That is a silent-coverage-loss shape: an eighth article added to one array and not the others
/// keeps every test green while going unguarded by two of the three. Adding it here reaches all of
/// them at once.
/// </summary>
internal static class ProcessGuideSet
{
    /// <summary>Articles the split produced, by <c>itemId</c>, entry article first.</summary>
    internal static readonly string[] SplitItemIds =
    [
        "process-modeling",
        "process-naming",
        "process-data-elements",
        "process-parameters",
        "process-perform-task",
        "process-send-email",
        "process-activity-connections"
    ];

    /// <summary>The same articles as repository-relative source paths, in the same order.</summary>
    internal static readonly string[] SplitPaths =
    [
        "guidance/mcp/guides/processes/process-modeling.md",
        "guidance/mcp/guides/processes/naming.md",
        "guidance/mcp/guides/processes/data-elements.md",
        "guidance/mcp/guides/processes/parameters.md",
        "guidance/mcp/guides/processes/perform-task.md",
        "guidance/mcp/guides/processes/send-email.md",
        "guidance/mcp/guides/processes/activity-connections.md"
    ];

    /// <summary>
    /// Process-folder articles the split did not touch. They share the response-size contract —
    /// the limit is a property of the transport, not of this change — but they are NOT part of the
    /// ENG-96132 go-live decision, so gate assertions must not sweep them up.
    /// </summary>
    internal static readonly string[] UnrelatedPaths =
    [
        "guidance/mcp/guides/processes/process-script-task.md",
        "guidance/mcp/guides/processes/run-process-button.md"
    ];

    /// <summary>Every article under the processes folder, split and unrelated alike.</summary>
    internal static string[] AllPaths => [.. SplitPaths, .. UnrelatedPaths];
}
