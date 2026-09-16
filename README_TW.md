# Automated Public Transport Planning — Preview

這個 branch 只放 **Preview 網頁**：一個沒有真實功能的靜態頁面，讓使用者可以透過實際觀察與操作去了解這個 Mod 打算做什麼。

> **警告**
> 頁面上的城市、路線、統計數字全部是模擬產生的，與遊戲、與實際 Mod 行為無關。

其他語言：[English](README.md)

## 線上瀏覽

https://spacesquare640.github.io/Cities_Skylines_1_Automated_Public_Transport_Planning/

## 內容區塊

| 區塊 | 說明 |
| --- | --- |
| Hero | 模組定位與基本資訊 |
| 核心功能 | 自動規劃／調整現有路線／刪除後重新規劃／遊戲內 UI |
| 互動體驗 | 模擬規劃器：調整覆蓋率、班距、預算與運具，動畫產生路網並更新統計 |
| 模組介面示意 | 仿遊戲內面板的外觀，按鈕無功能 |
| 安裝方式 | 預定的安裝流程 |

## 語言

介面預設語言為 **English**。標頭的語言選單可切換為以下 11 種語系，對應 Cities: Skylines I 官方支援的語言：

`en` · `zh-Hant` · `zh-Hans` · `ja` · `ko` · `de` · `fr` · `es` · `pt-BR` · `ru` · `pl`

英語內建於 `js/i18n.js`，因此頁面不會空白也不需等待請求。其餘語系按需從 `js/i18n/<tag>.json` 載入，任一 key 缺漏會自動回退英語。

## 檔案結構

```
index.html          頁面結構
css/style.css       樣式
js/i18n.js          i18n 核心、內建英語字典、語言選單
js/i18n/<tag>.json  其餘 10 個語系，按需載入
js/demo.js          模擬規劃器（純展示用，無真實演算法）
```

## 本地預覽

```bash
python -m http.server 8000
```

然後開啟 http://localhost:8000

## 其他 branch

- `Source_Code` — 模組本體原始碼
