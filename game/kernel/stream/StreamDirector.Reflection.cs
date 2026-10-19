using Campus.Kernel.Frame;

namespace Campus.Kernel.Stream;

public partial class StreamDirector
{
    private void OnReflectionChanged() => _reflectionDirty = true;
    private void RefreshReflectionWorld(double budgetMilliseconds)
    {
        var scenario = _reflections.Scenario;
        var material = scenario.IsValid ? _reflections.ProxyMaterial : null;
        if (material is not null && (_reflectionDirty || !ReferenceEquals(material, _reflectionMaterial)))
        {
            foreach (var group in _groups.OrderBy(pair => pair.Key).Select(pair => pair.Value))
                foreach (var bundle in group.Bundles)
                    foreach (var draw in bundle.Draws.Where(draw => draw.Reflects)) _materials.TrackReflection(material, draw);
            if (_interiors is not null)
                foreach (var bundle in _interiors.Prepared)
                    foreach (var draw in bundle.Draws.Where(draw => draw.Reflects)) _materials.TrackReflection(material, draw);
        }
        var ready = material is not null && _materials.ReflectionReady(material);
        if (_reflectionDirty || scenario != _reflectionScenario || !ReferenceEquals(material, _reflectionMaterial) || ready != _reflectionReady)
        {
            _reflectionDirty = false; _reflectionScenario = scenario; _reflectionMaterial = material; _reflectionReady = ready;
            _reflectionRefresh.Clear(); _reflectionCursor = 0;
            foreach (var group in _groups.OrderBy(pair => pair.Key).Select(pair => pair.Value))
                foreach (var bundle in group.Bundles) _reflectionRefresh.Enqueue(bundle);
            if (_interiors is not null) foreach (var bundle in _interiors.Prepared) _reflectionRefresh.Enqueue(bundle);
        }
        var start = FrameClock.MonotonicMilliseconds;
        while (_reflectionRefresh.TryPeek(out var bundle) && FrameClock.MonotonicMilliseconds - start < budgetMilliseconds)
        {
            if (bundle.IsDisposed || bundle.IsRetiring || _reflectionCursor >= bundle.Instances.Count)
            {
                _reflectionRefresh.Dequeue(); _reflectionCursor = 0; continue;
            }
            var index = _reflectionCursor++;
            bundle.RefreshReflectionOne(index, scenario, material,
                material is not null && _materials.ReflectionReady(bundle.Draws[index], material));
        }
    }
}
