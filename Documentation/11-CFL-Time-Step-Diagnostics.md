# CFL時間刻み診断

## 目的

SPH-SWEシミュレーションでは、Unityの`FixedUpdate`一回分を複数のサブステップへ分割して計算する。

診断ウィンドウでは、現在の粒子状態から次の内容を確認できる。

- CFL条件から求めた最大時間刻み
- Inspectorで設定した最大時間刻み
- 実際に選択される時間刻み
- 時間刻みを最も厳しく制限する粒子
- `FixedUpdate`一回分に必要な推定サブステップ数
- 直前の`FixedUpdate`で実際に完了したサブステップ数
- サブステップ上限によって処理しきれなかったシミュレーション時間

## 時間刻みの計算

粒子$i$の流体深さを$d_i$、速度を$\mathbf{v}_i$とすると、浅水波の伝播速度は次のように求める。

$$
c_i=\sqrt{g d_i}
$$

CFL条件による粒子ごとの時間刻みは次のとおり。

$$
\Delta t_i=C_{\mathrm{CFL}}\frac{H}{|\mathbf{v}_i|+c_i}
$$

| 記号 | 意味 |
|---|---|
| $g$ | 重力加速度 |
| $d_i$ | 粒子$i$の流体深さ |
| $c_i$ | 粒子$i$における浅水波の伝播速度 |
| $C_{\mathrm{CFL}}$ | Courant数 |
| $H$ | 全粒子共通の固定有効半径 |
| $\Delta t_i$ | 粒子$i$が許容する時間刻み |

全流体粒子の最小値を`CFL Time Step`とし、Inspectorの`Maximum Simulation Time Step`と比較する。

$$
\Delta t=\min\left(\Delta t_{\max},\min_i\Delta t_i\right)
$$

## Unity Editorでの確認手順

1. Unity Editor上部の`Tools`を開く。
2. `SPH-SWE > Diagnostics > Time Step Diagnostics`を選択する。
3. 開いたウィンドウを任意の場所へドッキングする。
4. Playボタンを押してPlay Modeへ入る。
5. `Automatic Refresh Enabled`をオンにすると表示が自動更新される。
6. 手動更新したい場合は`Refresh`を押す。

Consoleへ一行で記録したい場合は、Play Mode中に次を実行する。

```text
Tools > SPH-SWE > Diagnostics > Log Current Time Step Diagnostics
```

## 主な表示項目

| 表示 | 意味 |
|---|---|
| `CFL Time Step` | 現在の全流体粒子から求めたCFL時間刻み |
| `Maximum Configured Time Step` | Inspectorで設定した時間刻み上限 |
| `Selected Time Step` | 次の計算で使用される時間刻み |
| `Limiting Condition` | CFL条件と設定上限のどちらが時間刻みを制限しているか |
| `Particle Index` | 最小のCFL時間刻みを与えた粒子番号 |
| `Signal Speed` | 制限粒子の速度と波速の和 |
| `Estimated Required Count` | 現在の時間刻みで`Fixed Delta Time`を処理するための推定回数 |
| `Last Completed Count` | 直前の`FixedUpdate`で実際に完了した回数 |
| `Remaining Accumulated Time` | サブステップ上限などにより処理しきれず残った時間 |

`Estimated Required Count`が`Maximum Count`を超える場合は、シミュレーション時間が実時間から遅れる可能性があるため警告を表示する。

## 初回確認結果

密度校正後の現在のSceneで、次の結果を確認した。

```text
Selected Time Step = 0.002000秒
CFL Time Step = 0.007555秒
Last Completed Substep Count = 10
Estimated Required Substep Count = 10
```

現在はCFL条件ではなく、`Maximum Simulation Time Step = 0.002`が時間刻みを制限している。

このため、`Fixed Delta Time = 0.02`秒を処理するには通常10サブステップ必要となる。今後パフォーマンスを測定する際は、粒子数だけでなく、このサブステップ回数も計算負荷の主要因として扱う。

## その後のループ設計変更

Profiler計測により、Unityの`FixedUpdate`キャッチアップが一描画フレーム中のサブステップ数を大幅に増加させることが判明した。このため、シミュレーションの実行起点を`FixedUpdate`から`Update`へ変更した。

現在の診断ウィンドウでは、`Fixed Delta Time`の代わりに`Last Requested Simulation Time`を表示する。これは直前の描画フレームで処理対象となった、持ち越し分を含むシミュレーション時間である。

現在の設計については、[フレーム駆動のシミュレーションループ](13-Frame-Driven-Simulation-Loop.md)を参照する。
