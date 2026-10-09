# FlagsRally

訪れた国・地域やカスタムボード（スタンプラリー）の地点を「パスポートとスタンプ」の見た目で集める .NET MAUI アプリ。
ユーザーとのやりとりは日本語。コード・コメント・コミットメッセージ・PR は英語。

## 構成

- .NET 10 MAUI（`net10.0-android` が主。iOS / Mac Catalyst もターゲットだが実機確認は Android エミュレーターのみ）
- MVVM: CommunityToolkit.Mvvm（`[ObservableProperty]` / `[RelayCommand]` / `WeakReferenceMessenger`）
- 地図: Onion.Maui.GoogleMaps、地域の塗り分け: Syncfusion SfMaps
- 永続化: sqlite-net（`Repository/*`）、設定は `SettingsPreferences`（`IPreferences` ラッパー）
- Shell の TabBar: Passport（`MainPage`）/ Map（`LocationPage`）/ Collections / Settings。
  サブページは `AppShell.xaml.cs` で `Routing.RegisterRoute`、DI は `MauiProgram.cs`

| フォルダ | 中身 |
|---|---|
| `Views/` `ViewModels/` | 画面と VM。ページは `OnAppearing` で `Init()` を呼ぶ |
| `Repository/` | SQLite と Preferences。`Constants.cs` は秘密情報（下記） |
| `Services/` | `CustomBoardService`（ボードの読み込み・検証・保存）、`RegionalFlagsService`（国別の地域一覧）、`DriveBoardCatalog`（公開 Drive フォルダの一覧とダウンロード）、`MapFocusRequest` |
| `Models/` | `PinIcons`（ピンのスタイル）、`CustomBoard/*`（JSON・DB・表示用モデル） |
| `Helpers/` | `CheckInRange`（50 m 判定）、`DistanceFormatter`、`PinOverlapHelper` など |
| `Messages/` | `CustomBoardsChangedMessage(PlacesChanged)`、`PinStyleChangedMessage` |
| `FlagsRally_xunit/` | xUnit テスト（net10.0。Moq） |
| `docs/design/` | デザイン提案とアイコン素材 |

## コマンド

```bash
# テスト（数秒。変更のたびに実行）
dotnet test FlagsRally_xunit/FlagsRally_xunit/FlagsRally_xunit.csproj

# Android ビルド＋エミュレーターへインストール
dotnet build FlagsRally.csproj -f net10.0-android -t:Install

# 起動直後に落ちる・フォントやスプラッシュが消えたときはクリーンビルド
dotnet build FlagsRally.csproj -f net10.0-android -t:Rebuild
```

エミュレーターでの確認手順は `/android-verify` スキル、補助スクリプトは `tools/emu.sh`。

## 守ること

### 秘密情報
- `Repository/Constants.cs` は作業ツリーで本物のキーに書き換えてある。**絶対にステージ・コミットしない**（コミット済みの版はダミー値）。
- `Resources/Sample/*.json.encrypted`（未追跡）もコミットしない。
- `git add` は必ずパスを列挙する。`git add -A` / `git add .` / `git commit -a` は使わない。
  `.claude/hooks/guard_secrets.py` がこれらと、秘密ファイルを含むコミット・キーを含む push を止める。
- 古いローカル stash（2025-12-02 "WIP on master"）に本物のキーが入っている。push しない。

### Git / GitHub
- このマシンでは `git fetch/pull/push` に `-c http.sslBackend=schannel` が必要。
- 変更は 1 つの関心ごとに 1 コミット（追跡できるように）。push・PR 作成・マージはユーザーに頼まれてから。
- コミット末尾に `Co-Authored-By: Claude <noreply@anthropic.com>` 形式の署名、PR 本文末尾に `🤖 Generated with [Claude Code](https://claude.com/claude-code)`。
- PR はベース `master`、マージは `gh pr merge --merge`。CI はない。手順は `/ship-pr` スキル。

