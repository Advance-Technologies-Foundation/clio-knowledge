clio MCP process-formulas guide - expression sources and the formula vocabulary a condition also uses

Part of the process guide set. `process-modeling` is the entry point and indexes the rest.
This article is the authoritative owner of the `expression` mapping source, the formula vocabulary the
platform's interpreter accepts - which a flow condition uses too - how a parameter is referenced inside a
formula, and what the server validates and refuses. The BRANCH itself, including precedence and the
hazard of clearing the last one, belongs to `process-branch-conditions`.
== Formulas (`expression` sources and flow conditions) ==

A formula is NOT C#, and knowing what it actually is stops most wrong guesses. Creatio evaluates it with an
EXPRESSION INTERPRETER over a flat, case-sensitive name registry. That means, concretely:

- `Math.Round(1.5)` resolves; `System.Math.Round(1.5)` does NOT (no namespace-qualified names) and
  `math.Round(1.5)` does NOT (case-sensitive);
- no lambdas, no generics, no statements — ONE expression, on ONE line;
- the Creatio function library in scope is `FormulaUtilities`, and it has exactly FOUR members:
  `Min`, `Max`, `Avg`, `Mod`. There is no other Creatio HELPER library — which is not the same as saying a
  formula cannot do the thing: the ordinary .NET members are in scope too, so look for one before you
  conclude anything is impossible (see DO NOT INVENT A FUNCTION NAME below). What you must not do is guess
  a plausible Creatio name;
- date helpers live on `DateTimeUtilities` and are spelled WITHOUT a `Get` prefix: `StartOfMonth`,
  `StartOfWeek`, `StartOfYear`, `StartOfQuarter`, `StartOfHalfYear`, `StartOfHour`, plus `Day`, `Month`,
  `Time`, `DayOfWeek`, `DayInRange`. (`GetQuarter` is one of the few that really does carry the prefix, and
  it works in both forms: `DateTime.Now.GetQuarter()` and `DateTimeUtilities.GetQuarter(DateTime.Now)`.)
- `Math`, `DateTime`, `Guid`, `string`, `Convert`, `TimeSpan` and the ordinary operators are available,
  including the ternary `? :` and the null-coalescing `??`. This is the GUIDED set, not the enforced one:
  the registry is wider, and a formula is server-evaluated code rather than a sandbox. Stay inside the
  guided set unless you have a reason not to. An identifier the registry does not carry IS refused, by
  name — the thing not to rely on is the engine refusing something merely because it is unwise;
- Write a fractional number PLAIN - `1.2`, never `1.2m`, and this one has no exceptions. The platform's
  converter appends the decimal suffix itself, unconditionally and without checking for one, so a literal
  you suffixed arrives as `1.2mm` and the formula is refused with `')' or operator expected`. Measured: an
  agent that had read this article still wrote `1.2m`, because the suffix is documented further down only
  as something REFUSALS show you. Same for the `((decimal)…)` wrapper around a division - the converter
  adds it; do not write it.
- SAFETY, not style: never build a formula by pasting text you do not control - a record field, a user
  message, anything read back from the environment. A quote or bracket in that text does not fail
  safely, it changes what the expression MEANS, and the platform validates the result rather than your
  intent. Put such a value in a process PARAMETER and reference the parameter; author literals only
  from text you wrote.
