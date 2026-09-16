/* Preview 用的極簡雙語字典：所有內容皆為展示用假資料，與實際 Mod 行為無關。 */
window.I18N = {
  zh: {
    banner: "這是純展示用的 Preview 網頁 — 所有數據與規劃結果皆為模擬，不會連線、不含真實 Mod 功能。",
    brandSub: "Cities: Skylines I 模組 · 預覽",
    navFeatures: "功能", navDemo: "互動體驗", navPanel: "模組介面", navInstall: "安裝",

    heroEyebrow: "自動化大眾運輸規劃",
    heroTitle: "讓城市的公車、地鐵與電車路線自己長出來",
    heroLead: "分析人口、就業與通勤需求，一鍵產生完整的公共運輸路網；也可以只微調現有路線，或全部刪除後重新規劃。",
    heroCta: "開始互動體驗",
    metaGameL: "適用遊戲", metaDistL: "發布平台", metaLicL: "授權", metaLicV: "開源",

    featTitle: "核心功能",
    featLead: "以下說明模組預計提供的能力，Preview 中以模擬方式呈現。",
    f1t: "一鍵自動規劃",
    f1d: "掃描城市的住宅、商業與工業分區，依需求密度自動決定路線走向、站點位置與運具種類。",
    f2t: "調整現有路線",
    f2d: "保留你手動畫好的路線，只針對覆蓋不足或重疊過多的區段提出修正建議並套用。",
    f3t: "刪除後完全重規劃",
    f3d: "清空目前所有路線，從零重新計算一套全新的路網，適合城市大改造之後使用。",
    f4t: "遊戲內 UI 面板",
    f4d: "所有操作都在遊戲內的面板完成，可即時預覽規劃結果再決定是否套用。",

    demoTitle: "互動體驗：模擬規劃器",
    demoLead: "調整下方參數後按「自動規劃」，觀察路網如何生成。此處的城市、數據與演算法皆為展示用模擬。",
    ctrlTitle: "規劃參數",
    ctrlCoverage: "目標覆蓋率", ctrlHeadway: "目標班距", ctrlBudget: "預算等級", ctrlModes: "啟用運具",
    modeBus: "公車", modeMetro: "地鐵", modeTram: "電車",
    btnPlan: "自動規劃路線", btnAdjust: "調整現有路線", btnReset: "刪除全部並重新規劃",
    statLines: "路線數", statStops: "站點數", statCover: "覆蓋率", statWait: "平均候車", statRiders: "日運量", statCost: "每週支出",

    logIdle: "> 待命中，請設定參數後按下「自動規劃路線」。",
    logScan: "> 掃描城市分區…",
    logDemand: "> 建立通勤需求矩陣…",
    logRoute: "> 產生候選路線並最佳化…",
    logDone: "> 規劃完成（模擬結果）。",
    logAdjustNone: "! 目前沒有可調整的路線，請先執行自動規劃。",
    logAdjust: "> 微調既有路線的站距與重疊區段…",
    logAdjustDone: "> 調整完成，未變動你手動建立的路線。",
    logReset: "> 已刪除全部路線，重新規劃中…",
    logModeNone: "! 請至少啟用一種運具。",
    statusIdle: "未規劃", statusRun: "規劃中…", statusDone: "已套用（模擬）",

    uiTitle: "模組介面示意",
    uiLead: "以下為遊戲內面板的外觀示意，按鈕不具實際功能。",
    guTitle: "Automated Public Transport Planning",
    guTab1: "規劃", guTab2: "路線", guTab3: "選項",
    guPlanHint: "選擇規劃範圍與運具後執行，結果會先以預覽方式顯示。",
    guRowScope: "規劃範圍", guRowScopeV: "全市",
    guRowMode: "運具", guRowModeV: "公車 + 地鐵 + 電車",
    guRowKeep: "保留手動路線", guRowKeepV: "是",
    guPlanBtn: "開始規劃",
    guLinesHint: "自動產生的路線會標記來源，可單獨刪除或鎖定。",
    guOpt1: "規劃後自動購買車輛", guOpt2: "避開已有路線重疊", guOpt3: "允許跨區長途路線", guOpt4: "預設班距",

    insTitle: "安裝方式",
    ins1: "訂閱 Steam Workshop 上的「Automated Public Transport Planning」。",
    ins2: "啟動 Cities: Skylines，於「內容管理員 → 模組」中啟用本模組。",
    ins3: "進入存檔後，於遊戲內工具列開啟模組面板。",
    ins4: "設定規劃參數並執行，先預覽再套用。",
    insNote: "注意：本頁面為 Preview，尚未提供實際下載。實際版本釋出後會更新此處連結。",
    footNote: "Preview 網頁 · 內容為模擬展示，不代表最終功能。"
  },
  en: {
    banner: "Preview site only — every number and route here is simulated. No real mod functionality, no network calls.",
    brandSub: "Cities: Skylines I mod · Preview",
    navFeatures: "Features", navDemo: "Live Demo", navPanel: "Mod UI", navInstall: "Install",

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
  }
};

window.APP_LANG = "zh";

function applyLang(lang) {
  window.APP_LANG = lang;
  const dict = window.I18N[lang];
  document.documentElement.lang = lang === "zh" ? "zh-Hant" : "en";
  document.querySelectorAll("[data-i18n]").forEach(function (el) {
    const key = el.getAttribute("data-i18n");
    if (dict[key] !== undefined) el.textContent = dict[key];
  });
  const btn = document.getElementById("langToggle");
  if (btn) btn.textContent = lang === "zh" ? "EN" : "中文";
  document.dispatchEvent(new CustomEvent("langchange", { detail: lang }));
}

document.addEventListener("DOMContentLoaded", function () {
  applyLang("zh");
  const btn = document.getElementById("langToggle");
  if (btn) {
    btn.addEventListener("click", function () {
      applyLang(window.APP_LANG === "zh" ? "en" : "zh");
    });
  }
});
