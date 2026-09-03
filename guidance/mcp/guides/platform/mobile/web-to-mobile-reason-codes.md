clio MCP — mobile conversion: droppedElements reason codes

PURPOSE
The closed vocabulary of `guide.droppedElements[].reason` codes returned by
get-mobile-page-conversion-guide: why a source element did NOT reach the mobile page, and what to tell
the user about it. `reason` is a LIST of {code, params?}. Branch on `code`; read `params` for this
occurrence's values.

NOTHING HERE IS AN INSTRUCTION TO APPLY. droppedElements is the audit trail of what was NOT built —
you REPORT it. `guide.elementMap` holds everything to apply, and it carries no reason at all, because
every entry there is a deterministic operation whose own fields say what to do.

WHY A DROP NEEDS A CODE AND AN OPERATION DOES NOT
A dropped element produces no operation, so there is nothing to read the cause off — and the cause is
not derivable from the element's TYPE either. Measured on the OOTB Leads_FormPage: 11 of its 12 dropped
elements have `componentSuggestions[].category = "DirectMapping"`, i.e. a type that converts perfectly
well. Seeing only the name and type, you would read every one of them as conversion loss, and the
natural response to conversion loss is to re-insert it — putting a duplicate Save button beside the
mobile template's native one. The codes below split those 12 into four different things to say.

An UNKNOWN code means your clio is newer than this article. Report it verbatim and do not guess.

Read this alongside get-guidance `freedom-page-web-to-mobile-conversion`, which owns the conversion
flow itself.

NOT LOSS — report it, and re-insert NOTHING
  drop-inherited-chrome        Chrome inherited from the source page's own TEMPLATE, which the mobile
                               template provides natively (title container, back/save/cancel/close).
                               params.name; params.scope and params.target when it was an action the
                               converter considered retargeting. Re-adding it duplicates a native
                               element.
  drop-excluded-by-rule        A POSITIONAL exclusion the converter applied by rule: params.webType is
                               banned from params.hostType[params.slot] (params.host names the
                               instance). The same type OUTSIDE that position converts normally, so
                               seeing it dropped in one place and kept in another on the same page is
                               correct, not an inconsistency. It is NOT conversion loss: do NOT
                               re-insert the component — not into that host, not anywhere else on the
                               page — and do NOT ask whether to keep it, because asking re-opens a
                               decision the converter configuration already made.
  drop-parent-excluded         params.ancestor was excluded, so this element had no mobile parent left.
                               Re-creating it would rebuild the branch the rule exists to remove. A rule
                               targeting a CONTAINER type produces mostly this code, and it names the
                               very elements a user asks about — match an exclusion on BOTH codes.
  drop-empty-container         Every child dropped, so the converter removed the empty shell. Automatic
                               housekeeping: do not re-create it, do not re-parent anything into it, and
                               do not ask the user about it.

GENUINE LOSS — tell the user what is gone
  drop-unsupported-request     params.request is KNOWN-unsupported on the Mobile app, so the action is
                               lost. Say so.
  drop-unknown-request         params.request is in NEITHER the conversion map nor the bundled set. clio
                               cannot claim it is unavailable on mobile, only that it does not know it —
                               so if that custom request IS implemented on mobile, the action can be
                               re-added by hand. Offer that.
  drop-type-not-in-mobile-registry
                               params.webType has no mobile counterpart at all. The one cause you could
                               also have derived, from `componentSuggestions[].category = "unsupported"`.

A CONVERSION-RULES DEFECT — report the name, do not work around it
  drop-target-missing          params.target is absent from the mobile template, so the element could not
                               be placed. params.scope when it was inside a non-converting scope. This is
                               a rules-file problem, not a page problem: a rule retargets into a
                               container the target template does not have.

INSIDE A NON-CONVERTING SCOPE — nothing to do
  drop-no-rule-in-scope        No conversion rule matched this component inside params.scope.
  drop-not-an-action-in-scope  Inside params.scope and not itself a placeable action (no convertible
                               `clicked` of its own). Its nested actions were still flattened, so they
                               appear on their own.
