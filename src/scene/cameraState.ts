/** Mutable frame state is local to one scene, never attached to Three.js userData. */
export interface CameraFrame {
  phase: 'moving' | 'settling' | 'idle';
  settleAt: number;
  distance: number;
  revision: number;
}

export interface CameraMetrics { azimuth: number; metresPerPixel: number; target: string; moving: boolean; }

export const CAMERA_SETTLE_MS = 180;
export const createCameraFrame = (): CameraFrame => ({ phase: 'idle', settleAt: 0, distance: 0, revision: 0 });

/** A held pointer is still an interaction, even when MapControls reports no delta. */
export function advanceCameraMotion(frame: CameraFrame, now: number, busy: boolean): void {
  if (busy) {
    frame.phase = 'moving';
    frame.settleAt = now + CAMERA_SETTLE_MS;
  } else frame.phase = now < frame.settleAt ? 'settling' : 'idle';
}
