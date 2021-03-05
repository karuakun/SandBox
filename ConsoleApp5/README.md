# これは何？
リンクチェック切れです。
a タグを対象にリンクをたどります。

# 起動方法

`docker-compose.override.yml`などに下記の環境変数を定義し、`docker-compose up`してください。

``` yml:docker-compose.override.yml
version: "3.4"
services:
  app:
    environment: 
      - TARGET=https://karuakun.wordpress.com/ # 検査対象のURL
      - INCLUDE_REQUEST_STRINGS=                           # この文字を含むリクエストのみを検査の対象とします
      - EXCLUDE_REQUEST_STRINGS=                           # この文字を含むリクエストを検査の対象としません
      - INCLUDE_REFERENCED_STRINGS=karuakun.wordpress.com  # この文字列を含むページからのリクエストのみを検査の対象とします。
      - EXCLUDE_REFERENCED_STRINGS=                        # この文字列を含むページからのリクエストのみを検査の対象としません。
```
# 残り

- SPAなどの動的にURLを生成するものには非対応です。