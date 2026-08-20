# 粒子の初期配置

## 格子配置

粒子をX-Z平面へ規則的な格子として配置する。

$$
\mathbf{x}_{i,j}
=
\begin{pmatrix}
(i-\frac{N_x-1}{2})\Delta x \\
(j-\frac{N_z-1}{2})\Delta x
\end{pmatrix}
+\mathbf{x}_0
$$

| 記号 | 意味 |
|---|---|
| $N_x$ | X方向の粒子数 |
| $N_z$ | Z方向の粒子数 |
| $\Delta x$ | 粒子間隔 |
| $\mathbf{x}_0$ | シミュレーション領域の中心 |

総粒子数は次となる。

$$
N=N_xN_z
$$

初期値はX方向16粒子、Z方向16粒子とし、合計256粒子から検証する。

## 粒子間隔と有効半径

```text
Particle Spacing
→ 初期配置で隣り合う粒子の距離

Effective Radius
→ SPH計算で近傍として扱う最大距離
```

初期設定：

```text
Particle Spacing = 0.25
Effective Radius = 0.5
```

## 管理方法

1粒子につき1つのGameObjectは作成しない。すべての粒子を`SphSweParticle[]`へ格納し、1つの`SphSweSimulation`から管理する。

```text
避ける構成：1粒子 = 1 GameObject
採用する構成：全粒子 = 1つの配列
```

## Gizmos

Gizmosは、初期配置とCPU計算結果を確認するための仮表示として使用する。最終的なGameビューの描画には使用しない。

```text
初期段階：Gizmos
最終段階：GraphicsBuffer + 専用Shader
```

シミュレーション位置からワールド位置への変換では、`TransformPoint`を使い、管理GameObjectの移動・回転・スケールを反映する。

## 初期化タイミング

Inspectorに保存された設定値だけで粒子を生成する現段階では、`Awake()`から初期化する。他コンポーネントが粒子を利用する場合、原則として`Start()`以降に参照する。

