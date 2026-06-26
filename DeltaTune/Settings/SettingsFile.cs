using System;
using System.IO;
using System.Windows.Forms;
using Newtonsoft.Json;
using R3;

namespace DeltaTune.Settings
{
    public class SettingsFile : ISettingsFile, IDisposable
    {
        private readonly ISettingsService settingsService;
        private readonly string filePath;
        
        private readonly Subject<Unit> saveEvent = new Subject<Unit>();
        private IDisposable valueChangeSubscription;
        private IDisposable saveSubscription;
        private bool disableSaving = false;
        
        public SettingsFile(ISettingsService settingsService, string filePath)
        {
            this.settingsService = settingsService;
            this.filePath = Path.GetFullPath(filePath);

            var valueChangeDisposableBuilder = Disposable.CreateBuilder();
            settingsService.ScaleFactor.Subscribe(scale => saveEvent.OnNext(Unit.Default)).AddTo(ref valueChangeDisposableBuilder);
            settingsService.Position.Subscribe(pos => saveEvent.OnNext(Unit.Default)).AddTo(ref valueChangeDisposableBuilder);
            settingsService.ScreenName.Subscribe(pos => saveEvent.OnNext(Unit.Default)).AddTo(ref valueChangeDisposableBuilder);
            settingsService.ShowArtistName.Subscribe(state => saveEvent.OnNext(Unit.Default)).AddTo(ref valueChangeDisposableBuilder);
            settingsService.ShowPlaybackStatus.Subscribe(state => saveEvent.OnNext(Unit.Default)).AddTo(ref valueChangeDisposableBuilder);
            settingsService.HideAutomatically.Subscribe(state => saveEvent.OnNext(Unit.Default)).AddTo(ref valueChangeDisposableBuilder);
            settingsService.ScreenCaptureCompatibilityMode.Subscribe(state => saveEvent.OnNext(Unit.Default)).AddTo(ref valueChangeDisposableBuilder);
            settingsService.EnableDiscordRichPresence.Subscribe(state => saveEvent.OnNext(Unit.Default)).AddTo(ref valueChangeDisposableBuilder);
            valueChangeSubscription = valueChangeDisposableBuilder.Build();

            saveSubscription = saveEvent.DebounceFrame(1).Subscribe(_ => Save());
            
            Load();
        }

        private void Load()
        {
            disableSaving = true;
            
            try
            {
                if (File.Exists(filePath))
                {
                    string serialized = File.ReadAllText(filePath);
                    SettingsFileModel fileModel = JsonConvert.DeserializeObject<SettingsFileModel>(serialized);
                    fileModel.ToSettings(settingsService);
                    settingsService.IsFactorySettings = false;
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(
                    $"Failed to load settings from file at {filePath}.\nYou can still use the program, but your settings will not be saved.\n\nDetails:\n{e}",
                    ProgramInfo.Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                disableSaving = false;
            }
        }

        private void Save()
        {
            if(disableSaving) return;
            
            settingsService.IsFactorySettings = false;
            
            FileStream fileStream = null;
            try
            {
                if (File.Exists(filePath))
                {
                    fileStream = File.Open(filePath, FileMode.Truncate);
                }
                else
                {
                    fileStream = File.Create(filePath);
                }

                SettingsFileModel fileModel = new SettingsFileModel();
                fileModel.FromSettings(settingsService);
                string serialized = JsonConvert.SerializeObject(fileModel, Formatting.Indented);
                
                using (StreamWriter writer = new StreamWriter(fileStream))
                {
                    writer.Write(serialized);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(
                    $"Failed to save settings to file at {filePath}.\nYou can still use the program, but your settings will not be saved.\n\nDetails:\n{e.Message}\n{e.StackTrace}",
                    ProgramInfo.Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                fileStream?.Dispose();
            }
        }

        public void Dispose()
        {
            valueChangeSubscription?.Dispose();
            saveSubscription?.Dispose();
            saveEvent?.Dispose();
        }
    }
}