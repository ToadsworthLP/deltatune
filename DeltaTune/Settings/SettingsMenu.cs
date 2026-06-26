using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Forms;
using DeltaTune.Discord;
using DeltaTune.Window;
using SharpDX;

namespace DeltaTune.Settings
{
    public class SettingsMenu : ISettingsMenu
    {
        private static readonly float[] hideAutomaticallyDelayOptions = new[] { 1f, 2.5f, 5f, 7.5f, 10f };
        
        private readonly ISettingsService settingsService;
        private readonly Func<IDiscordService> discord;
        private ContextMenuStrip settingsMenuStrip;

        public SettingsMenu(ISettingsService settingsService, Func<IDiscordService> discord)
        {
            this.settingsService = settingsService;
            this.discord = discord;
        }

        public ContextMenuStrip GetSettingsMenu()
        {
            settingsMenuStrip = new ContextMenuStrip();
            settingsMenuStrip.AutoClose = true;
            settingsMenuStrip.TopLevel = true;

            settingsMenuStrip.Opening += (sender, args) =>
            {
                settingsMenuStrip.Items.Clear();
                
                ToolStripMenuItem headingItem = new ToolStripMenuItem();
                headingItem.Text = "DeltaTune";
                headingItem.Enabled = false;
                settingsMenuStrip.Items.Add(headingItem);
            
                settingsMenuStrip.Items.Add(new ToolStripSeparator());
            
                settingsMenuStrip.Items.Add(GetPositionMenuItem());
                settingsMenuStrip.Items.Add(GetScaleMenuItem());
                settingsMenuStrip.Items.Add(GetBehaviorMenuItem());
                
                settingsMenuStrip.Items.Add(new ToolStripSeparator());
                
                ToolStripMenuItem aboutItem = new ToolStripMenuItem();
                aboutItem.Text = "About...";
                aboutItem.Click += (o, eventArgs) => ShowAboutScreen();
                settingsMenuStrip.Items.Add(aboutItem);

                ToolStripMenuItem quitItem = new ToolStripMenuItem();
                quitItem.Text = "Quit";
                quitItem.Click += (o, eventArgs) => Application.Exit();
                settingsMenuStrip.Items.Add(quitItem);
            };
            
            return settingsMenuStrip;
        }

