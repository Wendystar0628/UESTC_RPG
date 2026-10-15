import { memo, useCallback, useSyncExternalStore, type RefObject } from 'react';
import PlaceIcon from '../../ui/PlaceIcon';
import type { AnnotationMarker } from './model';
import type { AnnotationStore } from './store';

interface Props {
  store: AnnotationStore;
  visible: boolean;
  icons: boolean;
  pointer: RefObject<[number, number]>;
  onCluster: (ids: string[]) => void;
  onSelect: (id: string) => void;
}

const MarkerButton = memo(function MarkerButton({ marker, store, icons, pointer, onCluster, onSelect }: Omit<Props, 'visible'> & { marker: AnnotationMarker }) {
  const bind = useCallback((element: HTMLButtonElement | null) => store.bind(marker.entity.id, element), [store, marker.entity.id]);
  return <button ref={bind} className={`map-marker ${marker.selected ? 'selected' : ''} ${marker.label ? 'with-label' : ''}`}
    data-anchor={marker.anchor.join(',')} data-entity-id={marker.entity.id} title={marker.entity.name}
    aria-label={marker.ids.length > 1 ? `${marker.ids.length}个相邻地点，展开列表` : `查看${marker.entity.name}`}
    onClick={event => {
      event.stopPropagation();
      if (event.detail > 0 && Math.hypot(event.clientX - pointer.current[0], event.clientY - pointer.current[1]) > 6) return;
      marker.ids.length > 1 ? onCluster(marker.ids) : onSelect(marker.entity.id);
    }}>
    {(icons || marker.selected || !marker.label) && <span className="marker-icon"><PlaceIcon name={marker.entity.icon} size={17} /></span>}
    {marker.label && <span className="marker-name">{marker.entity.name}</span>}
    {marker.ids.length > 1 && <span className="cluster-count">{marker.ids.length}</span>}
  </button>;
});

export default function AnnotationOverlay(props: Props) {
  const markers = useSyncExternalStore(props.store.subscribe, props.store.getSnapshot);
  return <div className="annotations" aria-label="校园地点标注" hidden={!props.visible}>
    {props.visible && markers.map(marker => <MarkerButton key={marker.entity.id} {...props} marker={marker} />)}
  </div>;
}
