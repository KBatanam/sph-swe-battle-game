# ネットワーク対戦設計

この文書は、`sph-swe-battle-game` にネットワーク対戦を追加するための設計方針と、
段階的な実装計画をまとめたものである。
実装で採用した内容と、検討中の案を区別して記録する。

## 目的

- 2人の人間が別々のPCから同じ試合に参加し、波で目的球を押し合えるようにする。
- まず同一LAN・直接接続で動作させ、その設計をインターネット対戦へ拡張する。
- サーバー／リレーはセルフホストできる構成を目指す。
- 本プロジェクトは学習用の試作であり、ネットワーク対応も段階的にMVPを積み上げる。

## 採用した技術

- **ネットワークライブラリ: Mirror（MITライセンス）**
  - Unity公式UPMパッケージではないため、GitHub Releasesの固定バージョンを
    `Assets/Mirror` へ導入してGit管理する。
  - 導入バージョンは `v96.11.3` を基準とする。
  - `Examples`、`Tests`、`Hosting` は参照関係が無いことを確認して除外し、
    リポジトリサイズを抑えている。
- **トランスポート: KCP（Mirror同梱、信頼性付きUDP）**
  - LANの直接IP接続で使用する。
  - WebGL以外のデスクトップ環境で動作する。
- **接続モデル: プレイヤーホストのリッスンサーバー方式**
  - ホストが自身のGPUで流体シミュレーションを実行し、試合状態の権威を持つ。
- **将来のオンライン化: セルフホストの軽量リレー（Light Reflective Mirror等）**
  - リレーは状態とイベントのパケット転送のみを担当し、GPUを必要としない。

## 前提となる制約

- GPU版の流体シミュレーションは非決定論的である。
  - 空間グリッドのアトミック加算により粒子順序が実行ごとに変わり、
    浮動小数点の加算順に依存して結果が一致しない。
  - 参照: `Assets/SphSwe/Shaders/Compute/SphSweSimulation.compute`
    の `InterlockedAdd` と密度・加速度の加算ループ。
- 決定論的になり得るのはCPU基準実装のみだが、固定ステップ駆動や
  造波・水面サンプルのAPIを持たない。
- このため**決定論的ロックステップは採用せず、ホスト権威の状態同期**とする。

## 権威配分

| 状態 | 権威 | 方式 |
|---|---|---|
| 流体・水面 | 各クライアントのローカル | 見た目の一致は求めない |
| 目的球の位置 | ホスト | `NetworkObjectiveBall` で同期・クライアントは補間 |
| スコア | ホスト | （M4で実装予定） |
| 側面波の発生 | ホスト | （M3で実装予定） |
| キャラ移動（左右X） | 各クライアント | `NetworkCharacterMovement`（ClientToServer同期） |
| 造波 | 発行者→ホスト | `NetworkCharacterActions` の Command でホスト流体へ適用 |
| 雷の発射 | 発行者→ホスト | Command でホストが命中判定・硬直を確定 |
| 硬直・クールダウン | ホスト | `NetworkCharacterStatus`（ServerToClient同期） |

陣営は、ホストが手前（Near、Player側）、ゲストが奥（Far、Enemy側）を担当する。
ゲスト側はカメラと左右入力を180度反転して操作する。
シミュレーション座標は両者で共有する。

## 同期設計

- **30Hz相当のスナップショット＋信頼性イベント**
  - 連続値は Mirror の `SyncVar` で同期する。
  - 離散イベント（造波・雷）は `[Command]` でホストへ送る。
- **予測・ロールバックは行わない（補間のみ）**
  - 自分のキャラはクライアント権威のため即時。
  - 相手キャラと目的球は受信値を補間して表示する。

## ネットワーク層の構成

配置: `Assets/SphSwe/Scripts/Networking/`

| ファイル | 役割 |
|---|---|
| `NetworkPlayerSide.cs` | 陣営（Near/Far）の定義 |
| `NetworkBattleSession.cs` | ホスト開始・IP直結参加・退出・接続状態 |
| `NetworkBattleCharacter.cs` | 陣営決定、スポーン位置、入力有効化、視点反転 |
| `NetworkCharacterMovement.cs` | 自キャラ左右位置のクライアント権威同期 |
| `NetworkCharacterStatus.cs` | 硬直・クールダウンのホスト権威同期 |
| `NetworkCharacterActions.cs` | 造波・雷のホストへの転送 |
| `NetworkObjectiveBall.cs` | 目的球位置のホスト権威同期とクライアント補間 |

