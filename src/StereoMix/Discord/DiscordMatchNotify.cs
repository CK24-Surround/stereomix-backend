using Discord;
using Discord.Webhook;

namespace StereoMix.Discord;

public class DiscordMatchNotify : IDisposable
{
    private const string WebhookUrlEnvironment = "DISCORD_WEBHOOK_URL";
    private readonly DiscordWebhookClient? _client;

    private readonly ILogger<DiscordMatchNotify> _logger;

    public DiscordMatchNotify(ILogger<DiscordMatchNotify> logger, IConfiguration configuration)
    {
        _logger = logger;

        var webhookUrl = configuration[WebhookUrlEnvironment];
        _client = webhookUrl is not null ? new DiscordWebhookClient(webhookUrl) : null;
    }

    public void Dispose()
    {
        _client?.Dispose();
        GC.SuppressFinalize(this);
    }

    public async Task NotifyRoomCreated(string userName, string gameVersion, string roomCode)
    {
        if (_client is null)
        {
            _logger.LogWarning("Discord Webhook URL is not set. Skipping Discord notification.");
            return;
        }

        var embed = new EmbedBuilder()
            .WithTitle($"'{userName}'님이 새로운 방을 생성했습니다.")
            .WithCurrentTimestamp()
            .WithColor(Color.Blue)
            .AddField("게임 버전", gameVersion)
            .AddField("방 코드", roomCode);

        var messageId = await _client.SendMessageAsync(embeds: new[] { embed.Build() }).ConfigureAwait(false);
        _logger.LogInformation("Discord notification sent. (NotifyRoomCreated) Message ID: {MessageId}", messageId);
    }

    public async Task NotifyRoomEntered(string userName, string gameVersion, string roomName, string roomCode)
    {
        if (_client is null)
        {
            _logger.LogWarning("Discord Webhook URL is not set. Skipping Discord notification.");
            return;
        }

        var embed = new EmbedBuilder()
            .WithTitle($"'{userName}'님이 방에 입장했습니다.")
            .WithCurrentTimestamp()
            .WithColor(Color.Orange)
            .AddField("게임 버전", gameVersion)
            .AddField("방 코드", roomCode)
            .AddField("방 이름", roomName);

        var messageId = await _client.SendMessageAsync(embeds: new[] { embed.Build() }).ConfigureAwait(false);
        _logger.LogInformation("Discord notification sent. (NotifyRoomEntered) Message ID: {MessageId}", messageId);
    }

    public async ValueTask NotifyRoomStateUpdated(string gameVersion, string roomName, string roomCode, string roomState)
    {
        if (_client is null)
        {
            _logger.LogWarning("Discord Webhook URL is not set. Skipping Discord notification.");
            return;
        }

        var embed = new EmbedBuilder()
            .WithTitle($"방 정보가 '{roomState}'로 업데이트되었습니다.")
            .WithCurrentTimestamp()
            .WithColor(Color.Green)
            .AddField("게임 버전", gameVersion)
            .AddField("방 코드", roomCode)
            .AddField("방 이름", roomName);

        var messageId = await _client.SendMessageAsync(embeds: new[] { embed.Build() }).ConfigureAwait(false);
        _logger.LogInformation("Discord notification sent. (NotifyRoomStateUpdated) Message ID: {MessageId}", messageId);
    }
}
