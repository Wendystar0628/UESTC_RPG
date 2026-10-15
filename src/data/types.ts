export type Vec3 = [number, number, number];
export type Bounds = [number, number, number, number];
export type Ring = [number, number][];
export type PolygonRings = Ring[];

export interface Category {
  id: string;
  label: string;
  icon: string;
  keywords: string[];
  children: { id: string; label: string; icon: string }[];
}

export interface Entity {
  id: string;
  name: string;
  named: boolean;
  aliases: string[];
  category: string;
  subcategory: string | null;
  icon: string;
  categoryBasis: string;
  categorySourceId?: string;
  kind: string;
  description: string;
  featureIds: string[];
  anchor: Vec3 | null;
  bounds: Bounds | null;
  priority: number;
  parentId: string | null;
  parentName: string | null;
  locationStatus: 'geometry' | 'point' | 'parent' | 'unlocated';
  height: number | null;
  levels: number | null;
  source: string;
  updatedAt: string | null;
  sourceTags: Record<string, string>;
  enrichment?: {
    reviewedAt: string;
    evidence: { title: string; url: string; fields: string[]; note: string; publishedAt?: string }[];
    memberSources: string[];
  };
}

export interface SceneFeature {
  id: string;
  entityId: string | null;
  kind: 'building' | 'road' | 'water' | 'green' | 'sport' | 'parking' | 'poi';
  polygons: PolygonRings[];
  height: number;
  heightStatus: 'source' | 'estimated' | 'unknown';
  category: string;
  sport?: string;
  roadType?: string;
  widthEstimated?: boolean;
}

export interface Dataset {
  meta: {
    name: string;
    date: string;
    source: string;
    license: string;
    origin: [number, number];
    crs: string;
    bounds: Bounds;
    areaM2: number;
    heightNote: string;
    issues: string[];
    stats: {
      buildings: number;
      places: number;
      namedPlaces: number;
      sourceHeights: number;
      levelHeights: number;
      features: number;
      categories: Record<string, number>;
    };
  };
  categories: Category[];
  boundary: PolygonRings;
  features: SceneFeature[];
  entities: Entity[];
}

export type LayerKey = 'building' | 'road' | 'water' | 'green' | 'sport' | 'icons' | 'names';
export type Layers = Record<LayerKey, boolean>;

export interface ViewCommand {
  seq: number;
  kind: 'entity' | 'cluster' | 'home' | 'north' | 'top' | 'perspective' | 'zoomIn' | 'zoomOut' | 'cancel';
  ids?: string[];
}

export async function loadCampusDataset(signal: AbortSignal): Promise<Dataset> {
  const response = await fetch(`${import.meta.env.BASE_URL}data/qingshuihe/campus.json`, { signal });
  if (!response.ok) throw new Error(`校园数据加载失败（HTTP ${response.status}）`);
  const data = await response.json() as Dataset;
  if (!data.meta?.bounds || !data.entities?.length || !data.features?.length || !data.categories?.length) {
    throw new Error('校园数据不完整，请运行 npm run data:build。');
  }
  return data;
}
