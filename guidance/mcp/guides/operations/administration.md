clio MCP user and role administration guide

Use this article to administer users, organizational and functional roles, manager groups,
memberships, licenses, IP access rules, delegation and system-operation permissions.
Record/dashboard grants remain owned by `get-guidance name=record-rights` and
`get-guidance name=dashboard-rights`; creating a grantee and granting record access are separate steps.

## Discover the supported contract

This generation requires the inspect-user/manage-user, inspect-role/manage-role,
inspect-access/manage-access and inspect-license/manage-license contracts. Use get-tool-contract
for the selected tool and call it through clio-run when it is not resident. Do not invent a direct
tool call merely because the name appears here. Every call identifies environment-name explicitly.

Inspect tools accept only their listed read actions. Management tools are destructive capabilities;
the user's authorization must cover the specific identities and changes. Preserve supplied scope.
Use exact GUIDs for mutations. Names are discovery filters, not unique identifiers. Read all pages
needed to resolve ambiguity; offset starts at 0 and limit is 1..200. User and role lists filter their
respective identity types before pagination.

The operation-position action requires Creatio 10.1.585.0 and ClioGate 2.0.0.52 or newer. It updates
native ordering and invalidates the backend and browser rights caches; native ordering alone left
cached decisions stale in the verified runtime. Inspect persisted priority after failure before retrying.

The remove-functional and role-redistribute actions require ClioGate 2.0.0.50 or newer. CLI and MCP
both enforce the requirement against the selected environment before mutation. Satisfy it with
install-gate, then retry. Native actions have no dependency on these new gateway methods.

## Identity model and manager meaning

SysAdminUnit stores users and roles. SysAdminUnitTypeValue identifies the kind:

| Value | Kind |
| --- | --- |
| 0 | Organization |
| 1 | Division |
| 2 | Manager group |
| 3 | Team |
| 4 | User |
| 5 | Portal/SSP identity kind |
| 6 | Functional role |
| 7 | Technical user |

ConnectionType also distinguishes internal and external identities. Do not infer identity kind,
login capability or connection type solely from the display name or role hierarchy.

A manager group is a special type-2 CHILD of an organizational role. It is not a person's Contact,
job title or a functional role named "Manager". Assign people to the manager group using add-member.
The backend computes inherited access from the organizational hierarchy. Manager membership can
confer access through subordinate roles and their functional roles; with
UserRolesPermissionsInheritanceByManager enabled, subordinate users and their roles also contribute.
Native non-inheritable role rules still apply. Do not describe manager assignment as a cosmetic label.

Use manage-role action=ensure-manager parent-id=<organization/division GUID>. Creating the parent
can already create its manager child. ensure-manager reuses that child, creates one only if missing,
and rejects multiple matching children. Never call SaveChiefsRole repeatedly: it can create duplicates.
A manager group cannot be moved to a different parent by update. Creating a missing manager group
for an external-user parent is currently unsupported by this tool; existing groups can be inspected.

Functional roles must be parented by another functional role or the built-in All employees
(a29a3ba5-4b0d-de11-9a51-005056c00008) / All external users
(720b771c-e7a7-4f31-9cfb-52cd21c3739f) anchor. The native functional tree excludes arbitrary
organizational parents, making their functional children invisible. The external anchor appears
when PortalUserManagementV2 is enabled. Keep the connection domain consistent when updating.

## Create and maintain users

1. Resolve an existing Contact and verify the intended login is not already used. User creation needs
   a Contact GUID; it does not create a Contact or silently adopt an existing account.
2. Have the operator provision a temporary account password in a process variable named
   `CLIO_ADMIN_PASSWORD_<SUFFIX>`, where SUFFIX uses uppercase letters, digits or underscores.
   This dedicated namespace is an explicit opt-in; never copy unrelated credentials into it.
   The variable must be available to the executing Clio/MCP process and removed after use.
   Pass only its name as password-env. Do not put a password in tool arguments, CLI arguments,
   examples, files, logs, comments or evidence. An agent must not echo the variable's value.
3. Call manage-user action=create with a new id, user-login, contact-id, password-env and the intended
   force-change-password value. external selects an external account. Read the returned identity.
4. Assign roles separately with manage-role action=add-member. The account can persist even if a
   later membership call fails. Inspect state before retrying; do not create a second account.
5. Inspect and assign licenses as a separate step. Account existence is not proof of usable login.

user-login is the target account name. CLI --login remains the administrator's connection credential;
it does not rename or select the managed account. Update accepts user-login, contact-id and active.
Deletion is explicit by GUID and retains native protected-user checks.

Existing technical users are inspectable, but this create flow provisions contact-backed accounts.
Technical-account and identity-provider provisioning are separate native workflows; do not simulate
them by directly writing SysAdminUnit type values or authentication tables.

## Unlock, disable and change password

inspect-user action=lock-status reads the native blocked state. manage-user action=unlock resets
native password/MFA lockout counters. Unlock is separate from activation: it preserves Active=false.
Use update active=true only when activation is part of the authorized change.

manage-user action=password takes id, password-env and force-change-password. It changes only the
password-related fields; it must not rename, reactivate or detach the Contact. LDAP-synchronized
accounts are rejected because their password authority is external. A server-accepted password is
not a fresh-login proof: password policy, forced change, account state, IP rules and licenses can
affect authentication. Where testing is authorized, use a fresh session and verify the old secret
fails and the new secret succeeds without logging either value.

