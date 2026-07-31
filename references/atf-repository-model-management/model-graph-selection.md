# Model Graph Selection

## Start Small

Identify the smallest useful model graph that satisfies the task before deciding to generate anything.

- Start from the exact schemas and members named in the request.
- Add lookup or detail relations only when the feature or test needs to navigate them.
- Prefer reusing an existing project model even if it exposes a few extra members, as long as it does not create ambiguity or conflict.

## Minimal Graph Rule

Do not assume the task needs the full schema neighborhood.

Example request shapes:

- `Account (Name, Owner, Owner.DecisionRole, Owner.DecisionRole.Name)`
- `Order (Number, Account, Opportunity.Title)`

For requests like these, choose the minimal set of models and members that supports the required traversal.

For small reporting tasks, the minimal graph is often just:

- one master model with the printed scalar fields
- one detail model with the lookup foreign key and any printed scalar fields

Example:

- `Contact (Name, Email, OwnedAccounts.Count, OwnedAccounts.Name when count == 1)`

This does not justify generating the entire environment model set.

## Reuse Before Generation

Before generating:

- inspect existing project models
- inspect previously generated models already checked into the workspace
- prefer extending a nearby model over introducing a parallel duplicate
- if a staged generated model already shows the exact reverse relation name, reuse that mapping pattern and hand-author the minimal production model

Report any tradeoff where reuse pulls in a slightly broader model than the task strictly needs.
