using System;
using DeltaTune.Media;
using DeltaTune.Settings;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.BitmapFonts;
using R3;
using Color = Microsoft.Xna.Framework.Color;

namespace DeltaTune.Display
{
    public class MusicTitleDisplay : IMusicTitleDisplay, IDisposable
    {
        public MediaInfo Content
        {
            get => content;
            set
            {
                content = value;
                UpdateText();
            }
        }

        public MusicTitleDisplayState State
        {
            get => state;
            set
            {
                state = value;
                animationTimer = 0;
            }
        }

        private const float AppearDelayLength = 0.5f;
        private const float AppearAnimationLength = 0.75f;
        private const float DisappearAnimationLength = 0.75f;
        private const float StayTime = 2.5f;
        private const float SlideInDistance = 24;
        private const float SlideOutDistance = 24;

        private readonly BitmapFont font;
        private readonly ISettingsService settingsService;
        private readonly IMediaFormatter formatter;
        private readonly Func<Vector2> windowSizeProvider;
        private readonly Observable<Vector2> windowSize;

        private MediaInfo content;
        private MusicTitleDisplayState state = MusicTitleDisplayState.Hidden;
        private Vector2 position = Vector2.Zero;
        private string text = string.Empty;
        private SizeF textSize;
        private Vector2 positionOffset;
        private float opacity;
        private double animationTimer;
        
        private readonly IDisposable windowSizeSubscription;
        private readonly IDisposable scaleFactorSubscription;
        private readonly IDisposable positionSubscription;
        
        public MusicTitleDisplay(BitmapFont font, Func<Vector2> windowSizeProvider, ISettingsService settingsService,
            IMediaFormatter formatter)
        {
            this.font = font;
            this.settingsService = settingsService;
            this.windowSizeProvider = windowSizeProvider;
            this.formatter = formatter;

            windowSize = Observable.EveryValueChanged(this, display => display.windowSizeProvider.Invoke());
            
            windowSizeSubscription = windowSize.Subscribe(_ => UpdatePosition());
            scaleFactorSubscription = settingsService.ScaleFactor.Subscribe(_ => UpdatePosition());
            positionSubscription = settingsService.Position.Subscribe(_ => UpdatePosition());
        }

        public void Update(GameTime gameTime)
        {
            float progress;
            switch (State)
            {
                case MusicTitleDisplayState.AppearingDelay:
                    if (animationTimer == 0)
                    {
                        opacity = 0;
                        positionOffset.X = 0;
                    }
                    
                    if(animationTimer >= AppearDelayLength) State = MusicTitleDisplayState.Appearing;
                    break;
                case MusicTitleDisplayState.Appearing:
                    if (animationTimer == 0)
                    {
                        opacity = 0;
                        positionOffset.X = 0;
                    }

                    progress = (float)(animationTimer / AppearAnimationLength);
                    
                    opacity = MathHelper.Clamp(progress * 1.5f - 0.25f, 0, 1);
                    positionOffset.X = InterpolateQuadratic(SlideInDistance * settingsService.ScaleFactor.Value, 0, progress);

                    if (animationTimer >= AppearAnimationLength)
                    {
                        State = MusicTitleDisplayState.Visible;
                    }
                    break;
                
                case MusicTitleDisplayState.Visible:
                    if (animationTimer == 0)
                    {
                        opacity = 1;
                        positionOffset.X = 0;
                    }

                    if (settingsService.HideAutomatically.Value != null && animationTimer >= settingsService.HideAutomatically.Value.Value)
                    {
                        State = MusicTitleDisplayState.Disappearing;
                    }

                    if (settingsService.HideAutomatically.Value == null && !settingsService.ShowPlaybackStatus.Value && animationTimer >= StayTime &&
                        (content.Status == PlaybackStatus.Stopped || content.Status == PlaybackStatus.Paused))
                    {
                        State = MusicTitleDisplayState.Disappearing;
                    }
                    break;
                
                case MusicTitleDisplayState.Disappearing:
                    if (animationTimer == 0)
                    {
                        opacity = 1;
                        positionOffset.X = 0;
                    }
                    
                    progress = (float)(animationTimer / DisappearAnimationLength);
                    opacity = MathHelper.Clamp((1 - progress) * 1.5f - 0.25f, 0, 1);
                    positionOffset.X = InterpolateQuadratic(-SlideOutDistance * settingsService.ScaleFactor.Value, 0, 1 - progress);
                    
                    if (animationTimer >= DisappearAnimationLength)
                    {
                        State = MusicTitleDisplayState.Hidden;
                    }
                    break;
            }
            
            if (State != MusicTitleDisplayState.Hidden)
            {
                animationTimer += gameTime.ElapsedGameTime.TotalSeconds;
            }
        }

        public void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            if (State != MusicTitleDisplayState.Hidden && State != MusicTitleDisplayState.AppearingDelay)
            {
                Vector2 finalPosition = position + positionOffset;
                
                if (settingsService.Position.Value.X > 0.5f)
                {
                    finalPosition.X -= textSize.Width * settingsService.ScaleFactor.Value;
                }
                
                Color finalColor = Color.White;
                finalColor.A = (byte)MathHelper.Clamp((int)Math.Round(opacity * 255), 0, 255);
                
                spriteBatch.DrawString(font, text, finalPosition, finalColor, 0, Vector2.Zero, settingsService.ScaleFactor.Value, SpriteEffects.None, 0);
            }
        }

        private void UpdateText()
        {
            text = formatter.Format(content);
            
            if (State == MusicTitleDisplayState.Disappearing || State == MusicTitleDisplayState.Hidden) return;
            
            textSize = font.MeasureString(text);
        }

        private void UpdatePosition()
        {
            position.X = settingsService.Position.Value.X * windowSizeProvider().X;
        }
        
        private static float InterpolateQuadratic(float a, float b, float t)
        {
            float oneMinusT = 1 - t;
            float progress = 1 - oneMinusT * oneMinusT;

            return MathHelper.Lerp(a, b, progress);
        }

        public void Dispose()
        {
            windowSizeSubscription?.Dispose();
            scaleFactorSubscription?.Dispose();
            positionSubscription?.Dispose();
        }
    }
}