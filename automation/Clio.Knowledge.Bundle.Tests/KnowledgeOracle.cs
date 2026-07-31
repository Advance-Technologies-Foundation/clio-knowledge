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
    /// Clio bytes captured at commit <c>49783ca4</c>, after Clio advanced 329 commits past the
    /// initial migration. Migration tests compare published articles against this capture.
    /// </summary>
    internal const string CurrentDirectory = "fixtures/oracles/clio-guidance-v1";

    /// <summary>
    /// Stable IDs whose published text is intentionally no longer byte-identical to Clio.
    /// Content ownership sits in this repository, so an article may be corrected, extended, or
    /// re-linked without a Clio release; listing the ID here is how that decision is recorded and
    /// reviewed. Every article absent from this set must still match the current oracle exactly.
    /// </summary>
    internal static readonly IReadOnlySet<string> IndependentlyEditedArticles =
        new HashSet<string>(StringComparer.Ordinal)
        {
            // Supporting reference articles were extracted into references/, so the parent guide
            // links to independently published content instead of restating it.
            "atf-repository-dev",
            "atf-repository-model-management",
            "atf-repository-tests",
            "composable-app-e2e-test-implementation",
            "configuration-entity-event-listener",
            "configuration-entity-event-listener-tests",
            "configuration-webservice",
            "configuration-webservice-tests",
            "creatio-composable-app-development",
            "creatio-freedom-iframe-section",
            "feature-toggle",
            "feature-toggle-tests",
            "sys-setting",
            "sys-setting-tests",
            // Expanded here after the snapshot; Clio never carried the added material.
            "page-schema-handlers",
            // Carry both an edit made here and an edit made in Clio, reconciled by a three-way merge.
            "mobile-page-modification",
            "page-modification"
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
