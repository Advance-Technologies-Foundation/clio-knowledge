clio MCP core rules - details

The rarely needed detail of `core-rules`, reached through it: `core-rules` is read first and every rule it
states applies here too. This article is the authoritative owner of the CLI `compile-package --wait` exit
codes, precompiled-package delivery, how to tell whether a compile, restart or process launch ran after
an answer that does not say so, the tools of an optional runtime check, and the OData foreign-key rejection checks. `core-rules` keeps every
invariant, the compile confirmation, and the rule never to call a timed-out compile or restart again.

## compile-package --wait (CLI)
- In clio builds whose `compile-package` accepts `--wait`, a C# compile error makes the command exit non-zero with the CSxxxx diagnostics (the new code is not loaded), and `--wait` blocks until the build has finished instead of returning when it was accepted, so pass `--wait` before a restart or probe. A `--wait` timeout without CSxxxx diagnostics (`--wait-timeout`, default 600 s, max 3600 s) also exits non-zero while the build may still be running: check compile-status before recompiling or restarting.

## Precompiled delivery
- Precompiled delivery is a separate path: build the package for the target Creatio runtime and compatible platform dependencies, include the built binaries in the package, then install it with `push-pkg`. A .NET 8 assembly cannot be deployed as a .NET Framework assembly; do not assume any .NET build matches every .NET target. Rebuild for the actual target or deliver supported source for target-side package compilation. Do not automatically recompile a compatible precompiled package after installation; restart Creatio to load the newly installed DLL, then verify its behavior.

