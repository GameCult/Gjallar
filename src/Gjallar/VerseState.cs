using System.Globalization;
using GameCult.Caching;
using GameCult.Caching.MessagePack;
using MessagePack;

internal sealed class GjallarVerseRuntime : IDisposable
{
    private static readonly CultRecordKey ProviderAdvertisementKey = new("provider:nightwing-gjallar");
    private static readonly CultRecordKey SurfaceStateKey = new("surface:nightwing-gjallar");
    private static readonly CultRecordKey RuntimeConfigKey = new("gjallar:runtime-config");
    private static readonly CultRecordKey FrameStatusKey = new("gjallar:frame-status");
    private static readonly CultRecordKey CommandBoundaryKey = new("gjallar:command-boundary");
    private static readonly CultRecordKey TransportProfileKey = new("gjallar:transport-profile");
    private static readonly CultRecordKey DaemonHealthKey = new("nightwing-gjallar");

    private readonly GjallarConfig config;
    private readonly CultCache cache;
    private readonly object publishLock = new();

    private GjallarVerseRuntime(GjallarConfig config, CultCache cache)
    {
        this.config = config;
        this.cache = cache;
    }

    public static GjallarVerseRuntime Create(GjallarConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.CultCachePath))
        {
            throw new InvalidOperationException("Gjallar CultCache witness path must be configured before the verse runtime can start.");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(config.CultCachePath) ?? ".");
        var cache = CultCacheMessagePack.Create(
            config.CultCachePath,
            new CultCacheOpenOptions
            {
                FlushOnDispose = true,
                StoreFlushOnDispose = true,
            });
        return new GjallarVerseRuntime(config, cache);
    }

    public void Publish(GjallarVersePulse pulse)
    {
        lock (publishLock)
        {
            Put(BuildProviderAdvertisement(pulse), ProviderAdvertisementKey);
            Put(BuildSurfaceState(pulse), SurfaceStateKey);
            Put(BuildRuntimeConfig(pulse), RuntimeConfigKey);
            Put(BuildFrameStatus(pulse), FrameStatusKey);
            Put(BuildCommandBoundary(pulse), CommandBoundaryKey);
            Put(BuildTransportProfile(pulse), TransportProfileKey);
            Put(pulse.IdunnHealth, DaemonHealthKey);
            cache.FlushAllBackingStores();
        }
    }

    public void Dispose() => cache.Dispose();

    private void Put<T>(T document, CultRecordKey key) where T : class =>
        cache.UpsertAsync(typeof(T), document, key).GetAwaiter().GetResult();

    private GjallarProviderAdvertisementRecord BuildProviderAdvertisement(GjallarVersePulse pulse) =>
        new()
        {
            RecordId = ProviderAdvertisementKey.Value,
            ProviderId = "nightwing-gjallar",
            ServiceId = "gjallar.framebuffer-compositor",
            VerseId = "nightwing.local",
            Title = "Nightwing Gjallar",
            Description = "Nightwing framebuffer compositor that lowers Odin/provider deck state into the shared wall display.",
            CanonicalService = "asgard.gjallar",
            LocatedService = "asgard.nightwing.gjallar",
            CultMeshAddress = "asgard.nightwing.gjallar/framebuffer",
            Status = "live",
            UpdatedAt = pulse.ObservedAt.ToString("O", CultureInfo.InvariantCulture),
            CapabilityIds =
            [
                "framebuffer.composition",
                "cultui-surface",
                "daemon-health",
                "cultcache-witness",
                "operator.mouse-local",
            ],
            Endpoints = new[]
            {
                config.Url,
                config.StatusPath,
                config.CultCachePath,
            }.Where(static value => !string.IsNullOrWhiteSpace(value)).ToArray(),
            Schemas =
            [
                new GjallarAdvertisedSchema
                {
                    SchemaId = "gamecult.eve.provider_advertisement.v1",
                    Owner = "gjallar.runtime",
                    Description = "Gjallar service identity, witness catalog, and display boundary projection.",
                },
                new GjallarAdvertisedSchema
                {
                    SchemaId = "gamecult.eve.surface_state.v1",
                    Owner = "gjallar.runtime",
                    Description = "Gjallar-owned operator surface describing the compositor body, transport debt, and live frame telemetry.",
                },
                new GjallarAdvertisedSchema
                {
                    SchemaId = "gjallar.runtime_config.v0",
                    Owner = "gjallar.runtime",
                    Description = "Framebuffer/deck wiring, witness path, and runtime-owned transport settings.",
                },
                new GjallarAdvertisedSchema
                {
                    SchemaId = "gjallar.frame_status.v0",
                    Owner = "gjallar.runtime",
                    Description = "Latest framebuffer composition pulse, deck receive state, and render timings.",
                },
                new GjallarAdvertisedSchema
                {
                    SchemaId = "gjallar.command_boundary.v0",
                    Owner = "gjallar.runtime",
                    Description = "Gjallar-owned control boundary and explicit non-ownership claims.",
                },
                new GjallarAdvertisedSchema
                {
                    SchemaId = "gjallar.transport_profile.v0",
                    Owner = "gjallar.runtime",
                    Description = "Current input/output transports and remaining compatibility debt.",
                },
                new GjallarAdvertisedSchema
                {
                    SchemaId = "idunn.daemon_health.v1",
                    Owner = "gjallar.runtime",
                    Description = "Daemon-owned health contract published over cultnet.transport.rudp.v0.",
                },
            ],
            Witnesses =
            [
                new GjallarAdvertisedWitness
                {
                    WitnessId = "gjallar.service.cc",
                    Path = config.CultCachePath,
                    FreshnessState = "fresh",
                    UpdatedAt = pulse.ObservedAt.ToString("O", CultureInfo.InvariantCulture),
                    Schemas =
                    [
                        "gamecult.eve.provider_advertisement.v1",
                        "gamecult.eve.surface_state.v1",
                        "gjallar.runtime_config.v0",
                        "gjallar.frame_status.v0",
                        "gjallar.command_boundary.v0",
                        "gjallar.transport_profile.v0",
                        "idunn.daemon_health.v1",
                    ],
                },
            ],
            Surfaces =
            [
                new GjallarAdvertisedSurface
                {
                    SurfaceId = "nightwing-gjallar",
                    Address = "asgard.nightwing.gjallar/framebuffer",
                    Kind = "dashboard",
                    Status = pulse.Status,
                    InputTransport = "compatibility.odin-websocket-deck",
                },
            ],
        };

    private GjallarSurfaceStateRecord BuildSurfaceState(GjallarVersePulse pulse)
    {
        var updatedAt = pulse.ObservedAt.ToString("O", CultureInfo.InvariantCulture);
        return new GjallarSurfaceStateRecord
        {
            RecordId = "surface:nightwing-gjallar",
            ProviderId = "nightwing-gjallar",
            Title = "Nightwing Gjallar",
            Version = pulse.ObservedAt.ToUnixTimeMilliseconds(),
            UpdatedAt = updatedAt,
            Surface = new GjallarSurfaceDocument
            {
                Schema = "gamecult.eve.surface.v1",
                Id = "nightwing-gjallar.surface",
                Title = "Nightwing Gjallar",
                Root = DashboardNode(
                    "nightwing-gjallar-root",
                    "Nightwing Gjallar",
                    $"{pulse.CatalogProviders} catalog / {pulse.ComposedProviders} composed / {pulse.Panels} visible panels",
                    pulse.Status,
                    GroupNode(
                        "nightwing-gjallar-frame",
                        "Frame",
                        MetricNode("nightwing-gjallar-status", "status", pulse.Status),
                        MetricNode("nightwing-gjallar-fps", "fps", pulse.MeasuredFps.ToString("0.0", CultureInfo.InvariantCulture)),
                        MetricNode("nightwing-gjallar-panels", "panels", pulse.Panels.ToString(CultureInfo.InvariantCulture)),
                        MetricNode("nightwing-gjallar-catalog", "catalog", pulse.CatalogProviders.ToString(CultureInfo.InvariantCulture)),
                        MetricNode("nightwing-gjallar-composed", "composed", pulse.ComposedProviders.ToString(CultureInfo.InvariantCulture))
                    ),
                    CardNode(
                        "nightwing-gjallar-transport",
                        "Transport",
                        TextNode("nightwing-gjallar-input-transport", $"input: compatibility.odin-websocket-deck ({pulse.ReceiveStatus})"),
                        TextNode("nightwing-gjallar-output-transport", "output: daemon-published-rudp-health + daemon-owned-cultcache-service-boundary"),
                        TextNode("nightwing-gjallar-witness", $"witness: {config.CultCachePath}"),
                        TextNode("nightwing-gjallar-status-path", $"status: {config.StatusPath}"),
                        TextNode("nightwing-gjallar-health-endpoint", $"health: {config.IdunnRudpHealth}")
                    ),
                    CardNode(
                        "nightwing-gjallar-cursor",
                        "Cursor",
                        TextNode("nightwing-gjallar-cursor-status", $"cursor: {pulse.CursorStatus}"),
                        TextNode("nightwing-gjallar-cursor-position", $"position: {pulse.CursorX}, {pulse.CursorY}"),
                        TextNode("nightwing-gjallar-last-click", $"last click: {pulse.LastClick}"),
                        TextNode("nightwing-gjallar-cursor-error", string.IsNullOrWhiteSpace(pulse.CursorError) ? "cursor error: none" : $"cursor error: {pulse.CursorError}")
                    ),
                    CardNode(
                        "nightwing-gjallar-scene",
                        "Scene",
                        TextNode("nightwing-gjallar-minimized", $"minimized: {pulse.MinimizedPanels}"),
                        TextNode("nightwing-gjallar-title-hits", $"title hits: {pulse.TitleHitRegions}"),
                        TextNode("nightwing-gjallar-gutters", $"gutter rows/cells: {pulse.GutterRows} / {pulse.GutterCells}"),
                        TextNode("nightwing-gjallar-marquee", $"marquee chars: {pulse.MarqueeChars}"),
                        TextNode("nightwing-gjallar-fetch", string.IsNullOrWhiteSpace(pulse.ProviderFetchError) ? "provider fetch: clean" : $"provider fetch: {pulse.ProviderFetchError}")
                    )
                ),
                Assets = [],
            },
        };
    }

    private static GjallarSurfaceNode DashboardNode(string id, string title, string summary, string status, params GjallarSurfaceNode[] children) =>
        new()
        {
            Id = id,
            Kind = "dashboard",
            Props = new GjallarSurfaceProps
            {
                Title = title,
                Summary = summary,
                Status = status,
            },
            Children = children,
        };

    private static GjallarSurfaceNode GroupNode(string id, string title, params GjallarSurfaceNode[] children) =>
        new()
        {
            Id = id,
            Kind = "group",
            Props = new GjallarSurfaceProps { Title = title },
            Children = children,
        };

    private static GjallarSurfaceNode CardNode(string id, string title, params GjallarSurfaceNode[] children) =>
        new()
        {
            Id = id,
            Kind = "card",
            Props = new GjallarSurfaceProps { Title = title },
            Children = children,
        };

    private static GjallarSurfaceNode MetricNode(string id, string label, string value) =>
        new()
        {
            Id = id,
            Kind = "metric",
            Props = new GjallarSurfaceProps
            {
                Label = label,
                Value = value,
                Text = $"{label}: {value}",
            },
            Children = [],
        };

    private static GjallarSurfaceNode TextNode(string id, string text) =>
        new()
        {
            Id = id,
            Kind = "text",
            Props = new GjallarSurfaceProps { Text = text },
            Children = [],
        };

    private GjallarRuntimeConfigRecord BuildRuntimeConfig(GjallarVersePulse pulse) =>
        new()
        {
            RecordId = RuntimeConfigKey.Value,
            DaemonId = "nightwing-gjallar",
            ServiceId = "gjallar.framebuffer-compositor",
            VerseId = "nightwing.local",
            FramebufferPath = config.FramebufferPath,
            Width = config.Width,
            Height = config.Height,
            RefreshHz = config.RefreshHz,
            DeckUrl = config.Url,
            StatusPath = config.StatusPath,
            CultCachePath = config.CultCachePath,
            FontPath = config.FontPath,
            MousePath = config.MousePath,
            IdunnRudpHealth = config.IdunnRudpHealth,
            IdunnDaemon = config.IdunnDaemon,
            IdunnHealthContract = config.IdunnHealthContract,
            UpdatedAt = pulse.ObservedAt.ToString("O", CultureInfo.InvariantCulture),
        };

    private GjallarFrameStatusRecord BuildFrameStatus(GjallarVersePulse pulse) =>
        new()
        {
            RecordId = FrameStatusKey.Value,
            DaemonId = "nightwing-gjallar",
            Status = pulse.Status,
            Frames = pulse.Frames,
            PaintMs = Math.Round(pulse.PaintMs, 2),
            MeasuredFps = Math.Round(pulse.MeasuredFps, 2),
            FramebufferWidth = config.Width > 0 ? config.Width : 1920,
            FramebufferHeight = config.Height > 0 ? config.Height : 1080,
            ReceiveStatus = pulse.ReceiveStatus,
            ReceiveError = pulse.ReceiveError,
            ProviderFetchError = pulse.ProviderFetchError,
            ProviderFetchUri = pulse.ProviderFetchUri,
            CatalogProviders = pulse.CatalogProviders,
            ComposedProviders = pulse.ComposedProviders,
            StateBytes = pulse.StateBytes,
            Panels = pulse.Panels,
            PanelFontUsage = pulse.PanelFontUsage.Select(static item => item?.ToString() ?? string.Empty).ToArray(),
            MinimizedPanels = pulse.MinimizedPanels,
            MinimizedTitles = pulse.MinimizedTitles,
            TitleHitRegions = pulse.TitleHitRegions,
            GutterCells = pulse.GutterCells,
            GutterRows = pulse.GutterRows,
            MarqueeChars = pulse.MarqueeChars,
            MarqueeSample = pulse.MarqueeSample,
            VisibleMarqueeRows = pulse.VisibleMarqueeRows,
            VisibleMarqueeHasStonks = pulse.VisibleMarqueeHasStonks,
            CursorStatus = pulse.CursorStatus,
            CursorError = pulse.CursorError,
            CursorActive = pulse.CursorActive,
            CursorX = pulse.CursorX,
            CursorY = pulse.CursorY,
            LastClick = pulse.LastClick,
            CultMathNative = pulse.CultMathNative,
            CopyMs = Math.Round(pulse.Timings.CopyMs, 2),
            DecorMs = Math.Round(pulse.Timings.DecorMs, 2),
            GutterMs = Math.Round(pulse.Timings.GutterMs, 2),
            PresentMs = Math.Round(pulse.Timings.PresentMs, 2),
            UpdatedAt = pulse.ObservedAt.ToString("O", CultureInfo.InvariantCulture),
        };

    private GjallarCommandBoundaryRecord BuildCommandBoundary(GjallarVersePulse pulse) =>
        new()
        {
            RecordId = CommandBoundaryKey.Value,
            DaemonId = "nightwing-gjallar",
            Mode = "read-only-runtime",
            WritesAccepted = false,
            OperatorInputAuthority = "nightwing-local-framebuffer-mouse",
            LifecycleAuthority = "idunn.local-command.restart + compatibility.systemd.gjallar.service",
            AcceptedCommands =
            [
                "local-titlebar-minimize-toggle",
            ],
            RejectedCommands =
            [
                "provider-surface-mutation",
                "verse-discovery-write",
                "daemon-health-override",
                "remote-layout-ownership",
            ],
            UpdatedAt = pulse.ObservedAt.ToString("O", CultureInfo.InvariantCulture),
        };

    private GjallarTransportProfileRecord BuildTransportProfile(GjallarVersePulse pulse) =>
        new()
        {
            RecordId = TransportProfileKey.Value,
            DaemonId = "nightwing-gjallar",
            CurrentState = "partial-rudp-health-and-provider-store-live",
            InputTransport = "compatibility.odin-websocket-deck",
            OutputTransport = "daemon-published-rudp-health + daemon-owned-cultcache-service-boundary",
            HealthContract = config.IdunnHealthContract,
            IdunnRudpHealth = config.IdunnRudpHealth,
            WitnessPath = config.CultCachePath,
            StatusPath = config.StatusPath,
            CurrentCutLine = "Gjallar now owns the Nightwing CultCache witness and daemon health publication; Odin WebSocket deck input remains the explicit compatibility transport debt until the native C# CultNet/RUDP intake path lands.",
            UpdatedAt = pulse.ObservedAt.ToString("O", CultureInfo.InvariantCulture),
        };
}

