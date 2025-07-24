# PhotonTrafficLogger 利用ガイド

PhotonTrafficLoggerは、Unity + Photon Fusionプロジェクトでネットワークトラフィックをモニタリング・ログ記録するためのツールです。

## 概要

- **リアルタイムモニタリング**: 60Hzでネットワークトラフィックを収集
- **詳細統計**: 各通信メソッドごとの統計データを記録
- **自動レポート生成**: JSON形式でログファイルと統計レポートを出力
- **エディタ統合**: Unityエディタから簡単に設定・操作可能

## セットアップ

### 1. システム要件
- Unity 6000以降
- Photon Fusion 2.x
- .NET Framework/Core対応

### 2. 初期設定
1. InGameMockシーンを開く
2. Play modeに入る
3. PhotonTrafficLoggerが自動的にシーンに追加される

## 使用方法

### エディタメニュー

Unityエディタの **September > Photon Traffic Logger** メニューからアクセス：

#### 設定画面
- **September > Photon Traffic Logger Editor**
  - ログ出力設定の変更
  - ログファイルの管理
  - 設定値の確認

#### ログ制御
- **Start Logging**: ログ記録を開始
- **Stop Logging**: ログ記録を停止
- **Clear Logs**: 蓄積されたログをクリア

#### トラブルシューティング
- **Force Initialize Logger**: 強制的にLoggerを初期化
- **Check Logger Status**: Logger状態の確認
- **Attach FusionStats to NetworkRunner**: FusionStatisticsコンポーネントを手動添付

### 設定項目

#### 基本設定
- **Enable Logger**: ログ機能の有効/無効
- **Console Output**: コンソールへのリアルタイム出力
- **Log Output Path**: ログファイルの保存先パス
- **Log File Limit**: 保持するログファイル数の上限

#### 自動設定
- シーン遷移時の自動初期化（InGameMockシーン対応）
- NetworkRunnerへのFusionStatisticsコンポーネント自動添付
- 60Hz間隔での自動データ収集

## 出力ファイル

### 1. リアルタイムログ（realtime-YYYYMMDDHHMMSS.json）
```json
[
  {
    "trafficIncoming": {
      "callerName": "NetworkActivity.Server.Receiving.StateSync",
      "totalMessageCount": 5,
      "totalPacketBytes": 320,
      "longestMessageBytes": 85,
      "timestamp": "2025-01-19T10:30:15.123Z",
      "networkObjectName": "NetworkRunner",
      "networkObjectId": 0
    },
    "trafficOutgoing": {
      "callerName": "NetworkActivity.Server.Sending.GameplayEvent",
      "totalMessageCount": 3,
      "totalPacketBytes": 150,
      "longestMessageBytes": 55
    },
    "timestamp": "2025-01-19T10:30:15.123Z"
  }
]
```

### 2. 統計レポート（report-YYYYMMDDHHMMSS.json）
```json
{
  "incomingStats": {
    "totalMessages": 450,
    "totalBytes": 28800,
    "maxMessageSize": 320,
    "minMessageSize": 24,
    "averageMessageSize": 64.0,
    "averageMessagesPerSecond": 2.25,
    "averageBytesPerSecond": 144.0
  },
  "outgoingStats": {
    "totalMessages": 380,
    "totalBytes": 19000,
    "maxMessageSize": 150,
    "minMessageSize": 32,
    "averageMessageSize": 50.0,
    "averageMessagesPerSecond": 1.9,
    "averageBytesPerSecond": 95.0
  },
  "reportStart": "2025-01-19T10:00:00.000Z",
  "reportEnd": "2025-01-19T10:05:00.000Z",
  "totalEntries": 1800,
  "methodStatistics": {
    "NetworkActivity.Server.Sending.GameplayEvent": {
      "methodName": "NetworkActivity.Server.Sending.GameplayEvent",
      "totalCalls": 120,
      "callsPerSecond": 0.4,
      "incomingStats": { /* 受信統計 */ },
      "outgoingStats": { /* 送信統計 */ },
      "FirstSeen": "2025-01-19T10:00:05.000Z",
      "LastSeen": "2025-01-19T10:04:58.000Z"
    }
  }
}
```

## 呼び出し元表記の理解

