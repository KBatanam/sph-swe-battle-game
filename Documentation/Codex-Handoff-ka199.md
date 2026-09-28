# Codex引き継ぎ情報（ka199個人用）

## この文書の適用範囲

この文書は、`ka199`が別のPCまたは新しいCodexタスクで作業を再開するための個人用引き継ぎメモである。

- プロジェクト全体の開発規約ではない。
- `ka199`以外の開発者やGitHubユーザーに、ここに記載したCodexの進め方、説明方法、編集許可の運用を強制しない。
- Unityのビルド、実行、Package、Project Settingsおよびシーン構成には影響しない。
- この文書を使用しない開発者は無視してよい。
- 共有の設計方針や正式な技術仕様については、`Documentation/README.md`および番号付き文書を正本とする。
- この文書と共有文書または実装が矛盾する場合は、共有文書と現在の実装を優先する。

## この文書の更新ルール

- `ka199`が別のPCまたは新しいCodexタスクでこのプロジェクトの作業を進めた場合も、作業終了時にこの文書を更新する。
- Codexは、`ka199`として行った作業によって引き継ぎ内容が変化した場合、実装済みの内容、検証結果、現在の作業位置および次に行うことをこの文書へ反映する。
- PC固有の絶対パス、Unity環境または導入済みツールに差異が生じた場合は、ほかのPCでも再開できるよう、その差異と必要な手順を記録する。
- 一時的な試行や破棄済みの変更は、現在の到達点として記録しない。
- 既存の記録を削除する必要がある場合は、削除前に`ka199`へ確認する。
- この更新ルールは`ka199`の作業にだけ適用し、ほかの開発者にはこの文書の更新を要求しない。

## 再開時の手順

1. GitおよびGit LFSをインストールする。
2. GitHubからリポジトリをcloneする。
3. Unity Hubへ既存プロジェクトとして追加する。
4. Unity 6.3 LTS（6000.3.22f1）でプロジェクトを開く。
5. Codexでclone先のプロジェクトをワークスペースとして開く。
6. 新しいCodexタスクへ、この文書と`Documentation/README.md`を最初に読ませる。
7. 実装内容と文書の記録が一致しているか、現在のGit差分とUnityのConsoleを確認する。

新しいCodexタスクへ最初に送る文面の例：

```text
Documentation/Codex-Handoff-ka199.mdとDocumentation/README.mdを読み、
現在の実装とGit差分を確認してください。
前回はGPU側のサブステップ時間管理を実装中で、
次はFinalizeSimulationTimeStepの追加から再開します。
この引き継ぎ文書に記載されたka199向けの進め方を維持してください。
```

## 開発環境

- プロジェクト名：`sph-swe-battle-game`
- Unity：Unity 6.3 LTS（6000.3.22f1）
- Render Pipeline：Universal Render Pipeline
- テンプレート：URP Empty Template（SRP）
- IDE：JetBrains Rider
- CPU版を検証基準として保持し、大規模粒子向けのCompute Shader版を別実装する。
- 動的可変有効半径は採用せず、シミュレーション全体で共通の有効半径を使用する。

PCごとにUnityプロジェクトの絶対パスは異なり得るため、以前のPCのパスを前提にしない。clone後は現在のワークスペースルートを基準にファイルを探す。

## ka199向けのCodex作業方針

以下は`ka199`とCodexの間だけで使用する進行上の希望であり、ほかの開発者へは適用しない。

