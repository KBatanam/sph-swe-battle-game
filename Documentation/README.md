# SPH-SWE Battle Game Documentation

このフォルダを、`sph-swe-battle-game`に関する理論・設計文書の正本とする。

文書はMarkdown形式で管理し、必要に応じてNotionへコピーまたはインポートする。

## 開発環境

- Unity 6.3 LTS（6000.3.22f1）
- Universal Render Pipeline 17.3.0
- URP Empty Template
- CPU版を基準実装として作成後、Compute Shaderへ移植する

## 文書一覧

1. [開発方針と描画設計](01-Development-Policy.md)
2. [座標系と粒子データ](02-Coordinates-And-Particle-Data.md)
3. [粒子の初期配置](03-Particle-Initialization.md)
4. [Poly6カーネル](04-Poly6-Kernel.md)
5. [密度推定](05-Density-Estimation.md)
6. [Spikyカーネル勾配](06-Spiky-Kernel-Gradient.md)
7. [流体深さ勾配による加速度](07-Fluid-Depth-Gradient-Acceleration.md)
8. [Viscosityカーネル](08-Viscosity-Kernel.md)
9. [時間積分とシミュレーション領域](09-Time-Integration-And-Simulation-Area.md)
10. [パラメータ校正とデバッグ用造波](10-Parameter-Calibration-And-Debug-Wave.md)

## 現在の到達点

- 粒子データ構造を実装済み
- 流体粒子と境界粒子をenumで分類
- 格子状の粒子初期配置を実装済み
- Gizmosによる配置確認を実装済み
- 2次元Poly6カーネルを実装済み
- 全粒子探索による密度推定を実装済み
- 密度に基づくGizmosの色分けを実装済み
- Spikyカーネル勾配を実装済み
- 流体深さ勾配による加速度を実装済み
- Viscosityカーネルを実装・検証済み
- 粘性加速度を実装・検証済み
- 半陰的オイラー法による時間積分を実装・検証済み
- シミュレーション領域と初期流体領域を分離する設計方針を決定
- 空間グリッドによる近傍探索を実装・検証済み
- 密度、勾配加速度、粘性加速度で近傍リストを共有
- 参照実装に合わせた密度校正を次の課題として決定
- 有効半径は全粒子共通の固定値とし、動的可変有効半径は採用しない

## Gitブランチ運用

- GitHubアカウント`hmdyt`は、`stable`ブランチにのみpushする。
- `hmdyt`の作業成果は`stable`ブランチへ順次マージする。
- `hmdyt`から`main`ブランチへ直接pushしない。
- `stable`ブランチには、定期的に`main`ブランチの最新変更を取り込む。

## 文書更新ルール

- 実装で採用した内容と検討中の案を区別する。
- 数式、記号の意味、参照実装、Unity実装の対応を記録する。
- コードと同じGitリポジトリで変更履歴を管理する。
- 高度な最適化は、CPU基準実装で正しさを確認してから採用する。
