# Generates docs/art/campaign-map.svg: the animated campaign route with each stop's status.
import sys
out = sys.argv[1]
W, H = 1200, 560
SEA = 352
GREEN = "#36b35a"
stops = [  # x, deck y, chapter, name, calls, colour, prop
    (250, 300, "2", "Nonna Lucia's\nTrattoria", "2 calls + Pasta Panic", "#ff8a65", "pot"),
    (390, 288, "3", "Brightwater\nPark", "Loose Screws + Up a Tree", "#66bb6a", "tree"),
    (530, 300, "4", "School\nScience Fair", "Catch the Hum + Slightly Too Big", "#ab47bc", "volcano"),
    (670, 284, "5", "Docks &amp;\nLighthouse", "Heavy Lifting + Lights Out", "#42a5f5", "lighthouse"),
    (810, 298, "6", "Funland\nPier", "Carousel Chaos + Stuck at the Top", "#ef5350", "wheel"),
    (950, 318, "7", "The Old Rig", "Going Down + Unit Zero", "#78909c", "rig"),
    (1090, 270, "8", "Founders'\nFestival", "Stage Fright + The Founders' Song", "#fbc02d", "stage"),
]
o = []
a = o.append
a(f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {W} {H}" width="{W}" height="{H}" font-family="\'Trebuchet MS\', Verdana, Arial, sans-serif">')
a('''<defs>
<linearGradient id="sky" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#0f1a3d"/><stop offset="0.38" stop-color="#3a5aa8"/><stop offset="0.62" stop-color="#9fd3ff"/><stop offset="1" stop-color="#d9f1ff"/></linearGradient>
<linearGradient id="sea" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#5ec4e8"/><stop offset="1" stop-color="#1f6f9e"/></linearGradient>
<radialGradient id="planet" cx="0.35" cy="0.3" r="0.8"><stop offset="0" stop-color="#7fd0ff"/><stop offset="1" stop-color="#1c4f9c"/></radialGradient>
</defs>''')
a(f'<rect width="{W}" height="{H}" rx="22" fill="url(#sky)"/>')
# stars
import random
random.seed(7)
for i in range(40):
    x, y = random.uniform(20, W - 20), random.uniform(14, 150)
    d = random.uniform(1.5, 4)
    a(f'<circle cx="{x:.0f}" cy="{y:.0f}" r="{random.uniform(0.8,1.8):.1f}" fill="#fff"><animate attributeName="opacity" values="1;0.2;1" dur="{d:.1f}s" repeatCount="indefinite"/></circle>')
a('<text x="600" y="44" font-size="30" font-weight="bold" fill="#fff" text-anchor="middle">The campaign: every stop is built</text>')
a('<text x="600" y="72" font-size="15" fill="#cfe0ff" text-anchor="middle">Green tick = the level generates and runs in Unity 6.6 on the dev PC (18 Oct 2026). Nobody has signed off a full playtest yet.</text>')

# Station (Chapter 1)
a('''<g transform="translate(110 150)">
<animateTransform attributeName="transform" type="translate" values="110 150;110 143;110 150" dur="5s" repeatCount="indefinite"/>
<g><animateTransform attributeName="transform" type="rotate" from="0" to="360" dur="30s" repeatCount="indefinite"/>
<rect x="-70" y="-6" width="140" height="12" rx="6" fill="#c9d6ff"/><rect x="-6" y="-70" width="12" height="140" rx="6" fill="#c9d6ff"/>
<rect x="-84" y="-16" width="22" height="32" rx="4" fill="#4fc3f7"/><rect x="62" y="-16" width="22" height="32" rx="4" fill="#4fc3f7"/></g>
<circle r="30" fill="#f4f6ff" stroke="#8b9bd6" stroke-width="4"/><circle r="14" fill="#ff8c26"><animate attributeName="fill" values="#ff8c26;#ffd23f;#ff8c26" dur="2s" repeatCount="indefinite"/></circle>
</g>''')
a(f'<g><circle cx="168" cy="108" r="13" fill="{GREEN}" stroke="#fff" stroke-width="3"/><path d="M161 108 l5 5 l9 -10" fill="none" stroke="#fff" stroke-width="3" stroke-linecap="round"/></g>')
a('<text x="200" y="140" font-size="17" font-weight="bold" fill="#fff">Ch 1 · Bureau Station</text>')
a('<text x="200" y="160" font-size="13" fill="#cfe0ff">3 training rooms + Coffee Crisis</text>')

