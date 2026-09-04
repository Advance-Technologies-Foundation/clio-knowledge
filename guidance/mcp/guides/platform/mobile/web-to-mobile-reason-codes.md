clio MCP — mobile conversion: reason codes

PURPOSE
The ONE closed vocabulary behind every `reason` in a get-mobile-page-conversion-guide response: why
something did NOT reach the mobile page, and what to tell the user about it. `reason` is always a LIST of
{code, params?}. Branch on `code`; read `params` for this occurrence's values.

Five fields carry it, grouped into four sections below:
  guide.droppedElements[].reason                  a source ELEMENT that was not built
  guide.requestConversions.droppedRequests[].reason   an action BINDING that was lost
  guide.requestConversions.flaggedRequests[].reason   an action BINDING kept but unverified
  guide.pageBusinessRules.droppedRules[].reason   a page BUSINESS RULE that does not convert
  guide.normalizations.<group>.skipped[].reason   a property NORMALIZATION the converter refused

One vocabulary, not five, so the same cause never arrives under two spellings. A code's prefix says what
kind of outcome it is: `drop-` gone, `flag-` kept but verify, `skip-` deliberately not done.

`params` never repeats a field the record already has. Every entry names its own subject — an element by
`webName`, a binding by `elementName` + `binding` + `webRequest` / `request`, a rule by `caption`, a
normalization by `name` + `properties` — so read those first and use `params` only for what the code adds.

NOTHING HERE IS AN INSTRUCTION TO APPLY. All five fields are the audit trail of what was NOT built —
you REPORT them. `guide.viewConfigDiff` holds everything to apply, and it carries no reason at all, because
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
  drop-container-no-mobile-equivalent
                               A CONTAINER whose params.webType is absent from the mobile registry: the
                               wrapper is not recreated, but its CHILDREN are — each one is in
                               viewConfigDiff already re-parented to params.target, so the branch is
                               FLATTENED, not lost. Contrast drop-type-not-in-mobile-registry below: same
                               cause on a LEAF, where it IS loss. Say the layout wrapper is gone if that
                               matters, and author NOTHING — re-creating it would insert a parent the diff
                               does not create, and the children already name their new one.

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


AN ACTION BINDING — requestConversions.droppedRequests[] / flaggedRequests[]

A binding lost because its ELEMENT was dropped carries THAT ELEMENT'S OWN CODE — one of the element
codes above, with the same params — not a code of its own. The element is why the action is gone, so the
two records say one thing in one vocabulary. Read such an entry as "see the element", and report the loss
ONCE, not twice. The codes below are the cases the BINDING owns: the first two happen while the element
itself survives, and the next two are reconciliation passes that ran after the binding was recorded.

  drop-request-chrome-native   The element was dropped as inherited chrome and the mobile template's
                               native control carries its OWN action. READ THE ENTRY'S webRequest: when it
                               is the platform's standard request for that control (crt.SaveRecordRequest
                               on a Save button) NOTHING is lost — say so and add nothing. When it is a
                               CUSTOM request (usr.*), the page had overridden that button's behaviour and
                               THAT is lost: tell the user, because the native control will do the
                               standard thing instead. This one entry is why chrome bindings are reported
                               at all instead of dropped silently.
  drop-request-unsupported     The entry's webRequest is KNOWN-unsupported on mobile, so the binding was
                               removed and THE COMPONENT STILL RENDERS — it is on the page, without that
                               action. Different from the element code drop-unsupported-request, where the
                               whole component is gone. params.note carries the conversion rule author's
                               own remark when the rules file has one; show it as detail, branch on the
                               code.
  drop-request-element-empty-container
                               The binding went with its container, which the empty-container pass
                               removed after the binding had been recorded. The container's own
                               droppedElements entry (drop-empty-container) has the detail. Automatic
                               housekeeping: nothing to re-create.
  drop-request-element-excluded
                               The binding went with its element, which an excludedComponents rule
                               removed. The element's own entry (drop-excluded-by-rule or
                               drop-parent-excluded) has the detail, and those codes' "re-insert NOTHING"
                               rule covers the binding too.

  flag-request-unmapped        KEPT, NOT LOST. The entry's request is in neither the conversion map nor
                               the bundled set, so it was carried VERBATIM onto the mobile element.
                               The component works; the action may or may not, and clio cannot tell.
                               Ask the user to verify that request exists on mobile. Do NOT remove the
                               binding and do NOT report it as conversion loss.


A PAGE BUSINESS RULE — pageBusinessRules.droppedRules[]

Each entry also carries `caption`, which is how the developer finds the rule to recreate. All four mean
"recreate this rule by hand"; the code says WHAT to change while doing it.

  drop-rule-condition-mixed-and-or
                               The condition mixes AND and OR across nested groups. A mobile page rule
                               supports ONE flat condition group with one logical operator, so emitting
                               it would change when the rule fires. Recreating it means splitting the
                               condition into separate rules.
  drop-rule-condition-unsupported-comparison
                               The condition uses a comparison with no mobile equivalent. Emitting it
                               would silently change the comparison. Recreating it means choosing a
                               different comparison, so the user has a decision to make.
  drop-rule-no-action-converts The condition was fine; every ELEMENT the rule's actions target was
                               dropped. Look those elements up in droppedElements first — if they were
                               dropped as not-loss (native chrome, a positional exclusion), the rule is
                               simply not needed on mobile, and recreating it would target nothing.
  drop-rule-condition-unconvertible
                               The condition cannot be converted and clio did not classify why. Report it
                               verbatim; recreate the rule by hand from the source page.


A SKIPPED NORMALIZATION — normalizations.<group>.skipped[]

  skip-normalization-path-blocked
                               The element carries a NON-OBJECT value at the path the rule stamps —
                               typically a whole-value binding — and a merging rule never overwrites one.
                               Replacing it with an object built from the rule alone would destroy the
                               binding and leave the component missing fields it needs while LOOKING
                               normalized, so the element keeps its source value there. The entry's own
                               `properties` name the refused paths, and `normalized` on the same group
                               shows what did get stamped. Nothing to do: this is the safe outcome, not a
                               failure. Mention it only if the user asks why one element looks different.