- 理論と実装を小さな単位に分け、上から順に説明する。
- 実装コードは原則としてコピー可能な形で提示し、`ka199`が確認しながら反映する。
- テストコードと`Documentation`の追記・更新はCodexが直接行ってよい。
- 既存文書の内容を削除する場合は、事前に`ka199`へ確認する。
- Unityプロジェクトへコードを書き込む場合は、その都度`ka199`へ許可を確認する。
- Unity操作では、Hierarchy、Inspector、Profilerなどの具体的な操作手順も示す。
- C#のローカル変数は、型が明確な場合は基本的に`var`を使用する。
- Runtimeで動的に文字列を構築する場合はZStringを使用する。Editor専用処理では文字列補間を使用してよい。
- boolの機能フラグは意味が明確な名称とし、機能の有効・無効を表す場合は`Enabled`で終える。
- 多少長くても、役割が分かる変数名と関数名を優先する。
- 不自然または冗長な改行を増やさない。
- クラスが長くなった場合はpartial classによる責務分割を検討する。
- Unity上の必須参照には、プロジェクト内で自作した`Required`属性を使用する。
- 高度な最適化は、CPU基準実装および検証処理との比較後に正式採用する。

## Codex作業の必須完了条件

以下は、`ka199`としてCodexと作業するときに省略してはならない。

1. 作業開始前に、現在の実装、関連する共有文書および既存のGit差分を確認する。
2. ユーザーが既に行った修正を尊重し、依頼と無関係なコードや設定を変更しない。
3. 理論、数式、設計判断、採用方針、Inspector設定、検証方法または検証結果が追加・変更された場合は、対応する`Documentation`内の共有文書を同じ作業内で必ず更新する。
4. 共有文書には、確定して正式採用した内容と、候補・検討中・一時的なデバッグ内容を区別して記録する。
5. 実装だけを進めて文書更新を後回しにしない。対応する文書がまだ存在しない場合は、既存の文書構成に合わせて新しいMarkdown文書を追加する。
6. 別PCまたは新しいCodexタスクでの再開に影響する変更があった場合は、この`Codex-Handoff-ka199.md`も同じ作業内で必ず更新する。
7. 実装を変更した場合は、変更内容に応じたコンパイル確認、既存検証または追加テストを実施する。実行できない場合は、未検証である理由と必要な確認手順を明記する。
8. 共有文書またはこの文書の既存内容を削除する場合は、削除前に`ka199`へ確認する。追記と事実関係の更新は確認を待たずに行ってよい。
9. Unityプロジェクトの実装コード、Shader、Scene、設定またはPackageへ直接書き込む場合は、その都度`ka199`から許可を得る。テストと`Documentation`は、許可済みの運用範囲でCodexが直接更新してよい。
10. 作業終了時に、変更したファイル、検証結果、未完了事項および次に行うことを`ka199`へ報告する。

この必須完了条件も`ka199`とCodexの作業にだけ適用し、ほかの開発者の作業手順を制約しない。

## 現在の基本設計

```text
CPU基準実装
  ↓ 数値と挙動を検証
Compute Shader版
  ↓ CPU版と比較
GPU上で空間グリッド、密度、加速度、CFL時間刻み、積分を実行
  ↓
将来はGPU粒子描画と水面再構成へ進む
```

主な方針：

- CPU版は正しさを確認する基準として残す。
- Compute Shader版では、サブステップごとのGPUからCPUへの同期を避ける。
- GPUの空間グリッドには、セルごとの粒子数、Exclusive Prefix Sum、セル順粒子インデックス配列を使用する。
- GPU粒子データはC#とHLSLで同じメモリ配置にする。
- 境界粒子は2層を正式採用している。
- CFL条件と最大時間刻み`0.004`秒を併用する。
- シミュレーション領域の外へ出ないよう、位置クランプと境界粒子を併用する。
- Render Graphは有効のままにし、将来の粒子深度、水厚、平滑化、法線復元およびURP合成で利用を検討する。

## Compute Shader版の到達点

GPU側には、少なくとも次の処理を実装済みである。

1. セル粒子数のクリア
2. 粒子の所属セル登録
3. Scanグループ内のセル開始位置計算
4. Scanグループ間の開始位置計算
5. セル開始位置のグローバル化
6. セル書き込み位置の初期化
7. 粒子インデックスのセル単位整列
8. Poly6カーネルによる密度と流体深さの計算
9. Spiky勾配とViscosityカーネルによる加速度計算
10. CFL条件による最小時間刻みのGPUリダクション
11. 半陰的オイラー法による速度と位置の更新
12. 速度上限とシミュレーション領域による位置制限
13. GPU時間管理状態の保持
14. 描画フレーム時間のGPU側への蓄積
15. サブステップ実行可否のGPU側判定
16. 非アクティブなサブステップにおける各計算カーネルの早期終了

