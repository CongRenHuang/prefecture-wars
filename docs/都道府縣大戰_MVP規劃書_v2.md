# 《都道府縣大戰》重製 MVP 規劃書 v2.0
### —— 含開發環境、工具鏈、各階段步驟與踩坑指南

> 定位：學習型 Side Project，程式開源（不含素材）、不上架、不營利
> 開發機：MacBook Pro (M1 Pro)　｜　目標裝置：Android 實機（iOS 可選）
> v2 更新：加入完整環境設定、開源套件採用時程、各里程碑操作步驟與常見坑

---

## 一、專案定位與成功標準（同 v1，摘要）

用範圍可控的題目走完「Unity 從零到手機運行」全流程，交付一個可放進履歷的開源 repo。DoD：(1) Android 實機穩定 60fps 跑完一局完整流程；(2) repo clone 即可編譯（placeholder 素材）；(3) 核心邏輯有單元測試＋CI badge；(4) README 附 Profiler 實測數據與 GIF。

---

## 二、開發環境設定（M1 Pro，Day 1 就完成）

### 2.1 可行性結論

M1 Pro 是本專案的理想開發機：Unity Editor 自 2021.2.16 起完全原生支援 Apple Silicon，Unity 6 為原生 ARM64 應用；2D 小專案在 M1 Pro 上編譯與進入 Play Mode 皆為秒級，16GB RAM 足夠。且 iOS 建置必須使用 Mac 上的 Xcode，這台機器是唯一能同時打包雙平台的環境。

### 2.2 安裝清單與順序

1. **Unity Hub** → 安裝 **Unity 6 LTS（6000.0.x）Apple Silicon 版**
   - 模組勾選：`Android Build Support`（含子項 OpenJDK、Android SDK & NDK Tools 全勾）＋ `iOS Build Support`（可選）
   - ⚠️ 坑：有開發者回報 Unity 6.3 在 Apple Silicon 上 Android build 仍有 Gradle 相關問題，**建議停留在 6.0/6.2 LTS 穩定版**，不要追最新 minor 版。
2. **IDE**：JetBrains **Rider**（非商業用途已免費，Unity 整合最佳）或 VS Code + C# Dev Kit。安裝後在 Unity `Preferences > External Tools` 指定。
3. **Git**：`brew install git git-lfs`；建 repo 時套用官方 Unity `.gitignore` 模板，另加一行 `Assets/_Private/`。
   - Unity 設定：`Edit > Project Settings > Editor` → Asset Serialization = **Force Text**、Version Control = **Visible Meta Files**（.meta 檔必須進版控，漏掉是新手最常見的 repo 損毀原因）。
4. **Android 手機**：設定 → 關於手機 → 連點版本號 7 次開啟開發者選項 → 開 USB 偵錯；接上 Mac 後 `adb devices` 確認可見（adb 在 Unity 安裝目錄的 SDK platform-tools 內，或 `brew install android-platform-tools`）。
5. **iOS（可選）**：安裝 Xcode（App Store），免費 Apple ID 即可真機測試，但簽名 7 天過期需重簽——僅在展示時臨時使用。

### 2.3 環境層常見坑

- **首次 Android build 卡在 Gradle/SDK license**：優先使用 Unity 自帶的 JDK/SDK/NDK（`Preferences > External Tools` 全部勾 Use Embedded），不要混用自裝的 Android Studio SDK，九成 Gradle 錯誤源自版本混用。
- **macOS 檔案系統預設不分大小寫**：CI（Linux）分大小寫，檔名引用大小寫不一致在本機能跑、CI 會炸——命名保持一致習慣。
- **Unity 版本鎖定**：團隊即使只有你一人，也在 README 記下精確版本號；Unity 不同 minor 版開同專案可能觸發資產重匯入。

---

## 三、開源套件採用時程表

原則：**MVP-0 只裝一個套件，之後遇到痛點才引入**——「知道何時引入什麼」本身是學習目標。所有套件皆 MIT 授權，可安心含入開源 repo。安裝方式統一用 UPM（`Window > Package Manager > Install package from git URL`）或 OpenUPM registry。

| 階段 | 套件 | 用途 | 取代/避免的東西 |
|---|---|---|---|
| MVP-0 | **UniTask**（Cysharp） | 零分配 async/await，處理回合流程、場景切換 | Coroutine 的 GC 與不可 try/catch |
| MVP-0 | Input System（官方） | 觸控/滑鼠統一綁定 | 舊 Input.GetMouse 寫兩套 |
| MVP-1 | **LitMotion** 或 PrimeTween | 零 GC tween（UI 彈窗、受擊震動） | DOTween（較舊、有分配） |
| MVP-2 | **VContainer**（hadashiA） | GC-free DI 容器；純 C# 類別可作進入點，強化邏輯/表現分離 | Singleton 滿天飛 |
| MVP-2 | **MessagePipe** 或 VitalRouter（選用） | 零分配訊息傳遞，解耦「戰鬥結果→戰略層」事件 | 到處互相引用 |
| MVP-2 | **Graphy** | 遊戲內 FPS/RAM 即時面板 | 自寫 debug UI |
| MVP-3 | **R3**（Cysharp，選用） | 新一代 Rx：兵力變動→UI 自動更新的資料綁定 | 手動同步 UI |
| MVP-3 | **ZLinq**（選用） | 零分配 LINQ | 熱路徑上的 LINQ GC |
| MVP-3 | **Unity Debug Sheet** | 遊戲內除錯選單（跳關、加錢） | 重跑流程測試 |
| MVP-3 | **GameCI**（GitHub Actions） | 免費 CI 跑測試/build，README 掛 badge | 手動驗證 |

