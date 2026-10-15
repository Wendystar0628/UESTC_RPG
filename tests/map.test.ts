import { describe, expect, it } from 'vitest';
import { readFileSync } from 'node:fs';
import type { Dataset, Entity, SceneFeature } from '../src/data/types';
import { buildSearchIndex, matchesCategories, normalize, searchPlaces } from '../src/data/search';
import { initialState, mapReducer } from '../src/app/mapState';
import { featureGeometry, pointInFootprint } from '../src/scene/geometry';

const data = JSON.parse(readFileSync(new URL('../public/data/qingshuihe/campus.json', import.meta.url), 'utf8')) as Dataset;
const index = buildSearchIndex(data.entities, data.categories);

describe('campus lookup', () => {
  it('finds the library and a source-recorded alternative name', () => {
    expect(searchPlaces(index, '图书馆', [], 'all')[0].category).toBe('library');
    expect(searchPlaces(index, '第一教学楼', [], 'all').some(e => e.name === '品学楼')).toBe(true);
  });

  it('normalizes width and case without confusing building numbers', () => {
    expect(normalize(' Ａ１区  ')).toBe('a1区');
    const fixture = [1, 10, 11].map(n => ({ ...data.entities[0], id: `fixture-${n}`, name: `测试${n}栋`, aliases: [], parentName: null })) as Entity[];
    expect(searchPlaces(buildSearchIndex(fixture, data.categories), '测试１栋', [], 'all').map(e => e.name)).toEqual(['测试1栋']);
  });

  it('searches outside filters only in global scope', () => {
    expect(searchPlaces(index, '图书馆', ['dining'], 'all').length).toBeGreaterThan(0);
    expect(searchPlaces(index, '图书馆', ['dining'], 'current')).toHaveLength(0);
    expect(searchPlaces(index, '餐厅', ['dining'], 'current').every(e => e.category === 'dining')).toBe(true);
  });

  it('matches parent and child categories, with unique result IDs', () => {
    const cafe = data.entities.find(e => e.subcategory === 'cafe')!;
    expect(matchesCategories(cafe, ['dining'])).toBe(true);
    expect(matchesCategories(cafe, ['dining:cafe'])).toBe(true);
    const results = searchPlaces(index, '', [], 'all');
    expect(new Set(results.map(e => e.id)).size).toBe(results.length);
  });
});

