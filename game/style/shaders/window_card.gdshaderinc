// window_card.gdshaderinc：窗卡与门厅卡的主线版程序化内容（WP-51b，contract-NN 申请登记）。
// 规格：WLD-INTERIORS；ART-FIDELITY（亮窗、门厅卡不是空盒子）；SYS-TIME（亮灭仍归 lamp_window_on）。
// 窗卡号 = VF-B 玻璃的窗位类型字节（COLOR.a），也是 window_cards 表的行号；表由 Campus.Gen.Interior.Shell.Cards.WindowCardTable
// 打包（RGBA32F，6 × 32），一行一种卡：进深权重、隔墙、侧墙距离、近层与背墙剪影、侧墙内容、窗帘或百叶、日间明度、夜间增益、吊顶、点缀。
// 主线版按卡号程序化画出各层（与 veg_card 的叶卡同一做法）；细节版换成 Texture2DArray 的同号层（表 x4.w 起的层号）。
// 由 toon_glass 在 toon.gdshaderinc 之后引入；只用统一控制流里算好的 aa（每像素对应的窗面米数），不在分支里取导数。
// 不读 TIME、不写色值；颜色只来自调用方传入的房间基调、灯色与调色板。

#ifndef CAMPUS_WINDOW_CARD_INCLUDED
#define CAMPUS_WINDOW_CARD_INCLUDED

const float WCARD_TAG = 51.0;
const int WCARD_DEFAULT = 5;           // 窗位类型 0（未定）按办公画

// 枚举与 WindowCardKinds.cs 一致
const int WC_F_DESKS = 1;
const int WC_F_BENCH = 2;
const int WC_F_SHELF = 3;
const int WC_F_BUNK = 4;
const int WC_F_STALL = 5;
const int WC_F_TABLES = 6;
const int WC_F_COUNTER = 7;
const int WC_F_EQUIP = 8;
const int WC_F_STAIR = 9;
const int WC_F_TURNSTILE = 10;
const int WC_F_ELEVATOR = 11;
const int WC_S_BOARD = 1;
const int WC_S_BUNK = 2;
const int WC_S_DOORS = 3;
const int WC_S_SHELF = 4;
const int WC_SCR_CURTAIN = 1;
const int WC_SCR_VENETIAN = 2;
const int WC_SCR_FROSTED = 3;
const int WC_SCR_ROLLER = 4;
const int WC_C_STRIPS = 1;
const int WC_C_PANELS = 2;
const int WC_C_SINGLE = 3;
const int WC_C_DOWN = 4;
const int WC_C_TRUSS = 5;
const int WC_A_EXIT = 1;
const int WC_A_AMBER = 2;
const int WC_A_BEDCURTAIN = 3;
const int WC_A_LIGHTBOX = 4;
const int WC_A_WATER = 5;
const int WC_A_INDICATOR = 6;
const int WC_A_LOBBY = 7;

// 观感参数（可调，主会话评审定值）
const float WC_K_BACK = 0.80;       // 各面明度：背墙、侧墙、地面、顶棚
const float WC_K_SIDE = 0.66;
const float WC_K_FLOOR = 0.50;
const float WC_K_CEIL = 0.88;
const float WC_K_FURN = 0.38;        // 剪影（家具、柜、床架）
const float WC_K_DEEP = 0.78;        // 亮灯时进深尽头压暗到的比例
const float WC_K_DEEP_DAY = 0.55;    // 白天关灯：靠窗亮、里面暗，读出进深
const float WC_FIX_GAIN = 1.55;      // 亮着的灯具相对房间基调
const float WC_FIX_DAY = 1.45;       // 白天关着的灯具（灯罩比顶棚浅，受天光）
const float WC_EXIT_GLOW = 0.9;      // 出口绿灯
const float WC_DETAIL_FADE0 = 0.07;  // 每像素窗面米数到这一档开始收细节，防远处闪烁
const float WC_DETAIL_FADE1 = 0.22;

