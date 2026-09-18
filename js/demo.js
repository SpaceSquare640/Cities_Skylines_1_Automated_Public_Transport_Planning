/* 模擬規劃器 —— 這裡沒有任何真實演算法，全部是為了「看起來像那麼回事」而寫的展示程式碼。 */
(function () {
  "use strict";

  var SVG = "http://www.w3.org/2000/svg";
  var W = 800, H = 560;

  var el = {
    map: null, zones: null, roads: null, lines: null, stops: null,
    legend: null, status: null, log: null
  };

  var state = { planned: false, running: false, lines: [] };

  /* ---------- 小工具 ---------- */
  function t(key) { return window.i18n.t(key); }
  function nfmt(n) { return n.toLocaleString(window.i18n.bcp47()); }

  function mk(tag, attrs) {
    var n = document.createElementNS(SVG, tag);
    for (var k in attrs) n.setAttribute(k, attrs[k]);
    return n;
  }

  // 可重現的偽亂數，讓同樣參數得到同樣的「規劃結果」
  function rng(seed) {
    var s = seed % 2147483647;
    if (s <= 0) s += 2147483646;
    return function () { s = (s * 16807) % 2147483647; return (s - 1) / 2147483646; };
  }

  function log(msg, cls) {
    var line = document.createElement("div");
    if (cls) line.className = cls;
    line.textContent = msg;
    el.log.appendChild(line);
    el.log.scrollTop = el.log.scrollHeight;
  }

  function clearLog() { el.log.innerHTML = ""; }

  /* ---------- 城市底圖 ---------- */
  var ZONE_COLORS = { res: "#1f3d2b", com: "#2b3550", ind: "#3d3320", office: "#1f3a44" };
  var zoneDefs = [];

  function buildCity() {
    var r = rng(4242);
    var types = ["res", "res", "res", "com", "ind", "office"];
    for (var gx = 0; gx < 8; gx++) {
      for (var gy = 0; gy < 6; gy++) {
        if (r() < 0.18) continue; // 留一些空地
        var type = types[Math.floor(r() * types.length)];
        var x = 30 + gx * 94, y = 26 + gy * 88;
        var w = 60 + r() * 24, h = 52 + r() * 20;
        zoneDefs.push({ x: x, y: y, w: w, h: h, cx: x + w / 2, cy: y + h / 2, type: type });
        el.zones.appendChild(mk("rect", {
          x: x, y: y, width: w, height: h, rx: 4,
          fill: ZONE_COLORS[type], opacity: 0.85
        }));
      }
    }
    // 主幹道
    for (var i = 1; i < 8; i++) {
      el.roads.appendChild(mk("line", { x1: i * 94 + 12, y1: 0, x2: i * 94 + 12, y2: H, stroke: "rgba(255,255,255,.10)", "stroke-width": 3 }));
    }
    for (var j = 1; j < 6; j++) {
      el.roads.appendChild(mk("line", { x1: 0, y1: j * 88 + 10, x2: W, y2: j * 88 + 10, stroke: "rgba(255,255,255,.10)", "stroke-width": 3 }));
    }
  }

  /* ---------- 規劃（模擬） ---------- */
  function enabledModes() {
    return Array.prototype.slice
      .call(document.querySelectorAll(".modes input[type=checkbox]"))
      .filter(function (c) { return c.checked; })
      .map(function (c) { return c.dataset.mode; });
  }

  var MODE_STYLE = {
    bus:   { color: "#f2a33c", width: 4,   dash: null },
    metro: { color: "#3fb9ff", width: 5.5, dash: null },
    tram:  { color: "#00d4a0", width: 3.5, dash: "10 5" }
  };

  function makeRoute(seed, mode) {
    var r = rng(seed);
    var pts = [];
    var count = mode === "metro" ? 5 : 7 + Math.floor(r() * 4);
    var pool = zoneDefs.slice();
    // 從某個角落出發，貪心地往下一個「還沒服務到」的分區走
    var cur = pool[Math.floor(r() * pool.length)];
    for (var i = 0; i < count && pool.length; i++) {
      pool.sort(function (a, b) {
        var da = Math.hypot(a.cx - cur.cx, a.cy - cur.cy);
        var db = Math.hypot(b.cx - cur.cx, b.cy - cur.cy);
        return da - db;
      });
      var pick = pool.splice(Math.min(1 + Math.floor(r() * 3), pool.length - 1), 1)[0];
      if (!pick) break;
      pts.push({ x: pick.cx, y: pick.cy });
      cur = pick;
    }
    return pts;
  }

  function pathFrom(pts) {
    // 用直角轉折畫線，比較像道路而不是直線飛過去
    var d = "M " + pts[0].x + " " + pts[0].y;
    for (var i = 1; i < pts.length; i++) {
      d += " L " + pts[i].x + " " + pts[i - 1].y + " L " + pts[i].x + " " + pts[i].y;
    }
    return d;
  }

  function drawLine(pts, mode, delay) {
    var st = MODE_STYLE[mode];
    var p = mk("path", {
      d: pathFrom(pts), fill: "none", stroke: st.color,
      "stroke-width": st.width, "stroke-linecap": "round", "stroke-linejoin": "round",
      opacity: 0.95
    });
    if (st.dash) p.setAttribute("stroke-dasharray", st.dash);
    el.lines.appendChild(p);

    var len = p.getTotalLength();
    if (!st.dash) {
      p.style.strokeDasharray = len;
      p.style.strokeDashoffset = len;
      p.style.transition = "stroke-dashoffset 1.1s ease-out";
      setTimeout(function () { p.style.strokeDashoffset = "0"; }, delay);
    }

    pts.forEach(function (pt, i) {
      var c = mk("circle", {
        cx: pt.x, cy: pt.y, r: mode === "metro" ? 6 : 4.5,
        fill: "#0a1017", stroke: st.color, "stroke-width": 2, opacity: 0
      });
      el.stops.appendChild(c);
      setTimeout(function () {
        c.style.transition = "opacity .3s";
        c.setAttribute("opacity", "1");
      }, delay + 200 + i * 60);
    });

    return pts.length;
  }

  function clearNetwork() {
    el.lines.innerHTML = "";
    el.stops.innerHTML = "";
    state.lines = [];
    state.planned = false;
  }

  function computeStats(stopCount, lineCount) {
    var cov = +document.getElementById("coverage").value;
    var spacing = +document.getElementById("spacing").value;
    var maxDetour = +document.getElementById("detour").value / 100;

    var coverage = Math.min(99, Math.round(cov * (0.82 + lineCount * 0.03)));

    // Routes come out somewhere under the ceiling the player set, never above it —
    // in the real planner that ceiling is a hard threshold, not a target.
    var detour = 1 + (maxDetour - 1) * (0.45 + Math.min(lineCount, 8) * 0.03);

    // Wider spacing means each stop draws from a larger catchment.
    var riders = Math.round(stopCount * 260 * (coverage / 100) * (spacing / 400));
    var perStop = stopCount > 0 ? Math.round(riders / stopCount) : 0;
    var cost = Math.round(lineCount * 2400 + stopCount * 260);

    document.getElementById("sLines").textContent = lineCount;
    document.getElementById("sStops").textContent = stopCount;
    document.getElementById("sCover").textContent = coverage + "%";
    document.getElementById("sDetour").textContent = detour.toLocaleString(window.i18n.bcp47(), {
      minimumFractionDigits: 2, maximumFractionDigits: 2
    });
    document.getElementById("sPassengers").textContent = nfmt(perStop);
    document.getElementById("sCost").textContent = "₡" + nfmt(cost);
  }

  function runPlan(fromScratch) {
    if (state.running) return;
    var modes = enabledModes();
    if (!modes.length) { log(t("logModeNone"), "warn"); return; }

    state.running = true;
    el.status.textContent = t("statusRun");
    clearLog();
    if (fromScratch) log(t("logReset"));
    log(t("logScan"));

    clearNetwork();

    var seedBase = +document.getElementById("coverage").value * 31
      + +document.getElementById("spacing").value * 17
      + +document.getElementById("detour").value * 7;

    setTimeout(function () { log(t("logDemand")); }, 450);
    setTimeout(function () { log(t("logRoute")); }, 900);

    var totalStops = 0, lineCount = 0;
    var coverageTarget = +document.getElementById("coverage").value;
    var perMode = Math.max(1, Math.round(coverageTarget / 35));

    modes.forEach(function (mode, mi) {
      for (var k = 0; k < perMode; k++) {
        var pts = makeRoute(seedBase + mi * 97 + k * 13, mode);
        if (pts.length < 2) continue;
        totalStops += drawLine(pts, mode, 1000 + (lineCount * 380));
        lineCount++;
        state.lines.push({ mode: mode, stops: pts.length });
      }
    });

    var finish = 1400 + lineCount * 380;
    setTimeout(function () {
      computeStats(totalStops, lineCount);
      log(t("logDone"), "ok");
      el.status.textContent = t("statusDone");
      state.running = false;
      state.planned = true;
    }, finish);
  }

  function runAdjust() {
    if (state.running) return;
    if (!state.planned) { log(t("logAdjustNone"), "warn"); return; }
    state.running = true;
    clearLog();
    log(t("logAdjust"));
    el.status.textContent = t("statusRun");

    // 只是把站點稍微挪動 + 重算統計，象徵「微調而非重做」
    Array.prototype.forEach.call(el.stops.children, function (c, i) {
      setTimeout(function () {
        c.setAttribute("cx", +c.getAttribute("cx") + (i % 2 ? 6 : -6));
      }, 60 * i);
    });

    setTimeout(function () {
      var stops = el.stops.children.length;
      computeStats(stops, state.lines.length);
      log(t("logAdjustDone"), "ok");
      el.status.textContent = t("statusDone");
      state.running = false;
    }, 900);
  }

  /* ---------- 介面綁定 ---------- */
  function bindControls() {
    var pairs = [
      ["coverage", "coverageOut", function (v) { return v + "%"; }],
      ["spacing", "spacingOut", function (v) { return v + " " + t("unitMetre"); }],
      ["detour", "detourOut", function (v) { return (v / 100).toFixed(2); }]
    ];
    pairs.forEach(function (p) {
      var input = document.getElementById(p[0]), out = document.getElementById(p[1]);
      input.addEventListener("input", function () { out.textContent = p[2](input.value); });
    });

    document.getElementById("btnPlan").addEventListener("click", function () { runPlan(false); });
    document.getElementById("btnAdjust").addEventListener("click", runAdjust);
    document.getElementById("btnReset").addEventListener("click", function () { runPlan(true); });

    document.querySelectorAll(".gu-tab").forEach(function (tab) {
      tab.addEventListener("click", function () {
        document.querySelectorAll(".gu-tab").forEach(function (x) { x.classList.remove("is-active"); });
        document.querySelectorAll(".gu-pane").forEach(function (x) { x.classList.remove("is-active"); });
        tab.classList.add("is-active");
        document.querySelector('.gu-pane[data-pane="' + tab.dataset.tab + '"]').classList.add("is-active");
      });
    });
  }

  function renderLegend() {
    el.legend.innerHTML =
      '<span><i class="dot bus"></i> ' + t("modeBus") + "</span>" +
      '<span><i class="dot metro"></i> ' + t("modeMetro") + "</span>" +
      '<span><i class="dot tram"></i> ' + t("modeTram") + "</span>";
  }

  document.addEventListener("DOMContentLoaded", function () {
    el.map = document.getElementById("cityMap");
    el.zones = document.getElementById("zonesLayer");
    el.roads = document.getElementById("roadsLayer");
    el.lines = document.getElementById("linesLayer");
    el.stops = document.getElementById("stopsLayer");
    el.legend = document.getElementById("mapLegend");
    el.status = document.getElementById("mapStatus");
    el.log = document.getElementById("log");

    buildCity();
    bindControls();
    renderLegend();
    el.status.textContent = t("statusIdle");
    log(t("logIdle"));
  });

  document.addEventListener("langchange", function () {
    // i18n renders once before this module has looked up its elements.
    if (!el.legend) return;
    renderLegend();
    var sp = document.getElementById("spacing");
    if (sp) document.getElementById("spacingOut").textContent = sp.value + " " + t("unitMetre");
    if (!state.running) {
      el.status.textContent = state.planned ? t("statusDone") : t("statusIdle");
      if (state.planned) {
        // Units and number formats are locale-dependent, so redraw the stats.
        computeStats(el.stops.children.length, state.lines.length);
      } else {
        clearLog();
        log(t("logIdle"));
      }
    }
  });
})();
