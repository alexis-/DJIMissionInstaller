namespace DJI_Mission_Installer.Services
{
  using System.Configuration;
  using System.IO;

  public class ConfigurationService : IConfigurationService
  {
    #region Constants & Statics

    private const string AppRegistryKey = @"SOFTWARE\DJI Mission Installer";

    #endregion

    #region Properties & Fields - Non-Public

    private readonly Dictionary<string, string> _defaults = new()
    {
      { nameof(KmzSourceFolder), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "DJI", "KMZ") },
      { nameof(GoogleMapsApiKey), string.Empty },
      { nameof(UseAdbByDefault), "True" }
    };

    private readonly Configuration _configuration;

    #endregion

    #region Constructors

    public ConfigurationService()
    {
      try
      {
        _configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
      }
      catch (ConfigurationErrorsException)
      {
        // If the config file is corrupted, create a new one
        File.Delete(ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None).FilePath);
        _configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
      }

      // Ensure all default settings exist
      foreach (var setting in _defaults)
        if (_configuration.AppSettings.Settings[setting.Key] == null)
          _configuration.AppSettings.Settings.Add(setting.Key, setting.Value);

      _configuration.Save(ConfigurationSaveMode.Modified);
      ConfigurationManager.RefreshSection("appSettings");
    }

    #endregion

    #region Properties Impl - Public

    public string KmzSourceFolder
    {
      get => GetSetting(nameof(KmzSourceFolder));
      set => SaveSetting(nameof(KmzSourceFolder), value);
    }

    public string GoogleMapsApiKey
    {
      get => GetSetting(nameof(GoogleMapsApiKey));
      set => SaveSetting(nameof(GoogleMapsApiKey), value);
    }

    public bool UseAdbByDefault
    {
      get => Convert.ToBoolean(GetSetting(nameof(UseAdbByDefault)));
      set => SaveSetting(nameof(UseAdbByDefault), value.ToString());
    }

    #endregion

    #region Methods Impl

    public void Save()
    {
      try
      {
        _configuration.Save(ConfigurationSaveMode.Modified);
        ConfigurationManager.RefreshSection("appSettings");
      }
      catch (ConfigurationErrorsException ex)
      {
        throw new InvalidOperationException("Failed to save configuration", ex);
      }
    }

    public void Reset()
    {
      foreach (var setting in _defaults)
        SaveSetting(setting.Key, setting.Value);
      Save();
    }

    #endregion

    #region Methods

    private string GetSetting(string key)
    {
      var value = ConfigurationManager.AppSettings[key];
      return string.IsNullOrEmpty(value) ? _defaults.GetValueOrDefault(key, string.Empty) : value;
    }

    private void SaveSetting(string key, string value)
    {
      if (_configuration.AppSettings.Settings[key] == null)
        _configuration.AppSettings.Settings.Add(key, value);
      else
        _configuration.AppSettings.Settings[key].Value = value;
    }

    #endregion
  }
}
