# RustyBank
RustyBankは、BaburyServerのサーバプラグインです。

## 前提プラグイン
[Economics](https://umod.org/plugins/economics)

[HumanNPC](https://umod.org/plugins/human-npc)(オプション)

## コマンド
|コマンド|説明|
|----|----|
|/bankui|銀行UIを表示する(コンフィグの設定に依存)|
|/banknpc add|NPCに対して動作ボタン(デフォルト'E')をクリックすることで、銀行UIを表示できる権限を付与する(HumanNPCのみ対応)|
|/banknpc remove|銀行UIを表示できるNPCに対して、表示権限を剥奪する(HumanNPCのみ対応)|

## 権限

- rustybank.admin
- rustybank.default
- rustybank.vip1
- rustybank.vip2
- rustybank.vip3

## コンフィグ
~~~
{
  "00_Global": {
    "Distance_From_Npc": 5.0,
    "Extension_Amount": 10000.0,
    "Extension_Enable": false,
    "Extension_Fee": 100000.0,
    "Extension_Limit": 500000.0,
    "Show_Version": true,
    "TransactExpirationTime": 5,
    "Use_Only_UI": false
  },
  "10_Admin": {
    "Fee": 200,
    "Max_Deposit_Balance": 300000,
    "Use_Fee": true
  },
  "11_Default": {
    "Fee": 300,
    "Max_Deposit_Balance": 150000,
    "Use_Fee": true
  },
  "12_Vip1": {
    "Fee": 250,
    "Max_Deposit_Balance": 200000,
    "Use_Fee": true
  },
  "13_Vip2": {
    "Fee": 230,
    "Max_Deposit_Balance": 250000,
    "Use_Fee": true
  },
  "14_Vip3": {
    "Fee": 200,
    "Max_Deposit_Balance": 300000,
    "Use_Fee": true
  }
}
~~~

## 利用条件
[MITライセンス](https://github.com/babusan77/RustyBank/blob/main/LICENSE)での公開です。ソースコードの使用規約等はMITライセンスに準拠します。