internal sealed class GjallarVersePulse
{
    public string Status { get; set; } = "starting";
    public int Frames { get; set; }
    public double PaintMs { get; set; }
    public double MeasuredFps { get; set; }
    public FrameTimings Timings { get; set; }
    public DateTimeOffset ObservedAt { get; set; }
    public string ReceiveStatus { get; set; } = "starting";
    public string ReceiveError { get; set; } = "";
    public string ProviderFetchError { get; set; } = "";
    public string ProviderFetchUri { get; set; } = "";
    public int CatalogProviders { get; set; }
    public int ComposedProviders { get; set; }
    public int StateBytes { get; set; }
    public int Panels { get; set; }
    public object[] PanelFontUsage { get; set; } = [];
    public int MinimizedPanels { get; set; }
    public string[] MinimizedTitles { get; set; } = [];
    public int TitleHitRegions { get; set; }
    public int GutterCells { get; set; }
    public int GutterRows { get; set; }
    public int MarqueeChars { get; set; }
    public string MarqueeSample { get; set; } = "";
    public string[] VisibleMarqueeRows { get; set; } = [];
    public bool VisibleMarqueeHasStonks { get; set; }
    public string CursorStatus { get; set; } = "";
    public string CursorError { get; set; } = "";
    public bool CursorActive { get; set; }
    public int CursorX { get; set; }
    public int CursorY { get; set; }
    public string LastClick { get; set; } = "";
    public bool CultMathNative { get; set; }
    public IdunnDaemonHealthRecord IdunnHealth { get; set; } = new();
}