describe('data and space', () => {
  it('finds source-tagged trees, commercial buildings and land parcels in their own categories', () => {
    const trees = searchPlaces(index, '行道树', ['landscape:tree_row'], 'current');
    expect(trees.length).toBeGreaterThan(0);
    expect(trees.every(e => e.sourceTags.natural === 'tree_row' && !e.named)).toBe(true);
    expect(searchPlaces(index, '商业街', ['service:commercial'], 'current').some(e => e.id === 'qsh:way:603368212')).toBe(true);
    for (const [id, subtype] of [['950008612', 'construction'], ['1061419130', 'brownfield'], ['1061419131', 'brownfield']]) {
      const parcel = data.entities.find(e => e.id === `qsh:way:${id}`)!;
      expect(parcel.category).toBe('other');
      expect(parcel.subcategory).toBe(subtype);
      expect(parcel.sourceTags.landuse).toBe(subtype);
      expect(matchesCategories(parcel, ['landscape:garden'])).toBe(false);
    }
  });

  it('inherits known building uses through nested parts while keeping tenants and unknown uses distinct', () => {
    for (const id of ['qsh:relation:11185936', 'qsh:way:814133732']) {
      const wing = data.entities.find(e => e.id === id)!;
      expect(wing.category).toBe('teaching');
      expect(wing.categorySourceId).toBe('qsh:relation:11185938');
    }
    const nestedPart = data.entities.find(e => e.id === 'qsh:way:779496152')!;
    expect(nestedPart.category).toBe('teaching');
    expect(nestedPart.categorySourceId).toBe('qsh:way:779496151');
    expect(nestedPart.named).toBe(false);
    expect(data.entities.find(e => e.id === 'qsh:way:806848410')?.category).toBe('library');
    const cafe = data.entities.find(e => e.id === 'qsh:node:9139805611')!;
    expect(cafe.category).toBe('dining');
    expect(cafe.subcategory).toBe('cafe');
    expect(data.entities.find(e => e.id === cafe.parentId)?.category).toBe('teaching');
    expect(data.entities.find(e => e.id === 'qsh:way:217855247')?.category).toBe('unknown');
  });

  it('finds the official management-building name and keeps its original alias', () => {
    const official = searchPlaces(index, '经管楼', [], 'all').find(e => e.kind === 'building')!;
    expect(official.id).toBe('qsh:way:795677483');
    expect(searchPlaces(index, '三号科研实验楼', [], 'all').some(e => e.id === official.id)).toBe(true);
    expect(official.enrichment?.evidence.some(e => e.url.startsWith('https://gis.uestc.edu.cn/'))).toBe(true);
  });

  it('connects the hospital and its wings to one searchable place', () => {
    const hospital = searchPlaces(index, '校医院', [], 'all').find(e => e.id === 'qsh:way:683651630')!;
    expect(hospital.category).toBe('medical');
    for (const id of ['osm:way:683651630', 'osm:way:683646724', 'osm:way:683646733']) {
      expect(hospital.featureIds).toContain(id);
      expect(data.features.find(f => f.id === id)?.entityId).toBe(hospital.id);
    }
    expect(data.entities.some(e => e.id === 'qsh:node:9633125265')).toBe(false);
  });

  it('keeps cafes separate from their host and avoids naming a sports building after its restaurant', () => {
    const cafe = data.entities.find(e => e.id === 'qsh:node:11990371355')!;
    expect(cafe.name).toBe('瑞幸咖啡');
    expect(cafe.parentId).toBe('qsh:way:828726392');
    expect(cafe.parentName).toBe('科创餐厅');
    const taoyuan = data.entities.find(e => e.id === 'qsh:node:5751791011')!;
    expect(taoyuan.parentId).toBe('qsh:way:601600526');
    expect(data.entities.find(e => e.id === taoyuan.parentId)?.named).toBe(false);
  });

  it('groups verified sites without assigning invented wing names or altering their heights', () => {
    const humanities = searchPlaces(index, '文科楼', [], 'all').find(e => e.id === 'qsh:way:883187548')!;
    const kindergarten = searchPlaces(index, '立人幼儿园', [], 'all').find(e => e.subcategory === 'kindergarten')!;
    expect(kindergarten.parentId).toBeNull();
    expect(humanities.parentId).toBeNull();
    expect(data.features.find(f => f.id === 'osm:way:1431090636')?.entityId).toBe(humanities.id);
    expect(data.features.find(f => f.id === 'osm:way:779496173')?.entityId).toBe(kindergarten.id);
    const part = data.features.find(f => f.id === 'osm:way:691679138')!;
    expect(part.entityId).toBe('qsh:way:691679140');
    expect(part.height).toBe(6.6);
    expect(part.heightStatus).toBe('estimated');
    expect(humanities.enrichment?.evidence.some(e => e.fields.includes('建筑关联'))).toBe(true);
  });

  it('has a valid place for every selectable feature', () => {
    const ids = new Set(data.entities.map(e => e.id));
    expect(data.features.filter(f => f.entityId && !ids.has(f.entityId)).map(f => f.id)).toEqual([]);
    expect(data.entities.filter(e => e.parentId && !ids.has(e.parentId)).map(e => e.id)).toEqual([]);
    expect(data.entities.filter(e => e.categorySourceId && !ids.has(e.categorySourceId)).map(e => e.id)).toEqual([]);
  });

  it('keeps source data in the campus metre-scale extent', () => {
    expect(data.meta.crs).toBe('EPSG:32648');
    expect(data.meta.bounds.every(n => Number.isFinite(n) && Math.abs(n) < 2500)).toBe(true);
    expect(data.entities.every(e => !e.anchor || e.anchor.every(Number.isFinite))).toBe(true);
  });

  it('preserves footprint holes, east/north axes, and vertical height', () => {
    const footprint: SceneFeature = { id: 'fixture', entityId: null, kind: 'building', polygons: [[[[0, 0], [20, 0], [20, 20], [0, 20], [0, 0]], [[5, 5], [5, 15], [15, 15], [15, 5], [5, 5]]]], height: 12, heightStatus: 'source', category: 'unknown' };
    expect(pointInFootprint(2, 2, footprint.polygons)).toBe(true);
    expect(pointInFootprint(10, 10, footprint.polygons)).toBe(false);
    const geometry = featureGeometry(footprint, false)!;
    geometry.computeBoundingBox();
    expect(geometry.boundingBox!.max.x).toBeCloseTo(20);
    expect(geometry.boundingBox!.max.z).toBeCloseTo(20);
    expect(geometry.boundingBox!.max.y - geometry.boundingBox!.min.y).toBeCloseTo(12);
    geometry.dispose();
  });
});

describe('selection and view commands', () => {
  it('toggles labels without clearing the selected building, filters or camera command', () => {
    const selected = mapReducer({ ...initialState, filters: ['teaching'] }, { type: 'select', id: 'library', focus: true, canFocus: true });
    const hidden = mapReducer(selected, { type: 'labels' });
    expect(hidden.labelsVisible).toBe(false);
    expect(hidden.selectedId).toBe(selected.selectedId);
    expect(hidden.filters).toBe(selected.filters);
    expect(hidden.command).toBe(selected.command);
    expect(mapReducer(hidden, { type: 'labels' })).toEqual(selected);
  });
  it('can focus the same entity twice, and cancel without a stale command', () => {
    const a = mapReducer(initialState, { type: 'select', id: 'library', focus: true, canFocus: true });
    const b = mapReducer(a, { type: 'select', id: 'library', focus: true, canFocus: true });
    expect(b.command.seq).toBeGreaterThan(a.command.seq);
    const cleared = mapReducer(b, { type: 'clear' });
    expect(cleared.selectedId).toBeNull();
    expect(cleared.command.kind).toBe('cancel');
  });

  it('never starts a camera flight for an unlocated entity', () => {
    const state = mapReducer(initialState, { type: 'select', id: 'unlocated-fixture', focus: true, canFocus: false });
    expect(state.selectedId).toBe('unlocated-fixture');
    expect(state.command.kind).toBe('cancel');
  });

  it('clears temporary selection when the user changes a filter', () => {
    const selected = mapReducer(initialState, { type: 'select', id: 'library', focus: true, canFocus: true });
    const filtered = mapReducer(selected, { type: 'filter', id: 'dining' });
    expect(filtered.selectedId).toBeNull();
    expect(filtered.filters).toEqual(['dining']);
  });
});
