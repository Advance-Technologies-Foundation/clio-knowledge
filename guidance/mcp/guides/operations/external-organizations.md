# External users and external organizations

Use the native external-user administration workflow to associate a login with an organization.
Do not equate a Contact's Account with an external login's organization membership.
Generic password, role and license operations remain owned by `get-guidance name=administration`;
custom endpoint routing belongs to `get-guidance name=portal-service-routing`.

## Distinguish the records

The following model was verified on Creatio 10.1.585.0 (.NET 8/PostgreSQL).

| Record or field | Meaning |
| --- | --- |
| `Account.Id` | Business organization/account record |
| `Contact.Id`, `Contact.AccountId` | Person and their business account/employer |
| External user's `SysAdminUnit.Id` | Login/security principal; distinct from Contact ID |
| User's `SysAdminUnit.ContactId` | Contact linked to the login |
| User's `ConnectionType=1` | External/SSP connection domain |
| User's `SysAdminUnitTypeValue=4` | User kind used by the tested native external-user creation flow |
| Organization's `SysAdminUnit.Id` | Organizational security role; distinct from Account ID |
| Organization's type `0`, connection type `1` | Organization kind in the external domain |
| User and organization `PortalAccountId` | Links to the same **Account ID**, not to the organization-role ID |
| `SysUserInRole.SysUserId` / `SysRoleId` | Direct user-to-role membership supplying the organization's role permissions |
| `SysAdminUnitInRole` | Computed effective membership; inspect it, do not author it as the source of truth |

Do not identify an external login solely by type `5`: the legacy type exists, but native account
creation in this lab returned type `4` with connection type `1`. Do not use the generic
`SysAdminUnit.AccountId` in place of `PortalAccountId`; the lab's generic Account field was empty.

For the basic organization flow, the organization role is beneath **All external users**
(`720b771c-e7a7-4f31-9cfb-52cd21c3739f`). The lab's assigned users had direct membership in their
organization role; do not require a separate direct root-membership row as the only evidence of
belonging to the external hierarchy. Inspect effective membership when evaluating inherited access.

## Native setup workflow

1. Resolve or create the intended Account and Contact records. Keep their IDs separate from user and role IDs.
2. In the main application's organizational-role administration, open **All external users** and
   create an organization associated with the Account. The Account page's portal-user workflow is
   another native entry point. Availability and wording depend on the installed UI/version.
3. Add existing contacts through that organization's external/portal users detail, or use its
   supported new-contact workflow. Do not just set `Contact.Account` or add an arbitrary role row.
4. Verify the resulting login, its `Contact`, `ConnectionType`, `PortalAccount`, organization role,
   and direct/effective role membership independently. Assign/verify the required licenses separately.
5. Test a fresh login as the actual external user. A successfully saved user or membership is not
   proof that authentication and licensed access work.

The [Creatio Academy administration guide](https://academy.creatio.com/guides/creatio-apps/products/more-apps/portal/manage-portal-in-main-application)
documents organization creation and adding contacts through the native UI. This lab exercised
the native server operations, not browser clicks. Since 8.0.9 the user/root-role captions were renamed
from Portal user / All portal users to External user / All external users; backend SSP names remain.

## Automation and independent readback

Discover contracts using `get-tool-contract`; use `clio-run` for non-resident tools. With Clio 8.1.0.132:

- `manage-user action=create external=true` creates an external login but has no organization/account
  argument. It is not, by itself, the complete organization-assignment workflow.
- `manage-role action=add-member` changes membership; its contract does not bind `PortalAccount`.
- `inspect-user` / `inspect-role` inspect identity and membership. Use `execute-esq` for explicit
  `SysAdminUnit.PortalAccount`, `Contact.Account` and organization-linkage readback; read `esq` and
  `esq-filters` before constructing queries. Read merged entity metadata before choosing columns.
- The lab created organization records through native DataService `VwSspAdminUnit` entity saves,
  preserving native entity behavior, and associated existing contact-backed external logins through
  `SspUserManagementService.CreateUsersByContactsIds`. There is no dedicated organization-management
  MCP tool in the inspected version. Use the native UI when those lifecycle contracts are unavailable;
  do not silently reduce organization assignment to a raw table update.

For the tested native association operation, an employee administrator sent:

```text
POST /rest/SspUserManagementService/CreateUsersByContactsIds
```

```json
{
  "request": {
    "accountId": "<Account GUID>",
    "contactIds": ["<Contact GUID>"],
    "userRoles": []
  }
}
```

Prerequisites: the external organization already exists for that Account; the caller has the native
portal-user administration permission. This example associated an existing active external login
with no prior PortalAccount. It did not send invitation emails. Inspect both the enclosing `success`
and every `contactInvites[*].success`, then read back the actual `sysAdminUnitId`, PortalAccount and
membership. Do not blindly repeat a partially successful request.

Moving an already-associated user to a different organization is a separate lifecycle operation:
Academy and installed source describe deactivation/replacement paths. That move was not exercised
here; do not promise it preserves the old login ID or simulate it with a single field update.

## What the affiliation does not prove

- An external user can exist without an external organization. In the lab, an unaffiliated user's
  Contact belonged to Account A, but the login's PortalAccount was empty and it belonged to neither
  organization role. The same user still authenticated and called the SSP route successfully.
- Organization membership supplies inherited role permissions; it does not by itself prove a
  particular custom endpoint enforces organization isolation. Apply the endpoint's business
  authorization and test another organization's records explicitly.
- Do not trust a requested Account ID as the caller's organization. Resolve affiliation from the
  authenticated identity and fail closed when the operation requires an organization but none or
  an ambiguous mapping exists. Do not fall back to the contact's employer.

## Evidence boundary

Two Accounts, two distinct external-organization roles, three Contacts and three external logins
were provisioned in an exclusive lab. Native assignment and independent MCP/DataService readbacks
agreed; five NUnit relationship assertions and 26 routing cases passed.
See [lab validation](docs://knowledge/com.creatio.clio/reference.portal-service-lab-validation).
Email invitations, self-registration, organization transfers, portal-administrator delegation,
license-distribution algorithms and browser layout were not tested.
