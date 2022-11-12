# PayForMeBot

Пример `appsettings.json`

```
{
  "RECEIPT_API_URL": "https://proverkacheka.com/api/v1/check/get",
  "RECEIPT_API_TOKEN": "<TOKEN>",
  "TELEGRAM_BOT_TOKEN": "<TOKEN>",
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Information",
        "System": "Warning"
      }
    }
  }
}
```