### asmdef を作らない判断

Mirrorは独自のasmdef（`Mirror`、`Mirror.Components`、`Mirror.Transports`）を持つ。
ゲームプレイ側のコードは現在 `Assembly-CSharp` にあり、asmdefから
`Assembly-CSharp` を参照することはできない。
そのためネットワーク層に専用asmdefを作らず、`Assembly-CSharp` に置いて
Mirrorのasmdef（Auto Referenced）を自動参照させる。
将来ゲームプレイ全体をasmdefへ移行する際に、ネットワーク層も分離できる。

## 既存クラスへの追加

- `WaveGenerator`: 波の発生成功時に `WaveGenerated` イベントを発行する。
- `LightningCannon`: 発射成功時に `LightningFired` イベントを発行し、
  クールダウン残り時間を権威値で上書きするメソッドを追加する。
- `StunStatus`: 硬直残り時間を権威値で上書きするメソッドを追加する。
- `PlayerController`: 左右入力に符号を掛ける `MovementInputSign` を追加する。

## セットアップ手順（Unity Editor）

1. `Assets/SphSwe/Scenes/sph_swe.unity` を開く。
2. メニュー `Tools/Battle/Setup/Configure Network Battle` を実行する。
   - `Network Manager` オブジェクトを作成し、`NetworkManager` と `KcpTransport` を追加。
   - `playerPrefab` に `PlayerCube.prefab` を設定。
   - `PlayerCube.prefab` に `NetworkIdentity` と各ネットワークコンポーネントを追加。
   - シーンの `ObjectiveBall` に `NetworkIdentity` と `NetworkObjectiveBall` を追加。
   - `NetworkBattleSession` にオフライン用キャラクター（`PlayerCube`/`EnemyCube`）を登録。
3. シーンを保存する。
4. 動作確認はParrelSyncまたは実機2台で行う。

補足:
- Mirrorのバージョンは `Assets/Mirror` をコミットして固定する。
- シーンオブジェクト（目的球）のスポーンが行われない場合は、
  Mirrorのシーンオブジェクト登録または `NetworkManager` の設定を確認する。

## 実装フェーズ

- **M1（実装済み）**: Mirror導入、セッション確立、キャラクターの陣営・入力・視点。
- **M2（一部実装済み）**: 自キャラ移動同期、目的球同期、硬直・クールダウン同期。
- **M3（一部実装済み）**: 造波・雷のイベント転送。側面波の同期は未実装。
- **M4（未実装）**: カウントダウン、勝敗条件（先取5点）、結果画面、再戦、切断処理。
- **M5（未実装）**: 入力ソース抽象化のリファクタ、AI練習モードの維持。
- **M6（未実装）**: セルフホストリレーによるオンライン化。

## 検証項目

- [ ] Mirror導入後、Unity 6.3 LTS でコンパイルできる。
- [ ] ホスト開始で自キャラが手前に1体スポーンする。
- [ ] ゲスト接続で両者に2体のキャラが表示される。
- [ ] 相手キャラの左右移動が補間されて表示される。
- [ ] 目的球の位置が両者で概ね一致する。
- [ ] 造波が両者の流体に反映される。
- [ ] 雷の硬直・クールダウンがホスト権威で一致する。
- [ ] 切断時にセッションが終了し、オフライン用キャラが復帰する。

## 既知のリスクと未解決事項

- **Mirror × Unity 6.3 は公式に未確認**
  - Mirrorの公式ドキュメントはUnity 6.1までを明記している。
  - M1の最初の確認として、Unity 6.3でのコンパイルとPlayを検証する。
- **Light Reflective Mirror はコミュニティfork**
  - 更新時はリレーと全クライアントのバージョンを揃える必要がある。
- **移動チート**
  - 自分のキャラをクライアント権威にしているため、移動改ざんが可能。
  - LANの学習用では許容し、競技化する場合はホスト権威へ変更する。
- **水面の見た目不一致**
  - GPU非決定性により、両者の水面形状は一致しない。勝敗（球・スコア）は一致させる。
- **シーンオブジェクトのスポーン**
  - Mirrorのシーンオブジェクト機構に依存するため、実機で確認する。
