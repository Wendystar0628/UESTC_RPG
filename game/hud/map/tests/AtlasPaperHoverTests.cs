using Campus.Contracts;
using Campus.Contracts.Atlas;
using Campus.Hud;
using Campus.Hud.Map;
using Campus.Hud.Motion;
using Campus.Hud.Style;
using Campus.Hud.Widgets;
using Campus.Kernel.Boot;
using Campus.Kernel.Settings;
using Campus.Kernel.World;
using Campus.Style.Materials;
using Campus.Style.Palette;
using GdUnit4;
using static GdUnit4.Assertions;
using Godot;

namespace Campus.Tests;

// GAME-254：光标停在标记上、视图被平移或拖动后，悬停与悬停预取仍停在原标记上。
// 预取按 AtlasController 的接法接上：地图纸的悬停给 DestinationPrefetch，每帧先推进地图纸、再推进预取。
[TestSuite]
public sealed class AtlasPaperHoverTests
{
    private static readonly MapEntry A = Entry("test-a", 0), B = Entry("test-b", 400), C = Entry("test-c", -400);

    // 界面建的主题、字体等原生资源要等包装终结才归还；整组跑完在这里收齐（DroppedNatives）。
    [After]
    public void ReleaseDroppedNatives() => DroppedNatives.Release();

    [TestCase]
    [TestCategory(TestReason.Regression)]
    [RequireGodotRuntime]
    public void PanningTheViewRehoversUnderTheCursorAndMovesThePrefetch()
    {
        using var fixture = new Fixture();
        var paper = fixture.Paper;
        var cursor = fixture.HoverOn(A);
        fixture.Frame(DestinationPrefetch.MarkerHoverSeconds * 0.5f);
        AssertThat(fixture.Hover?.Id == A.Id).OverrideFailureMessage("The fixture cursor does not hover marker A.").IsTrue();

        // 触发条件：键盘平移一帧后 A 已不在光标下，光标没动。
        fixture.Frame(DestinationPrefetch.MarkerHoverSeconds * 0.25f, Vector2.Right);
        AssertThat(paper.Cursor == cursor && paper.Markers.Pick(cursor, paper.View)?.Id != A.Id)
            .OverrideFailureMessage("Panning one frame did not move marker A away from the resting cursor.").IsTrue();
        AssertThat(fixture.Hover is null).OverrideFailureMessage("Panning A away from the cursor left the hover on A.").IsTrue();

        // 视图移到另一枚标记上：光标下的新标记成为悬停，满时长后预取它；A 从未被预取。
        paper.PanTo(C.At);
        fixture.Frame(0);
        AssertThat(paper.Markers.Pick(cursor, paper.View)?.Id == C.Id && fixture.Hover?.Id == C.Id)
            .OverrideFailureMessage("Moving the view onto marker C did not hover C under the resting cursor.").IsTrue();
        fixture.Frame(DestinationPrefetch.MarkerHoverSeconds);
        var issued = fixture.Service.Issued;
        AssertThat(issued.Count == 1 && issued[0].Id == C.Id && !issued[0].Released)
            .OverrideFailureMessage("The hover prefetch did not follow the view onto C, or still requested A.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Regression)]
    [RequireGodotRuntime]
    public void DraggingClearsTheHoverAndReturnsThePrefetchToTheCard()
    {
        using var fixture = new Fixture();
        var paper = fixture.Paper;
        fixture.Prefetch.ShowCard(B.Id, TravelStyle.Teleport);
        var cursor = fixture.HoverOn(A);
        fixture.Frame(DestinationPrefetch.MarkerHoverSeconds);
        var issued = fixture.Service.Issued;
        AssertThat(fixture.Hover?.Id == A.Id && issued[^1].Id == A.Id && !issued[^1].Released)
            .OverrideFailureMessage("The fixture hover on A did not take over the card's prefetch.").IsTrue();

        // 触发条件：按下左键并移动，地图纸开始拖动。
        var to = cursor + Vector2.Right * paper.Markers.IconSize * 2;
        paper._GuiInput(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true, Position = cursor });
        paper._GuiInput(new InputEventMouseMotion { Position = to });
        fixture.Frame(DestinationPrefetch.MarkerHoverSeconds);
        AssertThat(paper.View.ToLocal(A.At, paper.CanvasSize) != fixture.Rest)
            .OverrideFailureMessage("The drag did not move the map.").IsTrue();
        AssertThat(fixture.Hover is null).OverrideFailureMessage("Starting a drag did not clear the hover.").IsTrue();
        var card = issued[^1];
        AssertThat(card.Id == B.Id && !card.Released && issued.Count(lease => !lease.Released) == 1)
            .OverrideFailureMessage("Starting a drag did not release A and return to the card's destination.").IsTrue();

        // 松开后 A 跟着光标拖过来，仍在光标下：下一帧重新悬停。
        paper._GuiInput(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = false, Position = to });
        fixture.Frame(0);
        AssertThat(paper.Markers.Pick(to, paper.View)?.Id == A.Id && fixture.Hover?.Id == A.Id)
            .OverrideFailureMessage("After the drag ended the marker under the cursor was not hovered again.").IsTrue();
    }

