# CPU処理時間の計測

## 目的

粒子法シミュレーションの最適化では、粒子数だけを見て判断せず、各処理が一フレーム中に占める時間を測定する。

Unityの`ProfilerMarker`を使用し、Profiler上でSPH-SWE固有の処理時間を識別できるようにした。計測用コードは計算結果を変更しない。

## 追加した計測範囲

```text
SPH-SWE.FixedUpdate
└─ SPH-SWE.SimulateAdaptiveStep
   ├─ SPH-SWE.CalculateDensities
   │  └─ SPH-SWE.RebuildNeighborParticleIndices
   │     ├─ SPH-SWE.RebuildSpatialGrid
   │     └─ SPH-SWE.CollectNeighbors
   ├─ SPH-SWE.CalculateAccelerations
   │  ├─ SPH-SWE.FluidDepthGradient
   │  └─ SPH-SWE.Viscosity
   └─ SPH-SWE.IntegrateParticles

SPH-SWE.DrawGizmos
```

`SimulateAdaptiveStep`はサブステップごとに一回実行される。`FixedUpdate`一回につき10サブステップなら、その内側の密度計算や加速度計算も10回実行される。

## Unity Profilerでの確認手順

1. Unity Editor上部の`Window`を開く。
2. `Analysis > Profiler`を選択する。
3. Profilerウィンドウ左側で`CPU Usage`を有効にする。
4. 上部の対象が`Editor`になっていることを確認する。
5. `Deep Profile`はオフにする。
6. Playボタンを押してPlay Modeへ入る。
7. 数秒間動作させてから、一時停止ボタンを押す。
8. `CPU Usage`グラフから確認したいフレームを選択する。
9. 下部表示を`Hierarchy`へ切り替える。
10. 検索欄へ`SPH-SWE`と入力する。

`Hierarchy`の`Total`は子処理を含む時間、`Self`はその処理自身だけの時間を表す。まず`Total`が大きいマーカーを探し、その子処理へ順番に掘り下げる。

## Gizmos負荷の切り分け

SceneビューのGizmos描画はゲーム本体の描画方式ではなく、Editor上の確認機能である。粒子数が増えると`Gizmos.DrawSphere`自体が大きな負荷になる可能性がある。

次の二つの条件を同程度の時間測定し、比較する。

```text
条件A: Particle Gizmo Drawing Enabled = true
条件B: Particle Gizmo Drawing Enabled = false
```

条件Bで大幅に軽くなる場合、シミュレーション計算ではなくSceneビューのGizmos描画が主なボトルネックである。

## 計測時の注意

- Profilerウィンドウを開いていること自体にも計測負荷がある。
- `Deep Profile`はすべてのメソッドへ計測処理を追加して大幅に遅くなるため、通常の比較では使用しない。
- Editorの処理時間にはInspector、Sceneビュー、Gizmosなども含まれる。
- 最終的なゲーム性能はDevelopment Buildでも改めて測定する。
- 一フレームだけで判断せず、複数フレームの傾向を見る。
- 比較時は粒子数、サブステップ数、Gizmos設定を揃える。

## 最初に確認する値

次の順序でボトルネックを確認する。

1. `SPH-SWE.FixedUpdate`の合計時間
2. `SPH-SWE.SimulateAdaptiveStep`一回の時間と呼び出し回数
3. `SPH-SWE.CalculateDensities`と`SPH-SWE.CalculateAccelerations`の比率
4. 密度計算内のグリッド再構築と近傍収集の比率
5. 勾配加速度と粘性加速度の比率
6. `SPH-SWE.DrawGizmos`の時間

この測定結果を基準に、CPUコードの改善、Job System・Burst、Compute Shaderのどこへ進むかを決定する。

## 初回計測で判明した内容

初回計測では、一描画フレーム中に`FixedUpdate`が17回、`SimulateAdaptiveStep`が170回実行されていた。処理時間の約68.6%を`SPH-SWE.CollectNeighbors`が占めた。

この結果を受け、シミュレーションループを`Update`駆動へ変更した。現在のProfilerでは、最上位マーカーが`SPH-SWE.FixedUpdate`から`SPH-SWE.Update`へ変更されている。

以後の計測では`SPH-SWE.Update`を基準とし、一描画フレーム当たりの呼び出し回数が1回であることを確認する。
