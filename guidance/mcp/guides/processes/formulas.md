clio MCP process-formulas guide - expression sources, the formula vocabulary and branch conditions

Part of the process guide set. `process-modeling` is the entry point and indexes the rest.
This article is the authoritative owner of the `expression` mapping source, the formula vocabulary the
platform's interpreter accepts, how a parameter is referenced inside a formula, what the server validates
and refuses, and the CONDITION on a conditional flow.
A rule that lives in another article is cited by its article NAME and never repeated here, so a
name in backticks is a get-guidance topic to fetch, not a section to scroll to.
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
| system setting | `[#SysSettings.Code#]`; a form carrying the value-type suffix also round-trips. In the interpreted engine (the default) an UNSET setting THROWS at run time where the older compiled engine returned null — do not reference one that may be empty without a fallback |
| lookup record | `[#Lookup.{referenceObjectSchemaUId}.{recordId}#]` — both GUIDs |
| date / date-time / time | `[#DateValue.dd.MM.yyyy#]` / `[#DateTimeValue.dd.MM.yyyy HH:mm#]` / `[#TimeValue.HH:mm#]` |
| boolean constant | `[#BooleanValue.False#]` (a bare `false` also still works) |

REFERENCING A PARAMETER — the one thing that is not guessable, so read this before writing a formula that
uses one. A parameter is referenced by its **UId**, never by its name. There is no name-based form: a bare
`Price`, `[Price]`, `[#Price#]` and `[#Process parameters.Price#]` are ALL refused. Build the token
yourself, in two steps:

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
`process-perform-task`. Elsewhere the macro form is legitimate; on a lookup DEFAULT the bare Guid is
simply the better route. For an Integer or Float parameter it is equally the route whenever the
value has to be computed. Do NOT evaluate the arithmetic yourself and store the result as a constant: it
reads as success and silently replaces an expression that recomputes with a number that never will.

WHAT IS CHECKED, from `CrtProcessBuilder` 1.4.0.0 (the floor this clio requires is 1.4.0.3). Before an `expression` mapping or a flow condition is
stored, the server validates it and REFUSES a bad one, naming what is wrong:

- it must parse;
- every `[# … #]` parameter reference must resolve to a parameter IN THAT PROCESS — a dangling one is
  refused with the offending token named. This is what makes an `expression` referencing a parameter safe to
  author rather than a runtime gamble;
- an unknown identifier is refused with the identifier named;
- the result must fit the target parameter's DECLARED type. This matters more than it sounds, because
  conversion retypes numeric constants: a fractional literal becomes `decimal`, and so does anything
  containing a division (`1/2` is converted so it yields `0.5` rather than integer `0`). So `1.5` and
  `1 / 2` are REFUSED for an **Integer** parameter and accepted for a **Float** one — a Float parameter's
  CLR type is `decimal`. Plain integer arithmetic (`1 + 1`) fits both. The package runs this check EARLY,
  at the moment you write the formula. The platform's own pre-save validation covers the same ground for a
  formula that reaches it, so one refused here would have been refused later anyway with a worse message —
  but only the package's check names the offending token, and only it runs before anything is stored. A
  package too old to carry it does not run it at all;
- a macro family the package does not recognise is ACCEPTED with a warning rather than refused, so a
  process using a dialect this version has not seen still round-trips.

On an environment older than 1.4.0.0 none of THIS happens — the package does not check the formula, so
nothing names the offending token and nothing refuses before the write. 1.4.0.0 and .1 DO check, against
one numeric rule that disagreed with the platform's own pre-save gate; .2 already carries the corrected
rule, and what separates .2 from the .3 floor this clio requires is a different set of fixes. The platform's own pre-save gate
still runs at save time and still refuses what it refuses; what you lose is the early, specific message,
and anything that gate does not cover then fails at run time. clio refuses `create-business-process` / `modify-business-process` against such
an environment for exactly that reason; the fix is `install-process-builder`, not a workaround.

THAT REFUSAL MAKES EVERY "on an older package" FALLBACK IN THIS GUIDE SET UNREACHABLE. Where an article
explains what an older `CrtProcessBuilder` does differently — the `[#Lookup…#]` macro on a pre-1.3.1.1
package in `process-parameters` is the surviving example — those paragraphs are history, not a
branch to take: on a refusal from a CURRENT clio, run `install-process-builder`. Do not re-send a call in
an older dialect "because the package may be old" — this clio refused before the call left, so no dialect
reaches the server. Under an OLDER clio the call does go through and the old package rejects it itself;
that rejection is real and still means the package is behind, not that the contract is wrong.

