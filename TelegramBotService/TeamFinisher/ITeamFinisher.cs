using Telegram.Bot;

namespace TelegramBotService.TeamFinisher;

public interface ITeamFinisher
{
    Task FinishTeamAsync(ITelegramBotClient client, Guid teamId, CancellationToken cancellationToken);
}