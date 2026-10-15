import type { Category, Entity, Layers } from './types';

export const normalize = (value: string) => value.normalize('NFKC').toLocaleLowerCase().trim().replace(/\s+/g, ' ');

export function matchesCategories(entity: Entity, filters: string[]): boolean {
  return filters.length === 0 || filters.includes(entity.category) || filters.includes(`${entity.category}:${entity.subcategory}`);
}

export function layerVisible(entity: Entity, layers: Layers): boolean {
  return !(entity.kind in layers) || layers[entity.kind as keyof Layers];
}

export function categoryPath(entity: Entity, categories: Category[]): string {
  const category = categories.find(c => c.id === entity.category);
  const child = category?.children.find(c => c.id === entity.subcategory);
  return child ? `${category?.label} / ${child.label}` : category?.label || '待确认类型';
}

export function buildSearchIndex(entities: Entity[], categories: Category[]) {
  return entities.map(entity => {
    const category = categories.find(c => c.id === entity.category);
    const names = [entity.name, ...entity.aliases].map(normalize);
    const keywords = [category?.label || '', ...category?.keywords || [], categoryPath(entity, categories)].map(normalize);
    return { entity, names, keywords, text: [...names, ...keywords, normalize(entity.parentName || ''), entity.id].join(' ') };
  });
}

// A numeric token cannot silently match a different building number (1 vs 10).
function includesToken(text: string, token: string) {
  if (!/\d/.test(token)) return text.includes(token);
  const safe = token.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
  return new RegExp(`(?<!\\d)${safe}(?!\\d)`, 'u').test(text);
}

export function searchPlaces(index: ReturnType<typeof buildSearchIndex>, query: string, filters: string[], scope: 'all' | 'current') {
  const q = normalize(query);
  const tokens = q.split(' ').filter(Boolean);
  return index
    .filter(row => (scope === 'all' && q !== '' || matchesCategories(row.entity, filters)) && tokens.every(token => includesToken(row.text, token)))
    .map(row => {
      const { names, entity } = row;
      let score = !q ? 20 - entity.priority * 3 - (entity.named ? 2 : 0) : 6;
      if (q && (names[0] === q || entity.id === q)) score = 0;
      else if (q && names.slice(1).includes(q)) score = 1;
      else if (q && names.some(n => n.startsWith(q) && includesToken(n, q))) score = 2;
      else if (q && names.some(n => includesToken(n, q))) score = 3;
      return { entity, score };
    })
    .sort((a, b) => a.score - b.score || Number(!a.entity.anchor) - Number(!b.entity.anchor) || a.entity.name.localeCompare(b.entity.name, 'zh-CN', { numeric: true }) || a.entity.id.localeCompare(b.entity.id))
    .map(row => row.entity);
}
