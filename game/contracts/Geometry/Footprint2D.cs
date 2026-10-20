namespace Campus.Contracts;

public sealed record Footprint2D(FeatureId Feature, PolygonRings Rings, float MinY = 0f, float MaxY = 0f);