[CultDocument("gamecult.eve.provider_advertisement", "gamecult.eve.provider_advertisement.v1")]
[MessagePackObject(AllowPrivate = true)]
internal sealed class GjallarProviderAdvertisementRecord
{
    [Key(0)]
    [CultName]
    public string RecordId { get; set; } = string.Empty;

    [Key(1)] public string ProviderId { get; set; } = string.Empty;
    [Key(2)] public string ServiceId { get; set; } = string.Empty;
    [Key(3)] public string VerseId { get; set; } = string.Empty;
    [Key(4)] public string Title { get; set; } = string.Empty;
    [Key(5)] public string Description { get; set; } = string.Empty;
    [Key(6)] public string CanonicalService { get; set; } = string.Empty;
    [Key(7)] public string LocatedService { get; set; } = string.Empty;
    [Key(8)] public string CultMeshAddress { get; set; } = string.Empty;
    [Key(9)] public string Status { get; set; } = string.Empty;
    [Key(10)] public string UpdatedAt { get; set; } = string.Empty;
    [Key(11)] public string[] CapabilityIds { get; set; } = [];
    [Key(12)] public string[] Endpoints { get; set; } = [];
    [Key(13)] public GjallarAdvertisedSchema[] Schemas { get; set; } = [];
    [Key(14)] public GjallarAdvertisedWitness[] Witnesses { get; set; } = [];
    [Key(15)] public GjallarAdvertisedSurface[] Surfaces { get; set; } = [];
}

