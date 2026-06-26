using Microsoft.Xna.Framework;
using R3;

namespace DeltaTune.Settings
{
    public class SettingsService : ISettingsService
    {
        public bool IsFactorySettings { get; set; } = true;
        public ReactiveProperty<int> ScaleFactor { get; } = new ReactiveProperty<int>(3);
        public ReactiveProperty<Vector2> Position { get; } = new ReactiveProperty<Vector2>(PositionPresetHelper.GetFractionalPosition(PositionPreset.TopRight));
        public ReactiveProperty<string> ScreenName { get; } = new ReactiveProperty<string>(string.Empty);
        public ReactiveProperty<bool> ShowArtistName { get; } =  new ReactiveProperty<bool>(true);
        public ReactiveProperty<bool> ShowPlaybackStatus { get; } = new ReactiveProperty<bool>(false);
        public ReactiveProperty<float?> HideAutomatically { get; } = new ReactiveProperty<float?>(2.5f);
        public ReactiveProperty<bool> ScreenCaptureCompatibilityMode { get; } = new ReactiveProperty<bool>(false);
        public ReactiveProperty<bool> EnableDiscordRichPresence { get; } = new ReactiveProperty<bool>(true);
    }
}