struct WCard {
	float c3;      // 进深取 3 m 的累计权重
	float c45;     // 取 3 或 4.5 m 的累计权重
	float part;    // 隔墙概率
	float side;    // 侧墙在窗边外多远（m）
	int near;
	float near_z;
	int far;
	int sidek;
	int screen;
	float scr_p;
	float cov0;
	float cov1;
	float dayl;
	float nightg;
	int ceil;
	int accent;
	float sill;
	float rise;
};

WCard wcard_default() {
	// 表未上传或没有有效默认行：沿用已批办公卡常量。
	return WCard(0.45, 1.0, 0.3, 0.45, WC_F_DESKS, 1.4, WC_F_SHELF, WC_S_SHELF, WC_SCR_VENETIAN, 0.6, 0.15, 0.7,
			1.0, 0.95, WC_C_PANELS, 0, 0.9, 0.4);
}

WCard wcard_fetch(int kind) {
	// 未绑定 sampler 的默认纹理可能只有 1×1；任何 texelFetch 前先核尺寸。
	ivec2 size = textureSize(window_cards, 0);
	if (size.x != 6 || size.y != 32) {
		return wcard_default();
	}
	int row = kind <= 0 || kind > 31 ? WCARD_DEFAULT : kind;
	vec4 x5 = texelFetch(window_cards, ivec2(5, row), 0);
	if (x5.w != WCARD_TAG) {
		row = WCARD_DEFAULT;
		x5 = texelFetch(window_cards, ivec2(5, row), 0);
	}
	if (x5.w != WCARD_TAG) {
		return wcard_default();
	}
	vec4 x0 = texelFetch(window_cards, ivec2(0, row), 0);
	vec4 x1 = texelFetch(window_cards, ivec2(1, row), 0);
	vec4 x2 = texelFetch(window_cards, ivec2(2, row), 0);
	vec4 x3 = texelFetch(window_cards, ivec2(3, row), 0);
	vec4 x4 = texelFetch(window_cards, ivec2(4, row), 0);
	return WCard(x0.x, x0.y, x0.z, x0.w, int(x1.x + 0.5), x1.y, int(x1.z + 0.5), int(x1.w + 0.5),
			int(x2.x + 0.5), x2.y, x2.z, x2.w, x3.x, x3.y, int(x3.z + 0.5), int(x3.w + 0.5), x4.x, x4.y);
}

float wc_h(uint key, uint salt) {
	return lamp_hash01(lamp_hash_u(key) ^ (salt * 2654435761u));
}

// 一维带：lo..hi 之间为 1，边缘按 w 抗锯齿
float wc_band(float x, float lo, float hi, float w) {
	return smoothstep(lo - w, lo + w, x) * (1.0 - smoothstep(hi - w, hi + w, x));
}

float wc_box(vec2 p, vec2 lo, vec2 hi, float w) {
	return wc_band(p.x, lo.x, hi.x, w) * wc_band(p.y, lo.y, hi.y, w);
}

// 周期带：周期 per 里 [lo, hi) 一段为 1
float wc_rep(float x, float per, float lo, float hi, float w) {
	float f = x - per * floor(x / per);
	return max(wc_band(f, lo, hi, w), wc_band(f + per, lo, hi, w));
}

