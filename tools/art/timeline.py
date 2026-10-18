# Generates docs/art/timeline.svg: what got built when, and what's next.
import sys
W, H = 1200, 250
ev = [
    ("10 Oct", "Core bot", "Tether: power, pull, swing · first kitchen", "#36b35a"),
    ("11 Oct", "Chapters 1-2 + v0.3.0", "drones, gates, menu, first release + web build", "#36b35a"),
    ("12 Oct", "Chapters 3-5", "toon look, open world, custom bots, Ch 1-4 run in Unity", "#36b35a"),
    ("14 Oct", "Chapters 6-8", "Funland, Old Rig, the finale + credits", "#36b35a"),
    ("18 Oct", "All 20 scenes build", "Pump House 21/21 · reserve battery · lost parts come home", "#ffb020"),
    ("Next", "Playtest + co-op", "play every call start to finish, then 2-4 bots", "#aab3c8"),
]
o = []; a = o.append
a(f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {W} {H}" width="{W}" height="{H}" font-family="\'Trebuchet MS\', Verdana, Arial, sans-serif">')
a(f'<rect width="{W}" height="{H}" rx="22" fill="#fffaf2" stroke="#f0d9bd" stroke-width="3"/>')
a('<text x="40" y="44" font-size="24" font-weight="bold" fill="#1d2440">Build log</text>')
y = 110; x0, x1 = 120, 1080
step = (x1 - x0) / (len(ev) - 1)
a(f'<line x1="{x0}" y1="{y}" x2="{x1}" y2="{y}" stroke="#e3e8f3" stroke-width="8" stroke-linecap="round"/>')
done_x = x0 + step * 4
a(f'<line x1="{x0}" y1="{y}" x2="{x0}" y2="{y}" stroke="#36b35a" stroke-width="8" stroke-linecap="round"><animate attributeName="x2" from="{x0}" to="{done_x}" dur="2.4s" begin="0.2s" fill="freeze"/></line>')
a(f'<line x1="{done_x}" y1="{y}" x2="{x1}" y2="{y}" stroke="#aab3c8" stroke-width="4" stroke-dasharray="6 8"><animate attributeName="stroke-dashoffset" from="28" to="0" dur="1s" repeatCount="indefinite"/></line>')
for i, (date, title, sub, col) in enumerate(ev):
    x = x0 + step * i
    b = 0.2 + i * 0.5
    a(f'<g opacity="0"><animate attributeName="opacity" from="0" to="1" begin="{b:.1f}s" dur="0.4s" fill="freeze"/>')
    if i == 4:
        a(f'<circle cx="{x}" cy="{y}" r="14" fill="none" stroke="{col}" stroke-width="3"><animate attributeName="r" values="12;26" dur="1.6s" repeatCount="indefinite"/><animate attributeName="opacity" values="1;0" dur="1.6s" repeatCount="indefinite"/></circle>')
        a(f'<text x="{x}" y="{y-34}" font-size="12" font-weight="bold" fill="{col}" text-anchor="middle">YOU ARE HERE</text>')
    a(f'<circle cx="{x}" cy="{y}" r="12" fill="{col}" stroke="#fff" stroke-width="3"/>')
    a(f'<text x="{x}" y="{y-18 if i!=4 else y-50}" font-size="14" fill="#5b6380" text-anchor="middle">{date}</text>')
    a(f'<text x="{x}" y="{y+40}" font-size="15" font-weight="bold" fill="#1d2440" text-anchor="middle">{title}</text>')
    words = sub.split(" "); lines = [""]
    for w in words:
        if len(lines[-1]) + len(w) > 24: lines.append("")
        lines[-1] = (lines[-1] + " " + w).strip()
    for k, ln in enumerate(lines):
        a(f'<text x="{x}" y="{y+60+k*16}" font-size="12.5" fill="#5b6380" text-anchor="middle">{ln}</text>')
    a('</g>')
a('</svg>')
open(sys.argv[1], "w").write("\n".join(o))
