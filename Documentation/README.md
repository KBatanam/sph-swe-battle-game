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
11. [CFL時間刻み診断](11-CFL-Time-Step-Diagnostics.md)
12. [CPU処理時間の計測](12-CPU-Profiling.md)
13. [フレーム駆動のシミュレーションループ](13-Frame-Driven-Simulation-Loop.md)
14. [連続配列型の空間グリッド](14-Contiguous-Array-Spatial-Grid.md)
15. [時間刻みと境界粒子層数の比較](15-Time-Step-And-Boundary-Layer-Benchmark.md)
16. [Compute Shader移植方針](16-Compute-Shader-Migration.md)
17. [GPU粒子描画](17-GPU-Particle-Rendering.md)

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
- 参照実装の粒子質量、目標近傍粒子数、参照密度から固定有効半径と粒子間隔を算出する密度校正を実装・検証済み
- 校正後の中心粒子密度は`992.40`で、参照密度`998.29`に対する相対誤差は約`0.59%`
- EditorウィンドウからCFL時間刻み、制限粒子、実行サブステップ数を確認できる診断機能を実装済み
- Unity Profiler上で密度計算、近傍探索、加速度、積分、Gizmos描画の処理時間を個別に確認可能
- FixedUpdateのキャッチアップによる負荷増加を避けるため、シミュレーションを一描画フレーム当たり最大20サブステップのUpdate駆動へ変更
- 空間グリッドをDictionaryとセル別Listから、整数セル番号を使用する連続配列型へ変更
- 最大時間刻み4段階と境界粒子2・3層を比較するEditorベンチマークを実装
- 短時間試験では境界2層と最大時間刻み`0.004`秒を次の推奨試験設定とする
- 境界粒子は2層を正式採用し、Scene設定とスクリプトのデフォルト値へ反映済み
- 最大時間刻みは長時間安定性試験を通過した`0.004`秒を正式採用し、CFL条件と併用
- CPU版を検証基準として保持し、大規模粒子向けにCompute Shader版を別実装する方針を決定
- GPU転送用の48バイト粒子構造とメモリレイアウトテストを追加
- GPU側にフレーム経過時間、未処理時間、現在の時間刻み、完了サブステップ数を保持する時間管理状態を追加
- CFL時間刻みと未処理時間から実時間刻みを決定し、最大20サブステップのUpdate駆動GPUループを実装・検証済み
- Compute ShaderのParticle BufferをCPUへ戻さず、`Graphics.RenderPrimitives`で全粒子を一括描画
- `Simulation Area`と固定粒子間隔から流体粒子数を自動計算し、領域全体へ中央揃えで配置
- 境界粒子を含む13,504粒子でGPU連続シミュレーションと描画を検証済み
- Unity Editor上の参考値として200 FPS台を確認し、13,504粒子を現時点の想定上限として採用
- CPU版は初期生成と検証基準として残し、GPU連続実行時はCPU版の毎フレーム計算を停止
- 最終水面は固定格子メッシュの頂点高さを補間する方式、船は水面への反作用なしで高さ・法線・流速へ追従する方針

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
