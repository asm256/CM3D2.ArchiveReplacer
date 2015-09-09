# CM3D2.ArchiveReplacer

## 目的
非公式MODを再梱包も、展開もせずに使うためのツール

## 必要なもの
- ReiPatcher
- .NET Framework

## インストール方法
CM3D2_KAIZOU\\CM3D2x64_Data\\ManagedフォルダにCM3D2.ArchiveReplacer.Hook.dllを置く  
CM3D2_KAIZOU\\ReiPatcher\\PatchesフォルダにCM3D2.ArchiveReplacer.Patcher.dllを置く  

ReiPatcherからパッチを当てる

CM3D2_KAIZOU\\\_Dataフォルダに入れたいMODを展開する

\_Data\\なんとか衣装MOD\\GameData\\menu\\menu\\dress...  
みたいに深いとこに置いてもおｋ

(細かい話をすると、CM3D2は大抵の場合ディレクトリ構造を認識していません)

## 更新履歴
- 2015.9.5 初版
- 2015.9.5.1 サブディレクトリ対応
- 2015.9.6 バグ修正
  - \_Data内に同名ファイルが存在すると起動に失敗するバグの修正
  - ついでに色々情報埋め込んでみた
- 2015.9.9 簡易インストール対応
  - なぜインストーラにここまでベストを尽くしたのか自分でもわからん

## 既知の問題点
- **Deflarcとの併用は不可能**
- **script(\*.tjs,\*.ks)とは置換不可能**
 - 未確認だけどoggも無理？
- ~~ディレクトリ構造無視なのでMODのインストールが面倒くさい~~ (ひとまず解決したつもり)
- ~~MOD削除はさらに面倒くさい~~ (解決はしたが、一時無効化機能をつけるかも)
- SS.jpgだの1.pngだのが同名重複の可能性がある
 - 一応readme.txtについては対策してるけど…gitみたいな.ignore実装する？
- ネーミングセンス無さ過ぎて泣ける
 - \_Dataフォルダとかいい名前思いついたら改名したい
 - ArchiveReplacerについては半日考えてこんな名前しか思いつかなかった自分に泣ける

## 小ネタ
注意 x64環境でのみ可能です

1. 新しいフォルダを作る
2. そのフォルダをシフト＋右クリックから「コマンドウィンドウをここで開く」を選択
3. 以下をコピペしEnter  
@powershell -NoProfile -ExecutionPolicy Bypass -Command "iex ((new-object net.webclient).DownloadString('https://gist.githubusercontent.com/asm256/8f5472657c1675bdc77a/raw/netbuild.ps1'))"
4. ReiPatcherへのパスを聞かれるのでPathcher.dllが必要ならば  
例: C:\\KISS\\CM3D2_KAIZOU\\ReiPatcher\\  等と入力(要らないならそのままEnter)
5. フォルダの中にDLLができてるはず
