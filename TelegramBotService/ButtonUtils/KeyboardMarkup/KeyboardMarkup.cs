using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramBotService.ButtonUtils.KeyboardMarkup;

public class KeyboardMarkup : IKeyboardMarkup
{
    public InlineKeyboardMarkup GetInlineKeyboardMarkup(Guid guid, string priceText, string countText,
        string buyButtonText)
    {
        var buttons = new List<List<InlineKeyboardButton>>
        {
            new()
            {
                InlineKeyboardButton.WithCallbackData(priceText),
                InlineKeyboardButton.WithCallbackData(countText),
                InlineKeyboardButton.WithCallbackData(buyButtonText, guid.ToString()),
            }
        };

        return new InlineKeyboardMarkup(buttons);
    }

    public ReplyKeyboardMarkup GetReplyKeyboardMarkup(IEnumerable<string> buttonLabels)
    {
        var replyKeyboardMarkup = new ReplyKeyboardMarkup(new[]
        {
            buttonLabels.Select(buttonLabel => new KeyboardButton(buttonLabel)),
        })
        {
            ResizeKeyboard = true,
        };

        return replyKeyboardMarkup;
    }
}