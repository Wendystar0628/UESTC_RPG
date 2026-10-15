import { test, expect, type Page } from '@playwright/test';

async function ready(page: Page) {
  await page.goto('/?inspect=1');
  await expect(page.locator('.scene-canvas')).toHaveAttribute('data-textures-ready', 'true');
  await expect(page.locator('.map-view')).toHaveAttribute('data-camera-status', 'idle');
  await page.waitForTimeout(600);
}
const snapshot = (page: Page) => page.evaluate(() => structuredClone(window.__UESTC_RENDER__!));

test('resting map draws no frames; selection, filters and labels keep base geometry', async ({ page }) => {
  await ready(page);
  const before = await snapshot(page);
  await page.waitForTimeout(5000);
  const rested = await snapshot(page);
  expect(rested.frames).toBe(before.frames);
  await page.getByRole('combobox').fill('图书馆');
  await page.getByRole('button', { name: '图书馆 校区中部', exact: true }).click();
  await expect(page.locator('.entity-details h2')).toHaveText('图书馆');
  await expect(page.locator('.map-view')).toHaveAttribute('data-camera-status', 'idle');
  await expect(page.getByLabel('地点材质')).toContainText('校园实拍');
  await page.screenshot({ path: 'test-results/materials-library.png' });
  await page.getByRole('button', { name: '关闭地点详情' }).click();
  await page.getByRole('button', { name: '筛选餐饮', exact: true }).click();
  await page.getByRole('button', { name: '图层设置', exact: true }).click();
  await page.getByRole('checkbox', { name: '名称标注', exact: true }).uncheck();
  await page.getByRole('checkbox', { name: '名称标注', exact: true }).check();
  const after = await snapshot(page);
  expect(after.geometryBuilds).toBe(before.geometryBuilds);
  expect(after.geometryIds).toEqual(before.geometryIds);
});

test('quality and texture switching changes resources, not positions or IDs', async ({ page }) => {
  await ready(page);
  const before = await snapshot(page);
  await page.getByRole('button', { name: '图层设置', exact: true }).click();
  await page.getByRole('combobox', { name: '画面质量', exact: true }).selectOption('light');
  await expect(page.locator('.scene-canvas')).toHaveAttribute('data-quality', 'light');
  await expect(page.locator('.scene-canvas')).toHaveAttribute('data-textures-ready', 'true');
  await page.getByRole('checkbox', { name: /^表面贴图/ }).uncheck();
  await page.getByRole('checkbox', { name: /^表面贴图/ }).check();
  await page.getByRole('combobox', { name: '画面质量', exact: true }).selectOption('standard');
  await expect(page.locator('.scene-canvas')).toHaveAttribute('data-quality', 'standard');
  await expect(page.locator('.scene-canvas')).toHaveAttribute('data-textures-ready', 'true');
  const after = await snapshot(page);
  expect(after.geometryBuilds).toBe(before.geometryBuilds);
  expect(after.geometryIds).toEqual(before.geometryIds);
  await page.getByRole('button', { name: '关闭图层设置' }).click();
  await page.screenshot({ path: 'test-results/materials-overview.png' });
});

test('a missing texture leaves the map usable and can be retried', async ({ page }) => {
  const errors: string[] = [];
  page.on('pageerror', error => errors.push(error.message));
  await page.route('**/asphalt-color.webp', route => route.fulfill({ status: 404, body: 'missing test asset' }));
  await page.goto('/?inspect=1');
  await expect(page.locator('.material-status')).toContainText('1项贴图未加载');
  await expect(page.locator('.map-marker').first()).toBeVisible();
  const before = await snapshot(page);
  await page.unroute('**/asphalt-color.webp');
  await page.locator('.material-status').getByRole('button', { name: '重试' }).click();
  await expect(page.locator('.material-status')).toHaveAttribute('data-ready', 'true');
  expect((await snapshot(page)).geometryBuilds).toBe(before.geometryBuilds);
  expect(errors).toEqual([]);
});