[CultDocument("gjallar.runtime_config", "gjallar.runtime_config.v0")]
[MessagePackObject(AllowPrivate = true)]
internal sealed class GjallarRuntimeConfigRecord
{
    [Key(0)]
    [CultName]
    public string RecordId { get; set; } = string.Empty;

    [Key(1)] public string DaemonId { get; set; } = string.Empty;
    [Key(2)] public string ServiceId { get; set; } = string.Empty;
    [Key(3)] public string VerseId { get; set; } = string.Empty;
    [Key(4)] public string FramebufferPath { get; set; } = string.Empty;
    [Key(5)] public int Width { get; set; }
    [Key(6)] public int Height { get; set; }
    [Key(7)] public int RefreshHz { get; set; }
    [Key(8)] public string DeckUrl { get; set; } = string.Empty;
    [Key(9)] public string StatusPath { get; set; } = string.Empty;
    [Key(10)] public string CultCachePath { get; set; } = string.Empty;
    [Key(11)] public string FontPath { get; set; } = string.Empty;
    [Key(12)] public string MousePath { get; set; } = string.Empty;
    [Key(13)] public string IdunnRudpHealth { get; set; } = string.Empty;
    [Key(14)] public string IdunnDaemon { get; set; } = string.Empty;
    [Key(15)] public string IdunnHealthContract { get; set; } = string.Empty;
    [Key(16)] public string UpdatedAt { get; set; } = string.Empty;
}

