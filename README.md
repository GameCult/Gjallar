# Gjallar

Gjallar is a framebuffer-native, multi-scale TUI compositor.

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

The immediate body is Nightwing: a local machine with an attached framebuffer
display. The current feed comes from Odin's provider catalog. Odin discovers
what surfaces exist; Gjallar decides how to show them.

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

Run against Odin's provider catalog:

```bash
/opt/gamecult/gjallar/Gjallar \
  --fb /dev/fb0 \
  --odin-cultnet-rudp 192.168.1.66:17871 \
  --cultcache-path /var/lib/gjallar/gjallar.service.cc \
  --refresh-hz 60 \
  --stats-path /var/log/gjallar.status
```

In native CultNet/RUDP snapshot mode, Gjallar:

1. writes its typed CultCache witness and publishes its provider advertisement
   once to Odin's CultMesh/RUDP document ingress;
2. requests Odin's accepted provider-state snapshot over `cultnet.transport.rudp.v0`;
3. extracts provider-owned surface roots from Odin's accepted state;
4. builds an in-memory `gjallar.overview` surface;
5. packs panels into weighted pixel regions;
6. chooses a font/cell resolution for each region;
7. draws text, panels, gutters, and marquee;
8. writes one BGRA frame to the framebuffer.

`--odin-cultmesh-rudp` may be used when Odin's document ingress differs from
the snapshot endpoint. By default it reuses `--odin-cultnet-rudp`.

The older `--url ws://.../eve/deck` path remains as a compatibility fallback.

## Architecture

Current internal organs:

- `GjallarConfig`: runtime flags and display source selection.
- `GjallarRenderer`: receive loop, catalog composition, frame loop, telemetry.
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

If Odin decides where a panel goes on Nightwing, ownership leaked upward. If a
provider starts shaping itself for Nightwing instead of publishing a clean
surface, ownership leaked downward.

## Status

Early but live. Gjallar is already deployed on Nightwing as `gjallar.service`
and writes status telemetry such as:

```json
{
  "schema": "gamecult.gjallar.frame.v1",
  "receive": {
    "status": "catalog-composed",
    "catalogProviders": 4,
    "composedProviders": 3
  },
  "scene": {
    "panels": 3
  }
}
```

The horn is not polished. It is real enough to make noise.
