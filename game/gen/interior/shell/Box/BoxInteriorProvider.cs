using System.Globalization;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Gen.Geometry;
using Campus.Gen.Parts.Core;

namespace Campus.Gen.Interior.Shell.Box;

/// <summary>Lightweight world-shaped interior shell; complete rooms and traffic cores belong to GenericInteriorProvider.</summary>
public class BoxInteriorProvider : IInteriorProvider, IInteriorCatalogConfigurable
{
    private InteriorCatalogConfiguration? _configuration;
    public BakeInputDependencies BakeInputs => BakeInputDependencies.World;
    public BakeInputDependencies CollisionInputs => BakeInputDependencies.World;
    public IReadOnlyList<FeatureId> FeatureIds => _configuration?.Features ?? [];
    public int Priority => -1000;
    public bool Walkable => true;

    public void Configure(InteriorCatalogConfiguration configuration)
    {
        if (_configuration is not null) throw new InvalidOperationException("An interior provider is configured once per map.");
        if (configuration.BuildingType is not null || configuration.Seals.Count != 0)
            throw new ArgumentException("The lightweight shell takes its dimensions from the world; configured rooms and seals require a plan provider.");
        if (configuration.Features.Count == 0 || configuration.OpenFloors.Count == 0)
            throw new ArgumentException("Interior catalog requires features and open floors.");
        _configuration=configuration;
    }
    private InteriorCatalogConfiguration Require(FeatureId feature)
    {
        var config=_configuration ?? throw new InvalidOperationException("Interior provider is not configured.");
        if (!config.Features.Contains(feature)) throw new ArgumentException("Feature is outside this provider.");
        return config;
    }
    private IReadOnlyList<InteriorBuildingPart> Parts(BuildContext context,FeatureId feature) =>
        context.Memo((this,feature),()=>InteriorBuildingSource.Read(context.World,feature,Require(feature).Entrances.GetValueOrDefault(feature) ?? [],Require(feature).OpenParts));

    private static IReadOnlyList<int> Floors(InteriorCatalogConfiguration config, FeatureId feature, string source) =>
        config.OpenParts.SingleOrDefault(part=>part.FeatureId==feature && part.SourceId==source)?.Floors ?? [];

    public InteriorLayout Layout(BuildContext context,FeatureId feature)
    {
        var config=Require(feature); var parts=Parts(context,feature);
        var levels=parts.SelectMany(p=>p.Storeys).ToArray();
        foreach(var scope in config.OpenParts.Where(scope=>scope.FeatureId==feature))
            if(scope.Floors.Any(f=>!parts.Any(part=>part.SourceId==scope.SourceId && part.Storeys.Any(s=>s.Index==f))))
                throw new InvalidDataException($"Open floor is outside its world part: {feature.Value}/{scope.SourceId}.");
        var zones=new List<ZoneVolume>();
        foreach(var part in parts) foreach(var floor in Floors(config,feature,part.SourceId))
            zones.Add(new(floor,zones.Count,ZoneKind.Lobby,part.Footprint.Outer,new("interior.main"),
                StableHash.Hash32(context.WorldSeed,feature.Value,part.Key,floor.ToString(CultureInfo.InvariantCulture)),true));
        var entrances=config.Entrances.GetValueOrDefault(feature) ?? [];
        var points=levels.SelectMany(s=>s.Footprint).ToArray();
        return new(feature,levels,zones,[],entrances,[],config.FloorsFor(feature),[],
            new(new(points.Min(p=>p.X),levels.Min(s=>s.FloorY)-0.25f,points.Min(p=>p.Y)),
                new(points.Max(p=>p.X),levels.Max(s=>s.FloorY+s.Height),points.Max(p=>p.Y))),
            StableHash.Hash32(context.WorldSeed,feature.Value,"interior.world-shell"));
    }