// 近层与背墙的剪影：q = (沿窗面的 m，离地面的 m)，返回 (覆盖, 亮点)；亮点用于转门绿灯、设备指示灯、灯箱
vec2 wc_furniture(int kind, vec2 q, float room_w, float h, float w) {
	float c = 0.0;
	float g = 0.0;
	if (kind == WC_F_DESKS) {
		// 课桌一排：桌面、桌腿、椅背
		c = wc_band(q.y, 0.70, 0.77, w) * wc_rep(q.x + h * 1.3, 1.3, 0.0, 1.05, w);
		c = max(c, wc_band(q.y, 0.0, 0.70, w) * wc_rep(q.x + h * 1.3, 1.3, 0.02, 0.07, w));
		c = max(c, wc_band(q.y, 0.42, 0.88, w) * wc_rep(q.x + h * 1.3 + 0.35, 1.3, 0.0, 0.42, w) * 0.85);
	} else if (kind == WC_F_BENCH) {
		// 实验台加台上试剂架
		c = wc_band(q.y, 0.0, 0.88, w) * (1.0 - 0.6 * wc_rep(q.x, 1.8, 0.7, 1.1, w) * wc_band(q.y, 0.0, 0.6, w));
		c = max(c, wc_band(q.y, 1.30, 1.36, w) + wc_band(q.y, 1.70, 1.76, w));
		c = max(c, wc_band(q.y, 0.88, 1.76, w) * wc_rep(q.x, 1.8, 0.0, 0.05, w));
	} else if (kind == WC_F_SHELF) {
		// 柜与书架：一组组高柜，层板线留空
		float unit = wc_rep(q.x + h * 2.0, 2.2, 0.0, 1.8, w);
		float top = 1.9 + 0.25 * step(0.5, fract(h * 7.0 + floor((q.x + h * 2.0) / 2.2) * 0.37));
		c = unit * wc_band(q.y, 0.0, top, w) * (1.0 - 0.45 * wc_rep(q.y, 0.38, 0.30, 0.38, w));
	} else if (kind == WC_F_BUNK) {
		// 上床下桌正面：床板、桌面、两根立柱
		float unit = wc_rep(q.x + h * 2.0, 2.4, 0.0, 2.0, w);
		c = unit * (wc_band(q.y, 1.55, 1.78, w) + wc_band(q.y, 0.72, 0.78, w));
		c = max(c, wc_band(q.y, 0.0, 2.2, w) * (wc_rep(q.x + h * 2.0, 2.4, 0.0, 0.05, w) + wc_rep(q.x + h * 2.0, 2.4, 1.95, 2.0, w)));
	} else if (kind == WC_F_STALL) {
		// 隔间：门板下留缝
		c = wc_band(q.y, 0.15, 1.85, w) * (1.0 - 0.7 * wc_rep(q.x, 1.0, 0.0, 0.04, w));
	} else if (kind == WC_F_TABLES) {
		c = wc_band(q.y, 0.70, 0.76, w) * wc_rep(q.x + h, 2.0, 0.0, 1.5, w);
		c = max(c, wc_band(q.y, 0.0, 0.70, w) * wc_rep(q.x + h, 2.0, 0.7, 0.8, w));
		c = max(c, wc_band(q.y, 0.0, 0.45, w) * wc_rep(q.x + h + 0.2, 0.5, 0.0, 0.3, w) * 0.8);
	} else if (kind == WC_F_COUNTER) {
		// 打饭窗口或收银台，上方一条灯箱
		c = wc_band(q.y, 0.0, 1.0, w);
		g = wc_band(q.y, 2.05, 2.45, w) * wc_rep(q.x + h * 3.0, 3.0, 0.2, 2.6, w);
	} else if (kind == WC_F_EQUIP) {
		// 设备柜高低错落，上方一条管线
		float cell = floor((q.x + h * 3.0) / 1.4);
		float top = 1.2 + 0.9 * wc_h(uint(int(cell) + 4096), uint(h * 1024.0));
		c = wc_rep(q.x + h * 3.0, 1.4, 0.0, 1.2, w) * wc_band(q.y, 0.0, top, w);
		c = max(c, wc_band(q.y, 2.35, 2.5, w));
		g = wc_box(vec2(fract((q.x + h * 3.0) / 1.4) * 1.4, q.y), vec2(0.2, top - 0.3), vec2(0.27, top - 0.24), w);
	} else if (kind == WC_F_STAIR) {
		// 梯段斜着穿过窗：梯板、栏杆扶手一条线
		float y0 = 0.62 * (q.x + 0.3) - 0.2;
		c = wc_band(q.y - y0, -0.22, 0.0, w);
		c = max(c, wc_band(q.y - y0, 0.88, 0.94, w));
		c = max(c, wc_band(q.y - y0, 0.0, 0.92, w) * wc_rep(q.x, 0.9, 0.0, 0.03, w));
	} else if (kind == WC_F_TURNSTILE) {
		// 一排闸机，顶上待机绿灯
		float lane = wc_rep(q.x + 0.3, 0.95, 0.0, 0.22, w);
		c = lane * wc_band(q.y, 0.0, 1.02, w);
		g = wc_rep(q.x + 0.3, 0.95, 0.06, 0.16, w) * wc_band(q.y, 0.94, 1.0, w);
	} else if (kind == WC_F_ELEVATOR) {
		// 两扇电梯门（石材门套），门上楼层显示
		float cx = room_w * 0.5;
		float doors = wc_box(q, vec2(cx - 2.0, 0.0), vec2(cx - 0.8, 2.3), w) + wc_box(q, vec2(cx + 0.8, 0.0), vec2(cx + 2.0, 2.3), w);
		float seam = wc_band(q.x, cx - 1.42, cx - 1.38, w) + wc_band(q.x, cx + 1.38, cx + 1.42, w);
		c = clamp(doors, 0.0, 1.0) * (1.0 - 0.6 * seam) * 0.75;
		g = wc_box(q, vec2(cx - 1.55, 2.48), vec2(cx - 1.25, 2.6), w) + wc_box(q, vec2(cx + 1.25, 2.48), vec2(cx + 1.55, 2.6), w);
	}
	return vec2(clamp(c, 0.0, 1.0), clamp(g, 0.0, 1.0));
}

