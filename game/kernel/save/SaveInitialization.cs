using Campus.Contracts.Save;
using Campus.Kernel.Save.Format;

namespace Campus.Kernel.Save;

/// <summary>Owns the scanned repository until the save frame explicitly takes its process lock.</summary>
internal sealed class SaveInitialization : IDisposable
{
    private SaveRepository? _repository;
    internal IReadOnlyList<SlotSnapshot> Slots { get; }
    private SaveInitialization(SaveRepository repository, IReadOnlyList<SlotSnapshot> slots)
    { _repository = repository; Slots = slots; }

    internal static Task<SaveInitialization> Prepare(SaveRuntimeOptions options, SaveSections sections,
        CancellationToken cancellation) => Task.Run(() =>
    {
        cancellation.ThrowIfCancellationRequested();
        var repository = new SaveRepository(options.Directory, options.Enabled, sections);
        try
        {
            var slots = new List<SlotSnapshot>();
            foreach (var slot in SaveSlots.Auto)
            {
                cancellation.ThrowIfCancellationRequested();
                slots.Add(repository.Scan(slot));
            }
            cancellation.ThrowIfCancellationRequested();
            return new SaveInitialization(repository, slots);
        }
        catch { repository.Dispose(); throw; }
    });

    internal SaveRepository TakeRepository() => Interlocked.Exchange(ref _repository, null)
        ?? throw new InvalidOperationException("The prepared save repository was already taken.");

    public void Dispose() => Interlocked.Exchange(ref _repository, null)?.Dispose();
}
