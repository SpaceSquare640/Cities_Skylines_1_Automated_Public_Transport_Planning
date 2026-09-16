# Automated Public Transport Planning — Preview

This branch holds the **preview site**: a static page with no real functionality, built so people can look around and work out what the mod is meant to do.

> **Warning**
> Every city, route and statistic on the page is simulated. None of it reflects the game or any real mod behaviour.

Other languages: [繁體中文](README_TW.md)

## Live site

https://spacesquare640.github.io/Cities_Skylines_1_Automated_Public_Transport_Planning/

## Sections

| Section | Contents |
| --- | --- |
| Hero | What the mod is, and its basic details |
| Core features | Auto-plan / refine existing lines / wipe and replan / in-game UI |
| Live demo | Simulated planner: set coverage, headway, budget and modes, then watch a network animate in while the statistics update |
| Mod UI mock-up | How the in-game panel is meant to look; the buttons do nothing |
| Installation | The intended install flow |

## Languages

The interface defaults to **English**. The picker in the header switches to any of eleven locales, matching the languages Cities: Skylines I ships with:

`en` · `zh-Hant` · `zh-Hans` · `ja` · `ko` · `de` · `fr` · `es` · `pt-BR` · `ru` · `pl`

English is embedded in `js/i18n.js`, so the page never blanks or waits on a request. The other locales load on demand from `js/i18n/<tag>.json`, and any key a locale omits falls back to English.

## File layout

```
index.html          Page structure
css/style.css       Styles
js/i18n.js          i18n core, embedded English dictionary, language picker
js/i18n/<tag>.json  The other ten locales, loaded on demand
js/demo.js          Simulated planner (display only, no real algorithm)
```

## Running it locally

```bash
python -m http.server 8000
```

Then open http://localhost:8000

## Other branches

- `Source_Code` — the mod's own source code