[CultDocument("gjallar.frame_status", "gjallar.frame_status.v0")]
[MessagePackObject(AllowPrivate = true)]
internal sealed class GjallarFrameStatusRecord
{
    [Key(0)]
    [CultName]
    public string RecordId { get; set; } = string.Empty;

    [Key(1)] public string DaemonId { get; set; } = string.Empty;
    [Key(2)] public string Status { get; set; } = string.Empty;
    [Key(3)] public int Frames { get; set; }
    [Key(4)] public double PaintMs { get; set; }
    [Key(5)] public double MeasuredFps { get; set; }
    [Key(6)] public int FramebufferWidth { get; set; }
    [Key(7)] public int FramebufferHeight { get; set; }
    [Key(8)] public string ReceiveStatus { get; set; } = string.Empty;
    [Key(9)] public string ReceiveError { get; set; } = string.Empty;
    [Key(10)] public string ProviderFetchError { get; set; } = string.Empty;
    [Key(11)] public string ProviderFetchUri { get; set; } = string.Empty;
    [Key(12)] public int CatalogProviders { get; set; }
    [Key(13)] public int ComposedProviders { get; set; }
    [Key(14)] public int StateBytes { get; set; }
    [Key(15)] public int Panels { get; set; }
    [Key(16)] public string[] PanelFontUsage { get; set; } = [];
    [Key(17)] public int MinimizedPanels { get; set; }
    [Key(18)] public string[] MinimizedTitles { get; set; } = [];
    [Key(19)] public int TitleHitRegions { get; set; }
    [Key(20)] public int GutterCells { get; set; }
    [Key(21)] public int GutterRows { get; set; }
    [Key(22)] public int MarqueeChars { get; set; }
    [Key(23)] public string MarqueeSample { get; set; } = string.Empty;
    [Key(24)] public string[] VisibleMarqueeRows { get; set; } = [];
    [Key(25)] public bool VisibleMarqueeHasStonks { get; set; }
    [Key(26)] public string CursorStatus { get; set; } = string.Empty;
    [Key(27)] public string CursorError { get; set; } = string.Empty;
    [Key(28)] public bool CursorActive { get; set; }
    [Key(29)] public int CursorX { get; set; }
    [Key(30)] public int CursorY { get; set; }
    [Key(31)] public string LastClick { get; set; } = string.Empty;
    [Key(32)] public bool CultMathNative { get; set; }
    [Key(33)] public double CopyMs { get; set; }
    [Key(34)] public double DecorMs { get; set; }
    [Key(35)] public double GutterMs { get; set; }
    [Key(36)] public double PresentMs { get; set; }
    [Key(37)] public string UpdatedAt { get; set; } = string.Empty;
}

