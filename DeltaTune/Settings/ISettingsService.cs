using Microsoft.Xna.Framework;
using R3;

namespace DeltaTune.Settings
{
    public interface ISettingsService
    {
        bool IsFactorySettings { get; set; }
        ReactiveProperty<int> ScaleFactor { get; }
        ReactiveProperty<Vector2> Position { get; }
        ReactiveProperty<string> ScreenName { get; }
        ReactiveProperty<bool> ShowArtistName { get; }
        ReactiveProperty<bool> ShowPlaybackStatus { get; }
        ReactiveProperty<float?> HideAutomatically { get; }
        ReactiveProperty<bool> ScreenCaptureCompatibilityMode { get; }
        ReactiveProperty<bool> EnableDiscordRichPresence { get; }
    }
}