    private static MapEntry Entry(string id, float x) =>
        new(new AtlasMarker(new MarkerId(id), MarkerKind.Landmark, new LocalizedName(id, id), new System.Numerics.Vector2(x, 0), "", 0, 0, null));

    /// <summary>地图纸挂在根视口下，标记 A 在视图正中；悬停事件喂给预取，与 AtlasController 同序推进。</summary>
    private sealed class Fixture : IDisposable
    {
        private readonly UiThemeClock _clock;
        private readonly Control _host;

        public Fixture()
        {
            var settings = new HudSettings(new SettingsStore());
            var motion = new MotionPolicy(settings);
            var resources = new RequiredResources();
            _clock = new UiThemeClock(new WorldStateService(new TestWeatherProcess()), settings, motion, resources);
            var tokens = UiTokens.Load(resources);
            var colors = UiColors.Load(resources, new PaletteRegistrySource(resources).Catalog);
            var kit = new UiKit(tokens, colors, new UiMaterials(tokens, colors, new ShaderMaterialSource(resources)),
                new UiThemeBuilder(tokens, colors, resources), motion, _clock);
            _host = new Control { Name = "AtlasPaperHoverHost", Theme = kit.Theme.Theme, MouseFilter = Control.MouseFilterEnum.Ignore };
            ((SceneTree)Engine.GetMainLoop()).Root.AddChild(_host);
            Paper = new AtlasPaper(new MapHudContext(MapStyle.Load(kit), kit));
            _host.AddChild(Paper);
            Paper.Size = new Vector2(800, 600);
            Paper.SetBounds(new Rect2(-2000, -2000, 4000, 4000), 0.5f, 4);
            Paper.SetView(A.At, 1);
            Paper.Markers.SetEntries([A, B, C]);
            Prefetch = new DestinationPrefetch(Service.Request);
            Paper.HoverChanged += e => { Hover = e; Prefetch.Hover(e?.Id, DestinationPrefetch.MarkerHoverSeconds); };
            Frame(0);
            Rest = Paper.View.ToLocal(A.At, Paper.CanvasSize);
        }

        public AtlasPaper Paper { get; }
        public DestinationPrefetch Prefetch { get; }
        public Requests Service { get; } = new();
        public MapEntry? Hover { get; private set; }
        /// <summary>A 在视图未动时的屏幕位置。</summary>
        public Vector2 Rest { get; }

        public Vector2 HoverOn(MapEntry e)
        {
            var at = Paper.View.ToLocal(e.At, Paper.CanvasSize);
            Paper._GuiInput(new InputEventMouseMotion { Position = at });
            return at;
        }

        public void Frame(float dt, Vector2 pan = default)
        {
            Paper.Tick(dt, pan);
            Prefetch.Tick(dt);
        }

        public void Dispose()
        {
            Prefetch.Dispose();
            _host.Free();
            _clock.Free();
        }
    }

    private sealed class Requests
    {
        public List<TestAtlasPrefetch> Issued { get; } = [];

        public IAtlasPrefetch Request(MarkerId id, TravelStyle style)
        {
            var lease = new TestAtlasPrefetch(id);
            Issued.Add(lease);
            return lease;
        }
    }
}