## Did the compile, restart or process launch run?
Read this when compile-status or restart-status answers `not-found` or `requestfailed`, or run-process
answers `not-started` or `still-running`. The Long-running tools rule in `core-rules` still applies: do NOT
start another compile-creatio or restart-by-environment-name while one may be running, and every compile
needs the user's confirmation right before it.
- A compile-status or restart-status `not-found` means this MCP server session holds no record, not that nothing ran: a client that restarts its MCP server after giving up takes the record with it. For a compile, that not-found answer lists the environment's newest compilation-history rows, each with the UTC time its build finished (`finished-utc`) and how long before the answer (`finished-seconds-ago`), and `older-failures` for failed rows further back. A compile writes one row per project as each project's build ends, so only rows that finished after you called compile-creatio can be your compile's: compare their `finished-utc` with the `started-utc` of its in-progress answer, allowing a minute either way because the environment's clock and clio's can differ, or, without one, take your call to be at least as long ago as your client's timeout error says it waited, plus the time since. A row that finished clearly before your call is not yours, even one more than seven minutes old that looks like a finished compile. Other compiles and schema publishes write rows too, so match by time, and `succeeded: false` with errors means a build failed. Treat your compile as finished only when its newest row is more than seven minutes old - the application reloads about two minutes after the last row, and the margin also covers skew between clio's clock and the environment's - and do not restart before then; these rows cannot prove which compile wrote them, so ask the user before a restart that rests on them alone. If no row was written since your call, the compile has either not finished its first project yet or never started - a request still on its way when the MCP server restarted is lost with it: poll again in a minute or two. If a package or process-name compile has had ten minutes, or a full one about 20, and still no row was written since your call, treat it as not run and ask the user before compiling again. Do not read last-compilation-log for it: with no row since your call it can only return an earlier compile's verdict. When the answer has no `compilation-history` field (it carries `compilation-history-error`, or it comes from a clio that predates the field), fall back to last-compilation-log (through clio-run) - unless that error says the environment-name does not resolve, which last-compilation-log cannot fix either. Note that last-compilation-log reads the latest FINISHED compile and carries no time, so until the compile has had time to finish its verdict can be an earlier compile's: do not rely on it, or restart on it, before then. Even after that it cannot show that your compile ran - a compile whose request was lost with a restarted MCP server never ran - so report its verdict to the user as unconfirmed and ask before a restart that rests on it. For a restart, check that the environment answers (describe-environment, through clio-run; a restarted instance can take 1-10 minutes to warm up) instead of restarting again. When restart-status answers `requestfailed`, the restart request itself failed and no restart happened: ask the user, then retry once. run-process answers at the same MCP response deadline as compile-creatio and restart-by-environment-name. A `status: not-started` whose error says the launch request was never sent means nothing ran: call it again after a minute, and if that is not launched either, tell the user the environment is not answering. Any other `not-started` is the environment refusing the launch: act on the cause its error names instead of calling again. `status: still-running` means the request was sent and clio has no verdict - do NOT re-run it; judge the run from its effects or a later SysProcessLog row (its times can be the environment user's local time marked Z, so a time alone cannot tie a row to your launch), and if neither certainly shows it, tell the user the outcome is unconfirmed and ask before launching it again.

## Runtime check
A runtime check is optional and the user's choice. When you make one, use these tools instead of a hand-rolled
create/sleep/read/delete loop (clio 8.1.0.141 and later; `get-tool-contract` first):
- `run-process` with `wait` (default true: waits for a parked run; a run parked on a user task answers
  `waitingOn`, not a failure), `watch` (1-5 entities; the records that appeared come back as `created`) and
  `cleanup` (requires `watch`; deletes only the `created` records you created that are tied to this run).
- `trigger-smoke` for a record-triggered (signal) process, which `run-process` cannot launch: `entity`,
  `values` (the trigger record's columns), `watch` (1-5 entities, required), optionally `process` (only that
  process's runs decide success; others are reported as `unrelated`), `timeout` and `cleanup`. It creates the
  trigger record, reports the runs it started and what they created, then, unless `cleanup=false`, deletes the
  trigger and the records that point at it through a lookup. A created record with no such lookup stays (it is
  listed in `cleanup.skipped`), so the environment is not necessarily clean afterwards. It writes and deletes
  live records: confirm the target environment with the user first, as for any destructive tool.
- `page-smoke` only when the task needs a UI check of a Freedom UI form page (does it open, which buttons and
  fields show, what one button does) - use it INSTEAD of a hand-written Playwright script or browser steps, not
  as a routine step after every page change: `page` (the form page schema name), optionally `record-id` (edit
  mode; without it, add mode) and `timeout`. It opens the page headless with the environment's stored session
  and returns the visible `buttons`, `fields` and `consoleErrors`; `success=false` means the page did not open
  or the click was not made (`click.outcome` says which); with `click.outcome=unknown` the button may have been
  pressed, so read `click.created` before retrying. Console errors alone do not fail it, so read them. It is
  read-only unless you pass `click` (a button caption or element name), which presses that button like a user
  and can create or change records or start a process: treat it as a write and confirm the target environment
  with the user first. With `click`, `watch` (1-5 entities) lists the records that appeared in
  `click.created`; nothing is deleted. It needs a Chromium-based browser where clio runs, and asks the user
  first (clio 8.1.0.141 and later): its question offers a browser already on the machine (a
  headless shell Playwright or Puppeteer cached, an installed Chrome, Edge, Chromium or Brave), a one-time
  download of a pinned chrome-headless-shell from Chrome for Testing into `~/.clio/browsers/`, or no.
  `CHROME_PATH`, when set, replaces the question and a remembered `existing` or `download` answer (a warning
  says so), but not a remembered `none` (that still answers `browser-declined`), and never an answer the user
  gives on the call. A reply with `code=browser-consent-required` (or
  `browser-not-found`) and no `success` is a question for the USER, not a failure and not their consent, in
  the same way a CrtProcessBuilder refusal is not approval to install: relay `browserConsent.question` to
  the user verbatim, NEVER answer it for them or pick a browser yourself, then call page-smoke again with the
  `browser` (and, for `existing`, the `browser-path`) of the choice THEY picked. The answer is remembered, so
  the user is usually asked once; if a later call asks again, relay it again. `browser-declined` means the
  user said no: continue without a UI check. `browser-ready` means the download finished: call again without
  `browser`. `browser-downloading` means part of the download is kept (`browserDownload` says how much): call
  again without `browser` to resume it. `browser-download-failed`: tell the user; call again to retry, or ask the user before choosing
  another browser - never pick one for them.
- `odata-delete` with `dry-run`, `ids` or `cascade`, whose refusal carries an `explanation` - act on it
  instead of retrying.

## OData foreign-key rejection
- For an OData foreign-key rejection, check `get-tool-contract` for the write tool: clients whose contract describes a validated foreign-key hint can name the constraint and any table or column present in the server response. A missing referenced record can mean an incorrect lookup ID; it does not prove an OData mapping defect, and the rejected write can originate in an entity event handler. `on table '<T>'` names the written table; only `referenced table '<T>'` names the target. If the hint says the column or target is unknown, do not infer it from the constraint name. Inspect lookup metadata with `get-entity-schema-properties`, verify supplied IDs against the declared lookup schemas using the normal record-read workflow, and ask an administrator to resolve the physical constraint if still ambiguous. Do not guess or swap lookup IDs. For a delete-side `still referenced` hint, inspect the referencing rows and intended relationship before changing data. Continue to honor `record-created`, `diagnostic.side-effect` and `retry-guidance`; the hint does not establish whether the original write committed. Without this advertised hint, use the same metadata/readback checks and request the constraint details from an administrator when necessary.

## OData binary columns and record files
- `odata-create` cannot write a binary column: a file or image `Data` column, declared `Edm.Stream` in
  `$metadata` (for example `SysImage.Data`, `KnowledgeBaseFile.Data`). Creatio rejects such a row before it
  writes anything. Verified by readback on Creatio 10.2.760 (.NET 8) in October 2026: the answer was
  `The input was not valid.`, and no record was created. On clio <CLIO-UPLOAD-FILE-TBD> and later the row
  comes back with `record-created=false`, `diagnostic.side-effect=none`, and an error that names the binary
  column. Earlier clio versions report the side effect as unknown for the same rejection.
- Do not re-send the row with the binary value in another encoding. Create the row without the binary
  column.
- To attach a file to a record, call `upload-file` (clio <CLIO-UPLOAD-FILE-TBD> and later). Give the file
  entity (`ContactFile`, `AccountFile`, `KnowledgeBaseFile`), its lookup column to the parent record (`Contact`
  for `ContactFile`), and the parent record id. The tool reads the stored bytes back before it reports
  success. When it fails after the file was sent, it returns `file-id`: read that record with `odata-read`
  before you upload the file again. For an image in `SysImage`, call `upload-image`.
- The environment's file security policy can reject a file whose extension does not match its content. Tell
  the user; do not rename the file to pass the check.
- On a clio version without `upload-file`, tell the user that this clio cannot attach a file to a record,
  and ask them to update clio. Do not call the platform file API directly with a browser session.
