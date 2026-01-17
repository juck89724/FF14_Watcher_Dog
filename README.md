# 🎮 FF14 Watcher - 每日任務自動監控與推播助手

> **「打完每日，手機自動打勾。」**
> 讓你的 PC 自動幫你紀錄 Final Fantasy XIV 的每日隨機任務完成狀態，並即時推播到你的手機上。

---

## ✨ 這是什麼？

這是一個為 FF14 玩家設計的「自動化小工具」。
當你在電腦上打完每日隨機任務（Roulette）時，這個程式會透過視覺辨識（OCR）讀取遊戲對話框，自動判斷你打完的是「練級」、「討伐」還是「團隊副本」，並立刻將紀錄同步到你的手機 App。

**核心特色：**

* 🛡️ **安全無虞**：不讀記憶體、不修改封包，單純像「眼睛」一樣看畫面，絕無封號風險。
* 📱 **即時推播**：副本打完瞬間，手機發出「叮！」的通知。
* 📜 **歷史紀錄**：手機隨時查看過去幾天的任務完成紀錄。
* 🤝 **簡易配對**：手機掃描電腦上的 QR Code 即可連線。

---

## 🛠️ 使用前準備 (遊戲設定)

為了讓電腦「看清楚」你的遊戲文字，請務必在 FF14 遊戲內做以下設定：

1. **獨立聊天視窗**：
    * 在聊天欄位新增一個分頁（建議命名為 `OCR`）。
    * 將該分頁**拖曳出來**，變成獨立的小視窗。
2. **訊息過濾 (關鍵)**：
    * 進入「對話視窗設定 (Log Window Settings)」。
    * 只勾選以下項目：
        * ✅ **系統資訊 (System Messages)**
        * ✅ **掉落品信息 (Loot Notices)** (這條最重要！沒勾抓不到)
        * ✅ **自己的成長資訊 (Own Progression)**
3. **視覺優化 (必做)**：
    * **背景全黑**：將該視窗的「透明度」設為 **0 (不透明/全黑)**。
    * **字體加大**：建議將字體大小設為 **20pt** 以上。

---

## 🚀 安裝與執行指南

本專案分為三個部分，請依序設定：

### 第一步：雲端後端 (Firebase)

*這是用來轉送資料的免費雲端服務。*

1. 到 [Firebase Console](https://console.firebase.google.com/) 建立一個新專案。
2. 開啟 **Cloud Firestore** (資料庫) 與 **Cloud Messaging** (推播)。
3. 下載 `google-services.json` (給 Android 用) 與 `GoogleService-Info.plist` (給 iOS 用)。

### 第二步：PC 監控端 (Windows)

*這是負責看遊戲畫面的程式。*

1. 確認你的電腦是 **Windows 10 或 11** (需支援繁體中文 OCR)。
2. 開啟 `Watcher_PC` 專案。
3. 編譯並執行 `Watcher_PC.exe`。
4. 程式啟動後，會顯示一個 QR Code，等待手機掃描。

### 第三步：手機 App 端 (iOS/Android)

*這是用來接收通知的 App。*

1. 將 Firebase 設定檔放入 `Watcher_App` 對應目錄。
2. 使用 Flutter 編譯 App 並安裝到手機。
3. 打開 App，授權通知權限。
4. 點擊「掃描配對」，掃描電腦螢幕上的 QR Code。
5. 🎉 **配對成功！** 盡情享受遊戲吧！

---

## 📸 運作流程圖

1. **PC**：每 3 秒截圖檢查一次遊戲視窗。
2. **PC**：偵測到「獲得隨機任務獎勵」關鍵字。
3. **Cloud**：電腦將資料上傳雲端，並觸發推播。
4. **App**：手機收到通知：「你剛剛完成了 真濕婆討伐戰 (討伐戰)」。

---

## 🔧 進階開發者專區 (Optional)

如果你是開發者，想修改源碼或使用特殊開發環境，請參考以下資訊：

### 專案結構

* `/Watcher_PC` - C# .NET 8 Console App (OpenCvSharp + Windows OCR)
* `/Watcher_App` - Flutter App (Firebase + MobileScanner)
* `/Watcher_Backend` - Firebase Cloud Functions (Node.js)

### MacOS / 遠端開發者注意事項 (SSH)

如果你習慣使用 Mac 開發，但程式必須在 Windows 上跑：

1. 建議使用 **VS Code** 或 **Google Antigravity IDE**。
2. 在 Windows 遊戲機上安裝 **OpenSSH Server**。
3. 使用 IDE 的 **Remote - SSH** 功能連線至 Windows。
4. 這樣你就可以在 Mac 上寫 Code，但直接使用 Windows 的 CPU 與 API 進行編譯與除錯。

### 貢獻

歡迎提交 Issue 或 Pull Request！請確保你的 Code 符合現有的架構規範。

---

**Happy Gaming! 🌟**
*光之戰士，願水晶指引你的道路。*
