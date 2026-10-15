import { test, expect } from '@playwright/test';

for (const button of ['left', 'right'] as const) {
test(`held ${button === 'left' ? 'pan' : 'rotation'} keeps label identities, text and clusters stable, including pauses`, async ({ page }) => {
  await page.goto('/?inspect=1');
  await expect(page.locator('.scene-canvas')).toHaveAttribute('data-textures-ready', 'true');
  await expect(page.locator('.map-view')).toHaveAttribute('data-camera-status', 'idle');
  await page.waitForTimeout(700);
  await page.evaluate(() => {
    const origin = document.querySelector('.annotations')!.getBoundingClientRect();
    const tracked = [...document.querySelectorAll<HTMLButtonElement>('.map-marker')].filter(element => {
      const rect = element.getBoundingClientRect();
      return rect.left > origin.left + 180 && rect.right < origin.right - 180 && rect.top > origin.top + 160 && rect.bottom < origin.bottom - 160;
    }).map(element => ({ id: element.dataset.entityId!, anchor: element.dataset.anchor!.split(',').map(Number), signature: element.textContent + '|' + element.className }));
    const probe = { tracked: tracked.length, samples: 0, changes: [] as string[], done: false };
    (window as unknown as { stabilityProbe: typeof probe }).stabilityProbe = probe;
    const sample = () => {
      if (probe.done) return;
      const view = window.__UESTC_RENDER__?.annotationCamera;
      if (view) for (const item of tracked) {
        const [x, y, z] = item.anchor, m = view.viewProjection;
        const w = m[3] * x + m[7] * y + m[11] * z + m[15];
        const px = ((m[0] * x + m[4] * y + m[8] * z + m[12]) / w + 1) * view.width / 2;
        const py = (1 - (m[1] * x + m[5] * y + m[9] * z + m[13]) / w) * view.height / 2;
        if (px < 140 || px > view.width - 140 || py < 140 || py > view.height - 140) continue;
        probe.samples++;
        const element = document.querySelector<HTMLButtonElement>(`.map-marker[data-entity-id="${item.id}"]`);
        const signature = element && getComputedStyle(element).visibility === 'visible' ? element.textContent + '|' + element.className : 'missing';
        if (signature !== item.signature && !probe.changes.includes(item.id)) probe.changes.push(item.id);
      }
      requestAnimationFrame(sample);
    };
    requestAnimationFrame(sample);
  });
  const canvas = (await page.locator('canvas').boundingBox())!;
  const x = canvas.x + 85, y = canvas.y + 260;
  await page.mouse.move(x, y);
  await page.mouse.down({ button });
  for (let i = 0; i < 3; i++) {
    await page.mouse.move(x + 70, y + 30, { steps: 25 });
    await page.waitForTimeout(350);
    await page.mouse.move(x + 5, y + 5, { steps: 25 });
  }
  await page.waitForTimeout(500);
  await expect(page.locator('.map-view')).toHaveAttribute('data-camera-status', 'moving');
  const measured = await page.evaluate(() => {
    const probe = (window as unknown as { stabilityProbe: { tracked: number; samples: number; changes: string[]; done: boolean } }).stabilityProbe;
    probe.done = true;
    return probe;
  });
  await page.mouse.up({ button });
  console.log('Held-drag label stability:', measured);
  expect(measured.tracked).toBeGreaterThan(4);
  expect(measured.samples).toBeGreaterThan(100);
  expect(measured.changes).toEqual([]);
  await expect(page.locator('.map-view')).toHaveAttribute('data-camera-status', 'idle');
  await page.waitForTimeout(300);
  const frames = await page.evaluate(() => window.__UESTC_RENDER__!.frames);
  await page.waitForTimeout(600);
  expect(await page.evaluate(() => window.__UESTC_RENDER__!.frames)).toBe(frames);
});
}
