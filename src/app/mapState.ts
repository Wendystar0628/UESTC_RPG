import type { QualityMode } from '../scene/quality';
import type { Layers, LayerKey, ViewCommand } from '../data/types';

export interface MapState {
  selectedId: string | null;
  filters: string[];
  scope: 'all' | 'current';
  layers: Layers;
  strictHeight: boolean;
  textures: boolean;
  labelsVisible: boolean;
  quality: QualityMode;
  view: '3d' | 'top';
  command: ViewCommand;
}

export const initialState: MapState = {
  selectedId: null,
  filters: [],
  scope: 'all',
  layers: { building: true, road: true, water: true, green: true, sport: true, icons: true, names: true },
  strictHeight: false,
  textures: true,
  labelsVisible: true,
  quality: 'auto',
  view: '3d',
  command: { seq: 0, kind: 'home' },
};

export type Action =
  | { type: 'select'; id: string; focus: boolean; canFocus: boolean }
  | { type: 'clear' }
  | { type: 'filter'; id?: string }
  | { type: 'scope'; value: 'all' | 'current' }
  | { type: 'layer'; key: LayerKey }
  | { type: 'height' }
  | { type: 'textures' }
  | { type: 'labels' }
  | { type: 'quality'; value: QualityMode }
  | { type: 'command'; kind: ViewCommand['kind']; ids?: string[] };

export function mapReducer(state: MapState, action: Action): MapState {
  const command = (kind: ViewCommand['kind'], ids?: string[]): ViewCommand => ({ seq: state.command.seq + 1, kind, ids });
  switch (action.type) {
    case 'select': return { ...state, selectedId: action.id, command: command(action.focus && action.canFocus ? 'entity' : 'cancel', [action.id]) };
    case 'clear': return { ...state, selectedId: null, command: command('cancel') };
    case 'filter': return { ...state, selectedId: null, filters: !action.id ? [] : state.filters.includes(action.id) ? state.filters.filter(id => id !== action.id) : [...state.filters.filter(id => !id.startsWith(action.id + ':') && id !== action.id?.split(':')[0]), action.id], command: command('cancel') };
    case 'scope': return { ...state, scope: action.value };
    case 'layer': return { ...state, selectedId: null, layers: { ...state.layers, [action.key]: !state.layers[action.key] }, command: command('cancel') };
    case 'textures': return { ...state, textures: !state.textures };
    case 'labels': return { ...state, labelsVisible: !state.labelsVisible };
    case 'quality': return { ...state, quality: action.value };
    case 'height': return { ...state, strictHeight: !state.strictHeight };
    case 'command': return { ...state, selectedId: action.kind === 'home' ? null : state.selectedId, view: action.kind === 'top' ? 'top' : action.kind === 'perspective' ? '3d' : state.view, command: command(action.kind, action.ids) };
  }
}
