namespace DJI_Mission_Installer.Services
{
  public interface IImageService : IDisposable
  {
    Task<string> ProcessImageAsync(
      string             inputPath,
      string             outputPath,
      string             id,
      DateTime           lastModified,
      IProgress<double>? progress = null);

    Task<string> CreateDefaultImageAsync(
      string   outputPath,
      string   id,
      DateTime lastModified,
      int      width  = 800,
      int      height = 480);
  }
}
