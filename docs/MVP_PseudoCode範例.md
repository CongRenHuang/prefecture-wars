# 《都道府縣大戰》MVP Pseudo Code 範例
### 對應 MVP 規劃書 v2 的架構：Game.Core（純邏輯）／Game.Presentation（表現層）

> 說明：以下為 C# 風格的偽代碼，重點在展示架構切分、資料流與各系統職責，
> 非可編譯程式。`//` 為設計說明，`??` 表示留待實作時決定的細節。

---

## 1. 資料定義層（ScriptableObject，唯讀）

```pseudo
// 縣的靜態定義 —— 遊戲中永不修改，執行期狀態另外存
ScriptableObject PrefectureDef:
    id: int                      // 0..46（隱藏島之後擴充 47..50）
    displayName: string
    neighborIds: int[]           // 手刻隣接表（含原作非現實隣接，如 大阪—香川）
    trait: TraitType             // enum: 金山 | 神社 | 森林 | ... | 無

ScriptableObject UnitDef:
    id: UnitType                 // enum: 近戰 | 遠程 | 坦 | ...
    maxHp, attack, range, moveSpeed, attackInterval: number
    cost: int
    isAttackable: bool           // 坦 = false

ScriptableObject GameConfig:     // 全域平衡參數集中一處，調平衡不改程式
    incomePerLand = 50
    incomeGoldMine = 70
    maxUnitsPerLand = 25
    startMoney = 100
    startUnits = 3
```

## 2. 執行期狀態（純 C#，可序列化 = 存檔即是它）

```pseudo
class GameState:
    day: int
    factions: Faction[]              // 47 個勢力
    lands: LandState[]               // 47 塊土地

class Faction:
    id: int
    money: int
    isAlive: bool
    isPlayer: bool

class LandState:
    defId: int                       // 指回 PrefectureDef
    ownerFactionId: int
    building: BuildingType?          // MVP-4 才用
    garrison: Map<UnitType, int>     // MVP-0 先只有一種兵 → 就是一個 int
```

> 坑的對策體現在這裡：SO 只當字典查，所有會變的東西都在 GameState。

## 3. 回合狀態機（Game.Core）

```pseudo
class TurnSystem:
    state: GameState
    phase: enum { PlayerPhase, AiPhase, Settlement, GameEnd }

    func StartNewDay():
        state.day += 1
        for each faction in state.factions where isAlive:
            faction.money += Sum(該勢力每塊地的收入)     // 金山特判

    async func RunTurnLoop():                // UniTask 驅動整個流程
        loop:
            StartNewDay()
            await PlayerPhase()              // 等待玩家按「行動結束」
            await AiPhase()                  // 依縣 id 由北到南逐一行動
            Settlement()                     // 滅亡判定、勝負判定
            if PlayerDead(): return GameOver
            if PlayerOwnsAll(): return Victory   // MVP-4: 改為觸發隱藏島

    func Settlement():
        for each faction:
            if faction 領地數 == 0: faction.isAlive = false
            → 發出 FactionDestroyedEvent(id)   // MessagePipe，UI 播「○○縣滅亡」
```

## 4. 玩家指令與侵攻判定（MVP-0：比數字）

```pseudo
// Presentation 只負責把點擊翻譯成 Command，規則全在 Core
interface ICommand:  HireUnits | Invade | Reinforce | EndTurn

class InvasionResolver:
    func Resolve(attackerUnits, defenderLand) -> Result:
        // MVP-0：純數字比大小，攻方需留守至少 ?? 隻
        if attackerUnits > defenderLand.garrison:
            佔領：land.ownerFactionId = attacker
            殘兵 = attackerUnits - defenderLand.garrison   // 簡化損耗模型
        else:
            守方勝，雙方按比例損耗 ??
        // MVP-1 之後：這裡改為呼叫 BattleSimCore.Simulate(...)
```

## 5. 即時戰鬥模擬（MVP-1，純 C#、固定 tick）

