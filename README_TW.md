# Cities Skylines 1 — Automated Public Transport Planning

**Steam Workshop 名稱：** Automated Public Transport Planning
**適用遊戲：** Cities: Skylines I

這個 branch 只放 **模組本體的原始碼**。與 App 本體無關的任何代碼或資料（工具、素材、文件、個人筆記等）一律不進入這個 branch。

其他語言：[English](README.md)

## 模組功能

一套在遊戲內自動規劃大眾運輸的工具：

- **自動規劃** — 讀取城市分區與通勤需求，產生公車／地鐵／電車路網
- **調整現有路線** — 保留玩家手動建立的路線，只修正覆蓋不足或重疊過多的區段
- **刪除後重新規劃** — 清空既有路線並從零重新計算
- **遊戲內 UI** — 所有操作與規劃預覽都在遊戲內面板完成

## 狀態

開發初期。模組已可載入、唯讀盤點城市、放置站點；規劃器本身尚未撰寫。

## 建置

需要安裝遊戲，以及 MSBuild（Visual Studio 2022 Build Tools 即可）。**不需要**安裝
.NET Framework 3.5 targeting pack —— 本專案直接編譯至遊戲 `Cities_Data/Managed`
目錄下隨附的組件。

若建置找不到遊戲，將 `Local.props.example` 複製為 `Local.props` 並設定
`CitiesSkylinesDir`。另外設定 `ModsDir` 可讓建置成功後自動把組件複製進遊戲的本機模組
資料夾。`Local.props` 已被 git 忽略，不會把任何人的本機路徑帶進儲存庫。

```
MSBuild AutomatedPublicTransportPlanning/AutomatedPublicTransportPlanning.csproj /p:Configuration=Release
```

## 結構

```
AutomatedPublicTransportPlanning/
  Source/Mod.cs               IUserMod 進入點
  Source/Loader.cs            載入存檔時的唯讀城市盤點
  Source/Planning/            站點放置與 prefab 解析
  Source/Spike/               暫時性診斷碼，用完即移除
  Source/Util/Log.cs
```

## Branch 結構

| Branch | 內容 |
| --- | --- |
| `Source_Code`（default） | 模組本體原始碼 |
| `Cities_Skylines_1_Automated_Public_Transport_Planning_Preview` | 靜態 Preview 網頁，由 GitHub Pages 提供 |

**Preview 網址：** https://spacesquare640.github.io/Cities_Skylines_1_Automated_Public_Transport_Planning/
