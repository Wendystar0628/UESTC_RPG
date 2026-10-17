"""把一个目录里的截图拼成一张缩略总览（每行 4 张，图下标名字），存成同目录的 总览.jpg。
用法：python -I -X utf8 contact_sheet.py <截图目录>
"""
import os
import sys

from PIL import Image, ImageDraw, ImageFont


def main():
    d = sys.argv[1]
    names = sorted(f for f in os.listdir(d) if f.endswith('.png'))
    if not names:
        print('没有截图，跳过总览')
        return
    w, h, cols, label = 640, 360, 4, 30
    rows = (len(names) + cols - 1) // cols
    sheet = Image.new('RGB', (cols * w, rows * (h + label)), 'white')
    try:
        font = ImageFont.truetype('C:/Windows/Fonts/msyh.ttc', 20)
    except OSError:
        font = ImageFont.load_default()
    draw = ImageDraw.Draw(sheet)
    for i, n in enumerate(names):
        x, y = (i % cols) * w, (i // cols) * (h + label)
        sheet.paste(Image.open(os.path.join(d, n)).convert('RGB').resize((w, h)), (x, y))
        draw.text((x + 8, y + h + 4), n[:-4], fill='black', font=font)
    path = os.path.join(d, '总览.jpg')
    sheet.save(path, quality=88)
    print('总览：' + path)


if __name__ == '__main__':
    main()