參考資源：`insthync/awesome-unity3d`（分類齊全的開源總表）；開發輔助可試 **Unity-MCP**（連接 Claude 等 AI 助手到 Unity Editor 的開源 MCP server）。

**刻意不用**：DOTS/ECS（50 單位用不上）、Addressables（單機小專案）、任何付費插件（開源 repo 不可含）。在 README 寫明理由，展示技術判斷。

---

## 四、各里程碑：工具、步驟、坑

### MVP-0：戰略層骨架（2–3 週）

**工具**：Unity 6 LTS、UniTask、Input System、Rider、Git

**步驟**：
1. Unity Hub 以 **2D (URP)** 模板建專案；首次 commit 前確認 .gitignore 生效（repo 不應出現 Library/）。
2. 建立 asmdef 分層：`Game.Core`（純 C#，Inspector 中取消勾選對 UnityEngine 的假設性依賴、No Engine References 視需要）、`Game.Presentation`（引用 Core）、`Game.Tests`。
3. 定義 `PrefectureDef` ScriptableObject：縣名、隣接縣 ID 陣列、特色枚舉。手刻 47 縣隣接表（先開原作玩一晚做規格筆記存 `/Docs`，含原作故意的非現實隣接如大阪—香川）。
4. 地圖表現：每縣一個 Sprite（色塊）＋ `PolygonCollider2D`，點擊用 Physics2D Raycast；或更簡單——47 顆按鈕排成日本形狀（MVP 精神：先醜先通）。
5. 回合狀態機（純 C#）：`PlayerPhase → AiPhase → Settlement`，AI 先做「隨機攻打隣接最弱縣」。
6. 侵攻＝比兵力數字，勝者佔地；勢力領地歸零→滅亡；玩家滅亡/統一→結束畫面。
7. 存檔：領地歸屬 + 資金 + 兵力序列化為 JSON 寫入 `Application.persistentDataPath`。
8. 對經濟計算、滅亡判定寫第一批 EditMode 測試。

**坑**：
- ScriptableObject 在 Editor 內 Play Mode 修改會**永久改動資產**（不像場景物件會還原）——執行期狀態一律複製到 runtime class，SO 只當唯讀定義。這是新手最經典的坑。
- JsonUtility 不支援 Dictionary 與多型；需要就改用 UPM 版 Newtonsoft Json（`com.unity.nuget.newtonsoft-json`）。
- 純 C# 層不要偷用 `UnityEngine.Random`／`Debug.Log`，用注入的亂數與日誌介面，否則測試與加速模擬會被綁死。

### MVP-1：即時戰鬥（3–4 週）

**工具**：＋LitMotion/PrimeTween

**步驟**：
1. `BattleSimCore`（純 C#）：固定 timestep（如 0.05s/tick）推進；單位＝資料結構（位置、HP、冷卻），FSM：索敵→接近→攻擊。
2. 3 兵種（近戰/遠程/坦）以 `UnitDef` SO 定義參數；1 個戰術＋時限沙漏＋撤退。
3. Unity 層 `BattleView`：讀取 SimCore 狀態繪製；單位 GameObject 用**物件池**（自寫 100 行內的 Pool 即可，不需套件）。
4. 戰鬥結束將存活兵力回寫戰略層；場景間資料傳遞用一個純 C# 的 `GameSession` 物件（由 VContainer 前置準備，或先用 static 之後重構）。
5. 用 LitMotion 做受擊閃白/震動，體驗零分配 tween。

**坑**：
- **邏輯 tick 與渲染幀分離**：傷害判定放 `Update` 用 `deltaTime` 累加會產生幀率相依 bug；SimCore 以固定 tick 推進、View 只做插值，這是本階段最重要的架構課。
- 遠程單位「發射瞬間扣血」vs「彈道命中扣血」先選前者，彈道只是表現——否則邏輯表現分離會破功。
- 物件池歸還時記得重置狀態（HP、動畫、事件訂閱），殘留狀態 bug 極難查。

### MVP-2：AI、完整局與 Android 上機（3–4 週）

**工具**：＋VContainer、Graphy、（選）MessagePipe

