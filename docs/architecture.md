# Gjallar Architecture

## Objective

Gjallar owns framebuffer-native, multi-scale TUI composition: turning many
surface inputs into one dense, inspectable operator display where different
regions may use different text resolutions.

## Authority Map

- Owner: Gjallar owns display composition, pane partitioning, per-pane virtual
  text grids, font-scale selection, framebuffer lowering, and future sparse
  refresh output.
- Inputs: provider catalogs, provider-owned surface trees, display dimensions,
  font assets, runtime flags, and eventually direct CultMesh subscriptions.
- Outputs: `gjallar.overview` composition state, visible framebuffer frames,
  and renderer telemetry.
- Derived state: pane weights, pixel rectangles, chosen font sizes, gutter
  routes, dirty rectangles, glyph runs, and compact agent-readable projections.
- Forbidden writers: discovery systems do not decide Gjallar layout; providers
  do not tune themselves for Nightwing; framebuffer backends do not invent
  provider truth.
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
  -> synthetic gjallar.overview surface
  -> EveNode panel extraction
  -> weighted AABB packing
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
