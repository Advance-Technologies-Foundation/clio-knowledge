# Portal routing and external organizations: validation evidence

Validation date: 2026-09-21. Creatio 10.1.585.0, .NET 8, PostgreSQL; Clio 8.1.0.132;
installed guidance generation 1.15.47. This is a bounded lab observation, not a cross-version guarantee.

## Fixture and result

An exclusive local lab used a standalone configuration package with three `BaseService` classes:
default-only, SSP-only and dual-route. Each returned its authenticated user ID, contact ID,
connection CLR type and a fixed build revision. The package was built, activated by restart and
its revision read back. No service switched to a system connection or accepted a target user ID.

| Acceptance family | Cases | Result |
| --- | ---: | --- |
| Three services × two route prefixes × employee and three external identities | 24 | Passed |
| Anonymous default and SSP routes | 2 | Passed; default HTTP 302, SSP HTTP 403 |
| Native external-user account link and role membership | 2 | Passed |
| External-organization role kind/domain/Account/parent | 2 | Passed |
| Contact has an Account but login has no portal organization | 1 | Passed |
| Total NUnit integration cases | 31 | Passed |

For registered routes, matching identity domains returned HTTP 200; mismatched domains returned 403.
The absent counterpart of a single-route service returned 404. Each successful request returned the
exact expected user and connection class. An unaffiliated external user also succeeded on SSP routes.

Accounts and contacts were created through Clio MCP `execute-dataservice-batch`. Organization entity
saves used `VwSspAdminUnit`. Users were created through `manage-user external=true` with process-only
password variables. Existing external users were associated using the native
`SspUserManagementService.CreateUsersByContactsIds` service. Independent `execute-esq` and
`inspect-role` calls read back the graph. Route tests used `creatio.client` 2.0.2; the inspected Clio
MCP catalog had no generic service-call tool. Anonymous tests used a no-cookie, no-redirect client.

The two assigned users had type 4 / connection type 1, different Contacts and PortalAccounts, and
membership in their respective external-organization roles. Those roles had type 0 / connection
type 1 and the matching PortalAccount. All generic `SysAdminUnit.Account` fields were empty.
The unaffiliated user's Contact.Account was populated but its PortalAccount was empty.

## Explanatory source observations

Core trunk revision `e0d0f98b80c8fd26e305804c7cb3242b76baf072`:
`SspServiceRouteAttribute`, `DefaultServiceRouteAttribute`, `CustomServiceRouteProvider`,
`SspConfigServiceRouteProvider`, `SSPAuthorizeFilter`, `SspRouteAccessValidator` and `BaseService`.
These explain prefixes, explicit route selection, existing-session connection checks and the
service-list exclusion branch. They are internal explanatory evidence; readers need no source checkout.

Installed 10.1.585 configuration sources: `SspUserAdministrator`, `SspUserCreator`,
`SspUserManagementService`, `SspUserManagementServiceHelper`, `VwSspAdminUnitEventListener`.
These explain native account binding, membership, type/domain values and the organization lookup.
The deployed schema metadata independently confirmed `PortalAccount`, `Account` and `Contact`
as separate lookups.

Public terminology/setup reference:
[Manage Portal in the main application](https://academy.creatio.com/guides/creatio-apps/products/more-apps/portal/manage-portal-in-main-application).

## Boundaries and repeatability

The local fixture, request/response evidence, build log, test runner and TRX are retained with the lab;
they are not a published immutable reference repository. This document records acceptance, not a
downloadable reference release. Re-run the fixture before extending these claims to a new version.

No .NET Framework execution, SOAP/OAuth matrix, browser setup validation, email invitation,
organization transfer, licensing algorithm or custom business-data isolation was tested.
The initial FSM toggle returned a partial-success error; effective mode was read as on, then export
and initial compilation completed before package linking. No failed setup receipt was treated as success.