時間管理用の構造体は次の値を保持する。

```text
AccumulatedSimulationTime
CurrentSimulationTimeStep
CompletedSubstepCount
IsSimulationSubstepActive
```

C#とHLSLの双方で`float`2個と`uint`2個を同じ順序で配置し、1要素のstrideを16 bytesとしている。`const Stride`は構造体インスタンスのメモリには含まれない。

## 検証済みの内容

- C#とHLSL間の48 bytes粒子構造体コピー
- GPUセル登録
- グループ内Exclusive Prefix Sum
- 複数Scanグループの開始位置計算
- GPU空間グリッド構築
- CPU版とGPU版の密度・流体深さ比較
- CPU版とGPU版の加速度比較
- GPU CFL最小時間刻み
- GPU粒子積分後の位置と速度
- 検証時のGPU処理は、Editor専用の`AsyncGPUReadback.Request`で非同期に読み戻す。

直近で確認した代表的なログ：

```text
GPU minimum time step validation passed. Time step: 0.004.
GPU runtime density, acceleration, and integration validation passed. Particle count: 985.
```

clone後の環境では、Unityのバージョン、Scene、Inspector設定および直近のコミットによって結果が変わり得るため、再度検証する。

## 現在の作業位置

実装済み：

```text
BeginSimulationFrame
  ↓
フレーム経過時間をAccumulatedSimulationTimeへ加算
  ↓
BeginSimulationSubstep
  ↓
未処理時間と最大サブステップ回数からActive/Inactiveを決定
  ↓
Inactiveなら各計算カーネルを早期終了
```

まだ既存の実行経路を連続GPUシミュレーションへ切り替えていない。`ExecuteBeginSimulationFrame()`と`ExecuteBeginSimulationSubstep()`の部品は追加済みだが、既存の一回実行の検証経路を壊さない段階で止めている。

## 次に行うこと

次は`FinalizeSimulationTimeStep`を追加する。

目的：

1. CFL計算で得た最小時間刻みを読み取る。
2. CFL時間刻みと`AccumulatedSimulationTime`の小さい方を、今回の`CurrentSimulationTimeStep`にする。
3. 積分処理が`CurrentSimulationTimeStep`を使用できるようにする。
4. 積分完了後に未処理時間を減らし、`CompletedSubstepCount`を増やす。
5. 固定回数分Dispatchしても、不要になった残りのサブステップは早期終了させる。
6. 最後にCPU基準実装との比較テストを実行する。

GPU側の処理順序は、最終的に次の形を目指す。

```text
BeginSimulationFrame
  ↓
最大サブステップ回数分だけ以下をDispatch
  ├─ BeginSimulationSubstep
  ├─ 空間グリッド再構築と密度計算
  ├─ 加速度計算
  ├─ CFL最小時間刻み計算
  ├─ FinalizeSimulationTimeStep
  └─ 粒子積分と時間状態の更新
```

CPUは固定上限までDispatchを登録するが、実際に必要な回数はGPU上の`IsSimulationSubstepActive`で制御する。これにより、サブステップごとのGPUからCPUへの読み戻しと同期を避ける。

## 最新状況（2026年9月28日）

この節は、上に記録された以前の「現在の作業位置」と「次に行うこと」より新しい状態を表す。

実装済み：

- `FinalizeSimulationTimeStep`でCFL時間刻みと未処理時間の小さい方を実時間刻みにする。
- `IntegrateParticles`は`CurrentSimulationTimeStep`を使用する。
- `CompleteSimulationSubstep`で進めた時間を減算し、完了回数を増やす。
- `Update()`からフレーム時間を蓄積し、最大サブステップ数までGPU処理を登録する。
- 不要なサブステップはGPU側のフラグによって早期終了する。
- 旧一回実行向けの自動検証呼び出しを外した。
- GPUフレームループ後の時間状態を一度だけ`AsyncGPUReadback`で検証するEditor専用処理を追加した。

