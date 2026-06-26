using System;
using DeltaTune.Media;
using DeltaTune.Settings;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.BitmapFonts;

namespace DeltaTune.Display
{
    public class DisplayService : IDisplayService
    {
        private const double LastMediaInfoUpdateStopCheckDelay = 1;
        
        private readonly IMediaInfoService mediaInfoService;
        private readonly ISettingsService settingsService;
        private readonly IMediaFormatter mediaFormatter;
        private readonly Func<Vector2> windowSizeProvider;
        private readonly BitmapFont musicTitleFont;
        
        private MediaInfo currentMediaInfo;
        private IMusicTitleDisplay primaryDisplay;
        private IMusicTitleDisplay secondaryDisplay;

        private double lastMediaInfoUpdateTime;
        
        public DisplayService(IMediaInfoService mediaInfoService, ISettingsService settingsService, IMediaFormatter mediaFormatter, BitmapFont musicTitleFont, Func<Vector2> windowSizeProvider)
        {
            this.mediaInfoService = mediaInfoService;
            this.settingsService = settingsService;
            this.musicTitleFont = musicTitleFont;
            this.windowSizeProvider = windowSizeProvider;
            this.mediaFormatter = mediaFormatter;
        }

        public void BeginRun()
        {
            primaryDisplay = new MusicTitleDisplay(musicTitleFont, windowSizeProvider, settingsService, mediaFormatter);
            secondaryDisplay = new MusicTitleDisplay(musicTitleFont, windowSizeProvider, settingsService, mediaFormatter);
        }

        public void Update(GameTime gameTime)
        {
            bool titleChanged = false, artistChanged = false, statusChanged = false;
            while (mediaInfoService.UpdateQueue.TryDequeue(out MediaInfo mediaInfo))
            {
                titleChanged |= mediaInfo.Title != currentMediaInfo.Title;
                artistChanged |= mediaInfo.Artist != currentMediaInfo.Artist;
                statusChanged |= mediaInfo.Status != currentMediaInfo.Status;
                
                currentMediaInfo = mediaInfo;
                lastMediaInfoUpdateTime = gameTime.TotalGameTime.TotalSeconds;
            }

            bool shouldUpdateDisplayState = settingsService.ShowPlaybackStatus.Value ? titleChanged || artistChanged || statusChanged : titleChanged || artistChanged;

            // Even if playback status shouldn't be shown, show the song title again when resuming playback
            if (!shouldUpdateDisplayState && !settingsService.ShowPlaybackStatus.Value && statusChanged &&
                currentMediaInfo.Status == PlaybackStatus.Playing)
            {
                shouldUpdateDisplayState = true;
            }
            
            // If it's been too long since the last media info update, check if anything is still playing
            // If not, make the display disappear
            if (lastMediaInfoUpdateTime + LastMediaInfoUpdateStopCheckDelay < gameTime.TotalGameTime.TotalSeconds)
            {
                if(mediaInfoService.IsCurrentlyStopped())
                {
                    currentMediaInfo.Status = PlaybackStatus.Stopped;
                    
                    if (primaryDisplay.State != MusicTitleDisplayState.Disappearing &&
                        primaryDisplay.State != MusicTitleDisplayState.Hidden)
                    {
                        primaryDisplay.State = MusicTitleDisplayState.Disappearing;
                    }
                }
                
                lastMediaInfoUpdateTime = gameTime.TotalGameTime.TotalSeconds;
            }
            
            // If the display is hidden and doesn't show playback status,
            // only start showing it once the media state changes to playing
            if (!settingsService.ShowPlaybackStatus.Value && 
                shouldUpdateDisplayState && 
                currentMediaInfo.Status != PlaybackStatus.Playing && 
                primaryDisplay.State == MusicTitleDisplayState.Hidden)
            {
                shouldUpdateDisplayState = false;
            }
            
            if (shouldUpdateDisplayState)
            {
                switch (primaryDisplay.State)
                {
                    case MusicTitleDisplayState.Hidden:
                        SwapAndShowPrimaryDisplay();
                        break;
                    case MusicTitleDisplayState.AppearingDelay:
                        break;
                    case MusicTitleDisplayState.Appearing:
                        break;
                    case MusicTitleDisplayState.Visible:
                        // Make the slide animation appear even if the display doesn't disappear automatically
                        if (titleChanged || artistChanged)
                        {
                            SwapAndShowPrimaryDisplay();
                        }
                        else
                        {
                            primaryDisplay.State = MusicTitleDisplayState.Visible;
                        }

                        break;
                    case MusicTitleDisplayState.Disappearing:
                        SwapAndShowPrimaryDisplay();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            
            primaryDisplay.Update(gameTime);
            secondaryDisplay.Update(gameTime);

            if (primaryDisplay.State != MusicTitleDisplayState.Disappearing && primaryDisplay.State != MusicTitleDisplayState.Hidden)
            {
                primaryDisplay.Content = currentMediaInfo;
            }
        }
        
        public void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            primaryDisplay.Draw(spriteBatch, gameTime);
            secondaryDisplay.Draw(spriteBatch, gameTime);
        }
        
        private void SwapAndShowPrimaryDisplay()
        {
            (primaryDisplay, secondaryDisplay) = (secondaryDisplay, primaryDisplay);

            if (secondaryDisplay.State == MusicTitleDisplayState.Hidden)
            {
                primaryDisplay.State = MusicTitleDisplayState.Appearing;
            }
            else
            {
                if (secondaryDisplay.State != MusicTitleDisplayState.Disappearing)
                {
                    secondaryDisplay.State = MusicTitleDisplayState.Disappearing;
                }
                
                primaryDisplay.State = MusicTitleDisplayState.AppearingDelay;
            }
        }
    }
}