- you may call a METHOD on the result of a macro or a value: `[#SysVariable.CurrentDateTime#].AddDays(3)`,
  `DateTime.Now.ToString()` (the way to feed a date into a Text parameter), `"a" + "b"`,
  `!string.IsNullOrEmpty(X)` (`X` being a reference token, never a parameter's name). Combining two
  functions in one expression is fine too:
  `FormulaUtilities.Min(5, 3) + Math.Abs(-2)`.

**DO NOT INVENT A FUNCTION NAME.** This is the single most likely way to get a formula wrong, because the
CREATIO library is far smaller than it looks: there is no Creatio `Sum`, `Count`, `Concat`, `Format` or
`If`. That is a statement about Creatio's helpers, NOT about what a formula can express — the ordinary
.NET members are there: concatenation is `+` or `string.Concat`, formatting is `string.Format` or
`.ToString(...)`, a conditional is the ternary `? :`, and a conversion is `Convert.ToInt32(...)`. Reach for
the .NET member before you conclude anything is impossible. Only when neither a `FormulaUtilities` member,
a `DateTimeUtilities` helper, `Math`, nor a plain .NET member on `DateTime`/`string`/`Guid`/`Convert` will
do should you tell the user a formula cannot express it — a wrong "the platform cannot do that" is the
most expensive answer this guide can produce, because the user has no reason to re-check it. A guessed name is refused, not silently ignored, but it costs the
user a round trip.

MACRO FAMILIES — the `[# … #]` tokens a formula may reference:

| family | literal form |
|---|---|
| process / element parameter | a UId meta-path you BUILD from `describe-business-process` — see below |
| system variable | `[#SysVariable.CurrentUserContact#]`, `[#SysVariable.CurrentDateTime#]` |
| system setting | `[#SysSettings.Code#]`; a form carrying the value-type suffix also round-trips. an UNSET setting THROWS at run time — do not reference one that may be empty without a fallback |
| lookup record | `[#Lookup.{referenceObjectSchemaUId}.{recordId}#]` — both GUIDs |
| date / date-time / time | `[#DateValue.dd.MM.yyyy#]` / `[#DateTimeValue.dd.MM.yyyy HH:mm#]` / `[#TimeValue.HH:mm#]` |
| boolean constant | `[#BooleanValue.False#]` (a bare `false` also still works) |

REFERENCING A PARAMETER — the one thing that is not guessable, so read this before writing a formula that
uses one. A parameter is referenced by its **UId**, never by its name. There is no name-based form — but the
four wrong shapes do not all fail the same way, and the difference matters:

- a bare `Price` is REFUSED naming the identifier: `Formula value error: Parameter "Price" not found`.
- `[Price]` is REFUSED too, but does NOT name it — it faults on the bracket:
  `Formula value error: Expression expected (at index 0).` Measured; the two are not interchangeable.
- `[#Price#]` and `[#Process parameters.Price#]` are read as an unrecognised macro FAMILY, which no
  converter resolves, so the raw token reaches the interpreter and does not parse. Both are REFUSED, on a
  mapping and on a condition alike: `Formula value error: Expression expected (at index N)`, where N is
  where your `[#` starts. Recognise the shape, not the number — that fault on a formula containing `[#`
  means the family is wrong, not the syntax.

The same is true of a typo in a real family: `[#SysSettingz.Foo#]` is an unrecognised family, not an
unknown setting. Build the token yourself, in two steps:

1. call `describe-business-process` and take the parameter's `uid` (describe reports `uid`; it does NOT
   return a ready-made meta-path, so there is nothing to copy — you assemble it);
2. write the token around that UId, braces included:
   * a PROCESS parameter -> `[#[Parameter:{uid}]#]`
   * an ELEMENT output parameter -> `[#[Element:{elementUid}].[Parameter:{parameterUid}]#]`

Worked example — note the target is a FLOAT parameter: `Math.Ceiling` returns `decimal`, and a decimal
result into an Integer parameter is refused by the result-type rule below. `describe-business-process`
reports a process parameter `PriceParameter` with
`uid: c3f5635c-2aa2-4279-9464-b0b94b2f7a85`. To round it up into `PriceUpParameter`:

    {"op":"addMapping","mapping":{"targetProcessParameter":"PriceUpParameter",
     "expression":"Math.Ceiling([#[Parameter:{c3f5635c-2aa2-4279-9464-b0b94b2f7a85}]#])"}}

The designer then displays this as `RoundUp([#PriceParameter#])` — it resolves the UId back to the name, and it
shows the designer's own spelling of the function. Both directions of that conversion are the platform's;
you write the C# spelling and the UId, and the designer renders the friendly form.