        private ToolStripMenuItem GetPositionMenuItem()
        {
            ToolStripMenuItem positionItem = new ToolStripMenuItem();
            positionItem.Text = "Position";

            IDictionary<string, string> screenNameMappings = ScreenFriendlyNameProvider.GetAllMonitorsFriendlyNames();
            if (screenNameMappings.Count > 1)
            {
                ToolStripMenuItem headingItem1 = new ToolStripMenuItem();
                headingItem1.Text = "Screen";
                headingItem1.Enabled = false;
                positionItem.DropDownItems.Add(headingItem1);
                
                foreach (KeyValuePair<string, string> screenNameMapping in screenNameMappings)
                {
                    ToolStripMenuItem screenItem = new ToolStripMenuItem();
                    screenItem.Text = screenNameMapping.Value;
                    screenItem.Checked = screenNameMapping.Key == settingsService.ScreenName.Value;
                    screenItem.Click += (sender, args) => settingsService.ScreenName.Value = screenNameMapping.Key;
                    positionItem.DropDownItems.Add(screenItem);
                }
                
                positionItem.DropDownItems.Add(new ToolStripSeparator());
                
                ToolStripMenuItem headingItem2 = new ToolStripMenuItem();
                headingItem2.Text = "Location";
                headingItem2.Enabled = false;
                positionItem.DropDownItems.Add(headingItem2);
            }
            
            ToolStripMenuItem topLeftItem = new ToolStripMenuItem();
            topLeftItem.Text = "Top Left";
            topLeftItem.Checked = PositionPresetHelper.GetFractionalPosition(PositionPreset.TopLeft) == settingsService.Position.Value;
            topLeftItem.Click += (sender, args) => settingsService.Position.Value = PositionPresetHelper.GetFractionalPosition(PositionPreset.TopLeft);
            positionItem.DropDownItems.Add(topLeftItem);
            
            ToolStripMenuItem topRightItem = new ToolStripMenuItem();
            topRightItem.Text = "Top Right";
            topRightItem.Checked = PositionPresetHelper.GetFractionalPosition(PositionPreset.TopRight) == settingsService.Position.Value;
            topRightItem.Click += (sender, args) => settingsService.Position.Value = PositionPresetHelper.GetFractionalPosition(PositionPreset.TopRight);
            positionItem.DropDownItems.Add(topRightItem);
            
            ToolStripMenuItem bottomLeftItem = new ToolStripMenuItem();
            bottomLeftItem.Text = "Bottom Left";
            bottomLeftItem.Checked = PositionPresetHelper.GetFractionalPosition(PositionPreset.BottomLeft) == settingsService.Position.Value;
            bottomLeftItem.Click += (sender, args) => settingsService.Position.Value = PositionPresetHelper.GetFractionalPosition(PositionPreset.BottomLeft);
            positionItem.DropDownItems.Add(bottomLeftItem);
            
            ToolStripMenuItem bottomRightItem = new ToolStripMenuItem();
            bottomRightItem.Text = "Bottom Right";
            bottomRightItem.Checked = PositionPresetHelper.GetFractionalPosition(PositionPreset.BottomRight) == settingsService.Position.Value;
            bottomRightItem.Click += (sender, args) => settingsService.Position.Value = PositionPresetHelper.GetFractionalPosition(PositionPreset.BottomRight);
            positionItem.DropDownItems.Add(bottomRightItem);
            
            ToolStripMenuItem chapter1Item = new ToolStripMenuItem();
            chapter1Item.Text = "Original (Chapter 1)";
            chapter1Item.Checked = PositionPresetHelper.GetFractionalPosition(PositionPreset.Chapter1) == settingsService.Position.Value;
            chapter1Item.Click += (sender, args) => settingsService.Position.Value = PositionPresetHelper.GetFractionalPosition(PositionPreset.Chapter1);
            positionItem.DropDownItems.Add(chapter1Item);
            
            ToolStripMenuItem chapter5Item = new ToolStripMenuItem();
            chapter5Item.Text = "Original (Chapter 5)";
            chapter5Item.Checked = PositionPresetHelper.GetFractionalPosition(PositionPreset.Chapter5) == settingsService.Position.Value;
            chapter5Item.Click += (sender, args) => settingsService.Position.Value = PositionPresetHelper.GetFractionalPosition(PositionPreset.Chapter5);
            positionItem.DropDownItems.Add(chapter5Item);

            return positionItem;
        }
        
        private ToolStripMenuItem GetScaleMenuItem()
        {
            ToolStripMenuItem scaleItem = new ToolStripMenuItem();
            scaleItem.Text = "Size";

            for (int i = 1; i < 9; i++)
            {
                var factor = i;
                
                ToolStripMenuItem item = new ToolStripMenuItem();
                item.Text = $"{factor}";
                item.Checked = settingsService.ScaleFactor.Value == factor;
                item.Click += (sender, args) => settingsService.ScaleFactor.Value = factor;
                scaleItem.DropDownItems.Add(item);
            }
            
            return scaleItem;
        }