### コードの書き方
- 改行コードは既存ファイルに合わせる（多くは CRLF、一部 BOM 付き）。スクリプトで置換するときは CRLF を考慮する。
- 文字列は `Resources/AppResources.resx`（英）・`AppResources.ja.resx`（日）・`AppResources.Designer.cs` の 3 つを揃えて追加する。`tools/add_strings.py` を使う（`/add-strings`）。
- 色は `Resources/Styles/Colors.xaml` のトークン（Navy `#182A52`、Stamp `#B94047`、Paper / PaperCard / PaperEdge、Ink / InkMuted、Gold `#E9C46A`）。ライトテーマ固定。
- ページは `Style="{StaticResource PaperPage}"`（紙の背景）。暗黙の Page スタイルにすると CommunityToolkit の `PopupPage` にも効いてしまうのでキー付きスタイルにしてある。
- 見出しは `controls:StampLabel`（英字は JerseyclubGrunge、日本語は craftmincho に自動切り替え）。
- 画像は `Resources/Images/` に SVG/PNG を置けば `MauiImage` ワイルドカードで取り込まれる（参照は `.png`）。

## ハマりどころ

- **起動直後に落ちる**（logcat: `valid TextAppearance ... Theme.MaterialComponents`）→ コードではなくインクリメンタルビルドの不整合。`-t:Rebuild`。
- **フォントが Roboto になる／スプラッシュが紫一色** → `obj/.../resizetizer` だけ消すと再生成されない。`mauifont.*` `mauisplash.*` `mauiimage.*` の stamp も一緒に消すか Rebuild。
- **VS のデバッグ起動失敗後、毎回起動で固まる（bg anr）** → エミュレーターに `debug.mono.extra` が残っている。`adb shell setprop debug.mono.extra "''"`。
- **Git Bash から adb に `/sdcard` などを渡すとパスが変換される** → `export MSYS_NO_PATHCONV=1`。
- **`Pin` は値で等価比較される**（ラベルと座標）。地図からの削除は参照比較・インデックスで行う（`RemovePinFromMap`）。
- **`MoveCamera`/`AnimateCamera` の Task が返ってこないことがある** → `MoveCameraSafelyAsync`（3 秒タイムアウト）経由で呼ぶ。
- **CollectionView のセル再利用で別カードの画像が出る** → 1 つの Image に URL とローカル画像を両方流さない（`RemoteImageUrl` / `LocalImageFile` のように分ける）。
- **`Xamarin.AndroidX.Lifecycle.LiveData`** は MAUI が固定している版に合わせる。上げると NU1608。
- iOS ではボード並べ替えのドラッグとスクロールの競合を未対処（Android は `RequestDisallowInterceptTouchEvent`）。

## ドメインのメモ

- カスタムボード = JSON（`name`, `url` テンプレート, `width`, `height`, `locations[{code,title,subtitle,group,latitude,longitude}]`）。`.json.encrypted` は `CryptoService` で復号。
  - 他人が作ったボードを取り込むので、`CustomBoardService.IsValid` と 5 MB 上限（`CustomBoardFile.MaxBytes`）を通す。
  - 画像 URL の `{...}` は `code/title/subtitle/group` だけ（エスケープ済み）。訪問日などを外部に出さない。
  - 地点のキーは `"{BoardName}-{Code}"`（既知の衝突リスクあり、未対処）。JSON の並び順は `SortIndex` に保存。
- 「ボードを追加」は公開 Drive フォルダ（`DriveBoardCatalog.RootFolderId`）の embeddedfolderview を API キーなしで読む。フォルダにファイルを置けば一覧に出る。
- チェックイン可能範囲は現在地から 50 m（`CheckInRange.LimitKm`）。
- ピンのスタイルは Classic（既定）と Drop。`PinIcons.FileName` が `classic_*` / 無印の画像を返す。種類ごとの並び（未訪問・訪問済み・チェックイン・記録する場所）は両スタイルで揃える。

## 検証の作法

- 見た目に関わる変更は、エミュレーターでスクリーンショットを撮って自分で確認してから報告する。必要なら動画（`adb shell screenrecord`）を共有する。
- DB を壊しうる確認（全ボード削除、再インポートなど）の前は必ずバックアップし、終わったら戻してハッシュで一致を確認する（`tools/emu.sh db-backup` / `db-restore`）。
- 日本語表示の確認は `tools/emu.sh locale ja-JP`、終わったら `tools/emu.sh locale ""` で戻す。
