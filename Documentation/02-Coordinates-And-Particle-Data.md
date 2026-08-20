# 座標系と粒子データ

## シミュレーション座標系

SPH-SWEでは、水平方向の流れを2次元として計算する。

| SPH-SWE | Unity |
|---|---|
| 計算上のx座標 | X軸 |
| 計算上の第2座標 | Z軸 |
| 地形・水面の高さ | Y軸 |

```text
SPH-SWE position = (x, z)
Unity position   = (x, height, z)
```

粒子の計算位置、速度、加速度は`Vector2`で保持する。表示時に`Vector3`へ変換する。

## Vector2を使用する理由

- 不要な鉛直方向計算を減らす
- 浅水方程式の数式とコードを対応させる
- 近傍探索を2次元として実装できる
- GPUへ送るデータ量を抑える

## 粒子データ

現在の`SphSweParticle`は次の情報を保持する。

| フィールド | 意味 |
|---|---|
| `Position` | X-Z平面上の位置 |
| `Velocity` | X-Z平面上の速度 |
| `Acceleration` | X-Z平面上の加速度 |
| `Density` | SPHで推定した密度相当量 |
| `Mass` | 現在の粒子質量 |
| `InitialMass` | 初期状態の粒子質量 |
| `EffectiveRadius` | カーネルの有効半径 |
| `Type` | 流体粒子または境界粒子 |

## 粒子種別

```csharp
public enum SphSweParticleType : int
{
    Fluid = 0,
    Boundary = 1
}
```

基底型を`int`とし、参照実装および将来のHLSL側の整数値と対応させる。

## 密度と水深

参照実装では、密度相当量と基準密度から水深を求める。

$$
h_i=\frac{\rho_i}{\rho_0}
$$

| 記号 | 意味 |
|---|---|
| $h_i$ | 粒子$i$の水深 |
| $\rho_i$ | SPHで推定した密度相当量 |
| $\rho_0$ | 基準密度 |

地形高を$b(x,z)$とすると、表示上の水面高は次になる。

$$
y_i=b(x_i,z_i)+h_i
$$

水深は`SphSweParticle`へ重複保存せず、必要な処理側で`Density / ReferenceDensity`から導出する方針とする。

## 責務の分離

`SphSweParticle`はデータと初期化だけを担当する。水深計算はシミュレーション処理、Unityワールド座標への変換は表示・デバッグ処理へ置く。

