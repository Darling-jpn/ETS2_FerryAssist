# ETS2 Ferry Assist 導入・使用説明書

## はじめに

**ETS2 Ferry Assist** は、Euro Truck Simulator 2 の **Project Japan MOD** で遊ぶ際に、目的地までのフェリー乗船ルートを音声で教えてくれる便利なツールです。

> 「どこ行きのフェリーに乗ればいいの？」と聞くだけで、最適なフェリールートを教えてくれます。

---

## 動作環境

- Windows 11  
- .NET 8 ランタイム  
- VOICEVOX（音声合成エンジン）  
- Vosk（音声認識エンジン）のモデルファイル  

---

## インストール手順

### 1. 必要なソフトウェアのダウンロードとインストール

#### 1-1. .NET 8 ランタイムのインストール

1. 以下のURLから .NET 8 ランタイムをダウンロード：  
   [https://dotnet.microsoft.com/download/dotnet/8.0](https://dotnet.microsoft.com/download/dotnet/8.0)
2. 「Run desktop apps」の「Download x64」をクリック。
3. ダウンロードした `windowsdesktop-runtime-8.x.x-win-x64.exe` を実行し、インストール。


#### 1-2. テレメトリーデータ取得用の plugin ダウンロードと設置

1. 以下の URL にアクセスします：
   [https://github.com/RenCloud/scs-sdk-plugin/releases/tag/V.1.12.1](https://github.com/RenCloud/scs-sdk-plugin/releases/tag/V.1.12.1)
2. Assets にある release_v_1_12_1.zip(下図の赤枠部分)をダウンロードします：
3. ダウンロードが終わったら release_v_1_12_1.zip を展開し、Win64 フォルダ内の「scs-telemetry.dll」を
ETS2 インストールフォルダ(例: Euro Truck Simulator 2\bin\win_x64\plugins)にコピーまたは移
動します：
※plugins フォルダがない場合は新規に「plugins」という名前でフォルダを作成し、その中に入れてください。


#### 1-3. VOICEVOX のインストール

1. VOICEVOX 公式サイト：[https://voicevox.hiroshiba.jp/](https://voicevox.hiroshiba.jp/)
2. 「ダウンロード」ボタンから最新版を取得。
3. インストーラーを実行してインストール。
4. インストール完了後、**一度起動して正常動作を確認**し、**インストール先フォルダをメモ**しておく。  
   - 通常は `C:\Program Files\VOICEVOX` です。
5. VOICEVOX を終了する。

---

### 2. ETS2 Ferry Assist のインストール

1. `ETS2_FerryAssist.zip` を展開。
2. 任意の場所（デスクトップなど）に配置。
3. 展開後の構成例：

    ```
    ETS2_FerryAssist/
    ├── ETS2_FerryAssist.exe       （メイン実行ファイル）
    ├── Resources/
    │   └── ferry_routes.db        （フェリーデータベース）
    └── models/
        └── これは削除してください.txt
    ```

---

### 3. Vosk モデルの導入

1. 日本語モデルのダウンロード：[https://alphacephei.com/vosk/models](https://alphacephei.com/vosk/models)
2. 「Japanese model for Android/iOS」→ `vosk-model-small-ja-0.22.zip` をダウンロード。
3. ZIP を展開。
4. 展開されたフォルダの中身を `models` フォルダへコピー。
5. `models` フォルダ内の「これは削除してください.txt」は削除。

    正しい構成例：

    ```
    models/
    ├── vosk-model-small-ja-0.22am/
           ├── conf/
           ├── graph/
           └── ...
    ```

---

## 初回設定と使用方法

### 1. アプリケーションの起動と初期設定

1. `ETS2_FerryAssist.exe` をダブルクリックして起動。
2. VOICEVOX がバックグラウンド起動後、設定画面が表示される。

#### 設定内容：

- **デバッグを有効にする**（通常はチェック不要）
- **ホットキー設定**：任意のキー（例：F10）を設定
- **VOICEVOX 実行ファイルパス**を選択（通常は `C:\Program Files\VOICEVOX\VOICEVOX.exe`）
- **話者（Speaker）選択**：VOICEVOX で使用可能な声を選択

3. 設定完了後、「OK」ボタンをクリック。  
   - 設定は保存され、次回以降の再設定は不要。
4. Vosk による音声認識の初期化が行われる（数秒～数十秒）。
5. 「フェリー乗船サポートツールを起動しました。」という音声が流れる。

---

### 2. 使用方法

1. ETS2 Project Japan MAP で、フェリールートを確認したくなったら：

    - 設定した **ホットキー（例：F10）** を押す  
    - 「どうぞ」という音声が流れたら  
    - 「どこ行きのフェリー？」「どのフェリー？」などのフレーズで質問

2. アプリが音声で回答：

    - フェリーが必要な場合：  
      `新門司港から大阪南港経由で金沢港行きのフェリーに乗船してください`
    - フェリー不要な場合：  
      `この区間はフェリーは使用しません`

3. 常駐動作について：

    - **設定変更**：システムトレイのアイコンを右クリック → 設定  
    - **終了方法**：
        - ホットキー後に「終わり」「終了」と話す
        - トレイアイコン右クリック → 終了
        - コンソールウィンドウを閉じる

---

## トラブルシューティング

### 音声認識がうまく動作しない場合

- マイク接続と Windows の設定確認
- 「どうぞ」の音声後に話し始める
- 「どこ」や「フェリー」を含む発話にする
- 静かな環境でクリアに発声

### VOICEVOX が起動しない場合

- 実行ファイルパスの確認
- VOICEVOX 単体での起動確認
- アプリの再起動

### アプリがクラッシュする場合

- .NET 8 ランタイムの確認
- Vosk モデルの配置確認
- 「デバッグを有効にする」にチェックを入れて再起動 → コンソールでエラーメッセージを確認

---

## FAQ

**Q: 前回設定した VOICEVOX のパスやホットキー、話者は保存されますか？**  
A: はい、保存されます。

**Q: ETS2 の他の MAP でも使えますか？**  
A: いいえ、Project Japan MAP 専用です。

**Q: 日本語以外の言語でも使えますか？**  
A: 現在は日本語のみ対応です。

**Q: ゲームの起動前と後、どちらでこのアプリを起動すべきですか？**  
A: どちらでも問題ありませんが、ゲーム起動前の方が便利です。

**Q: 音声合成エンジンは変更できますか？**  
A: 現在は VOICEVOX のみ対応しています。

---

## お問い合わせ

問題が解決しない場合やご質問がある場合は、**私（ダーさん）**までお気軽にお問い合わせください。

## Special Thanks To
kuramochiaさん
(※kuramochia委員長のサポートがなければこのツールは日の目を見ることはなかったと思います)