# pod drop path
pod = "M 150 196 C 180 250, 210 250, 236 292"
a(f'<path d="{pod}" fill="none" stroke="#fff" stroke-width="3" stroke-dasharray="6 8" opacity="0.8"><animate attributeName="stroke-dashoffset" from="28" to="0" dur="1s" repeatCount="indefinite"/></path>')
a(f'''<g><animateMotion path="{pod}" dur="3.5s" repeatCount="indefinite" rotate="0"/>
<ellipse rx="10" ry="13" fill="#ffffff" stroke="#ff8c26" stroke-width="3"/><path d="M -6 12 L 0 24 L 6 12 Z" fill="#ffb020"><animate attributeName="opacity" values="1;0.3;1" dur="0.3s" repeatCount="indefinite"/></path></g>''')
a('<text x="200" y="232" font-size="12" fill="#ffffff" opacity="0.9">mission pod</text>')

# sea
a(f'<rect x="0" y="{SEA}" width="{W}" height="{H-SEA}" fill="url(#sea)"/>')
a(f'<rect x="0" y="{H-22}" width="{W}" height="22" fill="#1f6f9e"/>')
a(f'<path d="M0 {H} L0 {H-22} Q{W/2} {H-30} {W} {H-22} L{W} {H} Z" fill="#1f6f9e"/>')
for k, (dy, op, dur) in enumerate([(4, 0.7, 7), (26, 0.45, 9)]):
    wave = " ".join(f"q 15 -6 30 0 t 30 0" for _ in range(1))
    d = f"M -60 {SEA+dy} " + " ".join("q 15 -7 30 0 q 15 7 30 0" for _ in range(24))
    a(f'<path d="{d}" fill="none" stroke="#e8fbff" stroke-width="2.5" opacity="{op}"><animateTransform attributeName="transform" type="translate" from="0 0" to="60 0" dur="{dur}s" repeatCount="indefinite"/></path>')

# walkway path through deck tops
pts = [(x, y) for x, y, *_ in stops]
d = f"M {pts[0][0]} {pts[0][1]}"
for (x0, y0), (x1, y1) in zip(pts, pts[1:]):
    mx = (x0 + x1) / 2
    d += f" C {mx} {y0 + 26}, {mx} {y1 + 26}, {x1} {y1}"
a(f'<path d="{d}" fill="none" stroke="#f3e2c3" stroke-width="7" stroke-linecap="round"/>')
a(f'<path d="{d}" fill="none" stroke="#c9a46b" stroke-width="2" stroke-dasharray="3 7"/>')

# Pump House side call
a('<g transform="translate(110 492)">')
a('<path d="M 270 -186 C 250 -60, 120 -20, 38 -6" fill="none" stroke="#ffd23f" stroke-width="3" stroke-dasharray="5 6"><animate attributeName="stroke-dashoffset" from="22" to="0" dur="1.2s" repeatCount="indefinite"/></path>')
a('<rect x="-38" y="-14" width="76" height="34" rx="8" fill="#fff4dc" stroke="#e0a23b" stroke-width="3"/>')
a('<g transform="translate(0 3)"><animateTransform attributeName="transform" type="rotate" from="0 0 3" to="360 0 3" dur="4s" repeatCount="indefinite" additive="sum"/><circle r="9" fill="none" stroke="#3f8fd2" stroke-width="3"/><path d="M -9 0 H 9 M 0 -9 V 9" stroke="#3f8fd2" stroke-width="3"/></g>')
a(f'<circle cx="36" cy="-14" r="11" fill="{GREEN}" stroke="#fff" stroke-width="2.5"/><path d="M30 -14 l4 4 l8 -9" fill="none" stroke="#fff" stroke-width="2.5" stroke-linecap="round"/>')
a('<text x="48" y="2" font-size="14" font-weight="bold" fill="#fff">Side call: The Pump House</text>')
a('<text x="48" y="19" font-size="12" fill="#e2f6ff">3 rooms · self-test 21/21</text>')
a('</g>')