Version boundary: create/password require Creatio 10.1.585.0 or newer through the shared CLI/MCP
version checker, before reading or transmitting the secret. The local 10.1.585 runtime logs changed
column names for UpdateOrCreateUser.
Older native implementations can log the entire jsonObject, including UserPassword. Client redaction
cannot fix server-side logging. Confirm the target's native implementation before sending secrets;
do not claim the client guarantees secret-free logs on every Creatio version.

## Roles, memberships and functional associations

Create roles with a new GUID, name, type and existing parent-id. Types 0, 1, 3 and 6 use create;
type 2 uses ensure-manager. Update may rename or move a compatible non-manager role; cycles and
cross-connection-type moves are rejected. Root/system roles and roles with direct members or children
cannot be deleted by this flow. Remove or move dependencies deliberately before deleting a custom role.

add-member/remove-member changes direct SysUserInRole membership. The add path uses AddUserRoles,
which retains the native additional authentication checks for System administrators. Do not replace
it with AddUsersInRole or raw relationship inserts to bypass those checks.

inspect-role action=members id=<role GUID> reads that role's direct users; effective=true includes
users who receive the role through inheritance or delegation. To inspect the MANAGERS tab, resolve
the type-2 child and use members with its GUID.

inspect-role action=memberships user-id=<id> reads the user's direct roles; effective=true reads computed
SysAdminUnitInRole membership. Effective membership includes inheritance and delegation and is not
an editable source table. The service actualizes native membership and checks readback; a failed
actualization is not a successful permission change.

An organizational role's FUNCTIONAL ROLES tab is a SysFuncRoleInOrgRole association, not a user
membership. Use functional-roles to inspect and add-functional/remove-functional to change it.
The removal bridge preserves entity deletion checks, requires CanManageSolution and
CanManageAdministration, and also requires CanManageLicUsers if automatic redistribution is enabled.
Never substitute a generic entity-delete endpoint or raw SQL for the bridge.

Removal returns association readback plus a processing receipt. completed=false means the association
was removed but follow-up processing failed; inspect the receipt and effective membership before
recovery. licenseReconciliationRequired=true requires explicit reconciliation. An already-absent
association cannot prove a previous redistribution succeeded. Do not treat a successful retry or
redistributionScheduled=true as proof of final user-license assignments.

## Licenses

inspect-license action=user-list shows native available packages and assignment state. user-assign
and user-remove act on one package and user; assignment checks active state, availability and readback.
No available packages is a valid result, not permission to invent a license identity or bypass licensing.

role-list/role-assign/role-remove manage SysLicPackageInRole associations. Those changes do not prove
that users received or lost licenses. Run role-redistribute with the role GUID when distribution is
intended. It requires the native role-based distribution feature, CanManageSolution,
CanManageAdministration and CanManageLicUsers. include-manual defaults false; enabling it explicitly
permits changes to manually assigned licenses and needs that scope in the user's request.

The endpoint refreshes effective membership and invokes native ScheduleLicensesRedistribution.
The process schedules delayed work; the delay is controlled by the native setting. Read affected
users' assignments after the job runs and compare them with the intended state. Capacity, license
availability and native distribution policy still apply. Never report completion from a scheduling
receipt alone. Licensed assignment/redistribution requires its own runtime verification.

## IP rules, delegation and operation permissions

The ACCESS RULES tab on users/roles stores SysAdminUnitIPRange rows. Use inspect-access ip-list and
manage-access ip-create/ip-update/ip-delete. Mutations require the exact owning unit and rule GUID.
The supported write format is canonical IPv4. Native enforcement compares octet bounds independently;
it is not a single numeric interval. Every begin octet must be <= its corresponding end octet.
IPv6 writes and abbreviated IPv4 forms are rejected. IPVersion is a UI field, not a persisted column.
Rule persistence alone does not enable enforcement: native login checks rules only when
UseRestrictedIP is true or the server authentication configuration enables UseIPRestriction.
Inspect the setting through the existing syssetting tools. Enabling it affects the whole environment,
so do so only within the authorized scope. Verify actual login behavior from the intended client
address before calling an IP-policy change done.

Delegation points FROM grantor-id TO unit-id (the receiving user). delegate/revoke-delegation retain
native checks and inspect effective membership after actualization. Self-delegation and technical-user
delegation are rejected. Delegation is a separate access source from direct role membership; removing
one source does not remove access still conferred by another.

operations discovers SysAdminOperation IDs and codes; operation-grants inspects grants for one operation-id.
grant-operation/deny-operation/revoke-operation and operation-position use RightsService to preserve
native priorities, caches and privileged-operation checks. A grant row alone is not proof that the
user can execute the operation: conflicting rights and position affect resolution. Test the intended
operation with the affected identity where authorized. These system-operation permissions are not
record rights and are not the IP rules displayed on the role's ACCESS RULES tab.

## Evidence and handoff

The source/runtime investigation for Clio #968 traced AdministrationService, OrgStructureUser,
RightsService and native role-licensing processes. Local lifecycle verification used Creatio
10.1.585.0 on .NET 8/PostgreSQL. The external MCP AdministrationToolE2ETests covers account lifecycle,
manager reuse, inherited functional membership and removal, delegation and IP-rule CRUD. Manual
fresh-session probes covered password replacement, lockout recovery and IP denial with UseRestrictedIP
enabled, followed by restored login after rule removal. This does not establish
licensed distribution, IPv6 enforcement or behavior on every supported framework/version.

For a handoff, record target environment name, created GUIDs, direct/effective membership readback,
the action receipt, remaining license work and verification boundaries. Never include secrets.
Keep users and memberships environment-local. For dashboard/custom-role package bindings follow
dashboard-rights; this article does not change that binding contract or authorize shipping accounts.