WHAT A REFUSAL LOOKS LIKE, so you can correct it yourself instead of guessing. The message always names
the usage site and quotes the expression as YOU wrote it (not the converted form). The middle clause is
what tells you which mistake you made:

| you wrote | message contains | the fix |
|---|---|---|
| `FormulaUtilities.Sum(1, 2)` | `No applicable method 'Sum' exists in type 'FormulaUtilities'` | the function does not exist — there is no Sum |
| `System.Math.Abs(-1)` | `it references 'System', which does not exist` | drop the namespace: `Math.Abs(-1)` |
| `math.Round(1.5)` | `it references 'math', which does not exist` | case matters: `Math.Round(1.5)` |
| `DateTimeUtilities.GetStartOfMonth(...)` | `No applicable method 'GetStartOfMonth' exists in type 'DateTimeUtilities'` | drop the `Get` prefix: `StartOfMonth` |
| `1.5` into an Integer parameter | `its result cannot be used as Int32` | the target type cannot hold it |
| a reference to a parameter that is not there | the offending `[#…#]` token, verbatim | create the parameter, or fix the reference |

PARENTHESISE rather than relying on precedence. A condition like `a && b || c` is legal and its meaning is
not obvious to the next reader; write `(a && b) || c`. (`a`, `b`, `c` stand for whole sub-expressions
here, each of which references its parameters by UId meta-path like everything else.)

== Conditional flows and branch conditions ==

READ "REFERENCING A PARAMETER" UNDER FORMULAS BEFORE WRITING A CONDITION. Every parameter a condition
names is referenced by its UId meta-path — `[#[Parameter:{uid}]#]` — and never by its name; a bare
`Amount` is refused. Short names appear below to keep the rules readable: they describe the DECISION,
they are not the text you write.

A branch is a flow with a CONDITION. You do not build one — you build a plain flow and then set its
condition:

1. `create-business-process` (or `addFlow`) makes the flow. `flows[].kind` is still refused on the build
   path: a conditional branch cannot be declared there.
2. `modify-business-process` with `setFlowCondition` (`source`, `target`, `condition`) turns that flow into a
   conditional one.

NO GATEWAY IS NEEDED and none is created. The platform synthesizes an exclusive gateway for a conditional
flow whose source is not one, so a branch straight off an activity is legitimate — it is what the platform's
own tests rely on. Gateway ELEMENTS are still not buildable.

The condition must evaluate to a BOOLEAN. An integer is refused: the interpreted engine, which is the
default, does not coerce it the way the older compiled engine did.

An EMPTY condition is refused, and the reason is worth knowing because it is silent otherwise: the platform
stores an empty condition as the literal `true`, so an "empty" branch is an ALWAYS-TAKEN branch. To drop a
condition, remove the flow and add a plain one — there is no clear-condition operation. The replacement
lands LAST, and since precedence IS insertion order (below), that silently changes which sibling branch
runs: if the element has other conditional branches, re-add every one of them in the intended order. A
condition on a DEFAULT branch is refused.

BRANCH PRECEDENCE IS FLOW ORDER, and nothing in the metadata records it. Where two conditional branches
leave the same element, they are evaluated in the order the flows were added and the FIRST whose
condition is true is taken. So a branch that fires above 100 and a branch that fires above 1000 resolve
differently purely by which flow was added first, with no diagnostic and nothing a human can inspect. Add
the most specific branch FIRST, and report the order you chose and why — the order is the only thing that
records the intent, so it belongs in what you tell the user. `setFlowCondition` keeps a flow's position
when it converts it, so setting a condition never silently reorders your branches; remove-and-add does.

A conditional flow reads back through `describe-business-process` as `kind: "conditional"` with its
`condition` text, so you can verify what you wrote.

Corpus-attested condition shapes, most common first — these are what real processes use. `X`, `A` and
`B` stand for a REFERENCE TOKEN (`[#[Parameter:{uid}]#]`, a system variable, a system setting), never for a
parameter's name:
`X != Guid.Empty`, `X == true`, `X == "text"` / `X.Equals("text")`, `A && B`, numeric comparisons, a bare
boolean parameter, lookup-record equality, parameter-to-parameter comparison, `!string.IsNullOrEmpty(X)`,
`A || B`, `.Contains("x")`, `X != null`, `!X`, and date comparisons against `DateTime.MinValue`.
