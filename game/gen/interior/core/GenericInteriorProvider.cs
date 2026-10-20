using System.Globalization;
using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Gen.Geometry;
using Campus.Gen.Parts.Core;

namespace Campus.Gen.Interior;

/// <summary>World-shaped convex wings, their shared traffic cores and one common plan/geometry/collision path.</summary>
public sealed class GenericInteriorProvider : IInteriorProvider, IInteriorCatalogConfigurable
{
    private InteriorCatalogConfiguration? _configuration;
    private JsonElement _type;
    private DormitoryPlanOptions _options=new();
    public BakeInputDependencies BakeInputs=>BakeInputDependencies.World;
    public BakeInputDependencies CollisionInputs=>BakeInputDependencies.World;
    public IReadOnlyList<FeatureId> FeatureIds=>_configuration?.Features ?? [];
    public int Priority=>0;
    public bool Walkable=>true;
    private sealed record Wing(int Index,string Id,PolygonRings Footprint,IReadOnlyList<StoreyInfo> Storeys, IReadOnlyList<int> OpenFloors);
    private sealed record Junction(int A,int EdgeA,int B,int EdgeB,Vector2 Start,Vector2 End);
    private sealed record Building(InteriorLayout Layout,IReadOnlyList<InteriorPlan> Plans);
    public void Configure(InteriorCatalogConfiguration configuration)
    {
        if(_configuration is not null) throw new InvalidOperationException("An interior provider is configured once per map.");
        var type=configuration.BuildingType ?? throw new ArgumentException("A generated interior requires buildingType.");
        if(type.GetProperty("profile").GetString()!="double-loaded") throw new ArgumentException("Unknown building profile.");
        foreach(var property in type.EnumerateObject())
            if(property.Name is not ("profile" or "rooms" or "options"))
                throw new ArgumentException($"Unsupported building configuration '{property.Name}'; wing coordinates and building dimensions belong to the world.");
        _options=type.TryGetProperty("options",out var options)
            ? JsonSerializer.Deserialize<DormitoryPlanOptions>(options.GetRawText(),ContractJson.Options) ?? throw new JsonException("Null plan options.") : new();
        _type=type.Clone();_configuration=configuration;
    }
    private Building Of(BuildContext context,FeatureId feature)=>context.Memo((this,feature),()=>Generate(context,feature));
    private Building Generate(BuildContext context,FeatureId feature)
    {
        var config=_configuration ?? throw new InvalidOperationException("Interior provider is not configured.");
        if(!config.Features.Contains(feature)) throw new ArgumentException("Feature is outside this provider.");
        var entrances=config.Entrances.GetValueOrDefault(feature) ?? [];
        var source=InteriorBuildingSource.Read(context.World,feature,entrances,config.OpenParts);
        var wings=BuildWings(source,config,feature);
        var joins=FindJunctions(wings);
        var assignments=AssignEntrances(wings,joins,entrances);
        AddJunctionEntrances(wings,joins,assignments);
        var plans=BuildPlans(context,feature,config,wings,joins,assignments);
        var portals=ConnectPortals(wings,joins,assignments,plans);
        var levels=source.SelectMany(part=>part.Storeys).ToArray();var points=levels.SelectMany(s=>s.Footprint).ToArray();
        var layout=new InteriorLayout(feature,levels,plans.SelectMany(p=>p.Zones).ToArray(),portals,entrances,[],config.FloorsFor(feature),[],
            new(new(points.Min(p=>p.X),levels.Min(s=>s.FloorY),points.Min(p=>p.Y)),new(points.Max(p=>p.X),levels.Max(s=>s.FloorY+s.Height),points.Max(p=>p.Y))),
            StableHash.Hash32(context.WorldSeed,feature.Value,string.Join("/",plans.Select(p=>p.PlanHash.ToString(CultureInfo.InvariantCulture)))))
            {Doors=plans.SelectMany(p=>p.Doors).ToArray()};
        return new(layout,plans);
    }
    private static List<Wing> BuildWings(IReadOnlyList<InteriorBuildingPart> source,
        InteriorCatalogConfiguration config,FeatureId feature)
    {
        var wings=new List<Wing>();
        foreach(var part in source.Where(part=>config.OpenParts.Any(scope=>scope.FeatureId==feature && scope.SourceId==part.SourceId && InteriorOpenPart.IsIndoor(scope.Kind))))
            foreach(var polygon in InteriorWingDecomposition.Split(part.Footprint,feature.Value+"/"+part.Key))
                wings.Add(new(wings.Count,"wing-"+wings.Count.ToString(CultureInfo.InvariantCulture),polygon,
                    part.Storeys.Select(s=>s with { Footprint=polygon.Outer }).ToArray(),
                    config.OpenParts.Single(scope=>scope.FeatureId==feature && scope.SourceId==part.SourceId && InteriorOpenPart.IsIndoor(scope.Kind)).Floors));
        return wings;
    }
    private static List<Junction> FindJunctions(IReadOnlyList<Wing> wings)
    {
        var joins=new List<Junction>();
        for(var a=0;a<wings.Count;a++) for(var b=a+1;b<wings.Count;b++)
            for(var ea=0;ea<4;ea++) for(var eb=0;eb<4;eb++)
            {
                var x=wings[a].Footprint.Outer;var y=wings[b].Footprint.Outer;
                if(Vector2.Distance(x[ea],y[(eb+1)%4])<=QuantizedPlan.Tolerance && Vector2.Distance(x[(ea+1)%4],y[eb])<=QuantizedPlan.Tolerance)
                    joins.Add(new(a,ea,b,eb,x[ea],x[(ea+1)%4]));
            }
        return joins;
    }
    private static Dictionary<int,List<EntranceDecl>> AssignEntrances(IReadOnlyList<Wing> wings,
        IReadOnlyList<Junction> joins,IReadOnlyList<EntranceDecl> entrances)
    {
        var assignments=wings.ToDictionary(w=>w.Index,_=>new List<EntranceDecl>());
        foreach(var entrance in entrances)
        {
            var point=new Vector2(entrance.Center.X,entrance.Center.Z);
            var candidates=(from wing in wings from edge in Enumerable.Range(0,4)
                where !joins.Any(j=>(j.A==wing.Index&&j.EdgeA==edge)||(j.B==wing.Index&&j.EdgeB==edge))
                let a=wing.Footprint.Outer[edge] let b=wing.Footprint.Outer[(edge+1)%4]
                let distance=PlanLines.SegmentDistanceSquared(point,a,b)
                orderby distance,wing.Index,edge select(wing,edge,a,b,distance)).ToArray();
            if(candidates.Length==0 || candidates[0].distance>0.25f) throw new InvalidDataException($"Entrance '{entrance.Id}' has no owning exterior wing edge.");
            var owner=candidates[0];var d=owner.b-owner.a;float t=Vector2.Dot(point-owner.a,d)/d.LengthSquared();
            var on=owner.a+d*Math.Clamp(t,0,1);
            assignments[owner.wing.Index].Add(entrance with { Center=new(on.X,entrance.Center.Y,on.Y) });
        }
        return assignments;
    }
    private void AddJunctionEntrances(IReadOnlyList<Wing> wings,IReadOnlyList<Junction> joins,
        Dictionary<int,List<EntranceDecl>> assignments)
    {
        foreach(var join in joins)
        {
            foreach(var wing in new[]{wings[join.A],wings[join.B]})
            {
                int edge=wing.Index==join.A?join.EdgeA:join.EdgeB;
                var a=wing.Footprint.Outer[edge];var b=wing.Footprint.Outer[(edge+1)%4];var direction=QuantizedPlan.Unit(b-a);
                var inward=new Vector2(-direction.Y,direction.X);var centre=(a+b)*0.5f;
                foreach(var floor in wings[join.A].OpenFloors.Intersect(wings[join.B].OpenFloors).Order())
                {
                    var level=wing.Storeys.Single(s=>s.Index==floor);Vector3 P(Vector2 p)=>new(p.X,level.FloorY,p.Y);
                    assignments[wing.Index].Add(new($"join-{join.A}-{join.B}-{floor}",P(centre-inward*0.5f),P(centre+inward*0.5f),_options.DoorClearWidth,true)
                        {Center=P(centre),YawDeg=MathF.Atan2(-inward.X,-inward.Y)*180/MathF.PI});
                }
            }
        }
    }
    private List<InteriorPlan> BuildPlans(BuildContext context,FeatureId feature,
        InteriorCatalogConfiguration config,IReadOnlyList<Wing> wings,IReadOnlyList<Junction> joins,
        Dictionary<int,List<EntranceDecl>> assignments)
    {
        var plans=new List<InteriorPlan>();var remainingSeals=config.Seals.Where(s=>s.Building==feature).ToList();
        foreach(var wing in wings)
        {
            var grid=FacadeGridGenerator.Generate(wing.Footprint,_options.Facade);var frame=InteriorWingFrame.From(grid.Footprint);
            var rooms=new Dictionary<(int Floor,int Bay),InteriorRoomUse>();
            if(_type.TryGetProperty("rooms",out var selections)) foreach(var selection in selections.EnumerateArray())
            {
                if(selection.GetProperty("wing").GetInt32()!=wing.Index) continue;
                int edge=selection.GetProperty("side").GetInt32()==0?frame.Edge:(frame.Edge+2)%4;
                var bay=grid.Bays.Where(b=>b.Edge==edge).OrderBy(b=>frame.U(b.Center)).ElementAt(selection.GetProperty("bay").GetInt32());
                int floor=selection.GetProperty("storey").GetInt32();
                rooms.Add((floor,bay.Index),new(floor,bay.Index,selection.GetProperty("type").GetString() ?? throw new JsonException("Missing room type."),selection.GetProperty("open").GetBoolean()));
            }
            foreach(var entrance in assignments[wing.Index])
            {
                var point=new Vector2(entrance.Center.X,entrance.Center.Z);
                var bay=grid.Bays.Where(b=>QuantizedPlan.OnSegment(point,b.A,b.B)).OrderBy(b=>Vector2.DistanceSquared(b.Center,point)).FirstOrDefault();
                if(bay is null) throw new InvalidDataException($"Entrance '{entrance.Id}' is outside its wing facade grid.");
                if(bay.Edge!=frame.Edge && bay.Edge!=(frame.Edge+2)%4) continue;
                var row=grid.Bays.Where(b=>b.Edge==bay.Edge).OrderBy(b=>frame.U(b.Center)).ToArray();
                var columns=frame.RoomColumns(grid,_options);int column=Array.IndexOf(row,bay);
                if(column<columns.First || column>=columns.End) continue;
                int floor=wing.Storeys.OrderBy(s=>MathF.Abs(s.FloorY-entrance.Center.Y)).First().Index;
                rooms[(floor,bay.Index)]=new(floor,bay.Index,"public.entrance",true,ZoneKind.Lobby);
            }
            var seals=remainingSeals.Where(s=>PlanRings.Contains(wing.Footprint,s.A)&&PlanRings.Contains(wing.Footprint,s.B)).ToArray();
            foreach(var seal in seals) remainingSeals.Remove(seal);
            plans.Add(InteriorPlanGenerator.Generate(new(feature,wing.Id,context.WorldSeed,wing.Footprint,
                wing.Storeys.Where(s=>wing.OpenFloors.Contains(s.Index)).ToArray(),assignments[wing.Index],rooms.Values.ToArray(),seals)
                {Options=_options,ZoneIdBase=checked(wing.Index*65536),JoinedEdges=joins.Where(j=>j.A==wing.Index||j.B==wing.Index)
                    .Select(j=>j.A==wing.Index?j.EdgeA:j.EdgeB).ToArray()}));
        }
        if(remainingSeals.Count!=0) throw new InvalidDataException("A catalog seal has no owning wing.");
        return plans;
    }
    private static List<PortalDecl> ConnectPortals(IReadOnlyList<Wing> wings,IReadOnlyList<Junction> joins,
        Dictionary<int,List<EntranceDecl>> assignments,IReadOnlyList<InteriorPlan> plans)
    {
        var portals=plans.SelectMany(p=>p.Portals).Where(p=>p.ZoneA!=-1 || !joins.Any(j=>PlanLines.SegmentDistanceSquared(new((p.A.X+p.B.X)*0.5f,(p.A.Z+p.B.Z)*0.5f),j.Start,j.End)<=0.000001f)).ToList();
        foreach(var join in joins) foreach(var floor in wings[join.A].OpenFloors.Intersect(wings[join.B].OpenFloors).Order())
        {
            string key=$"join-{join.A}-{join.B}-{floor}";
            var first=plans[join.A];var second=plans[join.B];var entry=assignments[join.A].Single(e=>e.Id==key);
            var a=first.Portals.Single(p=>p.Storey==floor&&p.ZoneA==-1&&p.Kind==PortalKind.Door&&Vector3.Distance((p.A+p.B)*0.5f,entry.Center)<=0.01f);
            var other=assignments[join.B].Single(e=>e.Id==key);
            var b=second.Portals.Single(p=>p.Storey==floor&&p.ZoneA==-1&&p.Kind==PortalKind.Door&&Vector3.Distance((p.A+p.B)*0.5f,other.Center)<=0.01f);
            portals.Add(a with {ZoneA=a.ZoneB,ZoneB=b.ZoneB});
        }
        return portals;
    }
    public InteriorLayout Layout(BuildContext context,FeatureId feature)=>Of(context,feature).Layout;
    public IReadOnlyList<Collider> CollisionStorey(BuildContext context,FeatureId feature,int storey)=>
        Of(context,feature).Plans.SelectMany(plan=>InteriorPlanCollision.BuildStorey(plan,storey)).ToArray();
    public ContentOutput BuildStorey(BuildContext context,FeatureId feature,int storey,InteriorDetail detail)
    {
        _=detail switch { InteriorDetail.Proxy or InteriorDetail.Shell or InteriorDetail.Full=>0,_=>throw new ArgumentOutOfRangeException(nameof(detail)) };
        var building=Of(context,feature);if(!building.Layout.OpenStoreys.Contains(storey)) return ContentOutput.Empty;
        var sink=new InteriorGeometry(context.Catalog);var wall=new ToonSpec(new("wall.lit"),new("wall.shade"));
        foreach(var plan in building.Plans.Where(plan=>plan.Storeys.Any(level=>level.Index==storey)))
        {
            foreach(var slab in plan.Slabs.Where(s=>s.Storey==storey)) sink.Floor(new(slab.Ring),slab.TopY,slab.Thickness,wall,FootMat.Concrete);
            foreach(var span in plan.Walls.Where(w=>w.Storey==storey)) sink.Wall(span.A,span.B,span.BottomY,span.TopY,span.Thickness,
                span.Role==InteriorWallRole.WindowGlass?PartMaterials.Glazing():wall);
            foreach(var stair in plan.Stairs.Where(s=>s.LowerStorey==storey))
            {sink.Shapes.Steps(stair.UpFlight,wall);sink.Shapes.Steps(stair.ReturnFlight,wall);sink.Floor(new(stair.MidLanding.Ring),stair.MidLanding.TopY,stair.MidLanding.Thickness,wall,FootMat.Concrete);}
            var level=plan.Storeys.Single(s=>s.Index==storey);
            sink.Parts.Plate(plan.Facade.Footprint,level.FloorY+level.Height,_options.SlabThickness,wall);
        }
        return sink.Finish();
    }
    public NavFloorGraph Nav(BuildContext context,FeatureId feature,int storey)
    {
        var building=Of(context,feature);var nodes=new List<NavNode>();var edges=new List<NavEdge>();var zones=new Dictionary<int,NavNode>();
        NavNode Node(string key,int floor,Vector3 at)
        {var node=new NavNode(new(StableHash.Hash32(context.WorldSeed,feature.Value,key)),at,new(feature,checked((short)floor)),key);nodes.Add(node);return node;}
        void Link(NavNode a,NavNode b,NavEdgeKind kind,float width)=>edges.Add(new(a.Id,b.Id,kind,Vector3.Distance(a.P,b.P),width,b.P.Y-a.P.Y,NavFlags.Indoor,"interior.plan"));
        foreach(var zone in building.Layout.Zones.Where(z=>z.Open&&z.Storey==storey).OrderBy(z=>z.Id))
        {var p=PlanPolygons.Centroid(zone.Ring);var level=building.Plans.Single(plan=>plan.Zones.Any(item=>item.Id==zone.Id)).Storeys.Single(level=>level.Index==storey);zones.Add(zone.Id,Node("zone/"+zone.Id,storey,new(p.X,level.FloorY,p.Y)));}
        foreach(var portal in building.Layout.Portals.Where(p=>p.Storey==storey&&p.Kind==PortalKind.Door))
        {
            if(!zones.TryGetValue(portal.ZoneA,out var a)||!zones.TryGetValue(portal.ZoneB,out var b)) continue;
            var point=Node("portal/"+portal.ZoneA+"/"+portal.ZoneB,storey,(portal.A+portal.B)*0.5f);
            Link(a,point,NavEdgeKind.Room,Vector3.Distance(portal.A,portal.B));Link(point,b,NavEdgeKind.Room,Vector3.Distance(portal.A,portal.B));
        }
        foreach(var plan in building.Plans) foreach(var stair in plan.Stairs)
        {
            float width=Vector3.Distance(stair.UpFlight.P0,stair.UpFlight.P1);
            if(stair.LowerStorey==storey)
            {
                var bottom=Node("stair/"+stair.LowerZone+"/bottom",storey,((stair.UpFlight.P0+stair.UpFlight.P1)*0.5f) with {Y=plan.Storeys.Single(s=>s.Index==storey).FloorY});
                var landing=Node("stair/"+stair.LowerZone+"/landing",storey,(stair.UpFlight.P2+stair.UpFlight.P3)*0.5f);
                var returning=Node("stair/"+stair.LowerZone+"/return",storey,((stair.ReturnFlight.P0+stair.ReturnFlight.P1)*0.5f) with {Y=stair.MidLanding.TopY});
                var top=Node("stair/"+stair.UpperZone+"/top",stair.UpperStorey,(stair.ReturnFlight.P2+stair.ReturnFlight.P3)*0.5f);
                Link(zones[stair.LowerZone],bottom,NavEdgeKind.Corridor,width);Link(bottom,landing,NavEdgeKind.Stair,width);Link(landing,returning,NavEdgeKind.Corridor,width);Link(returning,top,NavEdgeKind.Stair,width);
            }
            if(stair.UpperStorey==storey)
            {var top=Node("stair/"+stair.UpperZone+"/top",storey,(stair.ReturnFlight.P2+stair.ReturnFlight.P3)*0.5f);Link(top,zones[stair.UpperZone],NavEdgeKind.Corridor,width);}
        }
        return new(feature,checked((short)storey),nodes,edges);
    }
}
