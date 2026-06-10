# Gjallar Architecture

## Objective

Gjallar owns framebuffer-native, multi-scale TUI composition: turning many
surface inputs into one dense, inspectable operator display where different
regions may use different text resolutions.

## Authority Map

- Owner: Gjallar owns display composition, pane partitioning, per-pane virtual
  text grids, font-scale selection, framebuffer lowering, and future sparse
  refresh output. Gjallar also owns local cursor state, title-bar hit testing,
  and minimized/restored panel state.
- Inputs: provider catalogs, provider-owned surface trees, display dimensions,
  font assets, runtime flags, mouse input, Odin's canonical slash-delimited
  marquee tape, and eventually direct CultMesh subscriptions.
- Outputs: `gjallar.overview` composition state, visible framebuffer frames,
  cursor presentation, top-tab minimized panel presentation, and renderer
  telemetry.
- Derived state: pane weights, pixel rectangles, chosen font sizes, one-row
  gutter blocks, an ECS-style structure-of-arrays gutter ribbon, ordered
  marquee queue objects, ribbon occupancy, dirty rectangles, glyph runs,
  title-bar hit regions, top-tab restore affordances, local
  minimized-panel keys, and compact agent-readable projections.
- Forbidden writers: discovery systems do not decide Gjallar layout; providers
  do not tune themselves for Nightwing; framebuffer backends do not invent
  provider truth; Gjallar does not derive marquee content from provider status
  summaries; Odin and providers do not own Nightwing-local minimized state.
- Shared paths: full-frame rendering, sparse refresh, future browser previews,
  and agent text capture should lower the same overview model.
- Deletion line: any code that both discovers provider truth and decides final
  screen composition belongs on one side or the other. Gjallar shows; discovery
  systems see.

## Current Pipeline

```text
Odin provider catalog
  -> display provider selection
  -> provider surface fetch
  -> canonical marquee tape passthrough
  -> synthetic gjallar.overview surface
  -> EveNode panel extraction
  -> weighted AABB packing
  -> local minimized-state application
  -> minimized top-tab strip reservation
  -> title-bar hit-region emission
  -> one-row gutter cell plan as an addressable SoA ribbon
  -> slash-delimited marquee queue object lowering into ribbon occupancy
  -> per-panel text pressure estimate
  -> PSF font selection
  -> frame draw commands
  -> BGRA framebuffer present
```

## Target Pipeline

```text
SurfaceSource[]
  -> GjallarOverview
  -> PaneTree
  -> PaneGrid[]
  -> RenderPlan
  -> backend
```

Backends:

- `FramebufferBackend`: full BGRA frame writes, current implementation.
- `SparseCellBackend`: dirty cell/glyph/rect pipe for high-frequency remote or
  terminal targets.
- `AgentTextBackend`: token-efficient navigation and panel capture for agents.

## First Product Claim

Gjallar is not a terminal multiplexer and not a dashboard theme. It is a
multi-resolution text compositor. The important gap is that each pane can become
its own virtual terminal scale while still belonging to one coherent display.

## Raster Font Authority

Gjallar owns TUI pixel typography for its framebuffer body. Eve GUI lowerers use
outline display/body families such as Montserrat, Zen Kaku Gothic New, M PLUS 1,
and Ubuntu Sans; those are not substitutes for Gjallar's raster cell fonts.

For any scale Gjallar claims as Japanese-capable, the font must provide Latin,
hiragana, and katakana in the same fixed cell grid. The target ladder is:

- display: roughly 16px or larger;
- body: roughly 12px;
- small: roughly 10px.

`PsfFont` reads Unicode-mapped PSF fonts and `FontAtlas` prefers
kana-capable faces when hiragana or katakana appears in panel titles or body text. The
asset line is still explicit: package or generate approved Unicode raster
atlases for the 16/12/10 ladder before claiming full Japanese pixel parity.
