import { useEffect, useMemo } from 'react';
import { useThree } from '@react-three/fiber';
import * as THREE from 'three';
import { mergeGeometries } from 'three/addons/utils/BufferGeometryUtils.js';
import type { Dataset } from '../data/types';
import { createLandscapeLayout, type PlantPlacement } from './landscape';

const NO_RAYCAST = () => {};

function organicCrown(variant: number): THREE.BufferGeometry {
  const lobes = variant === 2
    ? [[0, .63, 0, .75, .37, .71], [.12, .48, -.12, .9, .25, .77]]
    : variant === 1
      ? [[-.3, .6, .06, .7, .25, .68], [.29, .58, -.13, .67, .25, .66], [0, .78, .12, .7, .22, .63]]
      : [[0, .71, 0, .77, .28, .75], [-.34, .54, -.08, .63, .22, .6], [.29, .58, .14, .68, .25, .61]];
  const parts = lobes.map(([x, y, z, sx, sy, sz], index) => {
    const geometry = new THREE.IcosahedronGeometry(1, 1);
    const positions = geometry.getAttribute('position');
    for (let vertex = 0; vertex < positions.count; vertex++) {
      const px = positions.getX(vertex), py = positions.getY(vertex), pz = positions.getZ(vertex);
      const irregularity = 1 + .095 * Math.sin(px * 9 + py * 5 + index) * Math.cos(pz * 7 - py * 3);
      positions.setXYZ(vertex, px * irregularity, py * irregularity, pz * irregularity);
    }
    geometry.scale(sx, sy, sz); geometry.translate(x, y, z);
    return geometry;
  });
  const geometry = mergeGeometries(parts, false)!;
  parts.forEach(part => part.dispose());
  const crownPositions = geometry.getAttribute('position');
  let radius = 0, height = 0;
  for (let index = 0; index < crownPositions.count; index++) {
    radius = Math.max(radius, Math.hypot(crownPositions.getX(index), crownPositions.getZ(index)));
    height = Math.max(height, crownPositions.getY(index));
  }
  geometry.scale(1 / radius, 1 / height, 1 / radius);
  geometry.computeVertexNormals();
  const positions = geometry.getAttribute('position'), colors: number[] = [];
  for (let index = 0; index < positions.count; index++) {
    const tone = .7 + .3 * Math.min(1, Math.max(0, positions.getY(index)));
    colors.push(tone, tone, tone * .97);
  }
  geometry.setAttribute('color', new THREE.Float32BufferAttribute(colors, 3));
  return geometry;
}

function trunkGeometry(): THREE.BufferGeometry {
  const trunk = new THREE.CylinderGeometry(.09, .17, .68, 6, 1);
  trunk.translate(0, .34, 0);
  const branches = [-1, 1].map(direction => {
    const branch = new THREE.CylinderGeometry(.035, .075, .31, 5, 1);
    branch.rotateZ(direction * .64); branch.translate(-direction * .105, .48, .02);
    return branch;
  });
  const merged = mergeGeometries([trunk, ...branches], false)!;
  [trunk, ...branches].forEach(geometry => geometry.dispose());
  return merged;
}

function createInstances(geometry: THREE.BufferGeometry, material: THREE.Material, plants: PlantPlacement[], kind: string): THREE.InstancedMesh {
  const mesh = new THREE.InstancedMesh(geometry, material, plants.length);
  const transform = new THREE.Object3D(), tint = new THREE.Color();
  plants.forEach((plant, index) => {
    transform.position.set(plant.x, .05, plant.z);
    transform.rotation.set(0, plant.rotation, 0);
    transform.scale.set(plant.radius, plant.height, plant.radius);
    transform.updateMatrix(); mesh.setMatrixAt(index, transform.matrix);
    if (kind !== 'trunks') {
      tint.setHSL(.24 + plant.tone * .035, .24 + plant.tone * .09, .31 + plant.tone * .09);
      mesh.setColorAt(index, tint);
    }
  });
  mesh.name = `campus-landscape/${kind}`;
  mesh.instanceMatrix.needsUpdate = true;
  if (mesh.instanceColor) mesh.instanceColor.needsUpdate = true;
  mesh.computeBoundingSphere();
  mesh.castShadow = true; mesh.receiveShadow = true; mesh.raycast = NO_RAYCAST;
  return mesh;
}

/** Five static instance batches; no per-tree React nodes, textures or animation loop. */
export default function CampusLandscape({ data, visible }: { data: Dataset; visible: boolean }) {
  const { scene, invalidate } = useThree();
  const landscape = useMemo(() => {
    const layout = createLandscapeLayout(data), group = new THREE.Group();
    const crowns = [0, 1, 2].map(organicCrown), trunk = trunkGeometry();
    const foliage = new THREE.MeshStandardMaterial({ color: '#ffffff', vertexColors: true, roughness: .96 });
    const bark = new THREE.MeshStandardMaterial({ color: '#756a57', roughness: 1 });
    crowns.forEach((geometry, index) => {
      group.add(createInstances(geometry, foliage, layout.trees.filter(plant => plant.variant === index), `crowns-${index}`));
    });
    group.add(createInstances(trunk, bark, layout.trees, 'trunks'));
    group.add(createInstances(crowns[0], foliage, layout.shrubs, 'shrubs'));
    group.name = 'campus-landscape';
    const triangleCount = group.children.reduce((sum, child) => {
      const mesh = child as THREE.InstancedMesh;
      return sum + (mesh.geometry.index?.count || mesh.geometry.getAttribute('position').count) / 3 * mesh.count;
    }, 0);
    return { group, crowns, trunk, foliage, bark, stats: { trees: layout.trees.length, shrubs: layout.shrubs.length, drawCalls: 5, triangles: triangleCount, surveyed: false } };
  }, [data]);
  useEffect(() => {
    scene.userData.campusLandscape = landscape.stats;
    invalidate();
    return () => {
      landscape.group.children.forEach(child => (child as THREE.InstancedMesh).dispose());
      landscape.crowns.forEach(geometry => geometry.dispose()); landscape.trunk.dispose();
      landscape.foliage.dispose(); landscape.bark.dispose();
      delete scene.userData.campusLandscape;
    };
  }, [landscape, scene, invalidate]);
  useEffect(() => { invalidate(); }, [visible, invalidate]);
  return <primitive object={landscape.group} visible={visible} dispose={null} />;
}
