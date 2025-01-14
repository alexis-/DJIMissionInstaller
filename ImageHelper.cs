namespace DJI_Mission_Installer
{
  using System.IO;
  using System.Windows.Media.Imaging;

  public class ImageHelper
  {
    #region Methods

    public static BitmapImage LoadImageWithCopyOption(string imagePath)
    {
      var image = new BitmapImage();
      image.BeginInit();
      image.CacheOption   = BitmapCacheOption.OnLoad; // This ensures the file is closed after loading
      image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
      using (var stream = new FileStream(imagePath, FileMode.Open, FileAccess.Read))
      {
        image.StreamSource = new MemoryStream();
        stream.CopyTo(image.StreamSource);
      }

      image.StreamSource.Position = 0;
      image.EndInit();
      image.Freeze(); // Makes the image immutable and thread-safe
      return image;
    }

    #endregion
  }
}