test('labels stay on the rendered building anchor during pan, rotation and zoom; toggle preserves selection', async ({ page }) => {
  await ready(page);
  const before = await snapshot(page);
  await page.evaluate(() => {
    const probe = { frames: 0, samples: 0, maxError: 0, done: false };
    (window as unknown as { labelProbe: typeof probe }).labelProbe = probe;
    let previousFrame = -1;
    let previousMatrix = '';
    const sample = () => {
      if (probe.done) return;
      const rendered = window.__UESTC_RENDER__;
      const projection = rendered?.annotationCamera;
      if (rendered && projection && rendered.frames !== previousFrame) {
        previousFrame = rendered.frames;
        const signature = projection.viewProjection.join(',');
        if (signature !== previousMatrix) {
          previousMatrix = signature;
          const origin = document.querySelector('.annotations')!.getBoundingClientRect();
          const m = projection.viewProjection;
          let checked = 0;
          document.querySelectorAll<HTMLButtonElement>('.map-marker[data-anchor]').forEach(element => {
            if (getComputedStyle(element).visibility !== 'visible') return;
            const [x, y, z] = element.dataset.anchor!.split(',').map(Number);
            const w = m[3] * x + m[7] * y + m[11] * z + m[15];
            const expectedX = ((m[0] * x + m[4] * y + m[8] * z + m[12]) / w + 1) * projection.width / 2;
            const expectedY = (1 - (m[1] * x + m[5] * y + m[9] * z + m[13]) / w) * projection.height / 2;
            const rect = element.getBoundingClientRect();
            const error = Math.hypot(rect.x + rect.width / 2 - origin.x - expectedX, rect.bottom - origin.y - expectedY);
            probe.maxError = Math.max(probe.maxError, error);
            probe.samples++; checked++;
          });
          if (checked) probe.frames++;
        }
      }
      requestAnimationFrame(sample);
    };
    requestAnimationFrame(sample);
  });
  const canvas = (await page.locator('canvas').boundingBox())!;
  await page.mouse.move(canvas.x + 90, canvas.y + 300);
  await page.mouse.down();
  await page.mouse.move(canvas.x + 280, canvas.y + 350, { steps: 35 });
  await page.mouse.up();
  await page.mouse.down({ button: 'right' });
  await page.mouse.move(canvas.x + 380, canvas.y + 410, { steps: 30 });
  await page.mouse.up({ button: 'right' });
  await page.mouse.wheel(0, -240);
  await page.waitForTimeout(1000);
  const measured = await page.evaluate(() => {
    const probe = (window as unknown as { labelProbe: { frames: number; samples: number; maxError: number; done: boolean } }).labelProbe;
    probe.done = true;
    return probe;
  });
  expect(measured.frames).toBeGreaterThan(15);
  expect(measured.samples).toBeGreaterThan(100);
  expect(measured.maxError).toBeLessThan(1);
  console.log('Rendered label alignment:', measured);

  await page.getByRole('combobox').fill('图书馆');
  await page.locator('.result-main[title="图书馆 · 图书阅览"]').click();
  await expect(page.locator('.map-view')).toHaveAttribute('data-camera-status', 'idle');
  const target = await page.locator('.map-view').getAttribute('data-camera-target');
  const toggle = page.getByRole('button', { name: '建筑标签', exact: true });
  await toggle.click();
  await expect(toggle).toHaveAttribute('aria-pressed', 'false');
  await expect(page.locator('.map-marker')).toHaveCount(0);
  await expect(page.locator('.entity-details h2')).toHaveText('图书馆');
  await expect(page.locator('.map-view')).toHaveAttribute('data-camera-target', target!);
  await page.screenshot({ path: 'test-results/labels-hidden.png' });
  await toggle.click();
  await expect(page.locator('.map-marker.selected')).toBeVisible();
  await page.screenshot({ path: 'test-results/labels-desktop.png' });
  expect((await snapshot(page)).geometryIds).toEqual(before.geometryIds);
});
