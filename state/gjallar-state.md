# Gjallar State

Last updated: 2026-06-06

## Identity

Gjallar is the horn and the display body: a framebuffer-native, multi-scale TUI
compositor for dense operational surfaces.

His job is not merely to view Odin. Odin is the first upstream witness, and
Nightwing is the first physical body. The reusable product kernel is the
compositor itself:

```text
surface sources
  -> weighted pane tree
  -> per-pane virtual text grids
  -> per-pane font and cell resolution
  -> framebuffer frames now
  -> sparse cell / glyph / dirty-rect pipes later
```

## Body

- Repo: `E:\Projects\Gjallar`
- GitHub: `https://github.com/GameCult/Gjallar`
- Runtime: `src/Gjallar/Gjallar.csproj`
- Current deployment: Nightwing `gjallar.service`
- Current feed: Odin provider catalog at `ws://192.168.1.66:8797/eve/deck`
- Current status path on Nightwing: `/var/log/gjallar.status`
- Current marquee input: Odin's provider catalog `marqueeText`, built from the
  Stonks securities tape interwoven with ordered VoidBot poem lines.

Observed live status after marquee/gutter cut:

```json
{
  "schema": "gamecult.gjallar.frame.v1",
  "receive": {
    "status": "catalog-composed",
    "catalogProviders": 7,
    "composedProviders": 6
  },
  "scene": {
    "panels": 6,
    "gutterRows": 3,
    "gutterPolicy": "single-row-top-between-panels-bottom",
    "marqueeChars": 3101
  }
}
```

## Authority Map

- Owner: Gjallar owns display composition, pane partitioning, per-pane virtual
  grids, font-scale choice, gutter text continuity, frame/cell lowering, and
  renderer telemetry.
- Inputs: provider catalogs, provider-owned surface trees, display dimensions,
  font assets, runtime flags, Odin's canonical marquee tape, and later native
  CultMesh subscriptions.
- Outputs: `gjallar.overview` composition state, framebuffer frames, sparse
  refresh streams, and agent-readable panel captures.
- Derived state: pane weights, pixel rectangles, selected fonts, one-row gutter
  cells, marquee glyph positions, dirty rectangles, glyph runs, and compact text
  projections.
- Forbidden writers: discovery systems must not decide Gjallar layout; providers
  must not tune themselves for Nightwing; framebuffer backends must not invent
  provider truth; Gjallar must not synthesize status-noise marquee content from
  provider summaries.
- Cut line: code that discovers what exists belongs upstream. Code that decides
  what the canonical marquee says belongs upstream. Code that decides how a
  physical or virtual display shows it belongs in Gjallar.

## Product Thesis

There is a gap between terminal UI frameworks, terminal multiplexers,
framebuffer graphics libraries, and browser dashboards.

Gjallar's claim:

> A modern TUI compositor should be able to divide pixels into weighted regions,
> assign each region its own virtual text grid and resolution, and lower the
> result to human-visible and agent-readable surfaces.

The grandiose public phrase is allowed: a new paradigm of TUI. Keep it earned.
The implementation must keep moving toward a real compositor model, not just a
pretty Nightwing dashboard.

## Research Ledger

### Ratatui

Ratatui is a strong Rust framework for building TUIs. Its layout model produces
rectangular areas for widgets inside the terminal's grid. This is adjacent but
not the same problem: Ratatui helps one app organize widgets inside one terminal
cell coordinate system; Gjallar wants each pane to become its own virtual grid
and font scale over framebuffer pixels.

Source: `https://ratatui.rs/concepts/layout/`

### tmux

tmux organizes terminal work into sessions, windows, and panes. It is excellent
for splitting and preserving terminal sessions, but the panes share the attached
terminal's cell geometry. Gjallar's difference is per-pane resolution and direct
framebuffer/text-surface composition rather than many shells in one terminal.

Sources:

- `https://github.com/tmux/tmux/wiki/Getting-Started`
- `https://documentation.ubuntu.com/server/reference/other-tools/terminal-multiplexers/`

### Notcurses

Notcurses has a modern terminal rendering model built around planes, cells, and
piles, plus rich terminal graphics and optional pixel protocols such as Sixel
and Kitty. It is the closest conceptual neighbor for composited terminal
surfaces. Gjallar differs by treating the framebuffer as the primary body and
by making per-pane text resolution part of the compositor contract.

Sources:

- `https://notcurses.com/html/classncpp_1_1_pile.html`
- `https://manpages.ubuntu.com/manpages/noble/man3/notcurses.3.html`

### DirectFB

DirectFB / DirectFB2 is a lightweight framebuffer graphics/windowing layer with
graphics acceleration, input abstraction, windows, and display layers on top of
Linux framebuffer devices. It is substrate-adjacent, not the product gap:
DirectFB can help draw/window on framebuffer, but Gjallar's special claim is the
multi-resolution text compositor above that substrate.

Source: `https://directfb1.org/`

### Linux Framebuffer

The Linux framebuffer/fbdev path is older and often superseded by DRM in modern
systems, but it remains useful on consoles, older hardware, embedded-ish bodies,
and local appliance displays. Gjallar's current Nightwing deployment uses this
boring durable body: write pixels to `/dev/fb0`.

Source: `https://en.wikipedia.org/wiki/Linux_framebuffer`

## Current Implementation Notes

- `GjallarRenderer` owns catalog reads, provider fetches, frame loop, and
  telemetry.
- `BuildGjallarSurface` currently builds the in-memory `gjallar.overview`
  surface.
- `AabbPacker` owns weighted panel region packing.
- `FontAtlas.ForTextBox` picks a font per panel based on available pixel area
  and text pressure.
- `FrameDocument` draws text, panels, gutters, marquee glyphs, and fills. It
  lowers the incoming marquee as one continuous stream across the ordered gutter
  cells instead of assigning independent text to each gutter row.
- `FramebufferDevice` maps or writes the Linux framebuffer.
- 2026-06-06 live fix: rooted provider endpoints such as `/eve/deck/bifrost`
  must be resolved against the configured Odin deck authority before opening a
  provider stream. In .NET, `Uri.TryCreate(..., UriKind.Absolute, ...)` can
  interpret rooted paths as `file://` URIs, which made Gjallar render the
  unavailable pane while the provider surface was actually reachable.
- Provider fetch diagnostics now publish `providerFetchUri` and
  `providerFetchError` in `/var/log/gjallar.status` so transport failures are
  visible at the layer that produces the framebuffer symptom.
- 2026-06-06 marquee/gutter cut: Gjallar no longer curates provider-boundary
  ticker segments. Odin publishes the canonical tape; Gjallar consumes it from
  the provider catalog and enforces one glyph row for top padding, one per
  inter-panel gutter band, and one for bottom padding.
- Transport debt remains explicit: the live Nightwing body still consumes the
  compatibility Eve deck bridge. The target input organ is native CultNet /
  CultMesh typed state over the real GameCult transport, not a web-shaped
  provider fetch loop.

This is enough to run, but not enough to be proud forever. The next architectural
cut is to extract explicit model types:

```text
GjallarOverview
PaneTree
PaneGrid
RenderPlan
FramebufferBackend
SparseCellBackend
AgentTextBackend
```

## Next Questions

- What is the clean `PaneTree` data model?
- How should provider surface hints translate into pane weights without letting
  providers own final display layout?
- What is the first sparse refresh contract: dirty rectangles, dirty cells, or
  glyph runs?
- How should an agent navigate the same composed model without reading a full
  framebuffer dump?
- When should Gjallar publish its own state as CultCache `.cc` instead of only
  status JSON?
