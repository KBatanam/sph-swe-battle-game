# 流体深さ勾配による加速度

## 用語

今回計算する量には`Surface Slope`や`Water Depth Gradient`ではなく、`Fluid Depth Gradient`という名前を使用する。

- `Surface`だけでは、水面、水底、地形、描画メッシュのどれを指すか分かりにくい。
- シミュレーション対象は水に限定しないため、`Water`は使用しない。
- 密度を基準密度で割った流体深さの勾配を計算するため、`FluidDepthGradient`が計算内容に対応する。

## 基本式

粒子密度と流体深さの関係を次とする。

$$
d_i=\frac{\rho_i}{\rho_0}
$$

| 記号 | 意味 |
|---|---|
| $d_i$ | 粒子$i$が表す流体深さ |
| $\rho_i$ | SPHで推定した粒子密度 |
| $\rho_0$ | 流体の基準密度 |

流体深さ勾配による加速度は次である。

$$
\mathbf{a}_i
=
-g\nabla d_i
=
-\frac{g}{\rho_0}\nabla\rho_i
$$

SPHでは密度勾配を近傍粒子のSpikyカーネル勾配から近似する。

$$
\nabla\rho_i
=
\sum_j m_j\nabla W_{ij}
$$

## 参照実装との対応

参照C++実装では、対象粒子と近傍粒子のSpiky係数を加算している。

```cpp
acc += -m_gravity
       * m_p[j].mass
       * (m_aspiky + j_aspiky)
       * q * q
       * rij / r
       / m_density;
```

現在のUnity実装では全粒子が同じ有効半径を持つため、対象粒子のSpiky勾配を`2`倍して参照実装へ対応させる。

```csharp
var combinedSpikyGradient = spikyGradient * 2f;
```

粒子ごとに有効半径を変更する段階では、この仮定を外して対称化方法を再検討する。

## 計算順序

```text
粒子生成
  ↓
密度計算
  ↓
流体深さ勾配による加速度計算
  ↓
速度と位置の時間積分（未実装）
```

現段階では`Acceleration`のみを更新し、`Velocity`と`Position`は変更しない。

## 命名

```csharp
CalculateFluidDepthGradientAccelerations()
fluidDepthGradientAccelerationScale
fluidDepthGradientAcceleration
```

将来、水底または地形の高さ勾配を追加する場合は、次のように区別する。

```csharp
fluidDepthGradientAcceleration
groundHeightGradientAcceleration
```

両者を合計すると、自由表面全体の勾配による加速度に対応する。

## 検証結果

Editor専用のMenuItemテストをUniCortexから実行する。

```text
Tools/SPH-SWE/Tests/Run Kernel Tests
Tools/SPH-SWE/Tests/Validate Running Simulation Accelerations
```

2026年8月24日時点で、次を確認済みである。

- Spikyカーネル勾配の入力検証、中心、境界、影響範囲内、方向反転のテストがすべて成功した。
- Playモードで25粒子を生成し、16粒子に非ゼロの流体深さ勾配加速度が発生した。
- 全粒子の加速度に`NaN`またはInfinityが含まれていない。
- 対称な粒子配置における加速度の総和がほぼゼロになった。
- 対角に位置する粒子の加速度が互いに反対方向になった。
