export type QualityTier = 'light' | 'standard' | 'high';
export type QualityMode = 'auto' | QualityTier;

export const qualitySettings = {
  light: { label: '轻量', dpr: 1, shadowSize: 0, textureSize: 512, maxLabels: 25 },
  standard: { label: '标准', dpr: 1.5, shadowSize: 2048, textureSize: 1024, maxLabels: 45 },
  high: { label: '高清', dpr: 2, shadowSize: 2048, textureSize: 1024, maxLabels: 60 },
} as const;

/** Conservative initial choice; actual frame sampling may reduce the automatic tier. */
export function initialQuality(): QualityTier {
  return typeof window !== 'undefined' && (window.matchMedia('(pointer: coarse)').matches || window.innerWidth < 768) ? 'light' : 'standard';
}
