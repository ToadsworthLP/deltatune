using Microsoft.Xna.Framework;

namespace DeltaTune.Settings
{
    public class SettingsFileModel
    {
        public int ScaleFactor { get; set; } = 3;
        public Vector2 Position { get; set; } = PositionPresetHelper.GetFractionalPosition(PositionPreset.TopRight);
        public string ScreenName { get; set; } = string.Empty;
        public bool ShowArtistName { get; set; } =  true;
        public bool ShowPlaybackStatus { get; set; } = false;
        public float? HideAutomatically { get; set; } = 2.5f;
        public bool ScreenCaptureCompatibilityMode { get; set; } = false;
        public bool EnableDiscordRichPresence { get; set; } = true;

        public void FromSettings(ISettingsService settingsService)
        {
            ScaleFactor = settingsService.ScaleFactor.Value;
            Position = settingsService.Position.Value;
            ScreenName = settingsService.ScreenName.Value;
            ShowArtistName = settingsService.ShowArtistName.Value;
            ShowPlaybackStatus = settingsService.ShowPlaybackStatus.Value;
            HideAutomatically = settingsService.HideAutomatically.Value;
            ScreenCaptureCompatibilityMode = settingsService.ScreenCaptureCompatibilityMode.Value;
            EnableDiscordRichPresence = settingsService.EnableDiscordRichPresence.Value;
        }

        public void ToSettings(ISettingsService settingsService)
        {
            settingsService.ScaleFactor.Value = ScaleFactor;
            settingsService.Position.Value = Position;
            settingsService.ScreenName.Value = ScreenName;
            settingsService.ShowArtistName.Value = ShowArtistName;
            settingsService.ShowPlaybackStatus.Value = ShowPlaybackStatus;
            settingsService.HideAutomatically.Value = HideAutomatically;
            settingsService.ScreenCaptureCompatibilityMode.Value = ScreenCaptureCompatibilityMode;
            settingsService.EnableDiscordRichPresence.Value = EnableDiscordRichPresence;
        }
    }
}