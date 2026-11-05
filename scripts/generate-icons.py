"""
Lucide simgelerini (ISC lisansı) WPF Geometry kaynaklarına çevirir -> src/App/Resources/Icons.xaml

Tek seferlik betiktir; çalışma zamanında SVG ayrıştırılmaz. Sonuç depoya girer.
Kaynak olarak yerelde kurulu bir `lucide-react` paketinin `dist/esm/icons` klasörü gerekir:

    python scripts/generate-icons.py "<...>/node_modules/lucide-react/dist/esm/icons"

Her simge 24x24 birimlik tuvalde, çizgi tabanlıdır (Fill yok). Yalnız `path`, `circle`, `rect`, `line`,
`polyline`, `polygon` ve `ellipse` öğeleri desteklenir; hepsi tek path verisine çevrilir.
"""
import json
import re
import sys
from pathlib import Path

# (Koy anahtarı, Lucide adı)
ICONS = [
    ("Settings", "settings"),
    ("PanelTop", "panel-top"),
    ("PanelBottom", "panel-bottom"),
    ("Monitor", "monitor"),
    ("Palette", "palette"),
    ("Keyboard", "keyboard"),
    ("Activity", "activity"),
    ("Info", "info"),
    ("Power", "power"),
    ("Minus", "minus"),
    ("X", "x"),
    ("ChevronDown", "chevron-down"),
    ("ChevronUp", "chevron-up"),
    ("Check", "check"),
    ("ExternalLink", "external-link"),
    ("RotateCcw", "rotate-ccw"),
    ("RefreshCw", "refresh-cw"),
    ("Copy", "copy"),
    ("Sun", "sun"),
    ("Moon", "moon"),
    ("Laptop", "laptop"),
    ("TriangleAlert", "triangle-alert"),
    ("CircleCheck", "circle-check"),
    ("AppWindow", "app-window"),
    ("Pause", "pause"),
    ("Play", "play"),
    ("Languages", "languages"),
    ("Layers", "layers"),
    ("Gauge", "gauge"),
    ("Battery", "battery-medium"),
    ("Download", "download"),
    ("LifeBuoy", "life-buoy"),
    ("Github", "code-xml"),
    ("Wifi", "wifi"),
    ("Bluetooth", "bluetooth"),
]

TOKEN = re.compile(r"[a-zA-Z]|[-+]?(?:\d+\.?\d*|\.\d+)(?:[eE][-+]?\d+)?")


def fmt(n: float) -> str:
    s = f"{n:.4f}".rstrip("0").rstrip(".")
    return "0" if s in ("-0", "") else s


def normalize_path(d: str) -> str:
    """Sıkışık yazılmış path verisini (örn. yay bayrakları) boşluklu, WPF'in rahat okuduğu biçime çevirir."""
    out = []
    first_rel = False
    tokens = TOKEN.findall(d)
    # Birden çok öğe tek path'e eklenecek: baştaki göreli m, öncekine değil orijine göre olmalı.
    if tokens and tokens[0] == "m":
        tokens[0] = "M"
        first_rel = True
    i = 0
    cmd = None
    arg_counts = {"m": 2, "l": 2, "h": 1, "v": 1, "c": 6, "s": 4, "q": 4, "t": 2, "a": 7, "z": 0}
    while i < len(tokens):
        t = tokens[i]
        if t.isalpha():
            cmd = t
            out.append(t)
            i += 1
            if t.lower() == "z":
                continue
        n = arg_counts[cmd.lower()]
        if n == 0:
            continue
        if i >= len(tokens):
            break
        args = tokens[i : i + n]
        i += n
        out.append(" ".join(fmt(float(a)) if k not in (3, 4) or cmd.lower() != "a" else a for k, a in enumerate(args)))
        # Aynı komutun tekrarı: m ardından gelen çiftler l olarak yorumlanır, ama burada komutu açıkça yinelemek yeterli.
        if i < len(tokens) and not tokens[i].isalpha():
            nxt = "l" if (cmd == "m" or (cmd == "M" and first_rel)) else ("L" if cmd == "M" else cmd)
            first_rel = False
            cmd = nxt
            out.append(cmd)
    return " ".join(out)


def circle(cx, cy, r):
    return f"M{fmt(cx - r)} {fmt(cy)}a{fmt(r)} {fmt(r)} 0 1 0 {fmt(2 * r)} 0a{fmt(r)} {fmt(r)} 0 1 0 {fmt(-2 * r)} 0z"


def ellipse(cx, cy, rx, ry):
    return f"M{fmt(cx - rx)} {fmt(cy)}a{fmt(rx)} {fmt(ry)} 0 1 0 {fmt(2 * rx)} 0a{fmt(rx)} {fmt(ry)} 0 1 0 {fmt(-2 * rx)} 0z"


