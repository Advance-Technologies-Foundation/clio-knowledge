clio MCP record-rights guide

Manage record-level access rights (who may read/edit/delete a specific record, or a dashboard)
with two tools — NOT with hand-written ESQ into the rights tables:
- get-record-rights — list the current grants on one record.
- set-record-rights — grant or revoke one (grantee, operation) grant. DESTRUCTIVE (applies immediately).

Target the record with entity=<EntitySchemaName> + record-id=<guid> (both required):
- A normal entity record: entity=Contact record-id=<record Id>.
- A DASHBOARD (or any client-unit schema): entity=SysSchemaAdminUnit record-id=<schema UId>.
  Resolve the schema UId from its name yourself with execute-esq: select UId where Name = '<SchemaName>'
  on SysUserLevelSchema (personal schemas) first, then SysSchema (configuration schemas).
  For a DASHBOARD, ALSO read `get-guidance name=dashboard-rights` — grants are DATA (lost when the
  package moves to another environment); it covers re-applying them on the target vs shipping them
  as a package data binding.

To change record permissions from INSIDE a running business process rather than now, that is the
Change access rights element — read `get-guidance name=process-access-rights`. This article owns the
permission model and the direct grant/revoke path; note the level vocabularies differ (`granted`/`delegated`
here, `permit`/`delegate`/`restrict` there).

Grantee is a SysAdminUnit GUID (a role or user id). Resolve a name to its id yourself (e.g. execute-esq
on SysAdminUnit by Name) — names are NOT unique, so the tools take the id, not a name.
set-record-rights args: grantee=<guid>, operation=read|edit|delete, level=granted|delegated
(grant only; default granted), revoke=true to remove.

Where the rights live: the per-entity record-rights table Sys<Entity>Right (e.g. SysContactRight; for
schemas SysSchemaAdminUnitRight). get-record-rights reads it for you — do not query it directly.

DO NOT confuse two different layers:
- Record-level rights — Sys<Entity>Right / SysSchemaAdminUnitRight. Per-record "who can access this
  record/dashboard". This is what the UI "Set up access rights" dialog shows and what these tools manage.
- Schema OPERATION rights — SysSchemaOperationRight. A different operation/configuration layer.
  Querying it to answer "who has access to this dashboard" gives the WRONG answer.