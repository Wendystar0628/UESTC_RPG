import { useEffect, useMemo, useRef } from 'react';
import { useThree } from '@react-three/fiber';
import * as THREE from 'three';
import { materialPresets, materialVersion, type MaterialKey } from '../data/appearance';
import type { QualityTier } from './quality';

export interface TextureStatus { pending: number; loaded: number; failed: string[]; }
export type MaterialLibrary = Record<MaterialKey, THREE.MeshStandardMaterial>;
const keys = Object.keys(materialPresets) as MaterialKey[];
const families = [...new Set(keys.map(key => materialPresets[key].family))];
const normalFamilies = new Set(keys.filter(key => materialPresets[key].normalStrength > 0).map(key => materialPresets[key].family));

export function useSharedMaterials(tier: QualityTier, enabled: boolean, retry: number, onStatus: (status: TextureStatus) => void): MaterialLibrary {
  const { gl, invalidate } = useThree();
  const enabledRef = useRef(enabled);
  enabledRef.current = enabled;
  const textureSets = useRef(new Map<string, { color?: THREE.Texture; normal?: THREE.Texture }>());
  const library = useMemo(() => {
    const entries = keys.map(key => {
      const preset = materialPresets[key];
      const material = new THREE.MeshStandardMaterial({
        name: key, color: preset.color, roughness: preset.roughness,
        metalness: preset.metalness, vertexColors: true,
        normalScale: new THREE.Vector2(preset.normalStrength, preset.normalStrength),
      });
      const ground = ['asphalt', 'paving', 'concrete-ground', 'grass', 'turf', 'rubber-red', 'rubber-green', 'water', 'stone'].includes(key);
      material.polygonOffset = ground;
      material.polygonOffsetFactor = -1;
      material.polygonOffsetUnits = key === 'asphalt' || key === 'paving' ? -12 : key.startsWith('rubber') || key === 'turf' ? -10 : key === 'water' ? -8 : -5;
      return [key, material];
    });
    return Object.fromEntries(entries) as MaterialLibrary;
  }, []);

  useEffect(() => {
    let active = true;
    const loader = new THREE.TextureLoader();
    const textures = new Set<THREE.Texture>();
    const sets = new Map<string, { color?: THREE.Texture; normal?: THREE.Texture }>();
    textureSets.current = sets;
    const size = tier === 'light' ? 512 : 1024;
    const anisotropy = Math.min(gl.capabilities.getMaxAnisotropy(), tier === 'light' ? 2 : tier === 'high' ? 8 : 4);
    const jobs = families.flatMap(family => ['color', ...(tier !== 'light' && normalFamilies.has(family) ? ['normal'] : [])].map(role => ({ family, role })));
    let pending = jobs.length, loaded = 0;
    const failed: string[] = [];
    onStatus({ pending, loaded, failed: [] });
    for (const material of Object.values(library)) {
      material.map = null; material.normalMap = null; material.needsUpdate = true;
    }
    for (const { family, role } of jobs) {
      const path = `${import.meta.env.BASE_URL}assets/${materialVersion}/${size}/${family}-${role}.webp`;
      loader.load(path, texture => {
        if (!active) { texture.dispose(); return; }
        texture.colorSpace = role === 'color' ? THREE.SRGBColorSpace : THREE.NoColorSpace;
        texture.wrapS = texture.wrapT = THREE.RepeatWrapping;
        texture.minFilter = THREE.LinearMipmapLinearFilter;
        texture.magFilter = THREE.LinearFilter;
        texture.anisotropy = anisotropy;
        texture.generateMipmaps = true;
        textures.add(texture);
        const set = sets.get(family) || {};
        set[role as 'color' | 'normal'] = texture;
        sets.set(family, set);
        for (const key of keys) {
          if (materialPresets[key].family !== family) continue;
          const material = library[key];
          if (role === 'color') material.map = enabledRef.current ? texture : null;
          else if (materialPresets[key].normalStrength > 0) material.normalMap = enabledRef.current ? texture : null;
          material.needsUpdate = true;
        }
        pending--; loaded++;
        onStatus({ pending, loaded, failed: [...failed] });
        invalidate();
      }, undefined, () => {
        if (!active) return;
        pending--; failed.push(`${family}-${role}`);
        onStatus({ pending, loaded, failed: [...failed] });
        invalidate();
      });
    }
    invalidate();
    return () => {
      active = false;
      for (const material of Object.values(library)) { material.map = null; material.normalMap = null; material.dispose(); }
      textures.forEach(texture => texture.dispose());
      sets.clear();
    };
  }, [tier, retry, library, gl, invalidate, onStatus]);

  useEffect(() => {
    for (const key of keys) {
      const textures = textureSets.current.get(materialPresets[key].family);
      library[key].map = enabled ? textures?.color || null : null;
      library[key].normalMap = enabled && materialPresets[key].normalStrength > 0 ? textures?.normal || null : null;
      library[key].needsUpdate = true;
    }
    invalidate();
  }, [enabled, library, invalidate]);
  return library;
}