// 侧墙内容：q = (离窗的进深 m，离地面的 m)；返回 (覆盖, 点缀权重)
vec2 wc_side(int kind, vec2 q, float depth, float h, float w) {
	float c = 0.0;
	float a = 0.0;
	if (kind == WC_S_BOARD) {
		// 推拉黑板和投影幕，在侧墙中段
		c = wc_box(q, vec2(1.2, 0.9), vec2(depth - 0.8, 2.15), w);
	} else if (kind == WC_S_BUNK) {
		// 上床下桌：床板、床帘、桌面、钢架立柱
		float z0 = 0.35;
		float z1 = min(depth - 0.3, 2.45);
		c = wc_box(q, vec2(z0, 1.55), vec2(z1, 1.78), w);
		c = max(c, wc_box(q, vec2(z0, 0.72), vec2(z1, 0.78), w));
		c = max(c, wc_band(q.x, z0, z0 + 0.05, w) * wc_band(q.y, 0.0, 2.2, w));
		c = max(c, wc_band(q.x, z1 - 0.05, z1, w) * wc_band(q.y, 0.0, 2.2, w));
		a = wc_box(q, vec2(z0 + 0.1, 1.8), vec2(z1 - 0.1, 2.45), w) * step(0.35, h);
	} else if (kind == WC_S_DOORS) {
		// 走廊两侧的门：门扇略暗，门头一条
		c = wc_rep(q.x + h * 4.0, 4.2, 0.0, 1.0, w) * wc_band(q.y, 0.0, 2.1, w);
	} else if (kind == WC_S_SHELF) {
		c = wc_rep(q.x + h * 2.0, 2.4, 0.4, 2.0, w) * wc_band(q.y, 0.0, 1.95, w) * (1.0 - 0.4 * wc_rep(q.y, 0.4, 0.32, 0.4, w));
	}
	return vec2(clamp(c, 0.0, 1.0), a);
}

