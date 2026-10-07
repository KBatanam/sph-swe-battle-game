# GPU粒子描画

## 目的

Compute Shaderが更新した`GraphicsBuffer`をCPUへ読み戻さず、Vertex Shaderから直接参照して粒子を可視化する。

```text
Compute Shader
  ↓ GPU内のParticle Bufferを更新
Graphics.RenderPrimitives
  ↓ 同じBufferをVertex Shaderから参照
GPU粒子描画
```

CPU上で粒子ごとのGameObject、Mesh、RendererまたはColliderは生成しない。

## 描画方式

`Graphics.RenderPrimitives`による非IndexedのGPU Instancingを使用する。

```text
1インスタンス = 1粒子
1粒子          = 三角形2枚
1粒子の頂点数  = 6
```

Vertex Shaderでは、`SV_InstanceID`を粒子バッファの添字、`SV_VertexID`を6頂点の四角形座標の添字として使用する。四角形内の中心からの距離の二乗が1を超えるFragmentを`clip`し、円として表示する。

ビルボードのオフセットは`TransformViewToWorldDir`によってビュー空間からワールド空間へ変換する。手続き的描画は通常の`MeshRenderer`とTransformで結び付かないため、シミュレーション座標からワールド座標への変換には`_SimulationLocalToWorld`を明示的に渡す。

## SRP Batcher互換性

Materialプロパティは、共通HLSLファイルの単一の`UnityPerMaterial` CBUFFERへ配置する。粒子の`StructuredBuffer`は定数バッファへ含めず、GPUリソースとして別に宣言する。

実行時にはテンプレートMaterialを複製し、その専用MaterialへBufferと描画値を設定する。`MaterialPropertyBlock`は使用しない。

Material Inspectorのプレビューでは粒子バッファが接続されないため、次のローカルShaderキーワードで実行時Variantを分ける。

```text
SPH_SWE_PARTICLE_BUFFER_AVAILABLE
```

テンプレートMaterialのプレビューではBufferを要求せず、実行時Materialだけがキーワードを有効化して`_Particles`を参照する。実行時のみ有効にするVariantがBuild時に削除されないよう、`multi_compile_local`を使用する。

## 流体粒子と境界粒子の表示高さ

流体粒子の表示高さは次で求める。

```text
Ground Height + Fluid Depth × Fluid Depth Height Scale
```

境界粒子自身の密度と流体深さは壁付近の近傍補完に使用する計算値であり、表示高さとして使うと不規則に上下する。このため、境界粒子は正規化された基準水深`1`の高さへ固定して表示する。

最終的なゲーム用水面では境界粒子を表示せず、粒子表示はシミュレーションのデバッグ用途として残す。

## 粒子の初期配置

流体粒子数は手入力せず、固定粒子間隔と`Simulation Area Size`から軸ごとに自動計算する。

```text
Particle Count X = ceil(Simulation Area Size X / Particle Spacing)
Particle Count Z = ceil(Simulation Area Size Z / Particle Spacing)
```

配置幅は`(Particle Count - 1) × Particle Spacing`とし、`Simulation Center`を基準に中央揃えする。これにより、粒子中心を領域境界へ直接重ねにくくしながら領域全体を固定間隔で埋める。

## CPU版とGPU版の実行設定

CPU版は初期粒子生成、物理パラメータ保持および検証基準として残すが、GPU版の連続実行中はCPU版の毎フレーム計算を停止する。

```text
SphSweSimulation
└─ Simulation Execution Enabled: Off

SphSweGpuSimulation
└─ Simulation Execution Enabled: On
```

`SphSweSimulation`コンポーネント自体を無効化すると初期粒子を生成できないため、コンポーネントは有効なまま実行フラグだけを無効にする。

## FPS表示

画面左下にTextMeshProによるFPS表示を追加した。`Time.unscaledDeltaTime`を一定時間蓄積して平均FPSを計算し、ZStringのTextMeshPro拡張で表示文字列を更新する。

Editor上のFPSにはEditor自身やProfilerの負荷も含まれるため、最終評価はDevelopment Buildで行う。Editor上の表示は、同一条件における変更前後の比較値として扱う。

## 2026年9月29日の確認結果

`Simulation Area`全体を自動配置で埋めた結果、境界粒子を含む総粒子数は13,504個となった。

```text
Generated 13504 SPH-SWE particles.
Fluid density — Min: 992.39700, Max: 1113.57600, Average: 995.91950
GPU simulation time state validation passed. Accumulated: 0, Completed steps: 5.
GPU continuous particle validation passed. Particle count: 13504, moved fluid particles: 2940.
```

Unity Editor上の目視値では、この粒子数で200 FPS台を確認した。これは正式なBuildベンチマークではなく、GPU移植とGPU内完結描画によってリアルタイム実行へ十分な余裕が得られたことを示す参考値とする。現在の13,504粒子を想定上限とし、これ以上の粒子数増加は予定しない。

GPU粒子レイアウト、コピー、空間グリッド登録、部分Scan、グループ合計Scan、完全Scanおよび空間グリッド・密度計算の統合テストはすべて通過した。単体テストでは、連続実行Kernelと同じ条件にするため`IsSimulationSubstepActive = 1`の時間状態Bufferを対象Kernelへ接続する。

## 将来の水面描画と船

最終的なゲーム用水面は、規則的な三角形格子メッシュの各頂点で周辺粒子の水深を補間し、Vertex Shaderで高さを変形する方式を予定する。飛沫や水面の裏返りは表現対象としない。

船は水面へ反作用を与えず、水面高さ、法線および水平流速を参照する一方向連動とする。

```text
水面高さ → 船のY位置
水面法線 → 船のPitchとRoll
水平流速 → 船の水平移動への影響
船        → 水面への反作用なし
```

粒子ビルボード描画は、最終水面導入後もGPUシミュレーションの診断表示として残す。