```pseudo
class BattleSimCore:
    TICK = 0.05s                         // 邏輯與渲染分離的關鍵
    units: List<SimUnit>                 // 純資料，不是 GameObject
    timeLimit: countdown

    func Simulate(attackers, defenders, terrainBuffs) -> BattleResult:
        // AI 對 AI：直接 while 迴圈跑到底，不經過渲染 → 秒出結果
        while not IsOver(): Step()
        return CollectResult()

    func Step():                         // 玩家戰鬥：由 View 以真實時間呼叫
        for each unit in units where alive:
            switch unit.fsm:
                Seek:   target = 最近的敵方; if InRange → Attack else 前進
                Attack: if 冷卻到 → target.hp -= dmg(地形buff)   // 發射即扣血，彈道只是表現
                Dead:   skip
        timeLimit -= TICK
        if timeLimit <= 0: 守方判勝

class SimUnit:
    def: UnitDef; hp; posX; cooldown; side; fsm: enum{Seek, Attack, Dead}
```

```pseudo
// View 端（Presentation）：只讀 SimCore、做插值，絕不寫邏輯
class BattleView (MonoBehaviour):
    func Update():
        accumulator += deltaTime * speedMultiplier    // 2x/4x 加速只動這裡
        while accumulator >= TICK: sim.Step(); accumulator -= TICK
        for each simUnit: 對應的 pooledSprite.position = Lerp(prev, curr, accumulator/TICK)
```

## 6. 物件池（自寫，百行內）

```pseudo
class UnitViewPool:
    stack: Stack<UnitView>
    func Rent(def):  view = stack.PopOrInstantiate(); view.ResetState(def); return view
    func Return(v):  v.取消事件訂閱; v.SetActive(false); stack.Push(v)
    // 坑對策：Return 一定重置 HP 條、動畫、訂閱，否則殘留狀態 bug 難查
```

## 7. 戰略 AI（MVP-2，效用函數）

```pseudo
class StrategicAI:
    func TakeTurn(faction):
        // 1. 花錢：依局勢在雇兵/建築(MVP-4)間分配
        if 邊境守軍薄弱: 優先補守軍 else 攢進攻部隊

        // 2. 選侵攻目標：對每個隣接敵地打分
        for each target in 隣接敵地:
            score = (可派兵力 - target.garrison)          // 戰力差
                  * LandValue(target)                      // 收入 + 特色權重
                  / (1 + target擁有者的隣接敵數??)          // 風險
        best = argmax(score)
        if best.score > AGGRESSION_THRESHOLD:              // 每縣不同 → 個性
            Invade(best)   // AI vs AI → BattleSimCore.Simulate() 直接出結果
        else: 防守回合
```

## 8. 存檔（JSON）

```pseudo
class SaveService:
    path = Application.persistentDataPath + "/save.json"   // 坑：不可寫死路徑
    func Save(state: GameState): File.Write(path, ToJson(state))
    func Load() -> GameState?:  return File.Exists ? FromJson(...) : null
    // GameState 全為純資料 → 序列化即存檔，不需另設計存檔格式
```

## 9. 組裝（VContainer，MVP-2 引入）

```pseudo
class GameLifetimeScope : LifetimeScope:
    func Configure(builder):
        builder.Register<GameConfig>(從 SO 載入)
        builder.Register<TurnSystem>(Singleton)
        builder.Register<StrategicAI>(Singleton)
        builder.Register<SaveService>(Singleton)
        builder.RegisterEntryPoint<GameFlowController>()   // 純 C# 進入點

class GameFlowController : IAsyncStartable:
    async func StartAsync():
        state = save.Load() ?? NewGame(玩家選的縣)
        result = await turnSystem.RunTurnLoop()
        await ShowEnding(result)
```

## 10. 事件流（解耦 Core → UI）

```pseudo
// Core 只發事件，不知道 UI 存在（MessagePipe / 或先用 C# event）
events:
    DayStartedEvent(day)          → UI 更新日期與收入動畫
    BattleRequestedEvent(...)     → 切換到戰鬥場景
    FactionDestroyedEvent(id)     → 播「○○縣滅亡」跑馬燈
    LandCapturedEvent(land, by)   → 地圖色塊變色(tween)
```

---

## 附：MVP-0 第一週的實作順序建議

1. GameState + PrefectureDef + 隣接表資料（先 5 個縣就好，別急著填 47 個）
2. TurnSystem.StartNewDay 的收入計算 → **先寫測試再實作**
3. InvasionResolver 比數字版 → 測試
4. 才開 Unity 場景：5 顆色塊按鈕 + 點擊出兵
5. 跑通「5 縣小日本」完整一局 → 再回頭填滿 47 縣資料
