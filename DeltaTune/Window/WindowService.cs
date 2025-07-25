using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using DeltaTune.Settings;
using Microsoft.Xna.Framework;
using R3;
using Point = Microsoft.Xna.Framework.Point;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace DeltaTune.Window
{
    public class WindowService : IWindowService, IDisposable
    {
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        [DllImport("user32.dll")]
        private static extern int SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);
        [DllImport("dwmapi.dll")]
        private static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref int[] pMarInset);
        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        private const int GWL_EXSTYLE = -20;
        private const int LWA_ALPHA = 0x00000002;
        private const int WS_EX_LAYERED = 0x00080000;
        private const int WS_EX_TOPMOST = 0x00000008;
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_APPWINDOW = 0x00040000;
        private static readonly IntPtr HWND_TOPMOST = (IntPtr)(-1);
        private const int SWP_NOMOVE = 0x0002;
        private const int SWP_NOSIZE = 0x0001;
        
        private readonly Game game;
        private readonly GameWindow window;
        private readonly GraphicsDeviceManager graphicsDeviceManager;
        private readonly ISettingsMenu settingsMenu;
        private readonly ISettingsService settingsService;
        private readonly float lineHeight;
        
        private IDisposable scaleFactorSubscription;
        private IDisposable windowSizeSubscription;

        public WindowService(Game game, GraphicsDeviceManager graphicsDeviceManager, ISettingsMenu settingsMenu, ISettingsService settingsService, float lineHeight)
        {
            this.game = game;
            this.window = game.Window;
            this.graphicsDeviceManager = graphicsDeviceManager;
            this.settingsMenu = settingsMenu;
            this.settingsService = settingsService;
            this.lineHeight = lineHeight;
        }

        public void InitializeWindow()
        {
            IntPtr windowHandle = window.Handle;
            
            game.IsMouseVisible = true;
            game.IsFixedTimeStep = true;
            game.TargetElapsedTime = TimeSpan.FromTicks(TimeSpan.TicksPerSecond / ScreenRefreshRateProvider.GetScreenRefreshRate(windowHandle, 60));
            game.InactiveSleepTime = TimeSpan.Zero;
            graphicsDeviceManager.SynchronizeWithVerticalRetrace = true;

            window.IsBorderless = true;
            window.AllowUserResizing = false;
            
            SetWindowLong(windowHandle, GWL_EXSTYLE, WS_EX_TRANSPARENT | WS_EX_LAYERED | WS_EX_TOPMOST | WS_EX_TOOLWINDOW);
            SetLayeredWindowAttributes(windowHandle, 0, 255, LWA_ALPHA);
            
            int[] margins = { -1 };
            DwmExtendFrameIntoClientArea(window.Handle, ref margins);

            SetWindowPos(window.Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
            
            SetWindowScale(settingsService.ScaleFactor.Value);
            UpdateWindowPosition();
            
            CreateTrayIcon(windowHandle);
            
            scaleFactorSubscription = settingsService.ScaleFactor.Subscribe(scale =>
            {
                SetWindowScale(scale);
                UpdateWindowPosition();
            });
            
            windowSizeSubscription = settingsService.Position.Subscribe(_ => UpdateWindowPosition());
        }

        private void UpdateWindowPosition()
        {
            Vector2 fractionalPosition = settingsService.Position.Value;
            Rectangle currentScreenBounds = GetCurrentScreenBounds();
            Point windowPosition = new Point(
                0,
                (int)(currentScreenBounds.Height * fractionalPosition.Y)
            );

            if (fractionalPosition.Y > 0.5f)
            {
                windowPosition.Y -= (int)lineHeight * settingsService.ScaleFactor.Value;
            }
            
            window.Position = windowPosition;
        }

        private void SetWindowScale(int scaleFactor)
        {
            Rectangle currentScreenBounds = GetCurrentScreenBounds();
            SetWindowSize(new Point(currentScreenBounds.Width, (int)lineHeight * scaleFactor));
        }
        
        private Rectangle GetCurrentScreenBounds()
        {
            System.Drawing.Rectangle boundsSystemType = Screen.FromHandle(window.Handle).WorkingArea;
            return new Rectangle(boundsSystemType.X, boundsSystemType.Y, boundsSystemType.Width, boundsSystemType.Height);
        }

        private void SetWindowSize(Point size)
        {
            graphicsDeviceManager.PreferredBackBufferWidth = size.X;
            graphicsDeviceManager.PreferredBackBufferHeight = size.Y;
            graphicsDeviceManager.ApplyChanges();
        }

        private void CreateTrayIcon(IntPtr windowHandle)
        {
            Form form = (Form)Control.FromHandle(windowHandle);
            Icon icon = form.Icon;
            NotifyIcon notifyIcon = new NotifyIcon();
            notifyIcon.Icon = icon;
            notifyIcon.Text = "DeltaTune";
            notifyIcon.Visible = true;
            notifyIcon.ContextMenuStrip = settingsMenu.GetSettingsMenu();

            notifyIcon.BalloonTipTitle = "DeltaTune is now running!";
            notifyIcon.BalloonTipText = "Play some music to get started or right-click the DeltaTune icon in your system tray for customization options.";
            notifyIcon.ShowBalloonTip(1000);
        }

        public void Dispose()
        {
            scaleFactorSubscription?.Dispose();
            windowSizeSubscription?.Dispose();
        }
    }
}