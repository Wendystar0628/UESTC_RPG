using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Geometry;
using NetTopologySuite.Geometries;

namespace Campus.Gen.Interior;

/// <summary>Stable quadrangulation along internal diagonals; no orthogonalisation, fitted rectangle or invented vertex.</summary>
public static class InteriorWingDecomposition
{
    public static IReadOnlyList<PolygonRings> Split(PolygonRings footprint,string source)
    {
        if(footprint.Holes.Count!=0) throw new InvalidDataException($"Interior '{source}' cannot split a holed footprint into vertex-only convex quadrilateral wings.");
        var factory=new GeometryFactory();
        Polygon Shape(IReadOnlyList<Vector2> ring)=>factory.CreatePolygon(ring.Append(ring[0]).Select(p=>new Coordinate(p.X,p.Y)).ToArray());
        List<PolygonRings>? Divide(Vector2[] ring)
        {
            if(ring.Length<4 || ring.Length%2!=0) return null;
            var polygon=Shape(ring); if(!polygon.IsValid) return null;
            if(ring.Length==4)
            {
                for(var i=0;i<4;i++)
                    if(PlanGeometry.CrossFloat(ring[(i+1)%4]-ring[i],ring[(i+2)%4]-ring[(i+1)%4])<=0) return null;
                return [new(ring)];
            }
            var reflex=Enumerable.Range(0,ring.Length).Where(i=>PlanGeometry.CrossFloat(ring[(i+1)%ring.Length]-ring[i],ring[(i+2)%ring.Length]-ring[(i+1)%ring.Length])<0)
                .Select(i=>(i+1)%ring.Length).ToHashSet();
            var diagonals=(from a in Enumerable.Range(0,ring.Length)
                from b in Enumerable.Range(a+1,ring.Length-a-1)
                where b-a>=3 && b-a<=ring.Length-3 && (b-a)%2==1
                orderby reflex.Contains(a)||reflex.Contains(b)?0:1,a,b
                select(a,b));
            foreach(var (a,b) in diagonals)
            {
                var diagonal=factory.CreateLineString([new(ring[a].X,ring[a].Y),new(ring[b].X,ring[b].Y)]);
                if(!polygon.Covers(diagonal)) continue;
                var left=Divide(ring.Skip(a).Take(b-a+1).ToArray()); if(left is null) continue;
                var right=Divide(ring.Skip(b).Concat(ring.Take(a+1)).ToArray()); if(right is null) continue;
                return left.Concat(right).ToList();
            }
            return null;
        }
        var result=Divide(PlanGeometry.CanonicalRing(footprint.Outer,true));
        if(result is null) throw new InvalidDataException($"Interior '{source}' cannot split into convex quadrilateral wings; correct the world footprint.");
        return result.Select(w=>new PolygonRings(PlanGeometry.CanonicalRing(w.Outer,true))).OrderBy(w=>w.Outer.Min(p=>p.X)).ThenBy(w=>w.Outer.Min(p=>p.Y)).ToArray();
    }
}
