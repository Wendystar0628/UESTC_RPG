import { test, expect } from '@playwright/test';
import { mkdirSync, writeFileSync } from 'node:fs';

test('detailed campus: landmarks, inferred facades, source height and static landscape', async ({ page }) => {
  const errors: string[] = [];
  page.on('pageerror', error => errors.push(error.message));
  await page.setViewportSize({ width: 1920, height: 1080 });
  await page.goto('/?inspect=1');
  await expect(page.locator('.scene-canvas')).toHaveAttribute('data-textures-ready', 'true');
  await expect(page.locator('.map-view')).toHaveAttribute('data-camera-status', 'idle');
  mkdirSync('output/visual-upgrade', { recursive: true });
  await page.screenshot({ path: 'output/visual-upgrade/after-overview.png' });
  const normal = await page.evaluate(() => structuredClone(window.__UESTC_RENDER__!));
  expect(normal.landscape?.trees).toBeGreaterThan(900);
  expect(normal.landscape?.drawCalls).toBeLessThan(8);
  expect(normal.sportsMarkings?.courts).toBe(68);
  expect(normal.triangles).toBeLessThan(350_000);
  expect(normal.batches).toBeLessThan(190);
  for (const [name, filename] of [['图书馆', 'library'], ['学知苑10栋', 'dormitory'], ['体育馆', 'gym'], ['文科楼群', 'humanities'], ['主楼', 'main-building'], ['足球场', 'pitch']]) {
    await page.getByRole('combobox').fill(name);
    await page.locator(`.result-main[title^="${name} ·"]`).first().click();
    await expect(page.locator('.entity-details h2')).toHaveText(name);
    await expect(page.locator('.map-view')).toHaveAttribute('data-camera-status', 'idle');
    if (name === '体育馆') await expect(page.locator('.entity-details')).toContainText('26 m');
    if (name === '学知苑10栋') await expect(page.getByLabel('地点材质')).toContainText('示意');
    await page.screenshot({ path: `output/visual-upgrade/after-${filename}.png` });
    await page.getByRole('button', { name: '关闭地点详情' }).click();
  }
  await page.getByRole('button', { name: '图层设置', exact: true }).click();
  await page.getByRole('checkbox', { name: '绿地', exact: true }).uncheck();
  await page.getByRole('checkbox', { name: '绿地', exact: true }).check();
  await page.getByRole('checkbox', { name: /^仅显示来源楼高/ }).check();
  await expect.poll(async () => (await page.evaluate(() => window.__UESTC_RENDER__!.triangles))).toBeLessThan(normal.triangles / 2);
  await page.getByRole('checkbox', { name: /^仅显示来源楼高/ }).uncheck();
  await expect.poll(async () => (await page.evaluate(() => window.__UESTC_RENDER__!.triangles))).toBe(normal.triangles);
  const restored = await page.evaluate(() => structuredClone(window.__UESTC_RENDER__!));
  expect(restored.geometryIds).toEqual(normal.geometryIds);
  expect(errors).toEqual([]);
  writeFileSync('output/visual-upgrade/render-metrics.json', JSON.stringify({ viewport: [1920, 1080], normal, restored, pageErrors: errors }, null, 2));
});