        private ToolStripMenuItem GetBehaviorMenuItem()
        {
            ToolStripMenuItem behaviorItem = new ToolStripMenuItem();
            behaviorItem.Text = "Behavior";
            
            ToolStripMenuItem hideAutomaticallyItem = new ToolStripMenuItem();
            hideAutomaticallyItem.Text = "Hide Automatically";
            behaviorItem.DropDownItems.Add(hideAutomaticallyItem);
            
            ToolStripMenuItem hideNeverItem = new ToolStripMenuItem();
            hideNeverItem.Text = "Never";
            hideNeverItem.Checked = settingsService.HideAutomatically.Value == null;
            hideNeverItem.Click += (sender, args) => settingsService.HideAutomatically.Value = null;
            hideAutomaticallyItem.DropDownItems.Add(hideNeverItem);

            foreach (float option in hideAutomaticallyDelayOptions)
            {
                float delay = option;
                ToolStripMenuItem hideDelayItem = new ToolStripMenuItem();
                hideDelayItem.Text = $"After {delay.ToString("F1", NumberFormatInfo.InvariantInfo)} Seconds";
                hideDelayItem.Checked = settingsService.HideAutomatically.Value != null && MathUtil.NearEqual(settingsService.HideAutomatically.Value.Value, delay);
                hideDelayItem.Click += (sender, args) => settingsService.HideAutomatically.Value = delay;
                hideAutomaticallyItem.DropDownItems.Add(hideDelayItem);
            }
            
            ToolStripMenuItem showArtistNameItem = new ToolStripMenuItem();
            showArtistNameItem.Text = "Show Artist Name";
            showArtistNameItem.Checked = settingsService.ShowArtistName.Value;
            showArtistNameItem.Click += (sender, args) => settingsService.ShowArtistName.Value = !settingsService.ShowArtistName.Value;
            behaviorItem.DropDownItems.Add(showArtistNameItem);
            
            ToolStripMenuItem showPlaybackStatusItem = new ToolStripMenuItem();
            showPlaybackStatusItem.Text = "Show Playback Status";
            showPlaybackStatusItem.Checked = settingsService.ShowPlaybackStatus.Value;
            showPlaybackStatusItem.Click += (sender, args) => settingsService.ShowPlaybackStatus.Value = !settingsService.ShowPlaybackStatus.Value;
            behaviorItem.DropDownItems.Add(showPlaybackStatusItem);

            ToolStripMenuItem enableDiscordItem = new ToolStripMenuItem();
            enableDiscordItem.Text = "Enable Discord Rich Presence";
            enableDiscordItem.Checked = settingsService.EnableDiscordRichPresence.Value;
            enableDiscordItem.Click += (sender, args) => {
                settingsService.EnableDiscordRichPresence.Value = !settingsService.EnableDiscordRichPresence.Value;
                discord.Invoke()?.UpdateState();
            };
            behaviorItem.DropDownItems.Add(enableDiscordItem);
            
            ToolStripMenuItem screenCaptureCompatItem = new ToolStripMenuItem();
            screenCaptureCompatItem.Text = "Streamer Mode";
            screenCaptureCompatItem.Checked = settingsService.ScreenCaptureCompatibilityMode.Value;
            screenCaptureCompatItem.Click += (sender, args) =>
            {
                if (!settingsService.ScreenCaptureCompatibilityMode.Value)
                {
                    DialogResult result = MessageBox.Show($"Streamer Mode improves DeltaTune's compatibility with screen capture software by making it show up as an option for window capture and allowing its window to disappear behind others.\n" +
                                                          $"It is not intended for normal use outside of this purpose.\n\n" +
                                                          $"Do you want to enable it?", ProgramInfo.Name, MessageBoxButtons.YesNo, MessageBoxIcon.None);
                
                    if (result == DialogResult.Yes)
                    {
                        settingsService.ScreenCaptureCompatibilityMode.Value = !settingsService.ScreenCaptureCompatibilityMode.Value;
                    } 
                }
                else
                {
                    settingsService.ScreenCaptureCompatibilityMode.Value = !settingsService.ScreenCaptureCompatibilityMode.Value;
                }
            };
            behaviorItem.DropDownItems.Add(screenCaptureCompatItem);
            
            return behaviorItem;
        }
        
        private void ShowAboutScreen()
        {
            MessageBox.Show($"{ProgramInfo.Name} {ProgramInfo.Version}{ProgramInfo.VersionSuffix}\nCreated by {ProgramInfo.Author}\n\n{ProgramInfo.Credits}\n\n{ProgramInfo.Disclaimer}", ProgramInfo.Name, MessageBoxButtons.OK, MessageBoxIcon.None);
        }
    }
}