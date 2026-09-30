# SPH-SWE Battle Game

SPH-SWE（Smoothed Particle Hydrodynamicsによる浅水方程式）で計算した波を利用し、目的球を相手側へ押し込むUnity製の対戦ゲーム試作です。

流体シミュレーションはCompute Shader上で実行し、約13,500個の粒子から固定格子水面メッシュの高さと法線をGPU上で再構成します。CPU版は数値検証の基準実装として残しています。

## 作成目的

このプロジェクトは、主に次の技術を実践しながら学習するために作成しています。

- UnityとC#によるゲーム制作
- SPH-SWEを用いた粒子法シミュレーション
- Compute ShaderによるGPU並列計算
- URP、Shader、Render Graphを利用した独自描画
- Profilerを使用したCPU・GPU負荷の計測と最適化

完成済み製品ではなく、理論の理解、実装、計測、改善を繰り返す学習用のゲーム試作です。

## 開発環境

- Unity 6.3 LTS（6000.3.22f1）
- Universal Render Pipeline 17.3.0
- Input System 1.20.0
- TextMesh Pro
- ZString
- UniCortex

## 開発支援

設計の整理、理論の確認、コードレビュー、実装補助、テストおよび文書作成には、Codexの`GPT-5.6 Sol`、推論強度`Low（軽）`を使用しています。

Codexは開発支援に使用しているものであり、このゲームをclone、実行またはビルドするための依存関係ではありません。

