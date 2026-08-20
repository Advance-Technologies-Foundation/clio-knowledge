clio MCP describe-environment guide

PURPOSE
- describe-environment returns ONE source-independent JSON report for a Creatio instance.
  The field SET is the same with or without cliogate; only the cliogate-only fields drop out
  when cliogate is absent. Reason about every environment the same way.
- Read-only. It never mutates the environment.
- Target the environment with environment-name (PREFERRED). uri/login/password is an emergency
  fallback only when no environment is registered.

REQUIRED BASE PROBE FAILURES (exit 1)
- Invalid URI          -> use an absolute HTTP or HTTPS application URL.
- Unreachable/timeout  -> verify the URL and make sure the application is running.
- Authentication       -> verify credentials and authentication settings.
- Reachable non-Creatio content -> the URL does not appear to be a Creatio application.
- Malformed/unusable ApplicationInfoService response -> stable unexpected-response error.
These classifications are identical through CLI aliases and the MCP error envelope. Normal output
never contains raw HTML, response bodies, parser exceptions, credentials, cookies, or tokens.
Debug output adds only safe classification/type/status metadata.

BEST-EFFORT CONTRACT (exit 0 even when an optional source is missing)
The report is assembled from up to three sources, in order. A field's ABSENCE means the source
that supplies it was unavailable (older Creatio, cliogate not installed, or the caller lacks the
admin permission) — it is NOT an error. Only productName and licenseInfo strictly require cliogate.

1) ALWAYS — ApplicationInfoService.GetApplicationInfo (authenticated session, no permission gate,
   no cliogate):
   - coreVersion           Creatio platform version (e.g. "8.2.1.xxxx"). Use this to decide whether
                           a component/feature exists before assuming availability.
   - environmentType       Configured environment label (SysSetting "EnvironmentType"), may be "".
   - maintainer            Package maintainer code (e.g. "Customer", "Creatio").
   - user / userContact / userAccount   Logged-in user, contact and account (id + display name).
   - userCulture / primaryCulture / primaryLanguage   Locale of the user and of the system.
   - userTimezoneOffset / userTimezoneCode            User timezone (minutes offset + code).
   - workspace             Current workspace (id + display name).
   - moneyDisplayPrecision / maxEntitySchemaNameLength / freedomUiSchemaVersion   Platform limits.

2) WITHOUT cliogate — ApplicationInfoService.GetSystemEnvironmentInfo (admin-gated POST; requires
   the CanManageSolution system operation; exists only on newer Creatio):
   - dbEngineType          Database engine: "MSSql" | "PostgreSql" | "Oracle".
   - frameworkKind         Executing framework family: "Net" (.NET / .NET Core) | "NetFramework".
   - frameworkDescription  Detailed runtime string (e.g. ".NET 8.0.11", ".NET Framework 4.8").
   If CanManageSolution is not granted or the operation is absent, these are skipped silently
   (and may instead be backfilled from cliogate in step 3).

3) cliogate ONLY — GET /rest/CreatioApiGateway/GetSysInfo (normally supplied by cliogate
   >= 2.0.0.32):
   - productName           Creatio product/edition name (e.g. "studio"). NO core web service
                           exposes this — it is the one field that always needs cliogate.
   - licenseInfo           License metadata object: CustomerId, IsDemoMode (and related fields).
                           NOTE: CustomerId is the customer's licensing identifier — treat it as
                           sensitive; do not echo or paste it outside this environment context.
   cliogate also BACKFILLS dbEngineType / frameworkKind / frameworkDescription when step 2 did
   not provide them (older Creatio without GetSystemEnvironmentInfo), keeping the shape consistent.
   clio probes GetSysInfo as the authoritative capability check. Installed-package metadata is
   consulted only after that probe fails, so a stale inactive cliogate alias cannot veto a working
   endpoint.

WHEN TO USE
- Verify the platform version (coreVersion) before planning page/component work — pair with the
  get-component-info "latest-fallback" warning.
- Read dbEngineType + frameworkKind/frameworkDescription for deploy and troubleshooting decisions
  (now available WITHOUT cliogate when CanManageSolution is granted).
- Confirm productName / license status before applying edition-specific features.

INTERPRETING A SPARSE REPORT
- Do NOT infer cliogate installation state from missing fields alone. A sparse report can also mean
  GetSysInfo was denied, unavailable, or returned no usable data.
- When get-info emits a warning with the sparse report, follow its reason:
  - "not installed" -> install cliogate 2.0.0.32+ only if product/license fields are required.
  - "lowest detected cliogate alias version <version> is below required 2.0.0.32" -> update that
    alias when it is used by the target runtime; otherwise verify GetSysInfo access before changing
    a working package.
  - "<version> is installed, but GetSysInfo returned no data" -> do NOT reinstall merely because
    the fields are missing; verify CanManageSolution and GetSysInfo access for the caller.
  - installation/version could not be determined -> inspect the safe --debug classification and
    run list-packages; do not claim cliogate is absent without evidence.
- No warning with missing productName/licenseInfo -> GetSysInfo returned a usable but partial
  SysInfo object. Missing optional fields alone do not imply an installation or compatibility
  failure; do not reinstall cliogate without another failure signal.

EVIDENCE AND VERSION BOUNDARY
- GitHub issue Advance-Technologies-Foundation/clio#1138 captured a real false-positive warning:
  package listing reported cliogate 2.0.0.45 and gate-dependent commands worked, while get-info
  claimed cliogate 2.0.0.32+ was absent or incompatible.
- The capability-first warning rules apply to clio builds containing the #1138 fix. Older clio
  8.1 builds can emit the former generic warning; verify with list-packages before acting on it.
