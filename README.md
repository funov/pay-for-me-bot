# PayForMeBot

## Авторы

`@s_vanyaa`,
`@allstarswilldie`,
`@yanikiss`

## Описание

Этот бот может посчитать ваши совместные покупки.
Отправляйте ему названия продуктов и их цены, либо просто фотографии чеков,
а в конце мероприятия он отправит каждому сумму и реквизиты
для оплаты. 💸💸💸

Подробности в `@PayForMe_bot` /help

## Компоненты системы

`PaymentLogic` — парсер реквизитов и калькулятор выплат.

`ReceiptApiClient` — обращение к api получения информации о чеках.

`SqliteProvider` — работа с базой данных.

`TelegramBotService` — работа с telegram api (frontend).

## Запуск

* Runtime — **NET 6.0**
* Language — **C# 11.0** (latest)

Для запуска нужно убедиться, что в TelegramBotService есть
`appsettings.json` и `appsettings.Production.json`. Далее в базу данных `PayForMeBot.db`
в таблицу `BotPhrases` нужно написать фразы, которые будет использовать бот.

Запуск из `TelegramBotService/EntryPoint.cs`

## Конфигурация

В `appsettings.json` настраиваются логи, а в `appsettings.Production.json`
хранятся секреты проекта.

Пример `appsettings.Production.json` в TelegramBotService

```
{
  "RECEIPT_API_URL": <URL>,
  "RECEIPT_API_TOKEN": <TOKEN>,
  "TELEGRAM_BOT_TOKEN": <TOKEN>,
  "DbConnectionString": <ConnectionString>
}
```