- [GPT-5.6 Sol - OpenAI Docs](https://developers.openai.com/api/docs/models/gpt-5.6-sol)

## 導入した主な外部Package・Asset

| 名前 | 用途 | 導入方法・配置 |
|---|---|---|
| Universal Render Pipeline | ゲーム全体の描画基盤 | Unity Package Manager |
| Input System | Player入力 | Unity Package Manager |
| TextMesh Pro | 得点、クールタイム、FPSなどのUI | Unity Package |
| Unity Test Framework | Editor上の自動テスト | Unity Package Manager |
| ZString | Runtime文字列生成時のAllocation削減 | `Assets/ZString` |
| UniCortex | Unity Editorの外部操作と検証補助 | Git URL Package |
| OpenGameArt Lightning | 雷砲および硬直表現用テクスチャ | `Assets/ThirdParty` |

外部Assetのライセンスは、各Assetに同梱されたライセンス文書および本README末尾を参照してください。

## ゲーム内容

- Playerはフィールド手前、Enemyは奥側に配置されます。
- 波を発生させ、流体上の目的球を相手側の得点境界まで運ぶと得点になります。
- 得点後、目的球はフィールド中央へ戻ります。
- PlayerとEnemyは雷砲を使用でき、命中したキャラクターは一定時間硬直します。
- Enemyは目的球の位置に応じて移動、造波、雷砲攻撃を行います。
- 左右端からも定期的に波を発生させ、目的球が左右へ停滞しにくいようにしています。

## 操作

| 入力 | 操作 |
|---|---|
| `A` / `D` | Playerを左右へ移動 |
| 左クリック | Playerの正面から波を発生 |
| `Space` | クールタイム完了時に雷砲を発射 |

`W` / `S`による必殺技切り替えは将来実装予定です。

## 別環境での再現方法

### 1. 必要なソフトウェア

- Git
- Git LFS
- Unity Hub
- Unity 6.3 LTS（6000.3.22f1）

### 2. Clone

Git LFSを有効化してからcloneします。

```bash
git lfs install
git clone <repository-url>
```

すでにclone済みの環境でLFSファイルが不足している場合は、リポジトリ内で次を実行します。

```bash
git lfs pull
```

### 3. Unityで開く

1. Unity Hubの`Add`からcloneしたプロジェクトフォルダを追加します。
2. Unity 6.3 LTS（6000.3.22f1）で開きます。
3. Packageの解決とAssetのImportが完了するまで待ちます。
4. `Assets/SphSwe/Scenes/sph_swe.unity`を開きます。
5. ConsoleにErrorがないことを確認してPlayします。

`.sln`や`.csproj`はUnityが再生成できるため、環境差がある場合はUnity Editorから生成し直してください。

Package Managerの依存関係は`Packages/manifest.json`と`Packages/packages-lock.json`から復元されます。`Library`、`Temp`、`Logs`、`UserSettings`などは各環境でUnityが再生成するため、Gitでは共有しません。

別PCで同じ開発環境を再現する際は、次を確認してください。

```text
Unity Editor: 6000.3.22f1
Render Pipeline: Universal Render Pipeline 17.3.0
Main Scene: Assets/SphSwe/Scenes/sph_swe.unity
Git LFS: インストール後にgit lfs pullを実行
```

## 主な技術構成

### SPH-SWEシミュレーション

- Poly6カーネルによる密度・流体深さ計算
- Spikyカーネル勾配による流体深さ勾配計算
- Viscosityカーネルによる速度差の平滑化
- 半陰的オイラー法による時間積分
- CFL条件と最大時間刻みの併用
- 連続配列型空間グリッドによる近傍探索
- 境界粒子2層と領域クランプの併用

### GPU実装

- Compute Shader内で空間グリッド、密度、加速度、時間刻み、積分を実行
- Exclusive Prefix Sumでセルごとの粒子格納開始位置を構築
- サブステップごとのGPUからCPUへの同期を回避
- CPU版をGPU版の検証基準として保持

### 水面描画

- `128 × 128`の固定格子メッシュを使用
- 各メッシュ頂点の水深を近傍粒子からPoly6補間
- 隣接頂点の水深差から水面法線を計算
- 水深と法線をGraphicsBufferから頂点シェーダーへ直接受け渡す
- 水面描画ではCPUへのGPUデータ読み戻しを行わない
- 従来の粒子ビルボード描画はデバッグ用途として保持

## フォルダ構成

```text
Assets/SphSwe/
├─ Editor/          Editor拡張と検証メニュー
├─ Model/           キャラクター、水面、Prefab、Material
├─ PostProcess/     画面エフェクト用Asset
├─ Scenes/          Unity Scene
├─ Scripts/
│  ├─ Core/         粒子などの基本データ
│  ├─ Diagnostics/  FPS表示などの診断
│  ├─ Gameplay/     Player、Enemy、得点、目的球、攻撃
│  ├─ Gpu/          Compute Shader制御とGPUバッファ
│  ├─ Rendering/    水面、雷、キャラクター描画
│  ├─ Simulation/   CPU基準実装
│  ├─ UserInterface/
│  └─ Validation/
├─ Settings/        URP関連設定
├─ Shaders/         Compute Shaderと描画Shader
└─ VFX/             雷砲、硬直Particleなど
```

理論、設計判断、検証結果の正本は[`Documentation/README.md`](Documentation/README.md)から参照できます。

## 現在の状態と既知の課題

- ゲームループ、得点、Enemy AI、雷砲、硬直表現、水面メッシュ描画まで動作する試作段階です。
- ネットワーク対戦は未実装です。
- 水面補間対象となる粒子が見つからない頂点では、水深が0となり局所的なくぼみが見える場合があります。
- Enemyの造波頻度、雷砲命中率、硬直時間などは継続調整中です。
- `Viscosity Coefficient`は数値安定性にも関係するため、見た目の減衰調整だけを目的に大きく変更しないでください。

## Gitブランチ運用

- プロジェクトの最新かつ基本となる状態を確認する場合は、`main`ブランチを参照してください。
- 通常のclone、動作確認、レビューおよび派生作業も、特別な指定がなければ`main`を起点とします。
- GitHubアカウント`hmdyt`は`stable`ブランチにのみpushします。
- `hmdyt`の作業成果は`stable`ブランチへ順次マージします。
- `hmdyt`から`main`ブランチへ直接pushしません。
- `stable`ブランチには定期的に`main`ブランチの最新変更を取り込みます。

## 今後の展望

- マウスホイール、または`W` / `S`キーによる必殺技の切り替え
- 移動速度上昇、波の強化など、新しい必殺技の追加
- 必殺技選択状態を確認できるUIの追加
- Enemy AIの難易度と行動パターンの調整
- 水面補間に失敗した頂点へ発生する局所的なくぼみの改善
- 水面Shaderの透明感、反射、影および深度表現の改善
- サーバーへ接続するオンライン対戦機能の実装
- オンライン対戦を考慮した入力、シミュレーションおよびゲーム状態の同期設計

## Third-party assets

- Lightning texture by `wreaderror` — CC0 1.0  
  <https://opengameart.org/content/lightning>

詳細は[`Assets/ThirdParty/OpenGameArt/Lightning/LICENSE.md`](Assets/ThirdParty/OpenGameArt/Lightning/LICENSE.md)を参照してください。その他のPackageとAssetには、それぞれのライセンスが適用されます。
