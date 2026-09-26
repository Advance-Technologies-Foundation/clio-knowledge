clio MCP process-formulas-details guide — the designer's rendering, read-back, the refusal table, older packages

A details article of the process guide set, reached through `process-formulas`, which points here; `process-modeling` is the set's entry point.
This article is the authoritative owner of how the designer renders a stored formula and how a stored
formula or condition reads back in `describe-business-process`, the verbatim table of formula refusals and
their fixes, the per-request limit, and what an older CrtProcessBuilder package did with a formula. Split
out of `process-formulas`, which keeps the formula vocabulary, the parameter reference syntax, the worked
example, what the gate checks and what each refusal names, and the length bound; read that first.

The designer then displays the worked example of `process-formulas` as `RoundUp([#PriceParameter#])` — it
resolves the UId back to the name, and it shows the designer's own spelling of the function. Both
directions of that conversion are the platform's; you write the C# spelling and the UId, and the designer
renders the friendly form.

CONFIRM IT WITHOUT THE DESIGNER. No tool returns a designer link, so do not offer one — an invented URL is
worse than none. The check you can actually run is `describe-business-process`: a stored formula reads back
on the parameter as `source: "Script"` with your expression in `value` (NOT in an `expression` field — the
describe contract has no such field on a parameter). `source: "ConstValue"` there means the formula was
never stored as one and a constant went in instead. For a flow, the read-back is `kind: "conditional"` with
the `condition` text — which on a connector whose source enumerates activity results cannot tell a live
condition from one a result selection has superseded; `branchesOnActivityResult` is the field that can, and
`process-activity-result-branches` owns that dialect whole - including which of the two wins at run
time when a connector has both kinds of sibling. If a human is at a browser, `RoundUp([#PriceParameter#])` in the
designer is the same confirmation in friendlier spelling.

There is no per-REQUEST budget; a large batch is bounded by the request-item cap (1 000 items).

ON AN OLDER PACKAGE a bad formula is still refused, in the package's own words — and an older package
refuses MORE, not less (a 256 KB per-request budget; from 1.4.0.32 an unrecognised macro family on a NEW
condition). A refusal from a pre-1.4.0.41 environment is therefore not evidence the formula is bad:
update the package. Below 1.4.0.0 an `expression` mapping was stored unchecked and `setFlowCondition` did
not exist. clio refuses `create-business-process` / `modify-business-process` against an
environment below its enforced floor; the fix is `install-process-builder`, not a workaround.

THAT REFUSAL MAKES EVERY "on an older package" FALLBACK IN THIS GUIDE SET UNREACHABLE — they are history,
not a branch to take. On a refusal from a CURRENT clio, run `install-process-builder`; never re-send a
call in an older dialect, because clio refused before it left and no dialect reached the server.

WHAT A REFUSAL LOOKS LIKE. Every row is a verbatim measurement, prefixed by `Process validation failed:`
plus the element or parameter name:

| you wrote | message contains | the fix |
|---|---|---|
| `FormulaUtilities.Sum(1, 2) > 0` | `Formula value error: No applicable method 'Sum' exists in type 'FormulaUtilities' (at index 17).` | the function does not exist — there is no Sum |
| `System.Math.Abs(-1) > 0` | `Formula value error: Parameter "System" not found` | drop the namespace: `Math.Abs(-1)` |
| `math.Round(1.5) > 0` | `Formula value error: Parameter "math" not found` | case matters: `Math.Round(1.5)` |
| `DateTimeUtilities.GetStartOfMonth(DateTime.Now) > DateTime.MinValue` | `Formula value error: No applicable method 'GetStartOfMonth' exists in type 'DateTimeUtilities' (at index 18).` | drop the `Get` prefix: `StartOfMonth` |
| a formula split across two lines | `Formula value error: Expression contains invalid line break symbol. Use \n as new line character` | put it on one line — and note the expression is quoted as EMPTY here |
| `[Price] > 100` | `Formula value error: Expression expected (at index 0).` | brackets are not a reference; use the UId metapath |
| `1 +` | `Formula value error: Invalid Operation (at index 3).` | the expression is incomplete |
| `1.5` into an Integer parameter | `Error while executing expression "1.5m": Formula value error: Cannot convert type "Decimal" to "Int32"` | the target type cannot hold it — note the quoted `1.5m` |
| an Integer parameter as a whole condition | `Error while executing expression "Amount": Formula value error: Cannot convert type "Int32" to "Boolean"` | a condition must be bool — compare it |
| `[#Price#] > 100` | `Formula value error: Expression expected (at index 0).` | that is not a macro family; reference the parameter by UId |
| `[#[Parameter:{a-uid-not-in-this-process}]#] > 0` | `has an invalid value for the parameter "ConditionExpression". It references the process parameter <uid>, which is not in this process. Add the parameter first, or correct the reference.` | create the parameter, or fix the UId |

== Conditional flows and branch conditions ==

Moved to its own guide, `process-branch-conditions` (`get-guidance name=process-branch-conditions`).
It owns turning a plain flow into a conditional one, what a condition may contain, branch PRECEDENCE,
the activity-result case, and
the parallel-split hazard of clearing the last condition. `process-formulas` owns the formula vocabulary
both use.