### NetworkActivityパターン
PhotonTrafficLoggerは、スタックトレース解析により以下の形式で呼び出し元を特定します：

```
NetworkActivity.[役割].[通信方向].[アクティビティタイプ].[オブジェクト情報]
```

#### 役割
- **Server**: サーバーとしてのネットワーク処理
- **Client**: クライアントとしてのネットワーク処理

#### 通信方向
- **Sending**: 送信処理
- **Receiving**: 受信処理
- **Bidirectional**: 双方向通信

#### アクティビティタイプ
- **GameplayEvent**: ゲームプレイイベント（中程度のデータ量）
- **StateSync**: 状態同期（大容量データ）
- **ContinuousUpdate**: 継続的更新（小容量・高頻度）
- **LowActivity**: 軽微なアクティビティ
- **Idle**: アクティビティなし

#### オブジェクト情報
- **Players(N)**: プレイヤーオブジェクト数
- **Objects(N)**: その他ネットワークオブジェクト数

### 具体例
```
NetworkActivity.Server.Sending.GameplayEvent.Players(2)
→ サーバーが2人のプレイヤーに関するゲームプレイイベントを送信

NetworkActivity.Client.Receiving.StateSync.Objects(5)
→ クライアントが5つのオブジェクトの状態同期データを受信
```

## トラブルシューティング

### よくある問題

#### 1. ログが生成されない
- Play modeで実行していることを確認
- September > Photon Traffic Logger Editorで「Enable Logger」が有効になっているか確認
- InGameMockシーンに遷移しているか確認

#### 2. コンソールエラーが出る
```
PhotonTrafficLogger not found
```
→ September > Force Initialize Loggerを実行

#### 3. 空のJSONファイルが生成される
- NetworkRunnerが正常に動作しているか確認
- September > Attach FusionStats to NetworkRunnerを実行
- ネットワーク接続が確立されているか確認

#### 4. 最大パケットサイズが0
- 実際にネットワークトラフィックが発生しているか確認
- 時間を置いてから統計を確認（増分追跡のため）

### デバッグログの確認
コンソールで以下のログプレフィックスを検索：
- `[PhotonTrafficLogger]`: 一般的なログ情報
- `[PhotonTrafficLoggerInitializer]`: 初期化関連
- `[PhotonTrafficLoggerSettings]`: 設定関連

### ログファイルの場所
デフォルト: `{プロジェクトディレクトリ}/photon_logs/`

設定画面で変更可能。存在しないディレクトリの場合は自動作成されます。

## パフォーマンス考慮事項

### データ収集頻度
- デフォルト: 60Hz（1秒間に60回）
- 高頻度な収集によりリアルタイム性を確保
- パフォーマンスへの影響は最小限

### ファイル出力
- リアルタイムログ: 60エントリごと（1秒ごと）に保存
- 統計レポート: ログ停止時またはゲーム終了時に生成
- 古いログファイルは設定した上限数に基づいて自動削除

### メモリ使用量
- 蓄積ログはメモリ上に保持
- 長時間の実行では定期的なクリアを推奨

## カスタマイズ

### 設定の永続化
- PlayerPrefsを使用して設定を保存
- プロジェクト間で設定が共有される

### 拡張ポイント
- 新しいアクティビティタイプの追加
- カスタム統計メトリクスの実装
- 出力フォーマットの変更

## 技術仕様

### 主要コンポーネント
- **PhotonTrafficLogger**: メインロガー（MonoBehaviour）
- **PhotonTrafficLoggerSettings**: 設定管理
- **PhotonTrafficLoggerInitializer**: 自動初期化
- **PhotonTrafficLoggerEditorWindow**: 設定UI
- **MethodTrafficStats**: メソッド別統計
- **TrafficReport**: 統計レポート

### 依存関係
- Fusion.Statistics.FusionStatisticsManager
- Newtonsoft.Json（シリアライゼーション）
- UnityEngine.Networking（NetworkBehaviour）

### システム要件
- 最小メモリ: 50MB
- 推奨ストレージ: 100MB（ログファイル用）

---

## 更新履歴

### v1.0.0
- 初期リリース
- 基本的なトラフィック監視機能
- リアルタイムログとレポート生成
- エディタ統合

---

*このドキュメントは PhotonTrafficLogger v1.0.0 に対応しています。*