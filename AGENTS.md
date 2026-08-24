# Gjallar Agent Instructions

Gjallar is the renderer-neutral compositor for Odin-visible Eve surfaces.

Do not treat this repo as an Odin subfolder, a Nightwing deployment script, a
terminal multiplexer clone, or a dashboard skin. Gjallar owns aggregation,
tiling, and layout intent. Eve clients and optional TUI/framebuffer backends own
pixel lowering.

## Session Bootstrap

On fresh workspace load, read these before implementation:

1. `state/gjallar-state.md`
2. `README.md`
3. `docs/architecture.md`

Then restate the current owner map before making nontrivial changes.

## Operating Boundary

- Odin sees: Verse discovery, provider cataloging, schemas, routes, and accepted
  provider/proxy surfaces.
- Gjallar composes: overview membership, tiling, density, layout intent, and
  aggregate-surface publication.
- Yggdrasil hosts: the headless daemon beside Odin and Idunn.
- Eve clients lower: UIKit, browser, TUI, framebuffer, and future display bodies.
- Providers speak: each upstream surface owns its own truth.

If Odin starts deciding where panels go, ownership leaked upward. If Gjallar
opens a framebuffer in daemon mode, ownership leaked downward into a client
body. If providers tune themselves for a renderer instead of publishing clean
surfaces, ownership leaked into the provider.

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