**步驟**：
1. 戰略 AI 效用函數：`score = (我方兵力 − 守軍) × 收益(收入+特色) ÷ 風險(隣接敵數)`；資金在雇兵/建築間按局勢權重分配。
2. AI vs AI 戰鬥**不渲染**：直接呼叫 BattleSimCore 以最大速度跑完取結果——這裡驗證 MVP-1 架構的價值。
3. 引入 VContainer：`GameLifetimeScope` 註冊 TurnSystem、EconomySystem、SaveService 等，消滅 static/Singleton。
4. 戰鬥 2x/4x 加速（調 tick 倍率）與 AI 回合快轉。
5. **Android build 流程**：`File > Build Settings` 切換 Android → Player Settings 設定：Scripting Backend = **IL2CPP**、Target Architectures 勾 **ARM64**、Minimum API 26 → 接手機 **Build and Run** → 除錯看 `adb logcat -s Unity`。
6. 觸控：Input System 綁定 tap/drag/pinch（雙指縮放地圖）；UI 加 Safe Area 處理。

**坑**：
- **IL2CPP code stripping**：反射與 JSON 序列化的類型可能被裁剪，真機閃退但 Editor 正常。對策：`link.xml` 保留組件，或序列化類型加 `[Preserve]`。這是「Editor 能跑、手機不能跑」的頭號元兇。
- Editor 用滑鼠測觸控與真機行為不同（如多指），Input System 開啟 Touch Simulation，但 pinch 縮放務必真機驗證。
- 首次 IL2CPP build 需時較久（M1 Pro 上本專案約幾分鐘），屬正常；後續增量會快。
- UI 在手機解析度跑版：Canvas Scaler 設 `Scale With Screen Size`＋參考解析度，開發初期就設好，別等 MVP-2 才調。

### MVP-3：效能優化與作品集包裝（2–3 週）★ 履歷重點

**工具**：＋Memory Profiler（官方 package）、Unity Debug Sheet、GameCI、（選）R3/ZLinq

**步驟**：
1. Development Build ＋ Autoconnect Profiler 連**真機**採樣（Editor 數據失真，一定量真機）。
2. 壓測場景：雙方各 25 單位混戰，記錄基準幀率/GC Alloc。
3. GC 歸零清單：每幀 `new`、字串拼接（改快取/StringBuilder）、Update 中的 LINQ（改 for 或 ZLinq）、闭包捕獲、`GetComponent` 快取、事件退訂。
4. Sprite Atlas 打包全部 placeholder 圖，Frame Debugger 驗證合批、記錄 draw call 前後對比。
5. `Application.targetFrameRate = 120`，在高刷新率手機驗證；URP 2D 燈光做一個戰術特效，證明特效開啟不掉幀。
6. GameCI 設 GitHub Actions 跑 EditMode 測試；README 完稿：架構圖、優化前後數據表、GIF、badge、授權聲明（MIT，註明不含原作素材、規則參考致意 Suznooto）。

**坑**：
- 在 Editor 量效能是最常見的錯誤——Editor 的 Profiler 數據含編輯器開銷，**結論一律以真機 Development Build 為準**。
- `Debug.Log` 本身有顯著開銷與分配，正式量測前包一層條件編譯（`[Conditional("UNITY_EDITOR")]`）。
- GameCI 需要 Unity license 啟用（個人版免費，照官方文件產 license 檔存進 GitHub Secrets），第一次設定約半天，值得。
- iOS 若要展示：Xcode 開 Unity 匯出的專案、免費簽名裝機，7 天過期屬正常，錄好影片存證即可。

---

## 五、常見坑速查總表

| 症狀 | 原因 | 解法 |
|---|---|---|
| 真機閃退、Editor 正常 | IL2CPP stripping 裁掉反射/序列化類型 | link.xml / [Preserve] |
| Play Mode 改的數值永久生效 | 直接改到 ScriptableObject 資產 | SO 唯讀、執行期複製 |
| Gradle build 失敗 | 自裝 SDK 與 Unity 內建版本混用；或用到 6.3 已知問題 | External Tools 全用內建；停在 6.0/6.2 LTS |
| 隊友/CI 開專案全紅 | .meta 沒進版控 | Visible Meta Files + 檢查 .gitignore |
| 幀率不同機器行為不同 | 邏輯依賴 deltaTime | 固定 timestep 模擬 + 表現插值 |
| 存檔在真機讀不到 | 路徑寫死或用 Editor 路徑 | 一律 Application.persistentPath |
| 效能數據好看但手機卡 | 在 Editor 量測 | 真機 Development Build + Autoconnect Profiler |

## 六、時程總覽（同 v1）

兼職開發（每週 8–10 小時）：環境設定 1 週內併入 MVP-0，MVP-0～3 合計約 **3–4 個月**。每個里程碑結束打 git tag（v0.1、v0.2…），commit 歷史即開發能力證明。
