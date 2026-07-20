# Fixed Artifact Sources

## Test Packages Source

- Repository root:
  - `http://tscore-svn.tscrm.com:8050/svn/ts5conf/PackageStore/`
- Required packages:
  - `AutoTest`

Skill preflight lists/export artifacts into a local cache and searches for package artifacts
(`*.gz` / `*.zip`) when local artifacts are not found.

## Scope Note

This skill no longer auto-installs licenses or test users from SVN sources.

## Auth Note

SVN endpoints may require credentials.
If unauthenticated requests fail, provide credentials via svn auth options or stored credentials.
Current implementation uses SVN CLI `--username/--password` flags (credentials are not echoed by the script).
