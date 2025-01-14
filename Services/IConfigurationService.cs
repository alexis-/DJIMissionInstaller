namespace DJI_Mission_Installer.Services
{
  public interface IConfigurationService
  {
    string KmzSourceFolder  { get; set; }
    string GoogleMapsApiKey { get; set; }
    bool   UseAdbByDefault  { get; set; }
    void   Save();
    void   Reset();
  }
}