def prop(kind, x, y, col):
    if kind == "pot":
        return (f'<rect x="{x-12}" y="{y-20}" width="24" height="16" rx="4" fill="#8d6e63"/>'
                f'<g fill="#fff" opacity="0.85"><circle cx="{x-4}" cy="{y-26}" r="4"><animate attributeName="cy" values="{y-24};{y-44}" dur="2s" repeatCount="indefinite"/><animate attributeName="opacity" values="0.9;0" dur="2s" repeatCount="indefinite"/></circle>'
                f'<circle cx="{x+5}" cy="{y-26}" r="3"><animate attributeName="cy" values="{y-24};{y-44}" dur="2s" begin="1s" repeatCount="indefinite"/><animate attributeName="opacity" values="0.9;0" dur="2s" begin="1s" repeatCount="indefinite"/></circle></g>')
    if kind == "tree":
        return (f'<rect x="{x-3}" y="{y-34}" width="6" height="30" fill="#795548"/>'
                f'<g><animateTransform attributeName="transform" type="rotate" values="-3 {x} {y-4};3 {x} {y-4};-3 {x} {y-4}" dur="0.9s" repeatCount="indefinite"/>'
                f'<circle cx="{x}" cy="{y-42}" r="16" fill="#4caf50"/><circle cx="{x+9}" cy="{y-48}" r="5" fill="#ffcc80"/></g>')
    if kind == "volcano":
        return (f'<path d="M {x-18} {y-4} L {x-6} {y-30} L {x+6} {y-30} L {x+18} {y-4} Z" fill="#8d6e63"/>'
                f'<ellipse cx="{x}" cy="{y-32}" rx="9" ry="5" fill="#f8bbd0"><animate attributeName="ry" values="4;12;4" dur="1.6s" repeatCount="indefinite"/><animate attributeName="cy" values="{y-32};{y-40};{y-32}" dur="1.6s" repeatCount="indefinite"/></ellipse>')
    if kind == "lighthouse":
        return (f'<path d="M {x-8} {y-4} L {x-5} {y-52} L {x+5} {y-52} L {x+8} {y-4} Z" fill="#fff"/>'
                f'<rect x="{x-7}" y="{y-36}" width="14" height="7" fill="#e53935"/><rect x="{x-7}" y="{y-20}" width="14" height="7" fill="#e53935"/>'
                f'<circle cx="{x}" cy="{y-56}" r="5" fill="#ffeb3b"/>'
                f'<path d="M {x} {y-56} L {x+90} {y-74} L {x+90} {y-38} Z" fill="#fff59d" opacity="0.45"><animateTransform attributeName="transform" type="rotate" values="0 {x} {y-56};180 {x} {y-56};360 {x} {y-56}" dur="5s" repeatCount="indefinite"/></path>')
    if kind == "wheel":
        spokes = "".join(f'<line x1="{x}" y1="{y-30}" x2="{x}" y2="{y-54}" transform="rotate({k*45} {x} {y-30})" stroke="#fff" stroke-width="2"/>' for k in range(8))
        cars = "".join(f'<circle cx="{x}" cy="{y-54}" r="4" fill="#ffd54f" transform="rotate({k*90} {x} {y-30})"/>' for k in range(4))
        return (f'<line x1="{x-10}" y1="{y-4}" x2="{x}" y2="{y-30}" stroke="#90a4ae" stroke-width="3"/><line x1="{x+10}" y1="{y-4}" x2="{x}" y2="{y-30}" stroke="#90a4ae" stroke-width="3"/>'
                f'<g><animateTransform attributeName="transform" type="rotate" from="0 {x} {y-30}" to="360 {x} {y-30}" dur="9s" repeatCount="indefinite"/>'
                f'<circle cx="{x}" cy="{y-30}" r="24" fill="none" stroke="#ef5350" stroke-width="3"/>{spokes}{cars}</g>')
    if kind == "rig":
        return (f'<g transform="rotate(-8 {x} {y})"><rect x="{x-16}" y="{y-34}" width="32" height="30" fill="#607d8b"/><rect x="{x-4}" y="{y-56}" width="8" height="24" fill="#546e7a"/></g>'
                + "".join(f'<circle cx="{x}" cy="{SEA+20}" r="10" fill="none" stroke="#ffeb3b" stroke-width="2"><animate attributeName="r" values="8;60" dur="3s" begin="{k}s" repeatCount="indefinite"/><animate attributeName="opacity" values="0.9;0" dur="3s" begin="{k}s" repeatCount="indefinite"/></circle>' for k in range(3))
                + f'<text x="{x+30}" y="{SEA+60}" font-size="11" fill="#fff59d" font-weight="bold">· · · — — — · · ·<animate attributeName="opacity" values="1;0.2;1" dur="2s" repeatCount="indefinite"/></text>')
    if kind == "stage":
        fw = ""
        for k, (fx, fy, c) in enumerate([(x - 30, y - 70, "#ff5252"), (x + 26, y - 82, "#40c4ff"), (x, y - 100, "#ffd740")]):
            rays = "".join(f'<line x1="0" y1="-4" x2="0" y2="-16" transform="rotate({j*45})"/>' for j in range(8))
            fw += (f'<g transform="translate({fx} {fy})"><g stroke="{c}" stroke-width="3" stroke-linecap="round" opacity="0"><animate attributeName="opacity" values="0;1;0" dur="2.4s" begin="{k*0.8}s" repeatCount="indefinite"/>'
                   f'<animateTransform attributeName="transform" type="scale" values="0.4;1.2" dur="2.4s" begin="{k*0.8}s" repeatCount="indefinite"/>{rays}</g></g>')
        return (f'<rect x="{x-24}" y="{y-26}" width="48" height="22" rx="4" fill="#7e57c2"/><path d="M {x-26} {y-26} L {x} {y-44} L {x+26} {y-26} Z" fill="#ffca28"/>'
                f'<g transform="translate(0 0)">{fw}</g>')
    return ""

