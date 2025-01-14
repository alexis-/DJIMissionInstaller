namespace DJI_Mission_Installer.Services
{
  using System.IO;
  using SixLabors.Fonts;
  using SixLabors.ImageSharp;
  using SixLabors.ImageSharp.Drawing;
  using SixLabors.ImageSharp.Drawing.Processing;
  using SixLabors.ImageSharp.PixelFormats;
  using SixLabors.ImageSharp.Processing;
  using Path = System.IO.Path;

  public class ImageService : IImageService
  {
    #region Properties & Fields - Non-Public

    private readonly FontCollection _fonts;
    private readonly Font           _font;
    private readonly Color          _textColor;
    private readonly Color          _backgroundColor;
    private readonly SemaphoreSlim  _fileLock = new(1, 1);
    private const float             PADDING = 40; // Increased padding for better appearance

    #endregion

    #region Constructors

    public ImageService()
    {
      // Initialize fonts
      _fonts = new FontCollection();
      _fonts.AddSystemFonts();
      var family = _fonts.Get("Arial");
      _font = family.CreateFont(24);

      // Initialize colors
      _textColor = Color.White;
      _backgroundColor = new Color(new Rgba32(0, 0, 0, 128)); // 50% transparent black
    }

    public void Dispose()
    {
      _fileLock.Dispose();
    }

    #endregion

    #region Methods Impl

    public async Task<string> ProcessImageAsync(
      string             inputPath,
      string             outputPath,
      string             id,
      DateTime           lastModified,
      IProgress<double>? progress = null)
    {
      await _fileLock.WaitAsync();
      try
      {
        progress?.Report(0);

        if (!File.Exists(inputPath))
          return await CreateDefaultImageAsync(outputPath, id, lastModified);

        try
        {
          // Ensure the directory exists
          var directory = Path.GetDirectoryName(outputPath);
          if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

          // Load and process the image
          using (var image = await Image.LoadAsync<Rgba32>(inputPath))
          {
            progress?.Report(0.6);

            await AddOverlayTextAsync(image, id, lastModified);
            progress?.Report(0.8);

            await image.SaveAsJpegAsync(outputPath);
            progress?.Report(1.0);
          }

          return outputPath;
        }
        catch (Exception)
        {
          return await CreateDefaultImageAsync(outputPath, id, lastModified);
        }
      }
      finally
      {
        _fileLock.Release();
      }
    }

    public async Task<string> CreateDefaultImageAsync(
      string   outputPath,
      string   id,
      DateTime lastModified,
      int      width  = 800,
      int      height = 480)
    {
      await _fileLock.WaitAsync();
      try
      {
        // Ensure the directory exists
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory))
          Directory.CreateDirectory(directory);

        using (var image = new Image<Rgba32>(width, height, Color.White))
        {
          await AddOverlayTextAsync(image, id, lastModified);
          await image.SaveAsJpegAsync(outputPath);
        }

        return outputPath;
      }
      finally
      {
        _fileLock.Release();
      }
    }

    #endregion

    #region Methods

    private async Task AddOverlayTextAsync<TPixel>(Image<TPixel> image, string id, DateTime lastModified)
      where TPixel : unmanaged, IPixel<TPixel>
    {
      var text = $"{id}\n{lastModified:g}";

      // Calculate the maximum available width for text
      float maxWidth = image.Width - (PADDING * 2);

      // Find the appropriate font size that fits the width
      float fontSize = CalculateOptimalFontSize(text, maxWidth);
      var scaledFont = _font.Family.CreateFont(fontSize);

      // Create text options for measuring
      var textOptions = new RichTextOptions(scaledFont)
      {
        WrappingLength = maxWidth,
        LineSpacing = 1.2f,
        Dpi = 96
      };

      // Measure the text size with the new font
      var size = TextMeasurer.MeasureSize(text, textOptions);

      // Calculate center position
      var centerX = (image.Width - size.Width) / 2;
      var centerY = (image.Height - size.Height) / 2;
      
      // Update text options with centered position
      textOptions.Origin = new PointF(centerX, centerY);

      // Draw semi-transparent background and text
      image.Mutate(ctx =>
      {
        // Create background rectangle
        var rect = new RectangularPolygon(
          centerX - PADDING / 2,
          centerY - PADDING / 2,
          size.Width + PADDING,
          size.Height + PADDING);

        // Draw background
        ctx.Fill(_backgroundColor, rect);

        // Draw text
        ctx.DrawText(textOptions, text, _textColor);
      });

      await Task.CompletedTask; // For async consistency
    }

    private float CalculateOptimalFontSize(string text, float maxWidth)
    {
      float minSize = 12;
      float maxSize = 72;
      float targetSize = maxSize;

      // Binary search for the optimal font size
      while (maxSize - minSize > 1)
      {
        targetSize = (minSize + maxSize) / 2;
        var testFont = _font.Family.CreateFont(targetSize);
        var testOptions = new RichTextOptions(testFont)
        {
          WrappingLength = float.MaxValue, // Don't wrap text while measuring
          LineSpacing = 1.2f,
          Dpi = 96
        };

        var size = TextMeasurer.MeasureSize(text, testOptions);

        if (size.Width > maxWidth)
        {
          maxSize = targetSize;
        }
        else
        {
          minSize = targetSize;
        }
      }

      // Return a slightly smaller size to ensure we have some padding
      return minSize * 0.9f;
    }

    #endregion
  }
}