// 吊顶灯具：q = (沿窗面的 m，离窗的进深 m)，返回灯具覆盖
float wc_ceiling(int kind, vec2 q, float x0, float x1, float depth, float w) {
	float mid = 0.5 * (x0 + x1);
	if (kind == WC_C_STRIPS) {
		return wc_rep(q.x - mid + 0.6, 2.4, 0.0, 0.14, w) * wc_band(q.y, 0.8, depth - 0.4, w);
	}
	if (kind == WC_C_PANELS) {
		float grid = wc_rep(q.x, 0.6, 0.0, 0.025, w) + wc_rep(q.y, 0.6, 0.0, 0.025, w);
		float panel = wc_rep(q.x - mid, 2.4, 0.05, 1.15, w) * wc_rep(q.y, 1.8, 0.65, 1.15, w);
		return clamp(panel - 0.15 * grid, -0.15, 1.0);
	}
	if (kind == WC_C_SINGLE) {
		return wc_box(q, vec2(mid - 0.3, depth * 0.5 - 0.3), vec2(mid + 0.3, depth * 0.5 + 0.3), w);
	}
	if (kind == WC_C_DOWN) {
		float zr = q.y - 1.6 * floor(q.y / 1.6);
		float d = length(vec2(q.x - mid, zr - 0.8));
		return 1.0 - smoothstep(0.12 - w, 0.12 + w, d);
	}
	if (kind == WC_C_TRUSS) {
		float chord = wc_rep(q.y, 6.0, 0.0, 0.18, w);
		float lamp = 1.0 - smoothstep(0.28 - w, 0.28 + w, length(vec2(q.x - mid - 3.0 * floor((q.x - mid) / 3.0 + 0.5), q.y - 6.0 * floor(q.y / 6.0) - 3.0)));
		return lamp - 0.6 * chord;
	}
	return 0.0;
}

