using DeltaTune.Media;

namespace DeltaTune.Discord
{
	public interface IDiscordService
	{
		void UpdateState();
		void UpdateDisplay(MediaInfo mediaInfo);
	}
}