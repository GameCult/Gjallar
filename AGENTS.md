# Gjallar Agent Instructions

Gjallar is a framebuffer-native, multi-scale TUI compositor.

Do not treat this repo as an Odin subfolder, a Nightwing deployment script, a
terminal multiplexer clone, or a dashboard skin. Gjallar owns the product kernel
for weighted pane partitioning, per-pane virtual text grids, font-scale
selection, framebuffer lowering, and future sparse cell/glyph/dirty-rect pipes.

## Session Bootstrap

On fresh workspace load, read these before implementation:

1. `state/gjallar-state.md`
2. `README.md`
3. `docs/architecture.md`

Then restate the current owner map before making nontrivial changes.

## Operating Boundary

- Odin sees: Verse discovery, provider cataloging, schemas, routes, and accepted
  provider/proxy surfaces.
- Gjallar shows: overview composition, panel packing, density, text resolution,
  framebuffer/cell lowering, and display telemetry.
- Nightwing hosts: the physical machine/body currently running `gjallar.service`.
- Providers speak: each upstream surface owns its own truth.

If Odin starts deciding where panels go on Nightwing, ownership leaked upward.
If providers start shaping themselves for Nightwing instead of publishing clean
surfaces, ownership leaked downward.

## Research Habit

When comparing Gjallar to existing tools, preserve the distinction:

- TUI frameworks such as Ratatui help build one terminal-grid app.
- Multiplexers such as tmux split one terminal cell grid into panes.
- Notcurses offers terminal planes/piles and rich terminal graphics.
- DirectFB offers framebuffer graphics/windowing substrate.
- Gjallar's gap is multi-resolution text composition: weighted pixel regions,
  each with its own virtual grid and font scale, belonging to one coherent
  dashboard surface.

Update `state/gjallar-state.md` when research changes that belief.
