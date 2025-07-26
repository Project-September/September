# Unity/Photon通信量ロガー仕様書

## 機能概要

- 目的: FusionStatsを利用し、各NetworkObjectの通信量をチェックできるように情報を取得できるようにする
- 対象ユーザー: エンジニア
- 機能形式: Unityエディタ拡張、Unity内C#class

- バージョン情報:
  - Unity:6000.0.41f1
  - Photon: Fusion2

## 技術制約

- パフォーマンス要件: 特になし
- メモリ使用量制限: 特になし

## 設定項目

- Unityエディター内コンソールへの出力 on/off
- ログ出力先pathの指定 (デフォルト出力先: `Directory.GetCurrentDirectory()/log`)
- ログ保存数上限の指定 (ファイル数制限)

## 処理フロー

1. エディター上でロガー設定を行う
2. Unityエディターを実行
3. ログ保存数上限に応じた、データの削除
4. 設定に応じてログ出力
5. 設定更新がなければ 2へ戻る

## 行ってほしいタスク

- ロガー設定画面エディター拡張の作成
- FusionStatsとFusionStatsに関する関数を用いた通信量の取得機能の作成

## タスク詳細

### ロガー設定画面エディター拡張の作成

- 上記設定項目をPlayerPrefsなどで保存しておく

### Photonの関数を用いた通信量の取得機能の作成

- 上記設定項目を反映したロガーclassの作成
- Unity再生時にNetworkObjectを`FindObjectsOfType<NetworkObject>()`で検索し、FusionStatsコンポーネントを付与する
- ログメソッドはCallerMemberName属性を利用し、呼び出し元が分かるようにする
- 基本的にはエディター上のみで動作するが、DevelopmentBuild時のみ、インゲーム画面上で`IMGUI形式`でトラフィックをリアルタイムで確認できるようにする

## 設計

SOLID原則に沿った設計を最大限遵守してください

## ログ出力

### ログ出力仕様

FusionStatsデフォルトであるNetworkRunnerのティックレート (60Hz) でログ出力

### コンソール出力例

``` bash
[traffic-incoming] 呼び出し元: ClassName.MethodName \n 総メッセージ数: totalMessageCount \n 総パケットバイト数: totalPacketBytes \n 最大パケットサイズ: longestMessageBytes
[traffic-outgoing] 呼び出し元: ClassName.MethodName \n 総メッセージ数: totalMessageCount \n 総パケットバイト数: totalPacketBytes \n 最大パケットサイズ: longestMessageBytes
```

### json出力例

``` json
{
    "traffic-incoming":
    {
        "callerName": "ClassName.MethodName",
        "totalMessageCount": count,
        "totalPacketBytes": bytes,
        "longestMessageBytes": bytes
    },
    "traffic-outgoing":
    {
        "callerName": "ClassName.MethodName",
        "totalMessageCount": count,
        "totalPacketBytes": bytes,
        "longestMessageBytes": bytes
    }
}
```

## ログファイル仕様

- データ集計期間: リアルタイム値を保存するファイルと累積値、平均値をまとめたレポートファイルを作成
- ファイル形式: .json
- 命名規則: ログ形式名(realtime or report)-timestamp (例: realtime-20250714165130)
- NetworkObjectごとの個別ログ: 各ログにどのNetworkObjectから実行されたかを含み、レポートファイルに全体統計を含むようにする

## エラーハンドリング

例外をthrowする

## テスト要件

コンパイルエラーのチェックのみ

## リファレンス

[Photon関数リファレンス](https://doc-api.photonengine.com/en/dotnet/current/class_photon_1_1_realtime_1_1_load_balancing_peer.html)

[FusionStatsリファレンス](https://doc.photonengine.com/fusion/v1/manual/fusionstats)

[FusionStats調査レポート](https://claude.ai/public/artifacts/2eb0aa3e-e96c-4075-9b1d-a2e9157e1333)
