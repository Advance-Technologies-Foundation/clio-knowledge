clio MCP — Freedom UI web→mobile conversion: elementMap reason codes

PURPOSE
The closed vocabulary of elementMap[].reason codes returned by get-mobile-page-conversion-guide, and
what each one asks of you. reason is a LIST of {code, params?} — the first code classifies the element,
later ones are facts a later conversion pass added. It carries NO prose: the same conversion decision
reads identically on every run, so restating it in English per entry cost bytes and determinism without
adding information. Branch on `code`; read `params` for this occurrence's values.

An UNKNOWN code means your clio is newer than this article. Report it to the user verbatim and do not
guess — a code you cannot look up is the one case where acting is worse than asking.

Read this alongside get-guidance `freedom-page-web-to-mobile-conversion`, which owns the conversion
flow itself.

REASON CODES
elementMap[].reason is a LIST of {code, params?} — never prose. The first code classifies the element;
later ones are facts a later pass added. Branch on `code`; read `params` for the values. An unknown code
means your clio is newer than this article: report it, do not guess.

Codes with WORK TO DO:
  component-twin-no-baseline   the delta could NOT be computed (no web-template baseline). Configure
                               params.mobileName by merge-by-name per componentSuggestions. THIS IS THE
                               ONLY twin code that asks you to do something.
  unknown (parentSource)       not a reason code — see NEVER AUTHOR A PARENT.

Codes meaning DO NOTHING (report, never act):
  component-twin-nothing-to-carry  the page changes nothing on it. Leave params.mobileName exactly as the
                                   mobile template configures it; do NOT carry the web values over.
  component-twin-structural        a DIFFERENT mobile type, so no delta exists BY DESIGN; the how-to is
                                   type-driven and lives in componentSuggestions.
  component-twin-prebuilt          mobileValues carries the delta — merge it by name, add nothing.
                                   params.carryProperties, when present, names what was carried.
  template-twin                    the template provides it; merge onto params.mobileName.
  template-twin-attachments        as above, and retarget the attachments data source to the entity's
                                   file object.

Codes that just classify an insert:
  leaf-supported / container-supported        inserted as-is.
  leaf-retargeted / container-retargeted      a conversion template moved it (params.parent, .property).
  leaf-positioned / container-positioned      placed relative to an anchor (params.placement above/below,
                                              .anchor, .parent).
  re-homed-to-hostable-ancestor               the walk moved it out of params.from (params.fromType),
                                              which cannot hold arbitrary children. Placement changed —
                                              report it.
  synthesized-by-converter                    no web counterpart; params.role is tab-body / tab-area /
                                              anchor-placement, params.tab names the tab.
  container-no-mobile-equivalent              NOT inserted; children reparented to params.target.
  tab-indexed-before-template-tabs            an explicit index keeps a converted tab before the
                                              template's own trailing tabs. The converter owns this.
  anchor-moved-down                            the anchor moved params.rows row(s) to make room above it.
                                              Its whole new layoutConfig is in the merge entry.

Drop codes (report all, re-insert none):
  drop-empty-container              every child dropped.
  drop-excluded-by-rule             params.webType banned from params.hostType[params.slot].
  drop-parent-excluded              params.ancestor was excluded, so this had no mobile parent left.
  drop-inherited-chrome             web-template chrome the mobile template provides natively.
  drop-target-missing               params.target is absent from the mobile template — a RULES defect.
  drop-type-not-in-mobile-registry  params.webType has no mobile counterpart.
  drop-unsupported-request          params.request is known-unsupported on the Mobile app.
  drop-unknown-request              params.request is in NEITHER the map nor the bundled set. clio cannot
                                    say it is unavailable, only that it does not know it — so if that
                                    custom request IS implemented on mobile, re-add the action by hand.
  drop-no-rule-in-scope             no rule matched this component inside a non-converting scope.
  drop-not-an-action-in-scope       in such a scope and not itself a placeable action.
  action-retargeted                 folded into params.target (e.g. FloatingActionButton.menuItems).
  path-blocked-by-scalar            the element already carries a non-object value at params.path.
