using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Maps;
using Campus.Kernel.Instances;
using Godot;
using N = System.Numerics;
using KernelServices = Campus.Kernel.Registry.Kernel;

namespace Campus.Kernel.Boot;

/// <summary>Mounts the authored T0 cloud cards once; application flow changes only their visible counts.</summary>
[KernelService(typeof(IAppTitlePresentation))]
[KernelNode("title.clouds")]
public sealed partial class TitleCloudNode : Node, IAppTitlePresentation, IMapAware
{
    private readonly List<IInstancePool> _layers = [];
    private bool _visible;

    public void SetVisible(bool visible)
    {
        _visible = visible;
        foreach (var layer in _layers) layer.VisibleCount = visible ? layer.Capacity : 0;
    }

    public void OnMapLoaded(IMapContext context)
    {
        Release();
        if (context.Descriptor.Title is not { } title) return;
        var layout = JsonSerializer.Deserialize<TitleCloudLayout>(context.SourceInputs.Require("title-clouds.json").Span,
            ContractJson.Options) ?? throw new InvalidDataException("Title cloud layout is empty.");
        if (layout.Format != 1 || layout.Map != context.Descriptor.Id || layout.Name != title.CloudLayout)
            throw new InvalidDataException("Title cloud layout does not match the mounted map title.");
        var host = KernelServices.Get<IInstancePoolHost>();
        var prototype = CreatePrototype();
        foreach (var layer in layout.Layers.OrderBy(item => item.Layer))
        {
            if (layer.Cards.Count == 0) continue;
            var records = new float[layer.Cards.Count * InstanceStreamLayout.FloatsPerInstance];
            var min = new N.Vector3(float.MaxValue);
            var max = new N.Vector3(float.MinValue);
            for (var i = 0; i < layer.Cards.Count; i++)
            {
                var card = layer.Cards[i];
                var right = card.AxisX * card.Size.X;
                var up = N.Vector3.UnitY * card.Size.Y;
                var normal = card.AxisZ;
                var at = new N.Matrix4x4(right.X, right.Y, right.Z, 0,
                    up.X, up.Y, up.Z, 0, normal.X, normal.Y, normal.Z, 0,
                    card.P.X, card.P.Y, card.P.Z, 1);
                InstanceStreamLayout.Write(records.AsSpan(i * InstanceStreamLayout.FloatsPerInstance,
                    InstanceStreamLayout.FloatsPerInstance), at, card.Custom);
                foreach (var corner in new[] { card.P - right * .5f - up * .5f, card.P + right * .5f - up * .5f,
                    card.P - right * .5f + up * .5f, card.P + right * .5f + up * .5f })
                {
                    min = N.Vector3.Min(min, corner); max = N.Vector3.Max(max, corner);
                }
            }
            var bounds = new Aabb(new Vector3(min.X, min.Y, min.Z), new Vector3(max.X - min.X, max.Y - min.Y, max.Z - min.Z));
            var pool = host.Mount(new GeometryInstancePoolSpec($"title.clouds.{layer.Layer}",
                prototype, layer.Cards.Count, bounds, 1, GeometryInstance3D.ShadowCastingSetting.Off)
                { Map = context.Descriptor.Id });
            _layers.Add(pool);
            pool.WriteBuffer(records);
            pool.VisibleCount = _visible ? pool.Capacity : 0;
        }
    }

    private static GeometryPrototype CreatePrototype()
    {
        // VF-A requires a valid palette pair; the cloud shader reads its own existing sky palette colors.
        var surface = new GeometryBundle(new(new ToonSpec(new("snow.lit"), new("snow.shade")) { Line = LineClass.None }),
            [-.5f, .5f, 0, .5f, .5f, 0, .5f, -.5f, 0, -.5f, -.5f, 0],
            [0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 1], [0, 2, 1, 0, 3, 2],
            Uv: [0, 0, 1, 0, 1, 1, 0, 1])
            { Shader = ShaderId.CineCloud, Flags = [8, 8, 8, 8] };
        return new("title.cloud-card", [GeometryBuffer.Freeze(surface)],
            new(new N.Vector3(-.5f, -.5f, 0), new N.Vector3(.5f, .5f, 0)));
    }

    public void OnMapUnloading(MapId from) => Release();
    public override void _ExitTree() => Release();
    private void Release()
    {
        foreach (var layer in _layers) layer.Free();
        _layers.Clear();
    }
}
