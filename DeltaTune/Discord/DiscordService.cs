using System;
using DeltaTune.Media;
using DeltaTune.Settings;
using DiscordRPC;

namespace DeltaTune.Discord
{
	public class DiscordService : IDiscordService, IDisposable
	{
		private static readonly MediaInfo Default = new MediaInfo("", "", PlaybackStatus.Stopped);
		private readonly ISettingsService settingsService;
		private readonly DiscordRpcClient discord;

		private MediaInfo lastMediaInfo = Default;

		public DiscordService(ISettingsService settingsService)
		{
			this.settingsService = settingsService;
			this.discord = new DiscordRpcClient("1446140892850290911");
			this.discord.Initialize();
		}

		public void UpdateState()
		{
			if (!settingsService.EnableDiscordRichPresence.Value)
			{
				discord.ClearPresence();
			}
			else if (!lastMediaInfo.Equals(Default))
			{
				UpdateDisplay(lastMediaInfo);
			}
		}

		public void UpdateDisplay(MediaInfo mediaInfo)
		{
			lastMediaInfo = mediaInfo;
			if (!settingsService.EnableDiscordRichPresence.Value)
			{
				return;
			}
			discord.SetPresence(new RichPresence { State = mediaInfo.Artist, Details = mediaInfo.Title, Timestamps = Timestamps.Now, Assets = new Assets { LargeImageKey = "deltatune" }, Type = ActivityType.Listening, StatusDisplay = StatusDisplayType.Name });
		}

		public void Dispose()
		{
			discord.ClearPresence();
			discord.Dispose();
		}
	}
}