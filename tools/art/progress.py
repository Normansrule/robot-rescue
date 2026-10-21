# Generates docs/art/progress.svg: the animated "how far along" board.
import sys
out = sys.argv[1]
W = 1200
GREEN, AMBER, RED, GREY = "#36b35a", "#ffb020", "#ff6b5e", "#aab3c8"
INK, SUB = "#1d2440", "#5b6380"
rows = [
    # label, done, total, colour, note
    ("Campaign chapters", 8, 8, GREEN, "Chapters 1-8 generate clean in Unity 6.6"),
    ("Playable scenes", 20, 20, GREEN, "3 training rooms + 16 calls and Big Rescues + the Pump House · lint: 0 issues"),
    ("Big Rescues", 8, 8, GREEN, "Coffee Crisis → The Founders' Song"),
    ("Tools", 4, 4, GREEN, "Tether · Whisk · Screwdriver · Magnet"),
    ("Captured in Play Mode", 19, 20, GREEN, "gameplay GIFs below · Pump House not filmed yet"),
    ("Scripted self-tests", 1, 20, AMBER, "Pump House: 21/21 passed (3 more checks added today)"),
    ("Playtest recorded", 0, 20, RED, "no full playthrough by a person written up yet: the next big step"),
]
features = [
    ("Customizable bots", GREEN, "DONE"),
    ("Battery reserve + lost parts", AMBER, "NEW · TO TEST"),
    ("Puzzle resets + hints", AMBER, "PUMP HOUSE ONLY"),
    ("Co-op (2-4 bots)", GREY, "PLANNED"),
    ("Rescue Rush mode", GREY, "PLANNED"),
    ("Hand-made art + audio", GREY, "PLANNED"),
]
H = 150 + len(rows) * 60 + 40 + 2 * 54 + 30
s = []
a = s.append
a(f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {W} {H}" width="{W}" height="{H}" font-family="\'Trebuchet MS\', Verdana, Arial, sans-serif">')
a('''<defs>
<linearGradient id="bg" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#fff8ee"/><stop offset="1" stop-color="#eef6ff"/></linearGradient>
<clipPath id="r"><rect x="0" y="0" width="1" height="1"/></clipPath>
</defs>''')
a(f'<rect width="{W}" height="{H}" rx="22" fill="url(#bg)"/>')
a(f'<rect x="1.5" y="1.5" width="{W-3}" height="{H-3}" rx="21" fill="none" stroke="#f0d9bd" stroke-width="3"/>')
a(f'<text x="48" y="62" font-size="34" font-weight="bold" fill="{INK}">How far along is Robot Rescue?</text>')
a(f'<text x="48" y="94" font-size="17" fill="{SUB}">Status on 18 Oct 2026, from the last Unity run on the dev PC. Bars fill to the real counts.</text>')
# legend
lx = 820
for i, (c, t) in enumerate([(GREEN, "done + checked in Unity"), (AMBER, "built, partly tested"), (RED, "not started"), (GREY, "planned")]):
    y = 40 + i * 20
    a(f'<circle cx="{lx}" cy="{y}" r="6" fill="{c}"/><text x="{lx+14}" y="{y+5}" font-size="14" fill="{SUB}">{t}</text>')

# PIP peeking, blinking
a('''<g transform="translate(1110 66)">
<animateTransform attributeName="transform" type="translate" values="1110 66;1110 60;1110 66" dur="2.2s" repeatCount="indefinite"/>
<rect x="-30" y="-24" width="60" height="44" rx="12" fill="#f7f7ff" stroke="#c9cbe0" stroke-width="2"/>
<rect x="-23" y="-18" width="46" height="30" rx="8" fill="#141a33"/>
<g fill="#4fe3ff"><rect x="-14" y="-11" width="8" height="13" rx="3"><animate attributeName="height" values="13;13;1;13;13" keyTimes="0;0.9;0.94;0.98;1" dur="3.4s" repeatCount="indefinite"/></rect>
<rect x="6" y="-11" width="8" height="13" rx="3"><animate attributeName="height" values="13;13;1;13;13" keyTimes="0;0.9;0.94;0.98;1" dur="3.4s" repeatCount="indefinite"/></rect></g>
<line x1="0" y1="-24" x2="0" y2="-40" stroke="#7d8196" stroke-width="3"/><circle cx="0" cy="-42" r="5" fill="#ff8c26"><animate attributeName="fill" values="#ff8c26;#ffd23f;#ff8c26" dur="1.4s" repeatCount="indefinite"/></circle>
</g>''')

y0, bx, bw = 140, 330, 560
for i, (label, done, total, col, note) in enumerate(rows):
    y = y0 + i * 60
    begin = 0.3 + i * 0.25
    frac = done / total
    a(f'<text x="48" y="{y+22}" font-size="19" font-weight="bold" fill="{INK}">{label}</text>')
    a(f'<rect x="{bx}" y="{y+4}" width="{bw}" height="26" rx="13" fill="#e3e8f3"/>')
    w = max(0.0, bw * frac)
    if w > 0:
        a(f'<g><rect x="{bx}" y="{y+4}" width="0" height="26" rx="13" fill="{col}">'
          f'<animate attributeName="width" from="0" to="{w:.1f}" begin="{begin:.2f}s" dur="1.1s" fill="freeze" calcMode="spline" keySplines="0.2 0.8 0.3 1" keyTimes="0;1"/></rect>')
        # The fill animates once, to its real value: the motion is the information. No looping shimmer.
        a('</g>')
    else:
        a(f'<rect x="{bx+2}" y="{y+6}" width="22" height="22" rx="11" fill="none" stroke="{col}" stroke-width="3" stroke-dasharray="4 4">'
          f'</rect>')
    a(f'<text x="{bx+bw+18}" y="{y+24}" font-size="20" font-weight="bold" fill="{col if col!=GREY else SUB}" opacity="0">{done}/{total}'
      f'<set attributeName="opacity" to="1" begin="{begin+0.9:.2f}s"/></text>')
    a(f'<text x="{bx+6}" y="{y+48}" font-size="14" fill="{SUB}">{note}</text>')

y = y0 + len(rows) * 60 + 26
a(f'<text x="48" y="{y}" font-size="19" font-weight="bold" fill="{INK}">Systems</text>')
for i, (label, col, tag) in enumerate(features):
    cx = 48 + (i % 3) * 370
    cy = y + 16 + (i // 3) * 54
    begin = 2.2 + i * 0.15
    a(f'<g opacity="0"><set attributeName="opacity" to="1" begin="{begin:.2f}s"/>'
      f'<rect x="{cx}" y="{cy}" width="350" height="42" rx="21" fill="#ffffff" stroke="{col}" stroke-width="3"/>'
      f'<circle cx="{cx+22}" cy="{cy+21}" r="8" fill="{col}">' +
      (f'<animate attributeName="r" values="8;11;8" dur="1.6s" repeatCount="indefinite"/>' if col == AMBER else '') +
      f'</circle><text x="{cx+40}" y="{cy+27}" font-size="16" font-weight="bold" fill="{INK}">{label}</text>'
      f'<text x="{cx+336}" y="{cy+27}" font-size="11" font-weight="bold" fill="{col if col!=GREY else SUB}" text-anchor="end">{tag}</text></g>')
a('</svg>')
open(out, 'w').write("\n".join(s))
print(H)
