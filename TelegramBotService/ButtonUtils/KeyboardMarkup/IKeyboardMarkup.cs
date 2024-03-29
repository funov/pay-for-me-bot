using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramBotService.ButtonUtils.KeyboardMarkup;

public interface IKeyboardMarkup
{
    InlineKeyboardMarkup GetInlineKeyboardMarkup(Guid guid, string priceText, string countText, string buyButtonText);

    ReplyKeyboardMarkup GetReplyKeyboardMarkup(IEnumerable<string> buttonLabels);
}