[CultDocument("gjallar.command_boundary", "gjallar.command_boundary.v0")]
[MessagePackObject(AllowPrivate = true)]
internal sealed class GjallarCommandBoundaryRecord
{
    [Key(0)]
    [CultName]
    public string RecordId { get; set; } = string.Empty;

    [Key(1)] public string DaemonId { get; set; } = string.Empty;
    [Key(2)] public string Mode { get; set; } = string.Empty;
    [Key(3)] public bool WritesAccepted { get; set; }
    [Key(4)] public string OperatorInputAuthority { get; set; } = string.Empty;
    [Key(5)] public string LifecycleAuthority { get; set; } = string.Empty;
    [Key(6)] public string[] AcceptedCommands { get; set; } = [];
    [Key(7)] public string[] RejectedCommands { get; set; } = [];
    [Key(8)] public string UpdatedAt { get; set; } = string.Empty;
}

[CultDocument("gjallar.transport_profile", "gjallar.transport_profile.v0")]
[MessagePackObject(AllowPrivate = true)]
internal sealed class GjallarTransportProfileRecord
{
    [Key(0)]
    [CultName]
    public string RecordId { get; set; } = string.Empty;

    [Key(1)] public string DaemonId { get; set; } = string.Empty;
    [Key(2)] public string CurrentState { get; set; } = string.Empty;
    [Key(3)] public string InputTransport { get; set; } = string.Empty;
    [Key(4)] public string OutputTransport { get; set; } = string.Empty;
    [Key(5)] public string HealthContract { get; set; } = string.Empty;
    [Key(6)] public string IdunnRudpHealth { get; set; } = string.Empty;
    [Key(7)] public string WitnessPath { get; set; } = string.Empty;
    [Key(8)] public string StatusPath { get; set; } = string.Empty;
    [Key(9)] public string CurrentCutLine { get; set; } = string.Empty;
    [Key(10)] public string UpdatedAt { get; set; } = string.Empty;
}