// 一间房的颜色。
//   puv：窗格内位置（0..1），pane：窗格名义尺寸（m），rd：视线（窗面基：x 右、y 上、z 进房间）
//   key：窗号哈希键；tone：房间基调（白天用玻璃底色推、亮窗用灯色）；fix：灯具色；scr：窗帘、百叶的颜色
//   lit：灯开着的程度（0..1，影响灯具、床帘与灯箱）；night：夜的程度；aa：每像素窗面米数
//   force：窗覆盖的窗帘位（toon_window_curtain：0 按哈希、1 无帘、2 拉上）
//   glow：输出不参与明暗的点缀光（出口绿灯、指示灯、门厅卡的闸机绿灯）
vec3 wcard_room(WCard c, vec2 puv, vec2 pane, vec3 rd, uint key, vec3 tone, vec3 fix, vec3 scr, float lit, float night,
		float aa, int force, bool screen_enabled, inout vec3 glow) {
	vec3 base = tone;
	float hd = wc_h(key, 1u);
	float depth = hd < c.c3 ? 3.0 : (hd < c.c45 ? 4.5 : 6.0);
	vec3 p0 = vec3(puv * pane, 0.0);
	float x0 = -c.side;
	float x1 = pane.x + c.side;
	// 隔墙：窗面某处立一道墙，左右两间各自进深
	uint rkey = key;
	if (wc_h(key, 2u) < c.part) {
		float xp = pane.x * (0.3 + 0.4 * wc_h(key, 3u));
		if (p0.x < xp) {
			x1 = xp - 0.06;
			rkey = key * 2u + 1u;
		} else {
			x0 = xp + 0.06;
			rkey = key * 2u + 2u;
		}
		float hd2 = wc_h(rkey, 1u);
		depth = hd2 < c.c3 ? 3.0 : (hd2 < c.c45 ? 4.5 : 6.0);
	}
	vec3 bmin = vec3(x0, -c.sill, 0.0);
	vec3 bmax = vec3(x1, pane.y + c.rise, depth);
	float tz = (bmax.z - p0.z) / max(rd.z, 1e-4);
	float tx = abs(rd.x) > 1e-5 ? ((rd.x > 0.0 ? bmax.x : bmin.x) - p0.x) / rd.x : 1e6;
	float ty = abs(rd.y) > 1e-5 ? ((rd.y > 0.0 ? bmax.y : bmin.y) - p0.y) / rd.y : 1e6;
	float t = max(min(tz, min(tx, ty)), 0.0);
	vec3 hp = p0 + rd * t;
	float room_w = x1 - x0;
	float w = aa * (1.0 + 0.3 * hp.z);
	float hv = wc_h(rkey, 4u);
	float k;
	float fixture = 0.0;
	float accent = 0.0;
	if (t == tz) {
		k = WC_K_BACK;
		vec2 q = vec2(hp.x - x0, hp.y - bmin.y);
		vec2 f = wc_furniture(c.far, q, room_w, hv, w);
		k = mix(k, WC_K_FURN * 1.2, f.x);
		fixture = max(fixture, f.y);
		if (c.accent == WC_A_EXIT || c.accent == WC_A_LOBBY) {
			// 背墙门头的出口绿灯（走廊、楼梯间、门厅）
			float ex = 0.5 * room_w + (hv - 0.5) * 0.6 * room_w;
			float e = wc_box(q, vec2(ex - 0.2, 2.25), vec2(ex + 0.2, 2.42), w);
			glow += pal_cur(PAL_SIGN_GREEN) * e * WC_EXIT_GLOW * mix(0.35, 1.0, night);
		}
		if (c.accent == WC_A_LOBBY) {
			// 楼层索引牌：侧边一块冷白发光板
			float b = wc_box(q, vec2(0.25, 1.0), vec2(0.95, 2.1), w);
			fixture = max(fixture, b * 0.8);
		}
	} else if (t == ty) {
		if (rd.y > 0.0) {
			k = WC_K_CEIL;
			fixture = max(fixture, wc_ceiling(c.ceil, vec2(hp.x, hp.z), x0, x1, depth, w));
		} else {
			k = WC_K_FLOOR;
			if (c.accent == WC_A_WATER) {
				float rip = 0.5 + 0.5 * sin(hp.x * 3.1 + hp.z * 1.7 + hv * 6.0) * sin(hp.z * 2.3 - hp.x * 0.9);
				base = mix(tone, pal_cur(PAL_WATER_POOL_GLOW), 0.55);
				k = WC_K_FLOOR * (1.15 + 0.35 * rip * (1.0 - smoothstep(0.08, 0.3, w)));
			}
		}
	} else {
		k = WC_K_SIDE;
		vec2 sq = vec2(hp.z, hp.y - bmin.y);
		vec2 s = wc_side(c.sidek, sq, depth, wc_h(rkey, (rd.x > 0.0 ? 5u : 6u)), w);
		float dark = c.sidek == WC_S_BOARD ? 0.42 : (c.sidek == WC_S_DOORS ? 0.78 : 1.0);
		k = mix(k, c.sidek == WC_S_BOARD || c.sidek == WC_S_DOORS ? WC_K_SIDE * dark : WC_K_FURN * 1.15, s.x);
		accent = s.y;
	}
	k *= mix(1.0, mix(WC_K_DEEP_DAY, WC_K_DEEP, lit), clamp(hp.z / 6.0, 0.0, 1.0));
	vec3 col = base * k;
	// 灯具：开灯时亮、白天是浅色灯罩
	col = mix(col, mix(tone * WC_FIX_DAY, fix, lit), clamp(fixture, 0.0, 1.0));
	// 床帘：白天是彩色布面，开灯时整块透出暖光（SYS-TIME"宿舍床帘透光"）
	if (c.accent == WC_A_BEDCURTAIN && accent > 0.0) {
		float hc = wc_h(rkey, 7u);
		vec3 cloth = hc < 0.34 ? mix(tone, pal_cur(PAL_WATER_POOL_GLOW), 0.35)
				: (hc < 0.67 ? mix(tone, pal_cur(PAL_SIGN_GREEN), 0.22) : tone * 1.08);
		col = mix(col, mix(cloth * 0.9, fix * 0.85, lit * step(0.4, wc_h(rkey, 8u))), accent);
	}
	// 近层剪影（课桌成排时再往里摆两排）
	if (c.near > 0) {
		int rows = c.near == WC_F_DESKS || c.near == WC_F_TABLES ? 3 : 1;
		for (int i = 0; i < 3; i++) {
			if (i >= rows) {
				break;
			}
			float zn = c.near_z + float(i) * 1.25;
			if (zn > depth - 0.4) {
				break;
			}
			float tn = zn / max(rd.z, 1e-4);
			if (tn < t) {
				vec3 pn = p0 + rd * tn;
				if (pn.x > x0 && pn.x < x1 && pn.y > bmin.y && pn.y < bmax.y) {
					vec2 f = wc_furniture(c.near, vec2(pn.x - x0, pn.y - bmin.y), room_w, wc_h(rkey, 9u + uint(i)), aa * (1.0 + 0.3 * zn));
					col = mix(col, tone * WC_K_FURN * mix(1.0, WC_K_DEEP, zn / 6.0), f.x);
					if (f.y > 0.0) {
						glow += pal_cur(PAL_SIGN_GREEN) * f.y * (c.accent == WC_A_LOBBY ? 0.8 : 0.35) * mix(0.4, 1.0, night);
					}
					break;
				}
			}
		}
	}
	// 背墙或近层的亮点：设备指示灯、灯箱
	if (fixture > 0.0 && c.accent == WC_A_INDICATOR) {
		glow += pal_cur(PAL_SIGN_GREEN) * 0.25 * night * fixture;
	}
	// 远处收细节，只留平均明度，防闪烁
	float detail = 1.0 - smoothstep(WC_DETAIL_FADE0, WC_DETAIL_FADE1, aa);
	col = mix(tone * 0.74, col, detail);
	// 窗面上的窗帘、百叶、磨砂、卷帘
	int screen = !screen_enabled || force == 1 ? 0 : (force == 2 && c.screen == 0 ? WC_SCR_CURTAIN : c.screen);
	if (screen > 0 && (force == 2 || wc_h(key, 10u) < c.scr_p)) {
		float cov = force == 2 ? 1.0 : mix(c.cov0, c.cov1, wc_h(key, 11u));
		vec2 pm = puv * pane;
		float s = 0.0;
		float fold = 1.0;
		if (screen == WC_SCR_CURTAIN) {
			float hs = wc_h(key, 12u);
			float wl = hs < 0.4 ? cov : (hs < 0.7 ? cov * 0.5 : 0.0);
			float wr = hs < 0.4 ? 0.0 : (hs < 0.7 ? cov * 0.5 : cov);
			s = max(1.0 - smoothstep(wl * pane.x - aa, wl * pane.x + aa, pm.x), smoothstep((1.0 - wr) * pane.x - aa, (1.0 - wr) * pane.x + aa, pm.x));
			s *= step(0.001, wl + wr);
			fold = 0.9 + 0.1 * sin(pm.x * 26.0) * detail;
		} else if (screen == WC_SCR_VENETIAN) {
			float edge = (1.0 - cov) * pane.y;
			float down = smoothstep(edge - aa, edge + aa, pm.y);
			float slat = mix(wc_rep(pm.y, 0.07, 0.0, 0.05, aa), 0.72, smoothstep(0.012, 0.035, aa));
			s = down * mix(slat, 1.0, 0.25);
			fold = mix(0.95, 0.6, lit);   // 灯在百叶后面：叶片读成剪影
		} else if (screen == WC_SCR_FROSTED) {
			s = 1.0 - smoothstep(cov * pane.y - aa, cov * pane.y + aa, pm.y);
			fold = 1.0;
		} else if (screen == WC_SCR_ROLLER) {
			float edge = (1.0 - cov) * pane.y;
			s = smoothstep(edge - aa, edge + aa, pm.y);
			s = max(s, wc_band(pm.y, edge - 0.05, edge, aa));
			fold = 0.97;
		}
		col = mix(col, scr * fold, clamp(s, 0.0, 1.0));
	}
	return col;
}

#endif