CONFIRM IT WITHOUT THE DESIGNER. No tool returns a designer link, so do not offer one — an invented URL is
worse than none. The check you can actually run is `describe-business-process`: a stored formula reads back
on the parameter as `source: "Script"` with your expression in `value` (NOT in an `expression` field — the
describe contract has no such field on a parameter). `source: "ConstValue"` there means the formula was
never stored as one and a constant went in instead. For a flow, the read-back is `kind: "conditional"` with
the `condition` text. If a human is at a browser, `RoundUp([#PriceParameter#])` in the designer is the same
confirmation in friendlier spelling.

A COMPUTED DEFAULT for a parameter of ANY type is a mapping, not a `value`. `addParameter` / `setParameter`
take `value` as a literal constant, so an arithmetic or macro-bearing default cannot go there; the route is
a mapping with `targetProcessParameter` + `expression`, exactly as above. This is NOT a date/time special
case — date, date-time and time are the types where the mapping route is MANDATORY (their constants have
no literal form). A LOOKUP is NOT one of them: its default is a bare record Guid in `value`, which is the
preferred route and the only one an ActivityUserTask category accepts — see `process-parameters`.
On an ActivityUserTask's `ActivityCategory` specifically, reaching for `expression` with `[#Lookup…#]`
instead saves and compiles and then silently degrades the element's allowed-results list — see
`process-task-category`, which owns that rule, why `Activity.AllowedResult` is the wrong place to look
for the degradation, and what to do on a package too old to accept the constant. Elsewhere the macro
form is legitimate; on a lookup DEFAULT the bare Guid is
simply the better route. For an Integer or Float parameter it is equally the route whenever the
value has to be computed. Do NOT evaluate the arithmetic yourself and store the result as a constant: it
reads as success and silently replaces an expression that recomputes with a number that never will.

WHAT IS CHECKED, and BY WHOM. The **platform** validates every formula — an `expression` mapping and a
flow condition alike — at its own pre-save gate. From `CrtProcessBuilder` 1.4.0.41 the package adds
nothing to that verdict beyond the two checks under *What CLIO still checks* below. Two consequences
that change how you read a failure:

- **A bad formula fails the WHOLE call.** The refusal comes from the save, not from the operation that
  carried the formula, so a `modify-business-process` batch does not tell you which operation it was — it
  tells you which PARAMETER or FLOW. Nothing is written: a refused edit is atomic.
- **The message is the platform's.** It always names the parametrized element, the flow, or the parameter
  — whichever holds the bad value. A character index and the expression come with a PARSE fault only: a
  type mismatch names the types, an unknown identifier names the identifier, an unresolvable `[#…#]`
  reference names the reference and the remedy, and a newline quotes the expression as empty. Do not wait
  for an index that is not coming.
- **When the expression IS quoted, it is quoted as the platform's own converter left it, not as you wrote
  it.** A parameter reference appears as the parameter's NAME, a fractional literal gains an `m`, and a
  division gains a `((decimal)…)` wrapper. So `[#[Parameter:{24a7…}]#]` comes back as `Amount` and `1.5`
  comes back as `1.5m`; do not read that as the wrong formula having been validated.

What the gate refuses. Every message quoted here and in the table below is verbatim from a stand at core
10.0.731.0 — none of it is paraphrased or inferred:

- it must PARSE — `1 +` gives `Formula value error: Invalid Operation (at index 3).`;
- every identifier must resolve. An unknown one is named: `System.Math.Abs(-1)` gives
  `Formula value error: Parameter "System" not found`, and so does a case error (`math.Round`). A missing
  method is named with its type: `Formula value error: No applicable method 'Sum' exists in type
  'FormulaUtilities' (at index 17).`;
- every `[# … #]` parameter reference must resolve to a parameter IN THAT PROCESS. An unresolvable one is
  refused naming the UId and the remedy;
