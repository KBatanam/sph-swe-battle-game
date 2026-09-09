# 連続配列型の空間グリッド

## 背景

初期のUnity実装では、セルごとの粒子インデックスを次の構造で管理していた。

```csharp
Dictionary<Vector2Int, List<int>> particleIndicesByCell;
```

Profilerでは、20サブステップを実行する一描画フレームにおいて、`SPH-SWE.CollectNeighbors`が約52ms、シミュレーション処理時間の約69%を占めていた。

参照実装の`NNGrid`は、セル座標を整数ハッシュへ変換し、粒子インデックスをハッシュ順に連続配置している。各セルは、粒子配列内の開始位置と終了位置で表現される。

## 採用したデータ構造

Unity版も、セルごとに個別の`List<int>`を持つ構造から、次の連続配列へ変更した。

```text
particleCellCoordinates[]
particleCountsByCell[]
particleStartIndicesByCell[]
nextParticleWriteIndicesByCell[]
sortedParticleIndices[]
```

| 配列 | 役割 |
|---|---|
| `particleCellCoordinates` | 各粒子が所属するセル座標 |
| `particleCountsByCell` | 各セルに所属する粒子数 |
| `particleStartIndicesByCell` | 各セルの粒子が始まる位置 |
| `nextParticleWriteIndicesByCell` | グリッド構築中の次の書き込み位置 |
| `sortedParticleIndices` | セル単位で連続配置した粒子インデックス |

## グリッド再構築手順

1. 全粒子のセル座標を計算する。
2. 粒子が存在するセル範囲を求める。
3. 各セルの粒子数を数える。
4. 粒子数の累積和から、各セルの開始位置を計算する。
5. 各粒子インデックスを`sortedParticleIndices`へ配置する。

参照実装は一般的なソートを使用するが、Unity版ではセルごとの個数を先に数えることで、比較ソートを使用せずに連続配置する。

粒子数を$N$、使用するセル数を$C$とした場合、グリッド構築の計算量は概ね次のとおり。

$$
O(N+C)
$$

## 近傍収集

セルサイズは全粒子共通の固定有効半径と同じ値を使用する。そのため、通常は探索中心セルと周囲8セルの、合計9セルが候補になる。

各候補セルについて、`particleStartIndicesByCell`と`particleCountsByCell`から`sortedParticleIndices`の連続範囲を取得する。その範囲内の粒子だけに距離判定を行う。

```text
セル開始位置 = particleStartIndicesByCell[cellIndex]
セル終了位置 = セル開始位置 + particleCountsByCell[cellIndex]
```

## この方式の利点

- 周辺セルごとの`Dictionary`検索を排除できる。
- セルごとの`List<int>`参照を排除できる。
- 粒子インデックスを連続したメモリから読み取れる。
- 粒子数とセル範囲が変わらなければ配列を再利用できる。
- 一般的な比較ソートを必要としない。
- 将来のJob System、Burst、Compute Shaderへ移植しやすい。

## 検証結果

次のテストが連続配列型への変更後も成功することを確認した。

- 空間グリッドと総当たり探索の近傍結果の一致
- 空間グリッドと総当たり探索の密度計算結果の一致
- 参照粒子パラメータと中心密度
- 流体深さ勾配加速度
- 粘性加速度
- 時間積分
- CFL時間刻み
- 境界粒子

次はUnity Profilerで、変更前の`CollectNeighbors = 約52ms`を基準として処理時間を再測定する。
