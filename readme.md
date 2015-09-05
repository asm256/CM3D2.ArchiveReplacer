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
2015.9.5 初版  
2015.9.5.1 サブディレクトリ対応

## 既知の問題点
- **Deflarcとの併用は不可能**
- ~~ディレクトリ構造無視なのでMODのインストールが面倒くさい~~ (ひとまず解決したつもり)
- ~~MOD削除はさらに面倒くさい~~ (解決はしたが、一時無効化機能をつけるかも)
- ネーミングセンス無さ過ぎて泣ける
 - \_Dataフォルダとかいい名前思いついたら改名したい
 - ArchiveReplacerについては半日考えてこんな名前しか思いつかなかった自分に泣ける
