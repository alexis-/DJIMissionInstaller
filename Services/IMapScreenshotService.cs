namespace DJI_Mission_Installer.Services
{
  public interface IMapScreenshotService
  {
    Task<string> SaveMapScreenshotAsync(double latitude,
                                        double longitude,
                                        int    zoomLevel,
                                        string outputPath,
                                        int    width  = 640,
                                        int    height = 640);
  }
}
