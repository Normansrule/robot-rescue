/* Robot Rescue website: "Live Wire".
   Everything here is small hand-rolled physics: a verlet rope for the Tether, damped springs for doors and landings,
   and a pendulum for the swing. Each loop sleeps when nothing is moving and when it's off screen.
   Reduced motion: the cables hang still, power just switches on, and the playground jumps to each result. */
(() => {
  "use strict";

  const root = document.documentElement;
  const motionQuery = matchMedia("(prefers-reduced-motion: reduce)");
  let calm = motionQuery.matches;

  // ---- Colour tokens, read from the CSS so canvas and page always match -----------------------------------

  const T = {};
  const themeHooks = [];
  function readTokens() {
    const cs = getComputedStyle(root);
    for (const k of ["bg", "bg-raised", "bg-sunk", "line", "text", "muted", "live", "live-glow", "pip", "good", "warn", "bad"]) {
      T[k] = cs.getPropertyValue("--" + k).trim();
    }
  }

  const lightQuery = matchMedia("(prefers-color-scheme: light)");
  const currentTheme = () => root.dataset.theme || (lightQuery.matches ? "light" : "dark");
  const toggle = document.querySelector("[data-theme-toggle]");

  function syncTheme() {
    readTokens();
    if (toggle) toggle.setAttribute("aria-label", currentTheme() === "dark" ? "Switch to light mode" : "Switch to dark mode");
    themeHooks.forEach((f) => f());
  }
  if (toggle) {
    toggle.addEventListener("click", () => {
      root.dataset.theme = currentTheme() === "dark" ? "light" : "dark";
      try { localStorage.setItem("rr-theme", root.dataset.theme); } catch (e) { /* private mode: fine, just not saved */ }
      syncTheme();
    });
  }
  onChange(lightQuery, syncTheme);
  onChange(motionQuery, (e) => { calm = e.matches; themeHooks.forEach((f) => f()); });
  syncTheme();

  function onChange(q, f) { if (q.addEventListener) q.addEventListener("change", f); else if (q.addListener) q.addListener(f); }

  // ---- Shared helpers -----------------------------------------------------------------------------------

  const clamp = (v, a, b) => Math.max(a, Math.min(b, v));
  const lerp = (a, b, t) => a + (b - a) * t;
  const easeOut = (t) => 1 - Math.pow(1 - clamp(t, 0, 1), 3);

  /** Sizes a canvas to its box at the device pixel ratio (capped at 2). */
  function fitCanvas(c) {
    const r = c.getBoundingClientRect();
    const d = Math.min(window.devicePixelRatio || 1, 2);
    const w = Math.max(1, Math.round(r.width * d)), h = Math.max(1, Math.round(r.height * d));
    if (c.width !== w || c.height !== h) { c.width = w; c.height = h; }
    return { w: r.width, h: r.height, d };
  }

  /** An animation loop that only runs while `el` is on screen and `frame` says something is still moving. */
  function makeLoop(el, frame) {
    let raf = 0, last = 0, visible = true;
    const tick = (t) => {
      raf = 0;
      const dt = last ? Math.min(0.034, (t - last) / 1000) : 1 / 60;
      last = t;
      const more = frame(dt);
      if (more !== false && visible && !document.hidden) raf = requestAnimationFrame(tick);
      else last = 0;
    };
    const wake = () => { if (!raf && visible && !document.hidden) { last = 0; raf = requestAnimationFrame(tick); } };
    if ("IntersectionObserver" in window) {
      new IntersectionObserver((es) => { visible = es[es.length - 1].isIntersecting; if (visible) wake(); }).observe(el);
    }
    document.addEventListener("visibilitychange", wake);
    return wake;
  }

  /** A damped spring. Slightly under-damped by default so things settle with one small overshoot. */
  function spring(x, k = 170, c = 16) {
    return {
      x, v: 0, to: x, k, c,
      step(dt) { const a = -this.k * (this.x - this.to) - this.c * this.v; this.v += a * dt; this.x += this.v * dt; },
      get resting() { return Math.abs(this.x - this.to) < 0.05 && Math.abs(this.v) < 0.05; },
      snap(v) { this.x = this.to = v; this.v = 0; },
    };
  }

  /** A verlet rope: points remember where they were, and the gap between them is their velocity. */
  function makeRope(n) {
    const p = [];
    for (let i = 0; i <= n; i++) p.push({ x: 0, y: 0, px: 0, py: 0, w: 1 });
    return {
      p, n,
      place(ax, ay, bx, by) { for (let i = 0; i <= n; i++) { const t = i / n; const q = p[i]; q.x = q.px = lerp(ax, bx, t); q.y = q.py = lerp(ay, by, t); } },
      step(dt, g, seg, damp, iters) {
        for (const q of p) {
          if (q.w === 0) continue;
          const vx = (q.x - q.px) * damp, vy = (q.y - q.py) * damp;
          q.px = q.x; q.py = q.y;
          q.x += vx; q.y += vy + g * dt * dt;
        }
        for (let k = 0; k < iters; k++) {
          for (let i = 0; i < n; i++) {
            const a = p[i], b = p[i + 1];
            const dx = b.x - a.x, dy = b.y - a.y;
            const d = Math.hypot(dx, dy) || 0.0001;
            const wsum = a.w + b.w;
            if (!wsum) continue;
            const diff = (d - seg) / d / wsum;
            a.x += dx * diff * a.w; a.y += dy * diff * a.w;
            b.x -= dx * diff * b.w; b.y -= dy * diff * b.w;
          }
        }
      },
      energy() { let e = 0; for (const q of p) e += Math.abs(q.x - q.px) + Math.abs(q.y - q.py); return e; },
    };
  }

  function rrect(ctx, x, y, w, h, r) {
    r = Math.min(r, w / 2, h / 2);
    ctx.beginPath();
    ctx.moveTo(x + r, y);
    ctx.arcTo(x + w, y, x + w, y + h, r);
    ctx.arcTo(x + w, y + h, x, y + h, r);
    ctx.arcTo(x, y + h, x, y, r);
    ctx.arcTo(x, y, x + w, y, r);
    ctx.closePath();
  }

  function strokeRope(ctx, pts, color, width, glow) {
    ctx.save();
    ctx.lineCap = "round"; ctx.lineJoin = "round";
    ctx.strokeStyle = color; ctx.lineWidth = width;
    if (glow) { ctx.shadowColor = glow; ctx.shadowBlur = width * 3; }
    ctx.beginPath();
    ctx.moveTo(pts[0].x, pts[0].y);
    for (let i = 1; i < pts.length - 1; i++) {
      const mx = (pts[i].x + pts[i + 1].x) / 2, my = (pts[i].y + pts[i + 1].y) / 2;
      ctx.quadraticCurveTo(pts[i].x, pts[i].y, mx, my);
    }
    const e = pts[pts.length - 1];
    ctx.lineTo(e.x, e.y);
    ctx.stroke();
    ctx.restore();
  }

  /** A bright pulse travelling along a polyline, from fraction u0 to u1. */
  function strokePulse(ctx, pts, u, len, width) {
    const total = pathLength(pts);
    const a = clamp(u - len, 0, 1) * total, b = clamp(u, 0, 1) * total;
    if (b <= a) return;
    ctx.save();
    ctx.lineCap = "round";
    ctx.strokeStyle = "#FFFFFF";
    ctx.shadowColor = T.live; ctx.shadowBlur = width * 5;
    ctx.lineWidth = width;
    ctx.beginPath();
    walk(pts, a, b, ctx);
    ctx.stroke();
    ctx.restore();
  }
  function pathLength(pts) { let s = 0; for (let i = 1; i < pts.length; i++) s += Math.hypot(pts[i].x - pts[i - 1].x, pts[i].y - pts[i - 1].y); return s; }
  /** Draws the part of a polyline between distances a and b. */
  function walk(pts, a, b, ctx) {
    let s = 0, started = false;
    for (let i = 1; i < pts.length; i++) {
      const p0 = pts[i - 1], p1 = pts[i];
      const l = Math.hypot(p1.x - p0.x, p1.y - p0.y);
      if (s + l >= a && s <= b) {
        const t0 = clamp((a - s) / l, 0, 1), t1 = clamp((b - s) / l, 0, 1);
        if (!started) { ctx.moveTo(lerp(p0.x, p1.x, t0), lerp(p0.y, p1.y, t0)); started = true; }
        ctx.lineTo(lerp(p0.x, p1.x, t1), lerp(p0.y, p1.y, t1));
      }
      s += l;
    }
  }

  /**
   * PIP on canvas. There's one drawing of PIP: the detailed SVG in the "Your bot" section. The canvases clone it with
   * the right eye state and colours, rasterise it once per size, and draw that bitmap, so the hero, the playground and
   * the swatch preview always match. (x, y) is the pivot, rotated by `a`: PIP_HANG hangs PIP by the antenna bulb,
   * PIP_STAND stands it on its boots. `s` keeps the old 260-unit-tall scale, so callers' sizes didn't change.
   */
  const PIP_HANG = 4, PIP_STAND = 292, PIP_K = 260 / 300;
  const pipSprites = new Map();
  const pipReady = [];
  const pipSource = document.getElementById("pip");

  function pipSprite(state, res) {
    const key = state + "|" + res;
    let sp = pipSprites.get(key);
    if (sp) return sp;
    sp = { canvas: null };
    pipSprites.set(key, sp);
    if (!pipSource || typeof XMLSerializer === "undefined") return sp;
    const [eyesOn, blink, outline, shell, eye] = state.split("~");
    const svg = pipSource.cloneNode(true);
    svg.removeAttribute("id");
    svg.setAttribute("xmlns", "http://www.w3.org/2000/svg");
    svg.setAttribute("width", 240 * res); svg.setAttribute("height", 300 * res);
    svg.style.cssText = "--pip-outline:" + outline + ";--shell:" + shell + ";--eye:" + (eyesOn === "1" ? eye : "#26304F");
    const b = parseFloat(blink);
    svg.querySelectorAll(".pip-eye").forEach((e) => e.setAttribute("ry", (16 * b).toFixed(1)));
    svg.querySelectorAll(".pip-core").forEach((e) => e.setAttribute("ry", (7 * b).toFixed(1)));
    if (eyesOn !== "1") {
      svg.querySelector(".pip-face").removeAttribute("filter");
      svg.querySelectorAll(".pip-core, .pip-mouth, .pip-bulb-glow").forEach((e) => e.remove());
    }
    const img = new Image();
    img.onload = () => {
      const c = document.createElement("canvas");
      c.width = Math.ceil(240 * res); c.height = Math.ceil(300 * res);
      c.getContext("2d").drawImage(img, 0, 0, c.width, c.height);
      sp.canvas = c;
      pipReady.forEach((f) => f());
    };
    img.src = "data:image/svg+xml;charset=utf-8," + encodeURIComponent(new XMLSerializer().serializeToString(svg));
    return sp;
  }

  function drawPip(ctx, x, y, s, a, o) {
    const eyesOn = (o.eyes == null ? 1 : o.eyes) > 0.5 ? "1" : "0";
    const blink = String(Math.max(0.15, Math.round((o.blink == null ? 1 : o.blink) * 4) / 4));
    // Rasterise at the size it will be drawn (rounded, so a swinging PIP doesn't make a new bitmap every frame).
    const res = Math.max(0.2, Math.round(s * PIP_K * (o.res || 1) * 1.25 * 10) / 10);
    const outline = currentTheme() === "dark" ? "#24335F" : "#B9B2A2";
    const sp = pipSprite([eyesOn, blink, outline, o.shell || "#F7F7FF", o.eye || "#36DBFF"].join("~"), res);
    if (!sp.canvas) return;
    ctx.save();
    ctx.translate(x, y);
    ctx.rotate(a);
    if (o.squash) ctx.scale(1 + o.squash, 1 - o.squash);
    ctx.scale(s * PIP_K, s * PIP_K);
    ctx.drawImage(sp.canvas, -120, -(o.pivotY == null ? 150 : o.pivotY), 240, 300);
    ctx.restore();
  }

  // ---- The main line: current runs down the page as you scroll, and each section's socket lights up --------

  const mainline = document.querySelector(".mainline");
  const sockets = [...document.querySelectorAll("[data-socket]")];
  const hero = document.querySelector(".hero");
  const line = { y: [], end: 0, power: 0, on: false };

  function measureLine() {
    if (!mainline) return;
    const sy = window.scrollY;
    line.y = sockets.map((s) => {
      const top = parseFloat(getComputedStyle(s, "::before").top) || 0;
      return s.getBoundingClientRect().top + sy + top + 9;
    });
    line.end = line.y[line.y.length - 1] || 0;
    mainline.style.setProperty("--page-h", line.end + "px");
    wakeLine();
  }

  function lineTarget() {
    if (!line.on) return 0;
    if (calm) return line.end;
    const bottom = window.scrollY + window.innerHeight;
    if (bottom >= document.documentElement.scrollHeight - 4) return line.end;
    return clamp(window.scrollY + window.innerHeight * 0.62, 0, line.end);
  }

  let lineRaf = 0, lineLast = 0;
  function lineFrame(t) {
    lineRaf = 0;
    const dt = lineLast ? Math.min(0.05, (t - lineLast) / 1000) : 1 / 60;
    lineLast = t;
    const target = lineTarget();
    // Current moves fast but not instantly: an exponential chase, about 150 ms per section.
    line.power = calm ? target : lerp(line.power, target, 1 - Math.exp(-dt * 9));
    if (Math.abs(line.power - target) < 0.5) line.power = target;
    mainline.style.setProperty("--power", line.power.toFixed(1) + "px");
    sockets.forEach((s, i) => s.classList.toggle("is-live", line.on && line.y[i] <= line.power + 1));
    if (line.power !== target) lineRaf = requestAnimationFrame(lineFrame); else lineLast = 0;
  }
  function wakeLine() { if (mainline && !lineRaf) lineRaf = requestAnimationFrame(lineFrame); }
  function powerOn() {
    line.on = true;
    if (hero) hero.classList.add("is-lit");
    wakeLine();
  }

  window.addEventListener("scroll", wakeLine, { passive: true });
  window.addEventListener("resize", measureLine);
  if ("ResizeObserver" in window) new ResizeObserver(measureLine).observe(document.body);
  if (document.fonts && document.fonts.ready) document.fonts.ready.then(measureLine);
  measureLine();

  // ---- Hero: PIP hanging from its cable. Grab it and swing it ----------------------------------------------

  (function heroRope() {
    const canvas = document.getElementById("hero-canvas");
    const hint = document.getElementById("hero-hint");
    const ctx = canvas && canvas.getContext && canvas.getContext("2d");
    if (!ctx) { powerOn(); return; }

    const N = 16;
    const rope = makeRope(N);
    rope.p[0].w = 0;
    rope.p[N].w = 0.25;                // PIP is heavier than the cable
    let box = { w: 0, h: 0, d: 1 }, L = 200, s = 0.6, anchor = { x: 0, y: 0 };
    let grab = null, grabbed = false;
    let t = 0;                         // time since the intro started
    let eyes = calm ? 1 : 0, blink = 1, nextBlink = 3;
    let pulse = calm ? 2 : -1;         // where the current is on the cable: 0 at PIP, 1 at the top
    let live = calm;                   // the cable is lit once the pulse has passed
    let introDone = calm;

    function layout() {
      box = fitCanvas(canvas);
      anchor = { x: box.w * 0.5, y: -4 };
      s = clamp(box.h / 820, 0.42, 0.86);
      L = clamp(box.h * 0.4, 110, 330);
      rope.place(anchor.x, anchor.y, anchor.x, anchor.y + L);
      if (!calm && !introDone) { const e = rope.p[N]; e.px = e.x + 0; }
      draw();
    }

    function pipCentre() {
      const e = rope.p[N], q = rope.p[N - 1];
      const a = Math.atan2(-(e.x - q.x), e.y - q.y);
      return { x: e.x - Math.sin(a) * 127 * s, y: e.y + Math.cos(a) * 127 * s, a };
    }

    function step(dt) {
      t += dt;
      // The intro: eyes flicker on, PIP gets one push, then the current runs up the cable and lights the title.
      if (!introDone) {
        eyes = t < 0.35 ? 0 : t < 0.42 ? 1 : t < 0.5 ? 0.25 : 1;
        if (t >= 0.5 && t - dt < 0.5) { const e = rope.p[N]; e.px += 7; }
        if (t >= 0.75) pulse = (t - 0.75) / 0.42;
        if (pulse >= 1 && !live) { live = true; powerOn(); }
        if (pulse >= 1.3) introDone = true;
      }
      if (!calm) {
        nextBlink -= dt;
        if (nextBlink <= 0) { blink = Math.max(0.1, blink - dt * 14); if (blink <= 0.1) nextBlink = 3 + Math.random() * 3; }
        else blink = Math.min(1, blink + dt * 10);
      }

      const sub = 2;
      for (let i = 0; i < sub; i++) {
        if (grab) {
          const e = rope.p[N];
          // Keep the grab point within reach of the cable, so PIP can't be pulled through it.
          let gx = grab.x - grab.ox, gy = grab.y - grab.oy;
          const dx = gx - anchor.x, dy = gy - anchor.y, d = Math.hypot(dx, dy), max = L * 1.02;
          if (d > max) { gx = anchor.x + dx / d * max; gy = anchor.y + dy / d * max; }
          e.x = lerp(e.x, gx, 0.6); e.y = lerp(e.y, gy, 0.6);
        }
        rope.p[0].x = rope.p[0].px = anchor.x; rope.p[0].y = rope.p[0].py = anchor.y;
        rope.step(dt / sub, 2200, L / N, calm ? 0.9 : 0.995, 14);
      }
      draw();
      const still = rope.energy() < 0.08 && !grab;
      return !(still && introDone && (calm || nextBlink > 0.4));
    }

    function draw() {
      const { w, h, d } = box;
      ctx.setTransform(d, 0, 0, d, 0, 0);
      ctx.clearRect(0, 0, w, h);
      strokeRope(ctx, rope.p, live ? T.live : T.line, 4, live ? T["live-glow"] : null);
      if (pulse >= 0 && pulse < 1.3) {
        const pts = rope.p.slice().reverse();          // the current travels from PIP up to the socket
        strokePulse(ctx, pts, pulse, 0.3, 4);
      }
      // the socket the cable hangs from
      ctx.fillStyle = live ? T.live : T.line;
      rrect(ctx, anchor.x - 14, -8, 28, 18, 6); ctx.fill();
      const c = pipCentre();
      const e = rope.p[N];
      drawPip(ctx, e.x, e.y, s, c.a, { pivotY: PIP_HANG, eyes, blink: calm ? 1 : blink, res: box.d });
    }

    const wake = makeLoop(canvas, step);

    function toLocal(ev) { const r = canvas.getBoundingClientRect(); return { x: ev.clientX - r.left, y: ev.clientY - r.top }; }
    function overPip(p) { const c = pipCentre(); return Math.hypot(p.x - c.x, p.y - c.y) < 125 * s; }

    canvas.addEventListener("pointerdown", (ev) => {
      const p = toLocal(ev);
      if (!overPip(p)) return;
      const e = rope.p[N];
      grab = { x: p.x, y: p.y, ox: p.x - e.x, oy: p.y - e.y };
      canvas.setPointerCapture(ev.pointerId);
      canvas.classList.add("is-grabbing");
      if (!grabbed && hint) { grabbed = true; hint.classList.add("is-gone"); }
      wake();
    });
    canvas.addEventListener("pointermove", (ev) => {
      const p = toLocal(ev);
      if (grab) { grab.x = p.x; grab.y = p.y; wake(); }
      else canvas.style.cursor = overPip(p) ? "grab" : "default";
    });
    const release = () => {
      if (!grab) return;
      grab = null;
      canvas.classList.remove("is-grabbing");
      // Cap the throw so a flick swings PIP rather than launching it.
      const e = rope.p[N];
      const vx = e.x - e.px, vy = e.y - e.py, v = Math.hypot(vx, vy), max = 24;
      if (v > max) { e.px = e.x - vx / v * max; e.py = e.y - vy / v * max; }
      wake();
    };
    canvas.addEventListener("pointerup", release);
    canvas.addEventListener("pointercancel", release);
    canvas.style.cursor = "default";

    if ("ResizeObserver" in window) new ResizeObserver(() => { layout(); wake(); }).observe(canvas);
    themeHooks.push(() => { if (calm) { eyes = 1; live = true; introDone = true; pulse = 2; powerOn(); } draw(); wake(); });
    pipReady.push(() => { draw(); wake(); });
    layout();
    if (calm) powerOn();
    wake();
  })();

  // ---- The Tether Playground ------------------------------------------------------------------------------

  (function playground() {
    const canvas = document.getElementById("pg-canvas");
    const status = document.getElementById("pg-status");
    const buttons = [...document.querySelectorAll(".pg-btn")];
    const section = document.getElementById("playground");
    const ctx = canvas && canvas.getContext && canvas.getContext("2d");
    if (!ctx) {
      const fb = document.querySelector(".pg-fallback");
      if (fb) fb.hidden = false;
      buttons.forEach((b) => { b.disabled = true; });
      return;
    }

    // The world is 1000 x 500 units and gets scaled to fit. y points down.
    const W = 1000, H = 500, FLOOR = 400, GAP = [470, 640], G = 1800;
    const SOCKET = { x: 84, y: 286 };
    const SHELF = { x0: 104, x1: 236, y: 206 };
    const RING = { x: 555, y: 132, r: 20 };
    const DOOR = { x: 900, w: 54, top: 236 };
    const WIRE = [{ x: 84, y: 264 }, { x: 84, y: 44 }, { x: 927, y: 44 }, { x: 927, y: 226 }];
    const PS = 0.34;                                   // PIP's scale in the world
    const START_X = 318;

    let view = { s: 1, ox: 0, oy: 0, d: 1, w: 0, h: 0 };
    const rope = makeRope(18);
    rope.p[0].w = 0; rope.p[18].w = 0;

    let state;
    function fresh() {
      state = {
        pip: { x: START_X, y: FLOOR, vx: 0, vy: 0, a: 0, side: "left", squash: spring(0, 320, 14) },
        crate: { x: 170, y: SHELF.y, vx: 0, vy: 0, a: 0, w: 0, where: "shelf" },
        door: spring(0, 170, 16), wireLit: 0, powered: false,
        cable: null,                                   // { phase, target, t, end:{x,y}, from }
        swing: null, busy: false, t: 0, waves: 0,
        done: { power: false, pull: false, swing: false },
        hover: null,
      };
    }
    fresh();

    const hand = () => ({ x: state.pip.x + (state.pip.side === "left" ? 8 : -8), y: state.pip.y - 20 });
    const targetPoint = {
      power: () => ({ x: SOCKET.x, y: SOCKET.y }),
      pull: () => ({ x: state.crate.x + 32, y: state.crate.y - 32 }),
      swing: () => ({ x: RING.x, y: RING.y + RING.r }),
    };

    function say(text) { if (status) status.textContent = text; }
    function mark(mode) {
      state.done[mode] = true;
      const b = buttons.find((x) => x.dataset.mode === mode);
      if (b) { b.setAttribute("aria-pressed", "true"); b.classList.add("is-done"); }
      if (state.done.power && state.done.pull && state.done.swing) {
        say("That's the whole Tether: power, pull and swing. Every puzzle in the game is built from those three. Press R to go again.");
      }
    }

    // --- actions

    function act(mode) {
      if (mode === "reset") { reset(); return; }
      if (state.busy) return;
      if (state.done[mode]) {
        say({ power: "The door's already open.", pull: "The crate's already down.", swing: "PIP's already across." }[mode] + " Press R to reset.");
        return;
      }
      if (mode === "swing" && state.pip.side === "right") return;
      if (calm) { finishInstantly(mode); return; }
      state.busy = true;
      const h = hand();
      rope.place(h.x, h.y, h.x, h.y);
      state.cable = { mode, phase: "fire", t: 0, from: h, end: { ...h } };
      say({ power: "Aiming at the socket…", pull: "Hooking the crate…", swing: "Aiming at the ring…" }[mode]);
      wake();
    }

    function finishInstantly(mode) {
      if (mode === "power") { state.wireLit = 1; state.powered = true; state.door.snap(-170); say("Plugged in. Power runs along the wire and the door latches open."); }
      if (mode === "pull") { const c = state.crate; c.x = state.pip.side === "left" ? START_X - 82 : 300; c.y = FLOOR; c.a = 0; c.where = "floor"; say("Hooked the crate and tugged it off the shelf."); }
      if (mode === "swing") { state.pip.x = 800; state.pip.y = FLOOR; state.pip.side = "right"; say("PIP hooked the ring and swung across the gap."); }
      mark(mode);
      draw();
    }

    function reset() {
      fresh();
      buttons.forEach((b) => { if (b.dataset.mode !== "reset") { b.setAttribute("aria-pressed", "false"); b.classList.remove("is-done"); } });
      say("Back to the start. PIP is ready.");
      draw(); wake();
    }

    // --- simulation

    function step(dt) {
      state.t += dt;
      if (!calm) state.waves += dt;
      const pip = state.pip, crate = state.crate, cab = state.cable;

      if (cab) {
        cab.t += dt;
        const target = targetPoint[cab.mode]();
        if (cab.phase === "fire") {
          // The cable flies out fast and eases into the target.
          const k = easeOut(cab.t / 0.17);
          const h = hand();
          cab.end = { x: lerp(h.x, target.x, k), y: lerp(h.y, target.y, k) };
          if (cab.t >= 0.17) { cab.phase = "attached"; cab.t = 0; onAttach(cab); }
        } else if (cab.phase === "attached") {
          cab.end = target;
          attachedStep(cab, dt);
        } else if (cab.phase === "retract") {
          const k = easeOut(cab.t / 0.2);
          const h = hand();
          cab.end = { x: lerp(cab.from.x, h.x, k), y: lerp(cab.from.y, h.y, k) };
          if (cab.t >= 0.2) state.cable = null;
        }
      }

      // the swing
      const sw = state.swing;
      if (sw) swingStep(sw, dt);

      // door and wire
      if (state.wireLit > 0 && state.wireLit < 1) {
        state.wireLit = Math.min(1, state.wireLit + dt / 0.45);
        if (state.wireLit >= 1) { state.door.to = -170; }
      }
      state.door.step(dt);
      if (state.powered === false && state.door.to < 0 && state.door.x < -150) {
        state.powered = true; state.busy = false;
        say("Plugged in. Power runs along the wire and the door latches open.");
        mark("power");
        retract();
      }

      crateStep(crate, dt);
      pip.squash.step(dt);
      if (!sw) pip.a = lerp(pip.a, 0, 1 - Math.exp(-dt * 10));

      // the rope itself
      if (state.cable) {
        const h = hand(), e = state.cable.end;
        const d = Math.hypot(e.x - h.x, e.y - h.y);
        rope.p[0].x = h.x; rope.p[0].y = h.y;
        rope.p[18].x = e.x; rope.p[18].y = e.y;
        const slack = state.cable.phase === "attached" && state.cable.mode === "power" ? 1.08 : 1.01;
        for (let i = 0; i < 2; i++) rope.step(dt / 2, 1200, Math.max(1, d * slack / 18), 0.97, 10);
      }

      draw();
      const moving = state.cable || state.swing || !state.door.resting || !pip.squash.resting || crate.where === "moving" || crate.where === "falling" || crate.where === "settling" || (state.wireLit > 0 && state.wireLit < 1);
      return !!moving;
    }

    function onAttach(cab) {
      if (cab.mode === "power") {
        say("Plugged in. The current's on its way…");
        cab.pulse = 0;
      } else if (cab.mode === "pull") {
        state.crate.where = "moving";
        say("Hooked. Reeling the crate in…");
      } else if (cab.mode === "swing") {
        const p = state.pip;
        // Reel up to a point on the swing circle, then let gravity take over.
        const r = 196, th0 = -1.02;
        state.swing = { phase: "reel", t: 0, r, th: th0, w: 0, sx: p.x, sy: p.y - 20, th0 };
        say("Hooked the ring. Swing!");
      }
    }

    function attachedStep(cab, dt) {
      if (cab.mode === "power") {
        cab.pulse += dt / 0.28;
        if (cab.pulse >= 1 && state.wireLit === 0) state.wireLit = 0.001;
      }
    }

    function retract() {
      const c = state.cable;
      if (!c) return;
      c.phase = "retract"; c.t = 0; c.from = { ...c.end };
    }

    function swingStep(sw, dt) {
      const p = state.pip;
      sw.t += dt;
      if (sw.phase === "reel") {
        const k = easeOut(sw.t / 0.28);
        const tx = RING.x + Math.sin(sw.th0) * sw.r, ty = RING.y + Math.cos(sw.th0) * sw.r;
        p.x = lerp(sw.sx, tx, k); p.y = lerp(sw.sy, ty, k) + 20;
        p.a = lerp(0, -sw.th0 * 0.6, k);
        if (sw.t >= 0.28) { sw.phase = "swing"; }
      } else if (sw.phase === "swing") {
        // A pendulum: angular acceleration = -(g / r) sin(theta), stepped in small pieces.
        const n = 4, h = dt / n;
        for (let i = 0; i < n; i++) { sw.w += -(G / sw.r) * Math.sin(sw.th) * h; sw.th += sw.w * h; }
        p.x = RING.x + Math.sin(sw.th) * sw.r;
        p.y = RING.y + Math.cos(sw.th) * sw.r + 20;
        p.a = -sw.th * 0.6;
        if (sw.th > -sw.th0 * 0.78 && sw.w > 0) {
          // Let go on the way up: the cable's tangent speed carries PIP onto the far ledge.
          sw.phase = "fly";
          p.vx = sw.r * sw.w * Math.cos(sw.th);
          p.vy = -sw.r * sw.w * Math.sin(sw.th);
          retract();
          say("Let go…");
        }
      } else if (sw.phase === "fly") {
        p.vy += G * dt;
        p.x += p.vx * dt; p.y += p.vy * dt;
        const wall = DOOR.x - 40 + (state.door.x < -60 ? 70 : 0);
        if (p.x > wall) { p.x = wall; p.vx = -p.vx * 0.2; }
        if (p.y >= FLOOR && p.x > GAP[1] - 10) {
          p.y = FLOOR; p.vy = 0; p.vx = 0; p.side = "right";
          p.squash.v = 3.2;                           // a little landing squash
          state.swing = null; state.busy = false;
          say("PIP swung across the gap and stuck the landing.");
          mark("swing");
        }
      }
    }

    function crateStep(c, dt) {
      if (c.where === "moving") {
        // Tugged along the shelf: steady pull minus sliding friction.
        const h = hand();
        const dir = Math.sign(h.x - c.x) || 1;
        c.vx += dir * 1500 * dt;
        c.vx *= Math.exp(-dt * 5);
        c.x += c.vx * dt;
        c.a = -dir * 0.04;
        if (c.x - 32 > SHELF.x1 - 30 || c.x + 32 < SHELF.x0 + 30) {
          // Over the edge: unhook and let it drop.
          c.where = "falling"; c.w = c.vx * 0.012; c.vy = -40;
          retract();
          say("Off the shelf…");
        }
      } else if (c.where === "falling") {
        c.vy += G * dt;
        c.x += c.vx * dt; c.y += c.vy * dt;
        c.a += c.w * dt;
        const p = state.pip;
        if (p.side === "left" && c.x + 32 > p.x - 34) { c.x = p.x - 66; c.vx = -Math.abs(c.vx) * 0.3; p.squash.v = 1.2; }
        if (c.y >= FLOOR) {
          c.y = FLOOR;
          if (c.vy > 260) { c.vy = -c.vy * 0.28; c.vx *= 0.5; }
          else {
            c.vy = 0; c.vx = 0; c.where = "settling";
            c.settle = spring(c.a, 260, 20);
            c.settle.to = Math.round(c.a / (Math.PI / 2)) * (Math.PI / 2);
          }
        }
      } else if (c.where === "settling") {
        c.settle.step(dt); c.a = c.settle.x;
        if (c.settle.resting) {
          c.a = c.settle.to; c.where = "floor"; state.busy = false;
          say("Hooked the crate and tugged it off the shelf. Friction slows it, gravity does the rest.");
          mark("pull");
        }
      }
    }

    // --- drawing

    function layout() {
      const b = fitCanvas(canvas);
      const s = Math.min(b.w / W, b.h / H);
      view = { s, ox: (b.w - W * s) / 2, oy: (b.h - H * s) / 2, d: b.d, w: b.w, h: b.h };
      draw();
    }

    function label(text, x, y, align) {
      // At least 12 screen pixels, however small the playground is drawn.
      ctx.font = "600 " + Math.max(16, 12 / view.s).toFixed(1) + "px Figtree, system-ui, sans-serif";
      ctx.fillStyle = T.muted; ctx.textAlign = align || "center"; ctx.textBaseline = "middle";
      ctx.fillText(text, x, y);
    }

    function draw() {
      const { s, ox, oy, d, w, h } = view;
      ctx.setTransform(d, 0, 0, d, 0, 0);
      ctx.fillStyle = T["bg-sunk"]; ctx.fillRect(0, 0, w, h);
      ctx.setTransform(d * s, 0, 0, d * s, d * ox, d * oy);
      const top = -oy / s, bottom = (h - oy) / s;

      // water in the gap (Brightwater is a sea city)
      ctx.fillStyle = T["live-glow"];
      ctx.beginPath();
      ctx.moveTo(GAP[0], bottom);
      for (let x = GAP[0]; x <= GAP[1]; x += 10) ctx.lineTo(x, 458 + Math.sin(x * 0.06 + state.waves * 2.2) * (calm ? 0 : 3));
      ctx.lineTo(GAP[1], bottom); ctx.closePath(); ctx.fill();

      // floor
      ctx.fillStyle = T["bg-raised"];
      ctx.fillRect(-200, FLOOR, GAP[0] + 200, bottom - FLOOR + 10);
      ctx.fillRect(GAP[1], FLOOR, W - GAP[1] + 200, bottom - FLOOR + 10);
      ctx.fillStyle = T.line;
      ctx.fillRect(-200, FLOOR, GAP[0] + 200, 3);
      ctx.fillRect(GAP[1], FLOOR, W - GAP[1] + 200, 3);
      // ceiling
      ctx.fillStyle = T["bg-raised"]; ctx.fillRect(-200, top - 10, W + 400, 24 - top + 10);

      // wire from the socket to the door
      ctx.lineCap = "round"; ctx.lineJoin = "round";
      ctx.strokeStyle = T.line; ctx.lineWidth = 5;
      ctx.beginPath(); WIRE.forEach((p, i) => (i ? ctx.lineTo(p.x, p.y) : ctx.moveTo(p.x, p.y))); ctx.stroke();
      if (state.wireLit > 0) {
        const total = pathLength(WIRE);
        ctx.save();
        ctx.strokeStyle = T.live; ctx.shadowColor = T.live; ctx.shadowBlur = 10;
        ctx.beginPath(); walk(WIRE, 0, total * state.wireLit, ctx); ctx.stroke();
        ctx.restore();
        if (state.wireLit < 1) strokePulse(ctx, WIRE, state.wireLit, 0.12, 5);
      }

      // socket
      const sl = state.wireLit > 0;
      ctx.fillStyle = sl ? T.live : T["bg-raised"];
      ctx.strokeStyle = sl ? T.live : T.muted; ctx.lineWidth = 3;
      rrect(ctx, SOCKET.x - 18, SOCKET.y - 22, 36, 44, 9); ctx.fill(); ctx.stroke();
      ctx.fillStyle = sl ? T.bg : T.muted;
      rrect(ctx, SOCKET.x - 8, SOCKET.y - 9, 5, 12, 2); ctx.fill();
      rrect(ctx, SOCKET.x + 3, SOCKET.y - 9, 5, 12, 2); ctx.fill();
      label("socket", SOCKET.x, SOCKET.y + 40);

      // door: slides up into its frame
      const dTop = DOOR.top;
      ctx.save();
      ctx.beginPath(); ctx.rect(DOOR.x - 10, dTop, DOOR.w + 20, FLOOR - dTop); ctx.clip();
      if (state.door.x < -10) { ctx.fillStyle = T["live-glow"]; ctx.fillRect(DOOR.x, dTop, DOOR.w, FLOOR - dTop); }
      ctx.fillStyle = state.powered || state.wireLit >= 1 ? T.muted : T.line;
      rrect(ctx, DOOR.x, dTop + state.door.x, DOOR.w, FLOOR - dTop, 6); ctx.fill();
      ctx.fillStyle = T["bg-sunk"];
      for (let i = 0; i < 4; i++) ctx.fillRect(DOOR.x + 10, dTop + state.door.x + 24 + i * 34, DOOR.w - 20, 4);
      ctx.restore();
      ctx.fillStyle = T["bg-raised"]; ctx.fillRect(DOOR.x - 12, dTop - 14, DOOR.w + 24, 14);
      ctx.fillStyle = state.wireLit >= 1 ? T.live : T.line; ctx.fillRect(DOOR.x - 12, dTop - 4, DOOR.w + 24, 4);
      label("door", DOOR.x - 22, dTop + 30, "right");

      // shelf
      ctx.fillStyle = T["bg-raised"];
      rrect(ctx, SHELF.x0, SHELF.y, SHELF.x1 - SHELF.x0, 12, 4); ctx.fill();
      ctx.fillStyle = T.line; ctx.fillRect(SHELF.x0 + 10, SHELF.y + 12, 4, 26); ctx.fillRect(SHELF.x1 - 14, SHELF.y + 12, 4, 26);

      // ring
      ctx.strokeStyle = T.muted; ctx.lineWidth = 4;
      ctx.beginPath(); ctx.moveTo(RING.x, 24); ctx.lineTo(RING.x, RING.y - RING.r); ctx.stroke();
      ctx.strokeStyle = state.done.swing || (state.cable && state.cable.mode === "swing") ? T.live : T.text; ctx.lineWidth = 6;
      ctx.beginPath(); ctx.arc(RING.x, RING.y, RING.r, 0, Math.PI * 2); ctx.stroke();
      label("ring", RING.x + 46, RING.y, "left");

      // crate
      const c = state.crate;
      ctx.save();
      ctx.translate(c.x, c.y - 32); ctx.rotate(c.a);
      ctx.fillStyle = T.pip; rrect(ctx, -32, -32, 64, 64, 8); ctx.fill();
      ctx.strokeStyle = "rgba(27, 18, 6, 0.35)"; ctx.lineWidth = 4;
      ctx.beginPath(); ctx.moveTo(-22, -22); ctx.lineTo(22, 22); ctx.moveTo(22, -22); ctx.lineTo(-22, 22); ctx.stroke();
      ctx.restore();
      if (c.where === "shelf") label("crate", c.x, SHELF.y - 84);

      // aim reticle on hover
      if (state.hover && !state.busy) {
        const tp = state.hover === "pull" ? { x: c.x, y: c.y - 32 } : state.hover === "power" ? SOCKET : RING;
        ctx.save();
        ctx.strokeStyle = T.live; ctx.lineWidth = 3; ctx.setLineDash([8, 8]);
        ctx.lineDashOffset = calm ? 0 : -state.t * 30;
        ctx.beginPath(); ctx.arc(tp.x, tp.y, 52, 0, Math.PI * 2); ctx.stroke();
        ctx.restore();
      }

      // the Tether
      if (state.cable) {
        strokeRope(ctx, rope.p, T.live, 4, T["live-glow"]);
        const e = state.cable.end;
        ctx.fillStyle = T.live; ctx.beginPath(); ctx.arc(e.x, e.y, 6, 0, Math.PI * 2); ctx.fill();
        if (state.cable.mode === "power" && state.cable.pulse != null && state.cable.pulse < 1.2) strokePulse(ctx, rope.p, state.cable.pulse, 0.35, 4);
      }

      // PIP
      const p = state.pip;
      const facing = p.side === "left" ? 1 : -1;
      ctx.save();
      ctx.translate(p.x, p.y); ctx.scale(facing, 1); ctx.translate(-p.x, -p.y);
      drawPip(ctx, p.x, p.y, PS, p.a * facing, { pivotY: PIP_STAND, squash: clamp(p.squash.x * 0.08, -0.12, 0.18), res: view.s * view.d });
      ctx.restore();
    }

    // --- input

    function toWorld(ev) {
      const r = canvas.getBoundingClientRect();
      return { x: (ev.clientX - r.left - view.ox) / view.s, y: (ev.clientY - r.top - view.oy) / view.s };
    }
    function hit(p) {
      const c = state.crate;
      if (Math.hypot(p.x - SOCKET.x, p.y - SOCKET.y) < 50) return "power";
      if (Math.abs(p.x - c.x) < 54 && Math.abs(p.y - (c.y - 32)) < 54) return "pull";
      if (Math.hypot(p.x - RING.x, p.y - RING.y) < 56) return "swing";
      if (p.x > DOOR.x - 20 && p.y > DOOR.top - 20) return "power";
      return null;
    }
    canvas.addEventListener("pointermove", (ev) => {
      const m = hit(toWorld(ev));
      if (m !== state.hover) { state.hover = m; canvas.style.cursor = m ? "pointer" : "crosshair"; draw(); wake(); }
    });
    canvas.addEventListener("pointerleave", () => { state.hover = null; draw(); });
    canvas.addEventListener("click", (ev) => {
      const m = hit(toWorld(ev));
      if (m) act(m);
      else if (!state.busy) say("Try the socket, the crate or the ring.");
    });
    buttons.forEach((b) => {
      if (b.dataset.mode !== "reset") b.setAttribute("aria-pressed", "false");
      b.addEventListener("click", () => act(b.dataset.mode));
    });

    // 1, 2, 3 and R work while focus is in the playground (the picture or its buttons), so single-key shortcuts
    // never fire anywhere else on the page or fight a screen reader's own keys.
    canvas.tabIndex = 0;
    section.addEventListener("keydown", (ev) => {
      if (ev.altKey || ev.ctrlKey || ev.metaKey) return;
      const m = { "1": "power", "2": "pull", "3": "swing", r: "reset", R: "reset" }[ev.key];
      if (m) { ev.preventDefault(); act(m); }
    });

    const wake = makeLoop(canvas, step);
    if ("ResizeObserver" in window) new ResizeObserver(() => { layout(); wake(); }).observe(canvas);
    themeHooks.push(() => { draw(); wake(); });
    pipReady.push(() => { draw(); wake(); });
    canvas.setAttribute("role", "application");
    canvas.setAttribute("aria-label", "Tether playground: PIP on a ledge with a wall socket and a crate on a shelf, a ring over a gap, and a closed door on the far side. Press 1 to power the door, 2 to pull the crate, 3 to swing across, R to reset.");
    layout();
    wake();
  })();

  // ---- The calls board ------------------------------------------------------------------------------------

  (function callsBoard() {
    const list = document.getElementById("circuit");
    const frame = document.getElementById("clip-frame");
    const title = document.getElementById("clip-title");
    const place = document.getElementById("clip-place");
    const checks = document.getElementById("clip-checks");
    if (!list || !frame) return;

    // From the README's level-by-level tracker. Self-test and playtest are honest: none yet for campaign calls.
    const R = "v0.3.0";
    const chapters = [
      { n: "Chapter 1", place: "Bureau HQ", calls: [
        ["Test01_StackEmUp", "Training 1: Stack 'Em Up", 0, R],
        ["Test02_ConveyorCaper", "Training 2: Conveyor Caper", 0, R],
        ["Test03_SwingShift", "Training 3: Swing Shift", 0, R],
        ["Ch1_Boss_CoffeeCrisis", "Coffee Crisis", 1, R]] },
      { n: "Chapter 2", place: "Nonna Lucia's Trattoria", calls: [
        ["Ch2_Call01_PastaEmergency", "Pasta Emergency", 0, R],
        ["Ch2_Call02_WhiskBusiness", "Whisk Business", 0, R],
        ["Ch2_Boss_PastaPanic", "Pasta Panic", 1, R]] },
      { n: "Chapter 3", place: "Brightwater Park", calls: [
        ["Ch3_Call01_LooseScrews", "Loose Screws", 0], ["Ch3_Boss_UpATree", "Up a Tree", 1]] },
      { n: "Chapter 4", place: "School Science Fair", calls: [
        ["Ch4_Call01_CatchTheHum", "Catch the Hum", 0], ["Ch4_Boss_SlightlyTooBig", "Slightly Too Big", 1]] },
      { n: "Chapter 5", place: "Docks & Lighthouse", calls: [
        ["Ch5_Call01_HeavyLifting", "Heavy Lifting", 0], ["Ch5_Boss_LightsOut", "Lights Out", 1]] },
      { n: "Chapter 6", place: "Funland Pier", calls: [
        ["Ch6_Call01_CarouselChaos", "Carousel Chaos", 0], ["Ch6_Boss_StuckAtTheTop", "Stuck at the Top", 1]] },
      { n: "Chapter 7", place: "The Old Rig", calls: [
        ["Ch7_Call01_GoingDown", "Going Down", 0], ["Ch7_Boss_UnitZero", "Unit Zero", 1]] },
      { n: "Chapter 8", place: "Founders' Festival", calls: [
        ["Ch8_Call01_StageFright", "Stage Fright", 0], ["Ch8_Boss_TheFoundersSong", "The Founders' Song", 1]] },
      { n: "Side call", place: "Brightwater Reach", calls: [
        ["PumpHouse", "The Pump House", 0, null, { filmed: false, test: "21/21" }]] },
    ];

    const nodes = [];
    chapters.forEach((ch) => {
      const li = document.createElement("li");
      li.className = "chapter";
      const name = document.createElement("div");
      name.className = "chapter-name";
      name.innerHTML = ch.n + "<span></span>";
      name.querySelector("span").textContent = ch.place;
      const wire = document.createElement("div");
      wire.className = "chapter-wire";
      ch.calls.forEach(([id, label, big, released, extra]) => {
        const call = { id, label, big: !!big, released, chapter: ch, filmed: true, test: null, ...(extra || {}) };
        const b = document.createElement("button");
        b.type = "button";
        b.className = "node";
        b.dataset.built = "true";
        b.dataset.big = String(call.big);
        b.setAttribute("aria-pressed", "false");
        b.innerHTML = '<span class="pin" aria-hidden="true"></span><span class="node-name"></span>';
        b.querySelector(".node-name").textContent = label;
        if (call.big) { const t = document.createElement("span"); t.className = "tag"; t.textContent = "Big Rescue"; b.appendChild(t); }
        b.addEventListener("click", () => select(call, b));
        wire.appendChild(b);
        nodes.push([call, b]);
      });
      li.append(name, wire);
      list.appendChild(li);
    });

    let token = 0;
    function select(call, button) {
      nodes.forEach(([, b]) => b.setAttribute("aria-pressed", String(b === button)));
      const mine = ++token;
      title.textContent = (call.big ? "Big Rescue: " : "") + call.label;
      place.textContent = call.chapter.n + ", " + call.chapter.place;
      checks.replaceChildren(
        check("Builds and runs", true, "on"),
        check(call.filmed ? "Filmed in Play Mode" : "Not filmed yet", call.filmed, "film"),
        check(call.test ? "Self-test " + call.test : "No self-test yet", !!call.test, "test"),
        check("Not playtested yet", false),
        check(call.released ? "In release " + call.released : "Not released yet", !!call.released, "on"));
      frame.replaceChildren();

      if (!call.filmed) {
        frame.appendChild(msgEl("Not filmed yet. It builds and passes its self-test, but nobody has recorded it in Play Mode.", "empty"));
        return;
      }
      const img = new Image();
      img.alt = (call.big ? "Big Rescue: " : "") + call.label + ", a still from a Play Mode run";
      img.className = "is-loading";
      img.decoding = "async";
      const loading = msgEl("Loading the picture…");
      frame.append(img, loading);
      img.onload = () => {
        if (mine !== token) return;
        img.classList.remove("is-loading");
        loading.remove();
        const play = document.createElement("button");
        play.type = "button";
        play.className = "btn btn-small clip-play";
        play.textContent = "Play the clip";
        play.addEventListener("click", () => playClip(call, img, play, mine));
        frame.appendChild(play);
      };
      img.onerror = () => {
        if (mine !== token) return;
        img.remove();
        loading.replaceWith(msgEl("This picture didn't load. Check your connection and pick the call again.", "error"));
      };
      img.src = "media/" + call.id + ".png";
    }

    function playClip(call, img, play, mine) {
      play.disabled = true;
      play.textContent = "Loading the clip…";
      img.classList.add("is-loading");
      const gif = new Image();
      gif.onload = () => {
        if (mine !== token) return;
        img.src = gif.src;
        img.alt = (call.big ? "Big Rescue: " : "") + call.label + ", recorded in Play Mode";
        img.classList.remove("is-loading");
        play.remove();
      };
      gif.onerror = () => {
        if (mine !== token) return;
        img.classList.remove("is-loading");
        play.remove();
        frame.appendChild(msgEl("The clip didn't load, so here's the still. It may still be uploading; try again in a minute.", "error"));
      };
      gif.src = "media/gameplay/" + call.id + ".gif";
    }

    function msgEl(text, kind) {
      const p = document.createElement("p");
      p.className = kind === "empty" ? "clip-empty" : "clip-msg";
      if (kind === "error") p.dataset.kind = "error";
      p.textContent = text;
      return p;
    }
    function check(text, ok, kind) {
      const li = document.createElement("li");
      li.dataset.ok = String(ok);
      const dot = document.createElement("span");
      dot.className = "dot " + (ok ? "dot-" + kind : "dot-off");
      dot.setAttribute("aria-hidden", "true");
      li.append(dot, document.createTextNode(text));
      return li;
    }

    // Start on the training level that teaches the swing, since you just tried it above.
    const first = nodes.find(([c]) => c.id === "Test03_SwingShift");
    if (first) select(first[0], first[1]);
  })();

  // ---- Your bot: real catalogue colours ---------------------------------------------------------------------

  (function swatches() {
    const pip = document.getElementById("pip");
    if (!pip) return;
    const shells = [["Cloud", "#F7F7FF"], ["Sunny", "#FFE06B"], ["Coral", "#FF9985"], ["Mint", "#9EEDC7"], ["Sky", "#99CCFF"],
      ["Lilac", "#CCB3FF"], ["Peach", "#FFCCA8"], ["Bubblegum", "#FFB3D9"], ["Graphite", "#5C6173"], ["Sand", "#E6D6B3"]];
    const eyes = [["Cyan", "#36DBFF"], ["Amber", "#FFA726"], ["Mint", "#3BFF75"], ["Pink", "#FF52A3"], ["Violet", "#9152FF"],
      ["Lime", "#91FF24"], ["White", "#FFFFFF"]];

    function group(rowId, items, prop, labelWord) {
      const row = document.getElementById(rowId);
      if (!row) return;
      row.setAttribute("role", "radiogroup");
      row.setAttribute("aria-label", labelWord + " colour");
      const btns = items.map(([name, hex], i) => {
        const b = document.createElement("button");
        b.type = "button";
        b.className = "swatch";
        b.style.setProperty("--c", hex);
        b.setAttribute("role", "radio");
        b.setAttribute("aria-label", name);
        b.title = name;
        b.addEventListener("click", () => pick(i, false));
        b.addEventListener("keydown", (ev) => {
          const d = { ArrowRight: 1, ArrowDown: 1, ArrowLeft: -1, ArrowUp: -1 }[ev.key];
          if (ev.key === "Home") { ev.preventDefault(); pick(0, true); }
          else if (ev.key === "End") { ev.preventDefault(); pick(btns.length - 1, true); }
          else if (d) { ev.preventDefault(); pick((i + d + btns.length) % btns.length, true); }
        });
        row.appendChild(b);
        return b;
      });
      function pick(i, focus) {
        btns.forEach((b, j) => { b.setAttribute("aria-checked", String(i === j)); b.tabIndex = i === j ? 0 : -1; });
        pip.style.setProperty(prop, items[i][1]);
        if (focus) btns[i].focus();
      }
      pick(0, false);
    }
    group("shells", shells, "--shell", "Shell");
    group("eyes", eyes, "--eye", "Eye");
  })();
})();