- every `[# … #]` macro FAMILY must be one a converter resolves WHERE YOU USED IT. An unrecognised family
  is not converted, so what reaches the interpreter is the raw token and it does not parse: `[#Price#] > 100`
  gives `Formula value error: Expression expected (at index 0).` This is the answer to the tempting
  `[#Price#]` shorthand — it does not save. Measured over a mapping onto a plain process parameter,
  `[#UsrUnknownDialect.Something#]`, `[#ColumnValue.Id#]` and `[#SamplingColumnValue.Id#]` are all three
  refused the same way, the last two being real platform families used in a context no converter covers;
- the result must fit the target. For a **condition** that target is `bool`, strictly, with no coercion:
  `1 + 1` gives `Formula value error: Cannot convert type "Int32" to "Boolean"`. For a **mapping** it is
  the target parameter's DECLARED type, and conversion is what makes this bite: a fractional literal
  becomes `decimal`, and so does anything containing a division (`1/2` is converted so it yields `0.5`
  rather than integer `0`). So `1.5` and `1 / 2` are REFUSED for an **Integer** parameter and accepted for
  a **Float** one — a Float parameter's CLR type is `decimal`. Plain integer arithmetic (`1 + 1`) fits
  both;
- it must be a single line. A newline is refused with `Formula value error: Expression contains invalid
  line break symbol. Use \n as new line character` — the platform's own rule
  (`ProcessParameterValueProvider.ValidateExpression`), and the one class where the quoted expression
  comes back EMPTY. A bare carriage return with no line feed is NOT refused: the platform checks for `\n`
  only, and CR is whitespace to the interpreter, so such a formula parses and stores on one line.

What CLIO still checks, and it is two things.

**A blank `condition` is refused up front.** An empty condition is not "no condition": the platform
substitutes the literal `true`, producing an always-taken branch nobody asked for. This check is clio's
own, because the gate would ACCEPT it.

**Length, at most 2048 characters**, applied before anything is stored — the pre-save gate is what runs
the platform's macro converters, whose regexes have no match timeout, so a bound applied there would be
too late. 2048 is generous for a formula but NOT for one built by concatenation: a metapath reference is about 60 characters, so roughly thirty of
them exhaust it, and the cap applies to the text as you write it, before macros are resolved. The same
bound covers the paths that store a formula without any other check — a `changeData` value `expression`, a
Send email recipient, a performer contact, a connection expression, a filter condition expression.

There is no per-REQUEST budget; a large batch is bounded by the request-item cap (1 000 items).

What is NOT refused: the SHAPE of an expression — deep bracket nesting, long unary runs, long `? :`
chains — exactly as the visual designer accepts them, and neither clio's length bound nor the platform's
gate looks at it. Keep expressions FLAT, and never build one by concatenation or in a loop. The reason is
not style: the platform parses by recursive descent with no stack guard, so a sufficiently nested
expression ends the worker process rather than failing — uncatchable, taking every concurrent request
with it, and nothing in this path can refuse it. Measured on one stand (core 10.0.731.0) on 2026-09-01. No
CrtProcessBuilder version enters into it - the parser is the platform's - so treat it as one measurement,
not a platform constant, and as a defect only the platform can close.

Two things make "keep it flat" harder than it sounds, which is why the rule is written as a habit
rather than a limit. The depth you write is NOT the depth the parser sees — the platform's own converter
inflates it, so an expression with no brackets at all can arrive deeply nested. And clio's
character bound is not a mitigation: it bounds LENGTH, and the dangerous expression is short and dense
rather than long and flat.

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

PARENTHESISE rather than relying on precedence. A condition like `a && b || c` is legal and its meaning is
not obvious to the next reader; write `(a && b) || c`. (`a`, `b`, `c` stand for whole sub-expressions
here, each of which references its parameters by UId meta-path like everything else.)

== Conditional flows and branch conditions ==

Moved to its own guide, `process-branch-conditions` (`get-guidance name=process-branch-conditions`).
It owns turning a plain flow into a conditional one, what a condition may contain, branch PRECEDENCE,
the activity-result case, and
the parallel-split hazard of clearing the last condition. This guide owns the formula vocabulary both
use.
