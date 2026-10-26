clio MCP widget color and icon guide

Use this guide whenever you choose a color for a Freedom UI ANALYTICAL widget (a metric/indicator or a
chart) or an icon for a metric tile on a DASHBOARD or a HOME PAGE, new or existing, so the widget is
readable on its white card and looks like Creatio's own product dashboards.

This guide is the single owner of the widget color palette and the metric icon rule on dashboards and
home pages. It does NOT own where a widget goes, how big it is, its card theme or its title and text:
those are in `dashboard-and-home-page-layout`. For the rest of the runtime payload read `indicator-widget`
(`crt.IndicatorWidget`) or `chart-widget` (`crt.ChartWidget`) and call `get-component-info`.

Evidence: the colors and icons below were read with `get-page` from the 8 stock analytics dashboards and
4 stock home pages of a Sales/Service Enterprise environment on 2026-10-15 (platform version not read).
Each contrast ratio was computed with the WCAG 2 formula from the preset's swatch color in the widget
color picker against the white card.

## Color

Applies to every widget: a metric's `config.layout.color`, a chart's `config.color` and its
`series[*].color`. The number is the WCAG 2 contrast ratio of the preset's swatch color against the
white card.

- MUST NOT use in any widget (below 3:1, unreadable on white): `orange-red` 2.39, `turquoise` 2.33,
  `orange` 1.88, `light-green` 1.61.
- DEFAULT choice (4.5:1 or more): `dark-blue` 13.83, `navy-blue` 7.21, `forest-green` 6.73,
  `burnt-coral` 6.01, `vivid-purple` 5.79, `steel-blue` 5.78, `blue` 5.77, `violet` 5.30,
  `cadmium-red` 5.01, `dark-turquoise` 4.50.
- Allowed (3:1 to 4.5:1): `celestial-blue` 4.05, `coral` 3.65, `red` 3.50, `purple` 3.08, `green` 3.05,
  `rusty-orange` 3.03, `light-blue` 3.03, `bright-red` 3.02, `dark-green` 3.01.

Color follows MEANING, never variety; the same meaning keeps the same color on one page. As on the
stock dashboards:

| Meaning | Color | Stock example |
|---|---|---|
| Neutral count, amount or time | `steel-blue` | Number of accounts |
| Negative: overdue, missing data, expiring, inactive | `cadmium-red` | Accounts without primary contact |
| New, this month, inflow | `dark-turquoise` | New contacts this month |
| Attention: waiting, reopened | `burnt-coral` | Reopened cases |
| Good result | `forest-green` | First contact resolution |
| A separate category | `vivid-purple` or `navy-blue` | Emails, calls |

On a chart, don't recolor bars per value; the stock Accounts analytics dashboard's series use `blue`
and `burnt-coral`.

## Icon

Every metric tile MUST carry an icon (`config.layout.icon.iconName`) chosen by meaning; every metric on
the stock dashboards has one. Examples:

| Meaning | `iconName` |
|---|---|
| Accounts, contacts, orders | `work-icon`, `contact-group-icon`, `trolley-icon` |
| Amounts | `coins-icon`, `money-icon` |
| Overdue, inactive, response time | `hourglass-icon` |
| Duration, resolution time | `clock-icon` |
| Overdue response, overdue resolution | `message-warning-icon`, `problem-icon` |
| Waiting, reopened | `bell-icon` |
| Expiring | `fire-icon` |
| Missing data, contracts | `document-edit-icon`, `document-icon` |
| Goal, open opportunities, first contact resolution | `goal-icon` |
| Calls, emails | `phone-icon`, `mail-icon` |
| Satisfaction | `like-icon` |
| Resolved, closed | `checkmark-filled-icon` |
| New this month, in progress | `diagram-icon`, `level-icon` |

## Finish checklist

- No `orange-red`, `orange`, `light-green` or `turquoise` in any widget; every color follows meaning and
  one meaning keeps one color on the page.
- Every metric tile has an icon chosen by meaning.