    private void Compose(BuildContext context,FeatureId feature,int storey,InteriorGeometry sink)
    {
        var config=Require(feature);
        var material=new ToonSpec(new("wall.lit"),new("wall.shade"));
        foreach(var part in Parts(context,feature))
        {
            if(!Floors(config,feature,part.SourceId).Contains(storey)) continue;
            var floor=part.Storeys.FirstOrDefault(s=>s.Index==storey);
            if(floor is null) continue;
            sink.Floor(part.Footprint,floor.FloorY,0.25f,material,FootMat.Concrete);
            if(storey==Floors(config,feature,part.SourceId).Max()) sink.Plate(part.Footprint,floor.FloorY+floor.Height,0.25f,material);
            foreach(var ring in new[]{part.Footprint.Outer}.Concat(part.Footprint.Holes))
                for(var i=0;i<ring.Count;i++)
                {
                    var a=ring[i]; var b=ring[(i+1)%ring.Count]; var delta=b-a; float length=delta.Length();
                    var direction=delta/length;
                    var openings=new List<(float Start,float End,float Top)>();
                    foreach(var entrance in config.Entrances.GetValueOrDefault(feature) ?? [])
                    {
                        var point=new Vector2(entrance.Center.X,entrance.Center.Z);
                        float along=Vector2.Dot(point-a,direction);
                        if(along<0 || along>length || Vector2.Distance(point,a+direction*along)>0.5f ||
                            entrance.Center.Y<floor.FloorY || entrance.Center.Y>=floor.FloorY+floor.Height) continue;
                        float top=entrance.HeadY==0?entrance.Center.Y+WalkRules.DefaultDoorHeight:entrance.HeadY;
                        openings.Add((Math.Max(0,along-entrance.ClearWidth*0.5f),Math.Min(length,along+entrance.ClearWidth*0.5f),top));
                    }
                    float cursor=0;
                    foreach(var opening in openings.OrderBy(o=>o.Start))
                    {
                        if(opening.Start<cursor) throw new InvalidDataException($"Interior entrance openings overlap: {feature.Value}.");
                        Wall(cursor,opening.Start,floor.FloorY,floor.FloorY+floor.Height);
                        Wall(opening.Start,opening.End,opening.Top,floor.FloorY+floor.Height);
                        cursor=opening.End;
                    }
                    Wall(cursor,length,floor.FloorY,floor.FloorY+floor.Height);
                    void Wall(float from,float to,float bottom,float top)
                    {
                        if(to-from<=PlanGeometry.Tolerance || top-bottom<=PlanGeometry.Tolerance) return;
                        sink.Wall(a+direction*from,a+direction*to,bottom,top,0.24f,material);
                    }
                }
        }
    }
    public ContentOutput BuildStorey(BuildContext context,FeatureId feature,int storey,InteriorDetail detail)
    {
        _=detail switch { InteriorDetail.Proxy or InteriorDetail.Shell or InteriorDetail.Full=>0,_=>throw new ArgumentOutOfRangeException(nameof(detail)) };
        var sink=new InteriorGeometry(context.Catalog); Compose(context,feature,storey,sink); return sink.Finish();
    }
    public IReadOnlyList<Collider> CollisionStorey(BuildContext context,FeatureId feature,int storey)
    {
        var sink=new InteriorGeometry(context.Catalog){Draw=false}; Compose(context,feature,storey,sink); return sink.Colliders;
    }
    public NavFloorGraph Nav(BuildContext context,FeatureId feature,int storey)
    {
        var config=Require(feature); if(!config.FloorsFor(feature).Contains(storey)) return NavFloorGraph.Empty(feature,storey);
        // A lightweight shell declares only its actual entry landings. It does not invent stairs or room connectivity.
        var nodes=(config.Entrances.GetValueOrDefault(feature) ?? []).Where(e=>e.Open).Where(e=>Parts(context,feature)
            .Any(p=>Floors(config,feature,p.SourceId).Contains(storey) && p.Storeys.Any(s=>s.Index==storey && e.Inside.Y>=s.FloorY && e.Inside.Y<s.FloorY+s.Height)))
            .Select(e=>new NavNode(new(StableHash.Hash32(context.WorldSeed,feature.Value,e.Id)),e.Inside,new(feature,checked((short)storey)),e.Id)).ToArray();
        return new(feature,checked((short)storey),nodes,[]);
    }
}
