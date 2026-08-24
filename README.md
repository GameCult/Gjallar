# Gjallar

Gjallar is the renderer-neutral compositor for Odin-visible Eve surfaces.

Most terminal UI systems assume one cell grid. Most dashboards assume a browser.
Gjallar starts from a different premise: a live machine room deserves a surface
that can subdivide pixels into weighted regions, give each region its own text
resolution, and render dense operational state without pretending every panel
should share the same font, cadence, or scale.

Call it a new kind of TUI if you want to be grandiose. We do.

## What It Is

Gjallar is a tiling window manager for semantic text surfaces:

```text
surface sources
  -> weighted pane tree
  -> per-pane virtual text grids
  -> per-pane font and cell resolution
  -> framebuffer frames today
  -> sparse cell / glyph / dirty-rect pipes tomorrow
```

It is not curses, tmux, or a web dashboard. Those are useful ancestors and
neighbors, but they do not own this shape. Gjallar is built for dashboards where
one panel might need huge visible status text, another might need a tiny
high-density queue, and another might need a marquee or rapidly changing
operator signal, all on the same physical display.

## Why It Exists

The daemon body is Yggdrasil beside Odin and Idunn. Odin discovers and accepts
provider surfaces; Gjallar composes every visible surface into one tiled
`gjallar.overview` Eve document. Eve clients decide how to draw that document.

The product is broader than that first deployment:

- control-room wallboards without a browser stack;
- local appliance dashboards;
- agent and daemon operations cockpits;
- terminal-native observability surfaces;
- low-bandwidth or sparse-refresh display pipes;
- human-visible and agent-readable TUI contexts from the same composed model.

The useful abstraction is not "a dashboard." The useful abstraction is a
multi-resolution text compositor whose panes are real spatial surfaces, not
monospace boxes trapped inside one global terminal grid.

## Current Runtime

Gjallar is currently a C# executable:

```powershell
dotnet build .\src\Gjallar\Gjallar.csproj
```

Publish a self-contained Linux build:

```powershell
dotnet publish .\src\Gjallar\Gjallar.csproj -c Release -r linux-x64 --self-contained true -o .\scratch\publish\gjallar
```

Run the Yggdrasil composition daemon against Odin's local snapshot endpoint:

```bash
/srv/gjallar/current/Gjallar \
  --headless \
  --odin-cultnet-rudp 127.0.0.1:17871 \
  --odin-cultmesh-rudp 127.0.0.1:17871 \
  --cultcache-path /var/lib/gamecult/gjallar/gjallar.service.cc \
  --stats-path /var/lib/gamecult/gjallar/status.json
```

In native CultNet/RUDP snapshot mode, Gjallar:

1. writes its typed CultCache witness and publishes its provider advertisement
   once to Odin's CultMesh/RUDP document ingress;
2. requests Odin's accepted provider-state snapshot over `cultnet.transport.rudp.v0`;
3. extracts provider-owned surface roots from Odin's accepted state;
4. builds the typed `gjallar.overview` surface with Eve's shared contract;
5. persists and republishes that aggregate through CultCache/CultMesh/CultNet;
6. emits composition freshness health to Idunn.

The optional framebuffer backend remains a lowering/debug body. It is not
started by the Yggdrasil daemon and must never be required for aggregation.

`--odin-cultmesh-rudp` may be used when Odin's document ingress differs from
the snapshot endpoint. By default it reuses `--odin-cultnet-rudp`.

The older `--url ws://.../eve/deck` path remains only for local compatibility
testing and is not configured in the Yggdrasil service.

## Architecture

Current internal organs:

- `GjallarConfig`: runtime flags and display source selection.
- `GjallarRenderer`: receive loop and aggregate composition; framebuffer work
  is conditional on the explicit non-headless backend.
- `AabbPacker`: weighted region partitioning.
- `EveNode`: retained surface tree projection.
- `FrameDocument`: framebuffer drawing command surface.
- `FontAtlas` / `PsfFont`: Unicode-aware PSF raster font loading plus generated
  smaller fonts.
- `FramebufferDevice`: Linux `/dev/fb*` mapping or file-backed framebuffer.

TUI typography is raster-first. Gjallar should not rely on GUI webfont stacks
for pixel text. Any font scale advertised as Japanese-capable must provide
Latin, hiragana, and katakana in the same fixed cell grid. The target raster ladder is:

- display: roughly 16px or larger, for large labels and titles;
- body: roughly 12px, for dense readable panel text;
- small: roughly 10px, the smallest acceptable kana lane.

Current code can read Unicode-mapped PSF fonts and will prefer kana-capable
faces when a panel title or body contains kana. The remaining asset work is
to package or generate approved 16/12/10 raster atlases; Latin-only console
fonts are no longer a coherent answer for Japanese text.

The next durable cut is to make the compositor model explicit:

```text
GjallarOverview
  -> PaneTree
  -> PaneGrid
  -> RenderPlan
  -> FramebufferBackend | SparseCellBackend | AgentTextBackend
```

Today some of that model is still implicit inside the renderer. That is useful
scaffolding, not a final architecture.

## Relationship To Odin

Odin sees. Gjallar shows.

Odin owns Verse discovery, provider cataloging, schemas, routes, and accepted
provider/proxy surfaces. Gjallar consumes those surfaces and owns composition,
density, tiling, font scale, update cadence, and display lowering.

If Odin decides where a panel goes, ownership leaked upward. If a provider
starts shaping itself for one client instead of publishing a clean surface,
ownership leaked downward.

## Status

The Yggdrasil deployment body is defined under `deploy/` and `systemd/`.
Headless status explicitly reports framebuffer disablement:

```json
{
  "schema": "gamecult.gjallar.frame.v1",
  "mode": "yggdrasil-composition-daemon",
  "presentMode": "typed-eve-surface-publication",
  "framebuffer": { "enabled": false }
}
```

The horn is not polished. It is real enough to make noise.
