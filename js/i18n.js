/*
 * i18n for the preview site.
 *
 * English is the default and is embedded here, so the page is never blank and
 * never waits on a network request. Every other locale lives in js/i18n/<tag>.json
 * and is fetched on demand; any key a locale omits falls back to English.
 */
(function () {
  "use strict";

  var DEFAULT_LANG = "en";

  // Locale list mirrors the languages Cities: Skylines I ships with.
  var LOCALES = [
    { tag: "en",      label: "English" },
    { tag: "zh-Hant", label: "繁體中文" },
    { tag: "zh-Hans", label: "简体中文" },
    { tag: "ja",      label: "日本語" },
    { tag: "ko",      label: "한국어" },
    { tag: "de",      label: "Deutsch" },
    { tag: "fr",      label: "Français" },
    { tag: "es",      label: "Español" },
    { tag: "pt-BR",   label: "Português (BR)" },
    { tag: "ru",      label: "Русский" },
    { tag: "pl",      label: "Polski" }
  ];

  var EN = {
    banner: "Preview site only — every number and route here is simulated. No real mod functionality, no network calls.",
    brandSub: "Cities: Skylines I mod · Preview",
    navFeatures: "Features", navDemo: "Live Demo", navPanel: "Mod UI", navInstall: "Install",
    langLabel: "Language",

    heroEyebrow: "Automated public transport planning",
    heroTitle: "Let your city grow its own bus, metro and tram network",
    heroLead: "Analyses population, jobs and commuter demand, then generates a full transit network in one click — or just refines the lines you already drew, or wipes everything and starts over.",
    heroCta: "Try the demo",
    metaGameL: "Game", metaDistL: "Distribution", metaLicL: "License", metaLicV: "Open source",

    featTitle: "Core features",
    featLead: "What the mod is intended to do. Everything in this preview is simulated.",
    f1t: "One-click auto planning",
    f1d: "Scans residential, commercial and industrial zones, then picks route shapes, stop spacing and transport modes from demand density.",
    f2t: "Refine existing lines",
    f2d: "Keeps the lines you drew by hand and only proposes fixes where coverage is thin or lines overlap too much.",
    f3t: "Wipe and replan",
    f3d: "Clears every existing line and computes a brand new network from scratch — handy after a major city rebuild.",
    f4t: "In-game UI panel",
    f4d: "Everything runs from an in-game panel, with a preview of the plan before you commit to it.",

    demoTitle: "Live demo: simulated planner",
    demoLead: "Tune the parameters and press Auto-plan to watch a network appear. The city, the data and the algorithm are all mock-ups.",
    ctrlTitle: "Planning parameters",
    ctrlCoverage: "Target coverage", ctrlHeadway: "Target headway", ctrlBudget: "Budget level", ctrlModes: "Enabled modes",
    modeBus: "Bus", modeMetro: "Metro", modeTram: "Tram",
    btnPlan: "Auto-plan network", btnAdjust: "Refine existing lines", btnReset: "Delete all & replan",
    statLines: "Lines", statStops: "Stops", statCover: "Coverage", statWait: "Avg wait", statRiders: "Daily riders", statCost: "Weekly cost",
    unitMin: "min",

    logIdle: "> Idle. Set your parameters and press Auto-plan network.",
    logScan: "> Scanning city zones...",
    logDemand: "> Building commuter demand matrix...",
    logRoute: "> Generating candidate routes and optimising...",
    logDone: "> Plan complete (simulated).",
    logAdjustNone: "! Nothing to refine yet — run the auto-planner first.",
    logAdjust: "> Refining stop spacing and overlapping segments...",
    logAdjustDone: "> Refinement done. Hand-drawn lines were left untouched.",
    logReset: "> All lines deleted. Replanning from scratch...",
    logModeNone: "! Enable at least one transport mode.",
    statusIdle: "No plan", statusRun: "Planning...", statusDone: "Applied (simulated)",

    uiTitle: "Mod UI mock-up",
    uiLead: "How the in-game panel is meant to look. The buttons here do nothing.",
    guTitle: "Automated Public Transport Planning",
    guTab1: "Plan", guTab2: "Lines", guTab3: "Options",
    guPlanHint: "Pick a scope and the modes to use; results are shown as a preview first.",
    guRowScope: "Scope", guRowScopeV: "Whole city",
    guRowMode: "Modes", guRowModeV: "Bus + Metro + Tram",
    guRowKeep: "Keep manual lines", guRowKeepV: "Yes",
    guPlanBtn: "Start planning",
    guLinesHint: "Auto-generated lines are tagged, so they can be deleted or locked individually.",
    guOpt1: "Buy vehicles after planning", guOpt2: "Avoid overlapping existing lines", guOpt3: "Allow long cross-city routes", guOpt4: "Default headway",

    insTitle: "Installation",
    ins1: "Subscribe to \"Automated Public Transport Planning\" on the Steam Workshop.",
    ins2: "Launch Cities: Skylines and enable the mod under Content Manager -> Mods.",
    ins3: "Load a save and open the mod panel from the in-game toolbar.",
    ins4: "Set your planning parameters, preview, then apply.",
    insNote: "Note: this is a preview page — there is no download yet. The link will be updated when a real build ships.",
    footNote: "Preview site · simulated content, not the final feature set."
  };

  var loaded = { en: EN };

  var i18n = {
    lang: DEFAULT_LANG,
    locales: LOCALES,
    /** Translate a key, falling back to English and then to the key itself. */
    t: function (key) {
      var dict = loaded[i18n.lang];
      if (dict && dict[key] !== undefined) return dict[key];
      if (EN[key] !== undefined) return EN[key];
      return key;
    },
    /** BCP 47 tag for Intl / toLocaleString. */
    bcp47: function () { return i18n.lang; }
  };
  window.i18n = i18n;

  function render() {
    document.documentElement.lang = i18n.lang;
    document.querySelectorAll("[data-i18n]").forEach(function (el) {
      el.textContent = i18n.t(el.getAttribute("data-i18n"));
    });
    document.dispatchEvent(new CustomEvent("langchange", { detail: i18n.lang }));
  }

  function setLang(tag) {
    if (loaded[tag]) {
      i18n.lang = tag;
      remember(tag);
      render();
      return Promise.resolve();
    }
    return fetch("js/i18n/" + tag + ".json", { cache: "no-cache" })
      .then(function (r) {
        if (!r.ok) throw new Error("HTTP " + r.status);
        return r.json();
      })
      .then(function (dict) {
        loaded[tag] = dict;
        i18n.lang = tag;
        remember(tag);
        render();
      })
      .catch(function (err) {
        // A missing or unreachable locale must never break the page.
        console.warn("i18n: falling back to English for", tag, err);
        i18n.lang = DEFAULT_LANG;
        render();
      });
  }
  i18n.setLang = setLang;

  function remember(tag) {
    try { localStorage.setItem("apt-preview-lang", tag); } catch (e) { /* private mode */ }
  }
  function recall() {
    try { return localStorage.getItem("apt-preview-lang"); } catch (e) { return null; }
  }

  function buildPicker() {
    var sel = document.getElementById("langSelect");
    if (!sel) return;
    LOCALES.forEach(function (loc) {
      var opt = document.createElement("option");
      opt.value = loc.tag;
      opt.textContent = loc.label;
      sel.appendChild(opt);
    });
    sel.value = i18n.lang;
    sel.addEventListener("change", function () { setLang(sel.value); });
  }

  document.addEventListener("DOMContentLoaded", function () {
    // English is the default. A previously chosen language is restored, but the
    // browser's own locale never overrides the default on a first visit.
    var saved = recall();
    buildPicker();
    render();
    if (saved && saved !== DEFAULT_LANG) {
      var sel = document.getElementById("langSelect");
      if (sel) sel.value = saved;
      setLang(saved);
    }
  });
})();