[CultDocument("gamecult.eve.surface_state", "gamecult.eve.surface_state.v1")]
[MessagePackObject(AllowPrivate = true)]
internal sealed class GjallarSurfaceStateRecord
{
    [Key(0)]
    [CultName]
    public string RecordId { get; set; } = string.Empty;

    [Key(1)] public string ProviderId { get; set; } = string.Empty;

    [Key(2)] public string Title { get; set; } = string.Empty;
    [Key(3)] public long Version { get; set; }
    [Key(4)] public string UpdatedAt { get; set; } = string.Empty;
    [Key(5)] public GjallarSurfaceDocument Surface { get; set; } = new();
}

[MessagePackObject(AllowPrivate = true)]
internal sealed class GjallarSurfaceDocument
{
    [Key(0)] public string Schema { get; set; } = "gamecult.eve.surface.v1";
    [Key(1)] public string Id { get; set; } = string.Empty;
    [Key(2)] public string Title { get; set; } = string.Empty;
    [Key(3)] public GjallarSurfaceNode Root { get; set; } = new();
    [Key(4)] public object[] Assets { get; set; } = [];
}

[MessagePackObject(AllowPrivate = true)]
internal sealed class GjallarSurfaceNode
{
    [Key(0)] public string Id { get; set; } = string.Empty;
    [Key(1)] public string Kind { get; set; } = string.Empty;
    [Key(2)] public GjallarSurfaceProps Props { get; set; } = new();
    [Key(3)] public GjallarSurfaceNode[] Children { get; set; } = [];
}

[MessagePackObject(AllowPrivate = true)]
internal sealed class GjallarSurfaceProps
{
    [Key(0)] public string Title { get; set; } = string.Empty;
    [Key(1)] public string Text { get; set; } = string.Empty;
    [Key(2)] public string Label { get; set; } = string.Empty;
    [Key(3)] public string Value { get; set; } = string.Empty;
    [Key(4)] public string Status { get; set; } = string.Empty;
    [Key(5)] public string Summary { get; set; } = string.Empty;
    [Key(6)] public string Detail { get; set; } = string.Empty;
}

[MessagePackObject(AllowPrivate = true)]
internal sealed class GjallarAdvertisedSchema
{
    [Key(0)] public string SchemaId { get; set; } = string.Empty;
    [Key(1)] public string Owner { get; set; } = string.Empty;
    [Key(2)] public string Description { get; set; } = string.Empty;
}

[MessagePackObject(AllowPrivate = true)]
internal sealed class GjallarAdvertisedWitness
{
    [Key(0)] public string WitnessId { get; set; } = string.Empty;
    [Key(1)] public string Path { get; set; } = string.Empty;
    [Key(2)] public string FreshnessState { get; set; } = string.Empty;
    [Key(3)] public string UpdatedAt { get; set; } = string.Empty;
    [Key(4)] public string[] Schemas { get; set; } = [];
}

[MessagePackObject(AllowPrivate = true)]
internal sealed class GjallarAdvertisedSurface
{
    [Key(0)] public string SurfaceId { get; set; } = string.Empty;
    [Key(1)] public string Address { get; set; } = string.Empty;
    [Key(2)] public string Kind { get; set; } = string.Empty;
    [Key(3)] public string Status { get; set; } = string.Empty;
    [Key(4)] public string InputTransport { get; set; } = string.Empty;
}
