# Gjallar Architecture

## Objective

Gjallar owns renderer-neutral aggregation and tiling: turning every Odin-visible
provider surface into one typed Eve composition that GUI, TUI, framebuffer, and
agent clients can lower without recreating membership or layout authority.

## Authority Map

- Owner: Gjallar owns aggregate membership, pane partitioning, layout intent,
  visibility, and the versioned `gjallar.overview` surface.
- Inputs: provider catalogs, provider-owned surface trees, display dimensions,
  font assets, runtime flags, mouse input, Odin's canonical slash-delimited
  marquee tape, and Odin's accepted provider-state snapshot over CultNet/RUDP.
- Outputs: typed `gjallar.overview` composition state, a local CultCache witness,
  continuous CultMesh/CultNet surface publication, and composition health.
- Derived state: pane weights, pixel rectangles, chosen font sizes, one-row
  gutter blocks, an ECS-style structure-of-arrays gutter ribbon, ordered
  marquee queue objects, ribbon occupancy, dirty rectangles, glyph runs,
  title-bar hit regions, top-tab restore affordances, local
  minimized-panel keys, and compact agent-readable projections.
- Forbidden writers: discovery systems do not decide Gjallar layout; providers
  do not tune themselves for clients; Gjallar daemon mode does not open
  `/dev/fb0`, capture local input, or own client pixels; client lowerers do not
  decide aggregate membership or provider truth.
- Shared paths: EveCanvas, browser, TUI/framebuffer, sparse refresh, and agent
  capture all lower the same published overview model.
- Deletion line: any code that both discovers provider truth and decides final
  screen composition belongs on one side or the other. Gjallar shows; discovery
  systems see.

## Current Pipeline

```text
Odin accepted provider-state snapshot on Yggdrasil
  -> one startup provider advertisement to Odin over CultMesh/RUDP
  -> Odin accepted provider-state snapshot over CultNet/RUDP
  -> display provider selection
  -> provider surface extraction
  -> canonical marquee tape passthrough
  -> typed gjallar.overview Eve surface
  -> CultCache + CultMesh/CultNet publication
  -> Eve GUI/TUI/agent clients
```

Optional framebuffer lowering continues from the published overview:

```text
gjallar.overview
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

- display: 16x16;
- body: 14x14;
- small: 12x12.

`PsfFont` reads Unicode-mapped PSF fonts and `FontAtlas` selects from the
packaged Shinonome bitmap family. Default startup now treats that family as a
hard contract: if any required raster is missing, mis-sized, or missing kana,
Gjallar fails loudly instead of falling back to random system console fonts.

The repo-native verification path is:

- `tools/verify_bitmap_family.ps1`

That verifier builds Gjallar, runs specimen mode against a temp framebuffer, and
proves the shipped 12/14/16 family loads with zero missing glyphs for a mixed
Latin/hiragana/katakana specimen string including `メタめ`. The verifier uses
`--specimen-text-file` so UTF-8 specimen content does not depend on shell
argument encoding quirks.
