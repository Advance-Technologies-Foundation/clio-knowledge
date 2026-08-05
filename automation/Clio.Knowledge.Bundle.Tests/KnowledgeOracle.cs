namespace Clio.Knowledge.Bundle.Tests;

/// <summary>
/// Locates the captured Clio guidance oracles and records which published articles this
/// repository has deliberately taken ownership of.
/// </summary>
internal static class KnowledgeOracle
{
    /// <summary>
    /// Clio bytes frozen at commit <c>baa34546</c>, the evidence that the initial migration was
    /// byte-preserving. It is a historical record and must not be re-captured.
    /// </summary>
    internal const string InitialMigrationDirectory = "fixtures/oracles/clio-guidance-v0";

    /// <summary>
    /// Clio bytes captured at commit <c>5905e007</c>, the latest Clio master that still compiles
    /// guidance into the assembly. Migration tests compare published articles against this capture,
    /// so it is re-captured whenever Clio master edits an article this repository mirrors.
    /// </summary>
    internal const string CurrentDirectory = "fixtures/oracles/clio-guidance-v1";

    /// <summary>
    /// Stable IDs whose published text is intentionally no longer byte-identical to Clio.
    /// Content ownership sits in this repository, so an article may be corrected, extended, or
    /// re-linked without a Clio release; listing the ID here is how that decision is recorded and
    /// reviewed. Every article absent from this set must still match the current oracle exactly.
    /// </summary>
    /// <remarks>
    /// Each entry permanently un-guards one article, so an ID belongs here only while the byte
    /// comparison actually fails for it. Reference links canonicalized into <c>references/</c> are
    /// already reconciled by <see cref="ReferenceLinkMigration.NormalizeToFrozenLinkText"/> and are
    /// not a reason to list an ID. Before adding one, delete it and run the suite: if nothing goes
    /// red, the entry buys nothing and costs a guard.
    /// </remarks>
    internal static readonly IReadOnlySet<string> IndependentlyEditedArticles =
        new HashSet<string>(StringComparer.Ordinal)
        {
            // Clio's revision made the request catalog (get-request-info
            // crt.RunBusinessProcessRequest) authoritative for the run-process parameter contract
            // and retired run-process-button, an article this repository still publishes. Each of
            // the three below keeps Clio's revision verbatim and adds one pointer to
            // run-process-button as the authoring recipe around that contract; that pointer is the
            // entire divergence, and it is the only reason these IDs are listed.
            "mobile-page-modification",
            // Same pointer, added to the run-a-business-process row of the pre-edit GATE table.
            "page-modification",
            // Same pointer, added to the crt.RunBusinessProcessRequest row of the request table.
            "page-schema-handlers"
        };

    /// <summary>
    /// Stable IDs Clio no longer publishes, so the current oracle carries no bytes to compare
    /// against. This repository keeps them as independent content.
    /// </summary>
    internal static readonly IReadOnlySet<string> ArticlesRetiredInClio =
        new HashSet<string>(StringComparer.Ordinal)
        {
            // Folded into when-to-use-requests in Clio; kept here as a standalone article.
            "run-process-button"
        };

    /// <summary>Path of a stable ID's captured bytes inside the current oracle.</summary>
    internal static string CurrentResourcePath(string id) => $"{CurrentDirectory}/resources/{id}.md";

    /// <summary>Whether a published article must still be byte-identical to the current oracle.</summary>
    internal static bool MirrorsClio(string id) =>
        !IndependentlyEditedArticles.Contains(id) && !ArticlesRetiredInClio.Contains(id);
}
