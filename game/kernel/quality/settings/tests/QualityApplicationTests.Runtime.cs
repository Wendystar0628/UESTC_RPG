using Campus.Contracts;
using Campus.Contracts.Quality;
using Campus.Kernel.Quality.Settings;

namespace Campus.Tests;

public sealed partial class QualityApplicationTests
{
    private sealed class FakeRuntime : IQualityRuntime
    {
        public bool Booting { get; set; } = true;
        public bool Transitioning { get; set; }
        public bool QualityVeilActive { get; private set; }
        public bool OtherVeilActive { get; private set; }
        public float RefreshRate => 60;
        public Godot.Vector2I OutputSize => new(1920, 1080);
        public Godot.Vector2I ScreenSize => OutputSize;
        public bool FailDispatch { get; set; }
        public bool FailRollback { get; set; }
        public bool FailReadiness { get; set; }
        public bool Takeover { get; set; }
        public int RevealCount { get; private set; }
        public QualityView? EngineView { get; private set; }
        public List<string> Errors { get; } = [];
        public string? HoldAt { get; set; }
        public TaskCompletionSource<double> Held { get; } = new();
        public int HeldCalls { get; private set; }
        public int DispatchCount { get; private set; }
        // Deliberately ignore cancellation: the service must reject a boundary's late result itself.
        private Task Wait(string boundary)
        {
            if (HoldAt != boundary) return Task.CompletedTask;
            HeldCalls++;
            return Held.Task;
        }
        public Task<double> NextFrame(CancellationToken cancellationToken)
        {
            if (HoldAt != "frame") return Task.FromResult(.1);
            HeldCalls++;
            return Held.Task;
        }
        public IVeilLease Cover(CancellationToken cancellationToken)
        {
            QualityVeilActive = true;
            return new QualityLease(this, Wait("cover"));
        }
        private sealed class QualityLease(FakeRuntime runtime, Task covered) : IVeilLease
        {
            private bool _released;
            public Task Covered => covered;
            public bool IsCurrent => !_released && !runtime.OtherVeilActive;
            public Task Reveal(float seconds)
            {
                if (_released) return Task.CompletedTask;
                runtime.RevealCount++;
                Dispose();
                return runtime.Wait("reveal");
            }
            public void Dispose()
            {
                if (_released) return;
                _released = true;
                runtime.QualityVeilActive = false;
            }
        }
        public Task WaitForDestination(CancellationToken cancellationToken)
        {
            if (Takeover) OtherVeilActive = true;
            return FailReadiness ? Task.FromException(new TimeoutException("Fake destination exceeded 1.2 seconds.")) : Wait("destination");
        }
        public void Dispatch(QualityView view, in QualityChange change)
        {
            DispatchCount++;
            EngineView = view; // Model a consumer that mutates before it fails.
            if (FailDispatch) { FailDispatch = false; throw new InvalidOperationException("Fake apply consumer failed."); }
            if (FailRollback) throw new InvalidOperationException("Fake rollback consumer failed.");
        }
        public void ReportError(string message) => Errors.Add(message);
        public void ReportInfo(string message) { }
    }
}
