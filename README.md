obtdai様版の[VRGIN.Template](https://github.com/obtdai/VRGIN.Templete)を触ってなんとかコイカツ本編をVRで操作できるようにしたいもの

〇操作方法
新たに学校のアイコンのツールが追加されています
このツールの状態での操作（oculusでやってるのでviveでの確認してません）

・トリガー
HMDが向いている方向に移動（カメラも追従）
※ダッシュだと酔いやすい＆操作しにくかったので、歩き（Shift押し）にしてます。
※酔い注意

オマケ：しゃがむとその高さになるので実際にしゃがみながらやると楽しい
（ボタン足りないのでZキーはキーボードからになりますが……）

メモ:視線が上下すると酔いやすいのでしゃがみはあきらめて頭の高さとらずに足元の高さベースで一定の高さにしたほうがいいのかもしれない

・グリップ
カメラの位置にプレイヤーキャラを移動
押しっぱなしで現実で動くと仮想空間でも動き回れる

・スティック（タッチパッド）
上 F3
下 F4
左 F1
右 F8
中央 右クリック
※全て倒すだけでなく押し込む必要あり
※ポインターが浮いてるウィンドウにあたってるときはこの操作はできないので注意

・ぱいタッチなど、3Dモデルをクリックする操作について
VR上で真正面にアップで表示されている状態にしてウィンドウの真ん中らへんをクリックでなんとか
ようはHMDじゃなくて通常のディスプレイのほうでの表示をクリックする

〇その他改善
開始時にStandingModeで始まるように（Ctrl+C, Ctrl+C不要）
Shader適用時（Ctrl+F5）に同時にHDRを許可するように（ピンクや緑の変な色になるの対策）

〇既知の不具合など
移動時障害物にひっかかるとカメラとの対応がおかしくなる
近づいたときにキャラが消える（ゲーム側で、固定キャラ以外はカメラとかさなると消えるようになってる模様）
ホイールクリックが未割当（WindowsInput.InputSimulator.Mouseになかったんだもん）
色合いがちょっとおかしい気がする（ライティングかもしれない）
