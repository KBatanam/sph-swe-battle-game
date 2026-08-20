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

## 現在の到達点

- 粒子データ構造を実装済み
- 流体粒子と境界粒子をenumで分類
- 格子状の粒子初期配置を実装済み
- Gizmosによる配置確認を実装済み
- 2次元Poly6カーネルを実装済み
- 全粒子探索による密度推定を実装中

## 文書更新ルール

- 実装で採用した内容と検討中の案を区別する。
- 数式、記号の意味、参照実装、Unity実装の対応を記録する。
- コードと同じGitリポジトリで変更履歴を管理する。
- 高度な最適化は、CPU基準実装で正しさを確認してから採用する。