GPU時間状態とUpdate駆動ループの基本検証まで完了している。

次に行うこと：

1. 連続実行後のGPU粒子位置・速度を検証または可視化する。
2. Profilerで固定上限分のDispatchと早期終了のコストを計測する。

### 時間状態の検証結果

2026年9月28日、Unity Editor上で次の成功ログを確認した。

```text
GPU simulation time state validation passed. Accumulated: 0, Completed steps: 5.
```

GPU時間状態、Update駆動ループおよび連続実行後のGPU粒子状態の基本検証は完了した。次は、GPU粒子の可視化またはProfiler計測へ進む。

なお、`GroupMemoryBarrierWithGroupSync`を使用する次のカーネルでは、Barrier前の早期リターンによってカーネルが無効になったため、先頭の非アクティブ判定を削除した。

- `ScanCellParticleCountsByGroup`
- `ScanCellParticleCountGroupSums`
- `CalculateMinimumTimeStep`

これらを最適化する場合は、全スレッドがBarrierへ到達する構造を維持する。

### 連続GPU粒子の検証結果

2026年9月28日、連続実行後のGPU粒子について次の成功ログを確認した。

```text
GPU continuous particle validation passed. Particle count: 985, moved fluid particles: 24.
```

位置、速度、加速度、密度、流体深さの有限性、流体粒子の領域制限および境界粒子の固定を全985粒子が通過した。流体粒子24個の位置または速度が初期状態から変化したため、GPUフレームループによる継続的な粒子積分も確認できた。

次は、GPU粒子の可視化、またはProfilerによる固定上限分のDispatchと非アクティブ処理のコスト計測へ進む。

## 最新状況（2026年9月29日）

GPU粒子の可視化まで完了した。

- `Graphics.RenderPrimitives`でParticle BufferをCPUへ戻さず描画する。
- 1粒子を6頂点のビルボードとして描き、Fragment Shaderで円形に切り抜く。
- Materialプロパティは共通`UnityPerMaterial` CBUFFERへまとめた。
- Materialプレビューの未接続Buffer警告を避けるため、実行時だけローカルShaderキーワードを有効化する。
- 流体粒子数は`Simulation Area`と固定粒子間隔から自動計算する。
- 現在の総粒子数は境界粒子を含めて13,504個であり、これを想定上限とする。
- Editor上の参考値として200 FPS台を確認した。
- CPU版の`Simulation Execution Enabled`はオフ、GPU版はオンにしてSceneへ保存済みである。
- 画面左下にTextMeshProとZStringを使用したFPS表示を追加した。

GPU関連のMenuItemテスト7件は2026年9月29日にすべて成功した。連続実行対応後のKernelを単体テストする際は、`IsSimulationSubstepActive = 1`の時間状態Bufferを接続する必要がある。

詳細は`Documentation/17-GPU-Particle-Rendering.md`を参照する。

今後、ゲーム用水面は固定格子メッシュへ粒子水深を補間して描画する。船は水面へ反作用を与えず、水面高さ、法線および水平流速へ一方向に追従させる。この作業は後で行い、当面は現在の粒子ビルボードを診断表示として使用する。

## Gitおよび共有時の注意

- ソースコードと共有設計文書は通常どおりGitHubで共有する。
- この個人用文書が存在しても、ほかの開発者のUnity操作やCodex運用を変更する必要はない。
- `.codex/`は端末固有設定や機密情報を含む可能性があるため、Gitの管理対象にしない。
- Codexの会話履歴そのものはGitリポジトリに含まれない。
- この文書は会話履歴の完全な複製ではなく、別環境で安全に作業を再開するための要約である。
- GitHubアカウント`hmdyt`に関する正式なブランチ運用は`Documentation/README.md`の記載に従う。この個人用文書から追加の制約は課さない。