def rect(x, y, w, h, rx=0, ry=None):
    ry = rx if ry is None else ry
    if rx == 0 and ry == 0:
        return f"M{fmt(x)} {fmt(y)}h{fmt(w)}v{fmt(h)}h{fmt(-w)}z"
    return (
        f"M{fmt(x + rx)} {fmt(y)}h{fmt(w - 2 * rx)}a{fmt(rx)} {fmt(ry)} 0 0 1 {fmt(rx)} {fmt(ry)}"
        f"v{fmt(h - 2 * ry)}a{fmt(rx)} {fmt(ry)} 0 0 1 {fmt(-rx)} {fmt(ry)}"
        f"h{fmt(-(w - 2 * rx))}a{fmt(rx)} {fmt(ry)} 0 0 1 {fmt(-rx)} {fmt(-ry)}"
        f"v{fmt(-(h - 2 * ry))}a{fmt(rx)} {fmt(ry)} 0 0 1 {fmt(rx)} {fmt(-ry)}z"
    )


def points(p, close):
    nums = [float(v) for v in re.findall(r"[-+]?(?:\d+\.?\d*|\.\d+)", p)]
    pts = list(zip(nums[0::2], nums[1::2]))
    d = f"M{fmt(pts[0][0])} {fmt(pts[0][1])}" + "".join(f"L{fmt(x)} {fmt(y)}" for x, y in pts[1:])
    return d + ("z" if close else "")


def node_to_path(tag, a):
    f = lambda k, default=0.0: float(a.get(k, default))
    if tag == "path":
        return normalize_path(a["d"])
    if tag == "circle":
        return circle(f("cx"), f("cy"), f("r"))
    if tag == "ellipse":
        return ellipse(f("cx"), f("cy"), f("rx"), f("ry"))
    if tag == "rect":
        rx = f("rx")
        ry = float(a["ry"]) if "ry" in a else rx
        return rect(f("x"), f("y"), f("width"), f("height"), rx, ry)
    if tag == "line":
        return f"M{fmt(f('x1'))} {fmt(f('y1'))}L{fmt(f('x2'))} {fmt(f('y2'))}"
    if tag == "polyline":
        return points(a["points"], False)
    if tag == "polygon":
        return points(a["points"], True)
    raise ValueError(f"Desteklenmeyen öğe: {tag}")


def load_icon(folder: Path, name: str) -> str:
    text = (folder / f"{name}.mjs").read_text(encoding="utf-8")
    m = re.search(r"node:\s*\[", text)
    if not m:
        raise ValueError(f"{name}: node listesi bulunamadı")
    # Köşeli parantezleri (metin içindekiler hariç) eşleştirerek listenin sonunu bul
    start = m.end() - 1
    depth = 0
    in_str = False
    for pos in range(start, len(text)):
        ch = text[pos]
        if ch == '"':
            in_str = not in_str
        elif not in_str and ch == "[":
            depth += 1
        elif not in_str and ch == "]":
            depth -= 1
            if depth == 0:
                end = pos + 1
                break
    # JS nesne anahtarlarını JSON'a çevir
    raw = re.sub(r"(\{|,)\s*([A-Za-z_][A-Za-z0-9_]*)\s*:", r'\1 "\2":', text[start:end])
    nodes = json.loads(raw)
    return "".join(node_to_path(tag, attrs) for tag, attrs in nodes)


def main():
    if len(sys.argv) < 2:
        sys.exit(__doc__)
    folder = Path(sys.argv[1])
    out = Path(__file__).resolve().parent.parent / "src" / "App" / "Resources" / "Icons.xaml"

    lines = [
        '<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"',
        '                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">',
        "    <!--",
        "        Lucide simgeleri (https://lucide.dev), ISC Lisansı.",
        "        Copyright (c) for portions of Lucide are held by Cole Bemis 2013-2022 (Feather, MIT);",
        "        all other copyright (c) for Lucide are held by Lucide Contributors 2022.",
        "        Permission to use, copy, modify, and/or distribute this software for any purpose with or without fee",
        '        is hereby granted, provided that the above copyright notice and this permission notice appear in all copies.',
        "        THE SOFTWARE IS PROVIDED \"AS IS\" AND THE AUTHOR DISCLAIMS ALL WARRANTIES (bkz. https://github.com/lucide-icons/lucide/blob/main/LICENSE).",
        "",
        "        Bu dosya scripts/generate-icons.py ile üretilir, elle düzenlenmez.",
        "        Her geometri 24x24 birimlik tuvalde ÇİZGİ ikonudur: Fill kullanılmaz, KoyIcon denetimi Stroke ile çizer.",
        "    -->",
    ]
    for key, name in ICONS:
        lines.append(f'    <Geometry x:Key="Icon{key}">{load_icon(folder, name)}</Geometry>')
    lines.append("</ResourceDictionary>")
    out.write_text("\n".join(lines) + "\n", encoding="utf-8")
    print(f"{len(ICONS)} simge yazıldı: {out}")


if __name__ == "__main__":
    main()
