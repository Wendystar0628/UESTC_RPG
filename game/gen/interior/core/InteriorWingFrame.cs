using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Geometry;
using NetTopologySuite.Geometries;

namespace Campus.Gen.Interior;

internal sealed class InteriorWingFrame
{
    private readonly PolygonRings _footprint;
    private readonly Vector2 _origin, _along, _inward;
    public float Length { get; }
    public float Depth { get; }
    public int Edge { get; }
    private InteriorWingFrame(PolygonRings footprint,Vector2 origin,Vector2 along,Vector2 inward,float length,float depth,int edge)
    { _footprint=footprint;_origin=origin;_along=along;_inward=inward;Length=length;Depth=depth;Edge=edge; }
    public float U(Vector2 p)=>(float)PlanGeometry.Dot(p-_origin,_along);
    public float V(Vector2 p)=>(float)PlanGeometry.Dot(p-_origin,_inward);
    public Vector2 Point(float u,float v)=>PlanGeometry.Quantize(_origin+_along*u+_inward*v);
    public Vector3 Point3(float u,float y,float v) { var p=Point(u,v);return PlanGeometry.Quantize(new Vector3(p.X,y,p.Y)); }
    public Vector2[] Ring(float u0,float v0,float u1,float v1)=>[Point(u0,v0),Point(u1,v0),Point(u1,v1),Point(u0,v1)];
    public (float Lower,float Upper) Section(float u)
    {
        var intersections=new List<float>(); var ring=_footprint.Outer;
        for(var i=0;i<ring.Count;i++)
        {
            var a=ring[i];var b=ring[(i+1)%ring.Count];float ua=U(a),ub=U(b);
            if(u<MathF.Min(ua,ub)-PlanGeometry.Tolerance || u>MathF.Max(ua,ub)+PlanGeometry.Tolerance) continue;
            if(MathF.Abs(ub-ua)<=PlanGeometry.Tolerance) { intersections.Add(V(a));intersections.Add(V(b)); }
            else intersections.Add(V(a)+(V(b)-V(a))*(u-ua)/(ub-ua));
        }
        if(intersections.Count<2) throw new ArgumentException("A wing section is outside its convex footprint.");
        return(intersections.Min(),intersections.Max());
    }
    public Vector2[] Room(float u0,float u1,float corridorSide,bool near,float inset)
    {
        var a=Section(u0);var b=Section(u1);
        return near ? [Point(u0,a.Lower+inset),Point(u1,b.Lower+inset),Point(u1,corridorSide),Point(u0,corridorSide)]
            : [Point(u0,corridorSide),Point(u1,corridorSide),Point(u1,b.Upper-inset),Point(u0,a.Upper-inset)];
    }
    public Vector2[] Core(float cut,bool left,float inset)
    {
        var factory=new GeometryFactory();
        var ring=_footprint.Outer;
        var polygon=factory.CreatePolygon(ring.Append(ring[0]).Select(p=>new Coordinate(p.X,p.Y)).ToArray());
        float minU=ring.Min(U)-1,maxU=ring.Max(U)+1,minV=ring.Min(V)-1,maxV=ring.Max(V)+1;
        var clip=Ring(left?minU:cut,minV,left?cut:maxU,maxV);
        var box=factory.CreatePolygon(clip.Append(clip[0]).Select(p=>new Coordinate(p.X,p.Y)).ToArray());
        var area=polygon.Intersection(box).Buffer(-inset,1);
        if(area is not Polygon core || core.IsEmpty) throw new ArgumentException("The wing end cannot contain its traffic core.");
        return PlanGeometry.OpenRing(core.ExteriorRing.Coordinates.Select(c=>PlanGeometry.Quantize(new Vector2((float)c.X,(float)c.Y)))).ToArray();
    }
    public IReadOnlyList<PolygonRings> FloorRings(IEnumerable<IReadOnlyList<Vector2>> holes)
    {
        var factory=new GeometryFactory();
        Polygon Shape(IReadOnlyList<Vector2> ring)=>factory.CreatePolygon(ring.Append(ring[0]).Select(p=>new Coordinate(p.X,p.Y)).ToArray());
        NetTopologySuite.Geometries.Geometry shape=Shape(_footprint.Outer);
        foreach(var hole in holes) shape=shape.Difference(Shape(hole));
        var result=new List<PolygonRings>();
        void Add(NetTopologySuite.Geometries.Geometry geometry)
        {
            if(geometry is Polygon polygon)
                result.Add(new(PlanGeometry.OpenRing(polygon.ExteriorRing.Coordinates.Select(c=>new Vector2((float)c.X,(float)c.Y))),
                    Enumerable.Range(0,polygon.NumInteriorRings).Select(i=>PlanGeometry.OpenRing(polygon.GetInteriorRingN(i).Coordinates.Select(c=>new Vector2((float)c.X,(float)c.Y)),false)).ToArray()));
            else if(geometry is GeometryCollection) for(var i=0;i<geometry.NumGeometries;i++) Add(geometry.GetGeometryN(i));
            else throw new ArgumentException("A wing floor subtraction returned a non-area geometry.");
        }
        Add(shape);return result;
    }
    public (int First,int End) RoomColumns(FacadeGrid grid,DormitoryPlanOptions options)
    {
        var bays=grid.Bays.Where(b=>b.Ring==0&&b.Edge==Edge).OrderBy(b=>U(b.Center)).ToArray();
        float minimum=2*options.StairFlightWidth+options.StairWellWidth+2*options.ExteriorWallThickness;
        int first=1,end=bays.Length-1;
        while(first<bays.Length && U(bays[first-1].B)<minimum-PlanGeometry.Tolerance) first++;
        while(end>=0 && Length-U(bays[end].A)<minimum-PlanGeometry.Tolerance) end--;
        if(first>=end) throw new ArgumentException("The wing has no room band between its normative traffic cores.");
        return(first,end);
    }
    public static InteriorWingFrame From(PolygonRings footprint)
    {
        var ring=footprint.Outer;
        if(ring.Count!=4 || footprint.Holes.Count!=0) throw new ArgumentException("The plan kernel requires a convex quadrilateral wing.");
        for(var i=0;i<4;i++)
            if(PlanGeometry.CrossFloat(ring[(i+1)%4]-ring[i],ring[(i+2)%4]-ring[(i+1)%4])<=0)
                throw new ArgumentException("The plan kernel requires a counterclockwise convex quadrilateral.");
        int edge=Enumerable.Range(0,4).OrderByDescending(i=>Vector2.DistanceSquared(ring[i],ring[(i+1)%4])).ThenBy(i=>i).First();
        var a=PlanGeometry.Unit(ring[(edge+1)%4]-ring[edge]);var opposite=PlanGeometry.Unit(ring[(edge+3)%4]-ring[(edge+2)%4]);
        var along=PlanGeometry.Unit(a-opposite);var inward=new Vector2(-along.Y,along.X);var origin=ring[edge];
        float U(Vector2 p)=>(float)PlanGeometry.Dot(p-origin,along);
        float V(Vector2 p)=>(float)PlanGeometry.Dot(p-origin,inward);
        float min=MathF.Max(MathF.Min(U(ring[edge]),U(ring[(edge+1)%4])),MathF.Min(U(ring[(edge+2)%4]),U(ring[(edge+3)%4])));
        float max=MathF.Min(MathF.Max(U(ring[edge]),U(ring[(edge+1)%4])),MathF.Max(U(ring[(edge+2)%4]),U(ring[(edge+3)%4])));
        float lower=MathF.Max(V(ring[edge]),V(ring[(edge+1)%4]));float upper=MathF.Min(V(ring[(edge+2)%4]),V(ring[(edge+3)%4]));
        if(max<=min || upper<=lower) throw new ArgumentException("The convex wing has no straight corridor and traffic-core strip.");
        return new(footprint,origin+along*min+inward*lower,along,inward,max-min,upper-lower,edge);
    }
}
