"""WP-37 色彩脚本的快检：python -m unittest pipeline/colorscript/test_colorscript.py（项目 .venv）。"""

from __future__ import annotations

import json
import math
import os
import sys
import unittest

HERE = os.path.dirname(os.path.abspath(__file__))
if HERE not in sys.path:
    sys.path.insert(0, HERE)

import colorscript as cs  # noqa: E402
import oklab as ok  # noqa: E402
import selfcheck as sc  # noqa: E402


def de(p, q):
    return 100.0 * math.dist(p, q)


class ColorScriptTest(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.src = cs.Sources()
        cls.grid = cs.derive_grid(cls.src)

    def test_default_grid_matches_registered(self):
        gold = self.grid["materials"][("autumn", "golden")]
        self.assertLess(de(gold["wall"].lit, ok.hex_to_oklab("#FFE4BE")), 0.5)      # ART-STYLE §8.1 wall.lit
        self.assertLess(de(gold["wall"].shade, ok.hex_to_oklab("#A1A6BB")), 0.5)    # wall.shade
        self.assertLess(de(gold["wall"].shade_in, ok.hex_to_oklab("#C3A78C")), 0.5)  # 室内影（ART-STYLE §3.4）
        self.assertLess(de(gold["ginkgo"].shade, ok.hex_to_oklab("#B78102")), 1.0)
        self.assertLess(de(gold["ginkgo"].deep, ok.hex_to_oklab("#9A6C03")), 2.0)   # ginkgo.deep 校验值

    def test_default_state_is_golden_keyframe(self):
        """秋 17:30 沿一天插值的结果逐值等于黄金关键帧（默认状态"落金初灯"）。"""
        curve = cs.day_curves(self.src, self.grid["frames"])["autumn"]
        f = cs.frame_at(self.src, curve, "autumn", 17.5)
        g = next(x for x in self.grid["frames"]["autumn"] if x.id == "golden")
        self.assertAlmostEqual(f.r, g.r, places=4)
        self.assertLess(de(f.key_color, g.key_color), 0.05)
        for i in range(5):
            self.assertLess(de(f.sky_sun[i], g.sky_sun[i]), 0.05)
            self.assertLess(de(f.sky_anti[i], g.sky_anti[i]), 0.05)
        h, side, _ = cs.sun_altitude(17.5, "autumn")
        self.assertAlmostEqual(h, 5.48, delta=0.1)
        self.assertAlmostEqual(cs.sun_azimuth(17.5, "autumn"), 241.0, delta=0.1)

    def test_curve_hits_every_anchor(self):
        curves = cs.day_curves(self.src, self.grid["frames"])
        for s in cs.SEASONS:
            for k in self.grid["frames"][s]:
                f = cs.frame_at(self.src, curves[s], s, k.clock)
                self.assertAlmostEqual(f.r, k.r, places=3, msg=f"{s} {k.id}")
                self.assertLess(de(f.sky_sun[4], k.sky_sun[4]), 0.1, msg=f"{s} {k.id}")

    def test_no_overshoot(self):
        curve = cs.day_curves(self.src, self.grid["frames"])["autumn"]
        rs = [k.r for k in self.grid["frames"]["autumn"]]
        for i in range(24 * 12):
            f = cs.frame_at(self.src, curve, "autumn", i / 12.0)
            self.assertGreaterEqual(f.r, min(rs) - 1e-9)
            self.assertLessEqual(f.r, max(rs) + 1e-9)

    def test_selfcheck_has_no_failures(self):
        res = sc.run(self.src, self.grid)
        fails = [r for r in res if r[0] == "FAIL"]
        self.assertEqual(fails, [], sc.format_report(fails))

    def test_clear_weather_is_identity(self):
        g = next(x for x in self.grid["frames"]["autumn"] if x.id == "golden")
        f = cs.apply_weather(self.src, g, cs.clear_weather())
        self.assertEqual(f.r, g.r)
        self.assertEqual(f.sky_sun, g.sky_sun)
        wv = cs.clear_weather()
        wv.update({"Overcast": 1.0, "Cloud": 1.0})
        o = cs.apply_weather(self.src, g, wv)
        self.assertGreaterEqual(o.r, 0.92 - 1e-9)                   # max(r, 0.88 + 0.04g)
        self.assertAlmostEqual(o.intensity, g.intensity * 0.5, places=6)   # 阴天主光 −1.0 EV
        for c in o.sky_sun:
            L, C, _ = ok.lab_to_lch(c)
            self.assertGreaterEqual(L, 0.72 - 1e-9)                 # 阴天天色 L ≥ 0.72
            self.assertGreaterEqual(C, 0.02 - 1e-9)                 # 禁止中性灰

    def test_palette_probe_shape(self):
        curve = cs.day_curves(self.src, self.grid["frames"])["autumn"]
        out = cs.sample_horizon(self.src, curve, "autumn")
        self.assertEqual(len(out), 24)
        for rgb in out:
            self.assertTrue(all(0.0 <= c <= 1.0 for c in rgb))

    def test_deterministic(self):
        import render_review as rr
        a = json.dumps(rr.grid_json(self.src, self.grid), ensure_ascii=False, sort_keys=True)
        b = json.dumps(rr.grid_json(cs.Sources(), cs.derive_grid(cs.Sources())), ensure_ascii=False, sort_keys=True)
        self.assertEqual(a, b)


if __name__ == "__main__":
    unittest.main()
