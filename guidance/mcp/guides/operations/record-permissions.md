clio MCP record-permissions guide

Choose and verify Creatio record permissions before implementing grants or runtime rules.
This is the decision entry point. `record-rights` owns direct stored-grant tools;
`record-permission-extensions` owns runtime predicates and their combination contract;
`process-access-rights` owns the Change access rights process element. Read the selected owner before acting.

| Requirement | Approach | Canonical owner |
|---|---|---|
| Explicit users/roles may access a particular record; grants survive sessions | Persisted grants | `record-rights` |
| A process changes those grants when a business event happens | Persisted grants changed by Change access rights | `process-access-rights` |
| Access follows current user context, relationships or record attributes at query time | Runtime SQL predicates | `record-permission-extensions` |
| An existing grant needs an additional business restriction, or an additional business rule may grant access | Choose a native combination mode deliberately | `record-permission-extensions` |
| User/role membership, operation rights, lockout or licensing | Administration, separate from the record rule | `administration` |

Examples: an explicit case team can use stored grants; a document owner may edit only while unlocked;
a published document may be readable by other internal users; relationship-derived territory access can
use a parameterized EXISTS predicate. The document rules have an executable reference. A territory or
hierarchical-role variant needs its own data, indexes and denied-operation tests; it is not certified by
that example.

Before changing access, write the expected matrix: representative identity, object operation, record
context, stored grant and expected result. Include a user without access, each of read/edit/delete,
and a relevant context change. Confirm whether access should update immediately with the rule or only
when a process changes stored grants. Do not duplicate continuously changing relationships into stored
grants without defining how stale grants are removed.

`get-record-rights` reports persisted grants, NOT a complete effective-access decision. An extension can
change the result without changing those rows. Object/operation permissions, identity membership and
privileged bypasses remain separate considerations; a successful administrator query proves none of the
ordinary user's restrictions. Test with the user's own authenticated session and verify persisted effects
of edits/deletes independently. Do not substitute an administrator's "rights for another user" overload
for that login test.

Read `record-permission-extensions` for the applicable platform version, failure behavior and access-path
limits. Use `list-knowledge-examples` to find `atf.creatio.record-permissions-reference`; obtain its pinned
revision and follow its README to build and execute the permission matrix on an exclusive disposable
stand. The reference owns fixture/deployment details, not universal business policy.
