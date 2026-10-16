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
        self.assertLess(de(gold["wall"].shade, ok.hex_to_oklab("#8796B1")), 0.5)    # wall.shade（默认格 r 0.72，max-110 110d）
        self.assertLess(de(gold["wall"].shade_in, ok.hex_to_oklab("#C3A78C")), 0.5)  # 室内影（ART-STYLE §3.4）
        self.assertLess(de(gold["ginkgo"].shade, ok.hex_to_oklab("#B78102")), 1.0)
        self.assertLess(de(gold["ginkgo"].deep, ok.hex_to_oklab("#9A6C03")), 2.0)   # ginkgo.deep 校验值

    def _golden_material(self, token, hex_albedo, cls):
        f = next(x for x in self.grid["frames"]["autumn"] if x.id == "golden")
        return cs.derive_material(self.src, token, token, cls, ok.hex_to_oklab(hex_albedo), f)

    def test_interior_lit_raises_lightness_only(self):
        """ART-STYLE §3.4：室内受光（光斑目标色）只抬 L，上限 litLMax；C 取受光色的 C、不放大；色相不变。
        第 3 行反算着色器增益，toon 合成后正好落在目标色上；第 2 行没有增益项。"""
        ip = self.src.derive["interior"]
        checked = 0
        for (season, key), mats in self.grid["materials"].items():
            for token, m in mats.items():
                if "variantOf" in m.meta:
                    continue                                  # 变体在受光、影上做同一偏移，不重新推导
                L, C, h = ok.lab_to_lch(m.lit)
                patch = cs.interior_patch(self.src, m.lit)
                Lp, Cp, hp = ok.lab_to_lch(patch)
                msg = f"{season} {key} {token}"
                self.assertLessEqual(Lp, ip["litLMax"] + 1e-9, msg)
                self.assertAlmostEqual(Lp, min(ip["litLMax"], L * ip["litGain"]), places=9, msg=msg)
                self.assertLessEqual(Cp, C + 1e-9, msg)       # 色域内取原 C，越出色域只降彩度
                if C > 0.01 and Cp > 0.01:
                    self.assertAlmostEqual(ok.hue_diff(h, hp), 0.0, places=4, msg=msg)
                self.assertTrue(ok.in_gamut(ok.oklab_to_linear(m.lit_in)), msg)
                self.assertLess(de(cs.interior_composite(self.src, m.shade_in, m.lit_in), patch), 1e-4, msg)
                checked += 1
        self.assertGreater(checked, 1000)

    def test_interior_lit_high_chroma_colours_do_not_flare(self):
        """消防红、砖红：室内受光的 C 不超过原受光色的 C，L ≤ 0.9；着色器合成后的光斑色相与受光色相一致。"""
        for token, albedo, cls in (("equip.fireRed", "#A7424B", "metal"), ("brick", "#C2573D", "ground")):
            m = self._golden_material(token, albedo, cls)
            L, C, h = ok.lab_to_lch(m.lit)
            Lc, Cc, hc = ok.lab_to_lch(cs.interior_composite(self.src, m.shade_in, m.lit_in))
            self.assertLessEqual(Cc, C + 1e-6, token)
            self.assertLessEqual(Lc, 0.9 + 1e-6, token)
            self.assertGreater(Lc, L, token)                  # 抬了 L
            self.assertAlmostEqual(ok.hue_diff(h, hc), 0.0, places=3, msg=token)
            self.assertLessEqual(ok.lab_to_lch(m.lit_in)[0], 0.9 + 1e-6, token)   # 第 3 行存的色在影与目标之间，L 同样不超 0.9

    def test_interior_lit_low_chroma_colours_keep_their_chroma(self):
        """低彩度色：光斑色的 C、色相与受光色一致，只有 L 抬到 min(0.9, L × 增益)。"""
        for token in ("roof.grey", "frame.alu", "stone.beige", "wall", "glass"):
            m = self.grid["materials"][("autumn", "golden")][token]
            L, C, h = ok.lab_to_lch(m.lit)
            Lc, Cc, hc = ok.lab_to_lch(cs.interior_composite(self.src, m.shade_in, m.lit_in))
            self.assertAlmostEqual(Cc, C, delta=1e-4, msg=token)
            self.assertAlmostEqual(ok.hue_diff(h, hc), 0.0, delta=0.05, msg=token)
            self.assertAlmostEqual(Lc, min(0.9, L * 1.27), delta=1e-4, msg=token)

    def test_interior_shade_has_no_gain_term(self):
        """第 2 行室内影色没有光增益项：彩度只有影色规则的 ×1.1（上限 +chromaMulMax），不随窗光放大。"""
        cap = self.src.derive["shade"]["chromaMulMax"]
        for (season, key), mats in self.grid["materials"].items():
            warm_c = next(f for f in self.grid["frames"][season] if f.id == key).ab_colors["interior.warm"][0]
            for token, m in mats.items():
                if "variantOf" in m.meta:
                    continue
                Ca = ok.lab_to_lch(m.albedo)[1]
                Cs = ok.lab_to_lch(m.shade_in)[1]
                self.assertLessEqual(Cs, max(Ca + cap, warm_c) + 1e-9, f"{season} {key} {token}")

    def _veg_at(self, src, token, season, clock):
        frames = cs.season_frames(src)
        f = cs.frame_at(src, cs.day_curves(src, frames)[season], season, clock)
        name, cls, alb, ref = cs.vegetation_albedos(src, frames)[season][token]
        return cs.derive_material(src, token, name, cls, alb, f, meta={"vegetation": True}, hero=ref), ref

    def test_hero_guard_yellow_never_greener(self):
        """lit.heroGuard：银杏、黄色落叶在秋 21:00、19:00、06:30 的受光色相不往绿越过英雄帧色相，彩度不低于保底；
        4 季 × 12 帧上所有黄橙英雄色同样不越过。"""
        floor = self.src.derive["lit"]["heroGuard"]["chromaFloor"]
        ratio = self.src.derive["lit"]["chromaCap"]["ratio"]
        for token in ("ginkgo", "deciduous"):
            for clock in (21.0, 19.0, 6.5):
                m, ref = self._veg_at(self.src, token, "autumn", clock)
                _, C, h = ok.lab_to_lch(m.lit)
                msg = f"{token} {clock}"
                self.assertTrue(ref.guard, msg)
                self.assertLessEqual(ok.hue_diff(ref.h_ref, h), 1e-9, msg)
                self.assertGreaterEqual(C, min(floor * ref.c_ref, math.hypot(m.shade[1], m.shade[2]) / ratio) - 1e-9, msg)
        for (season, key), mats in self.grid["materials"].items():
            for token, (_, _, _, ref) in self.grid["vegetation"][season].items():
                if ref.guard:
                    h = ok.lab_to_lch(mats[token].lit)[2]
                    self.assertLessEqual(ok.hue_diff(ref.h_ref, h), 1e-9, f"{season} {key} {token}")

    def test_hero_guard_ginkgo_night_value(self):
        """秋 21:00 银杏受光：保底被本帧 chromaCap 限住，落在 #937F4F 附近。"""
        m, _ = self._veg_at(self.src, "ginkgo", "autumn", 21.0)
        self.assertEqual(ok.oklab_to_hex(m.lit), "#937F50")

    def test_hero_guard_floor_within_chroma_cap(self):
        """彩度保底在任何时段都不越过 chromaCap：固有彩度 ≥ minAlbedoC 的黄橙英雄色，受光 C ≤ 影 C / ratio
        （4 季 × 12 关键帧，加每季 0:00–23:30 每半小时）。"""
        cap = self.src.derive["lit"]["chromaCap"]
        checked = 0

        def check(m, msg):
            if math.hypot(m.albedo[1], m.albedo[2]) >= cap["minAlbedoC"]:
                self.assertLessEqual(math.hypot(m.lit[1], m.lit[2]), math.hypot(m.shade[1], m.shade[2]) / cap["ratio"] + 1e-9, msg)
                return 1
            return 0

        for (season, key), mats in self.grid["materials"].items():
            for token, (_, _, _, ref) in self.grid["vegetation"][season].items():
                if ref.guard:
                    checked += check(mats[token], f"{season} {key} {token}")
        frames = self.grid["frames"]
        curves = cs.day_curves(self.src, frames)
        for season in cs.SEASONS:
            for i in range(48):
                f = cs.frame_at(self.src, curves[season], season, i / 2.0)
                for token, (name, cls, alb, ref) in self.grid["vegetation"][season].items():
                    if ref.guard:
                        m = cs.derive_material(self.src, token, name, cls, alb, f, meta={"vegetation": True}, hero=ref)
                        checked += check(m, f"{season} {i / 2.0} {token}")
        self.assertGreater(checked, 500)

    def test_hero_guard_leaves_other_tokens_unchanged(self):
        """不在黄橙类里的色名（季相锚点按该季英雄帧判），关掉护栏前后的推导逐值相同；黄橙类就是下面这份清单。"""
        import copy
        off = cs.Sources()
        off.derive = copy.deepcopy(off.derive)
        off.derive["lit"]["heroGuard"]["minHeroC"] = 10.0
        grid_off = cs.derive_grid(off)
        guarded = {(s, t) for s in cs.SEASONS for t, (_, _, _, ref) in self.grid["vegetation"][s].items() if ref.guard}
        expected = {("autumn", t) for t in ("ginkgo", "deciduous", "metaseq", "cherry", "crop", "reed", "lotus")}
        expected |= {("winter", t) for t in ("ginkgo", "reed")}
        expected |= {(s, t) for s in cs.SEASONS for t in ("bloom.yellow", "camphor.red")}
        self.assertEqual(guarded, expected)
        checked = 0
        for (season, key), mats in self.grid["materials"].items():
            for token, m in mats.items():
                base = m.meta.get("variantOf", token)
                if (season, base) in guarded:
                    continue
                b = grid_off["materials"][(season, key)][token]
                for w in ("lit", "shade", "shade_in", "lit_in", "deep"):
                    self.assertEqual(getattr(m, w), getattr(b, w), f"{season} {key} {token} {w}")
                checked += 1
        self.assertGreater(checked, 1000)
        for token in ("grass", "willow", "maple", "camphor", "bloom.capsule"):
            for clock in (21.0, 19.0, 6.5):
                self.assertEqual(self._veg_at(self.src, token, "autumn", clock)[0].lit,
                                 self._veg_at(off, token, "autumn", clock)[0].lit, f"{token} {clock}")

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
