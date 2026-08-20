# Poly6カーネル

## SPHカーネル

SPHでは、粒子$i$の物理量を周囲の粒子から推定する。

密度推定は次の形になる。

$$
\rho_i
=
\sum_j m_j W(\lVert\mathbf{x}_i-\mathbf{x}_j\rVert,h)
$$

カーネル関数は近い粒子へ大きな重みを与え、有効半径以上離れた粒子へは`0`を返す。

## 2次元Poly6カーネル

参照実装が使用するカーネルは次である。

$$
W_{\mathrm{poly6}}(r,h)
=
\begin{cases}
\displaystyle
\frac{4}{\pi h^8}(h^2-r^2)^3
& 0\leq r<h \\
0
& r\geq h
\end{cases}
$$

| 記号 | 意味 |
|---|---|
| $r$ | 2粒子間の距離 |
| $h$ | 有効半径 |
| $W$ | 距離に応じた重み |

## 距離の二乗

Poly6では$r^2$を直接使用するため、平方根を計算する必要がない。

CPU版ではGPU版との対応を明確にするため、距離の二乗を明示的な`float`乗算で計算する。

```csharp
var differenceX =
    particle.Position.x - neighbor.Position.x;

var differenceZ =
    particle.Position.y - neighbor.Position.y;

var squaredDistance =
    differenceX * differenceX
    + differenceZ * differenceZ;
```

HLSLでは次に対応する。

```hlsl
float squaredDistance = dot(positionDifference, positionDifference);
```

## 入力検証

- 距離の二乗が負の場合は、呼び出し側の不具合として例外を発生させる。
- 有効半径が`0`以下の場合も例外を発生させる。
- 距離が有効半径以上の場合は正常な状態なので、エラーにせず`0`を返す。

## 性能方針

- `Mathf.Pow()`を使用せず、固定回数の乗算で$h^8$を求める。
- 初期実装では可読性と正しさを優先する。
- 有効半径が共通の場合、係数$4/(\pi h^8)$のキャッシュを後で検討する。
- GPU版では例外を使用できないため、C#側で入力を検証してから値を渡す。

