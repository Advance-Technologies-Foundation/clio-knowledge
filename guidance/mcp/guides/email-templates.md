clio MCP email-template content guide

Scope and applicability
- Use this guide for reading, editing, or copying content for a Creatio marketing email (`BulkEmail`) or message template (`EmailTemplate`).
- Resolve the live schemas for `get-email-template` and `update-email-template` through `get-tool-contract` before the first call. This article owns workflow and storage semantics, not request-schema duplication.
- The dedicated tools require a Creatio environment whose OData surface exposes the host record and relevant content entities. Current-designer behavior was verified on Creatio 10.1.687.0 (.NET Framework 4.8) with `BfEmailTemplate` from `CrtEmailDesigner`; legacy fields were also observed on Creatio 10.1.623.0. Treat other versions as capability-probed: read first and report a missing entity or route instead of inventing a fallback format.

Storage model
- `BulkEmail` and `EmailTemplate` are host records. The `email-id` argument is the host record GUID, not a `BfEmailTemplate` row GUID.
- Current Beefree designer content is stored in `BfEmailTemplate`, keyed by `EmailId`. Preserve `PageJson` as the editable designer source and `PageHtml` as its rendered output; keep `AmpHtml` and `TemplateVersion` with the same variant.
- Legacy marketing-email content is exposed as `BulkEmail.TemplateSubject`, `TemplateBody`, and `TemplateConfig`.
- Primary legacy message-template content is exposed as `EmailTemplate.Subject`, `Body`, `TemplateConfig`, `ConfigType`, and `IsHtmlBody`. Translations are separate `EmailTemplateLang` rows selected by `language-id` (`SysLanguage.Id`).
- Beefree `PageJson` and legacy `TemplateConfig` are different formats. MUST NOT put Beefree JSON into `TemplateConfig`, synthesize one format from the other, or claim that successful legacy HTML opens as editable Beefree content.

Read before write
- Call `get-email-template` immediately before every update. It returns all discovered legacy and Beefree variants plus an independent SHA-256 checksum for each variant.
- For the default Beefree variant, omit `language`. For a specific Beefree language, pass `language`; if that variant does not exist, the response includes `exists=false` and a checksum that authorizes guarded creation.
- For a translated legacy message template, pass `language-id`; if no `EmailTemplateLang` row exists, the response includes `exists=false` and a checksum that authorizes guarded creation. `language-id` is unsupported for `BulkEmail`.
- Keep the checksum only with the exact host, format, and language variant that produced it. A checksum mismatch means another edit occurred; read again and reapply the intended change. MUST NOT substitute a stale checksum or retry blindly.

Update workflow
- Call `update-email-template` with `confirm=true`, the same `email-id`, the exact `format` (`beefree` or `legacy`), the selected language identity, and that variant's latest `expected-checksum`.
- For `beefree`, send complete `page-json` and `page-html`. A missing selected row is created; an existing row is patched. Omitted `amp-html` and `template-version` preserve existing values.
- For `legacy`, send only fields intentionally changed. Omitted values are preserved. `config-type` applies only to the primary `EmailTemplate` row, not `EmailTemplateLang`; `language-id` applies only to message-template translations.
- Read the host again after the write. Verify the selected variant's returned content and checksum; the update receipt alone is not final proof.

Copy without format conversion
- The target `BulkEmail` or `EmailTemplate` host MUST already exist. These tools copy content variants; they do not create host records.
- Canonical copy flow: `get-email-template` on the source -> choose one format/language variant -> `get-email-template` on the target with the same language selector -> `update-email-template` on the target using the target's checksum and the source variant's content -> read the target back.
- Preserve the source format. Copy Beefree to Beefree and legacy to legacy. If the target lacks the selected variant, use the target's returned `exists=false` checksum to create it.
- Repeat the guarded flow per language; do not assume the primary variant represents every translation.

Platform-service boundary
- Creatio source contains `BfEmailTemplateExtendedCopyService` and `BFTemplateTransformerService`, but inspected implementations do not expose a supported public REST endpoint for this workflow. MUST NOT invent a `call-service` route for those internal types.
- Conditional-display metadata maintained by platform-internal copy logic is not represented by the email-content tools. When a source template depends on conditional-display records, report that limitation and verify the copied template in the Creatio designer rather than claiming a complete semantic clone.

Evidence
- Advance-Technologies-Foundation/clio#1218: live `BfEmailTemplate` and legacy storage probes, source inspection, and checksum-guarded Beefree MCP round trip.
- Focused Clio unit and MCP E2E coverage: `EmailTemplateToolTests` and `EmailTemplateToolE2ETests`.