for i, (x, y, ch, name, calls, col, kind) in enumerate(stops):
    begin = 0.4 + i * 0.35
    a(f'<g opacity="0"><animate attributeName="opacity" from="0" to="1" begin="{begin:.2f}s" dur="0.5s" fill="freeze"/>')
    for sx in (-34, -12, 12, 34):
        a(f'<rect x="{x+sx-3}" y="{y+8}" width="6" height="{SEA-y+6}" fill="#c69c6d"/>')
    a(f'<rect x="{x-48}" y="{y-4}" width="96" height="14" rx="4" fill="{col}" stroke="#ffffff" stroke-width="2"/>')
    a(prop(kind, x, y, col))
    a(f'<circle cx="{x+44}" cy="{y-8}" r="12" fill="{GREEN}" stroke="#fff" stroke-width="3"/><path d="M{x+38} {y-8} l4 4 l8 -9" fill="none" stroke="#fff" stroke-width="3" stroke-linecap="round"/>')
    lines = name.split("\n")
    ty = SEA + 44 if i % 2 == 0 else SEA + 104
    if kind == "rig": ty = SEA + 104
    a(f'<text x="{x}" y="{ty}" font-size="13" font-weight="bold" fill="#ffffff" text-anchor="middle">Ch {ch}</text>')
    for k, ln in enumerate(lines):
        a(f'<text x="{x}" y="{ty+17+k*16}" font-size="14" font-weight="bold" fill="#ffffff" text-anchor="middle">{ln}</text>')
    a('</g>')

# PIP travelling the walkway
a(f'''<g><animateMotion path="{d}" dur="14s" repeatCount="indefinite" rotate="0"/>
<g transform="translate(0 -16)"><rect x="-9" y="-10" width="18" height="14" rx="4" fill="#f7f7ff" stroke="#c9cbe0" stroke-width="1.5"/>
<rect x="-6" y="-7" width="12" height="8" rx="2" fill="#141a33"/><circle cx="-2.5" cy="-3" r="1.5" fill="#4fe3ff"/><circle cx="2.5" cy="-3" r="1.5" fill="#4fe3ff"/>
<ellipse cx="0" cy="9" rx="8" ry="7" fill="#f7f7ff" stroke="#c9cbe0" stroke-width="1.5"/>
<line x1="0" y1="-10" x2="0" y2="-16" stroke="#7d8196" stroke-width="1.5"/><circle cx="0" cy="-17" r="2.5" fill="#ff8c26"/></g></g>''')

# coming-later pods
a('<g opacity="0.75"><text x="1150" y="122" font-size="13" fill="#cfe0ff" text-anchor="end">later pods (planned): Moon Mine · Jungle Lab</text></g>')
a('</svg>')
open(out, "w").write("\n".join(o))
