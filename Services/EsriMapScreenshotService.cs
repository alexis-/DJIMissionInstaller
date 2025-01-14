namespace DJI_Mission_Installer.Services
{
  using System.IO;
  using System.Net.Http;

  public class EsriMapScreenshotService : IMapScreenshotService
  {
    #region Constants & Statics

    private const string BaseUrl = "https://services.arcgisonline.com/arcgis/rest/services/World_Imagery/MapServer/export";

    #endregion

    #region Properties & Fields - Non-Public

    private readonly HttpClient _httpClient;

    #endregion

    #region Constructors

    public EsriMapScreenshotService()
    {
      _httpClient = new HttpClient();
    }

    #endregion

    #region Methods Impl

    public async Task<string> SaveMapScreenshotAsync(
      double latitude,
      double longitude,
      int    zoomLevel,
      string outputPath,
      int    width  = 640,
      int    height = 640)
    {
      ValidateParameters(latitude, longitude, zoomLevel, width, height);

      if (string.IsNullOrWhiteSpace(outputPath))
        throw new ArgumentException("Output path cannot be empty", nameof(outputPath));

      try
      {
        // Convert lat/long to Web Mercator
        var (x, y) = LatLongToWebMercator(latitude, longitude);

        // Calculate the extent based on zoom level and image size
        var extent = CalculateExtent(x, y, zoomLevel, width, height);

        var url      = BuildMapUrl(extent, width, height);
        var response = await _httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
          var errorContent = await response.Content.ReadAsStringAsync();
          throw new HttpRequestException(
            $"ESRI Map request failed with status {response.StatusCode}.\nResponse: {errorContent}");
        }

        var imageBytes = await response.Content.ReadAsByteArrayAsync();

        // Ensure directory exists
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory))
          Directory.CreateDirectory(directory);

        // Save the image
        await File.WriteAllBytesAsync(outputPath, imageBytes);

        return outputPath;
      }
      catch (Exception ex)
      {
        throw new Exception($"Failed to get map image: {ex.Message}", ex);
      }
    }

    #endregion

    #region Methods

    private void ValidateParameters(double latitude, double longitude, int zoomLevel, int width, int height)
    {
      if (latitude < -90 || latitude > 90)
        throw new ArgumentOutOfRangeException(nameof(latitude), "Latitude must be between -90 and 90");

      if (longitude < -180 || longitude > 180)
        throw new ArgumentOutOfRangeException(nameof(longitude), "Longitude must be between -180 and 180");

      if (zoomLevel < 0 || zoomLevel > 23)
        throw new ArgumentOutOfRangeException(nameof(zoomLevel), "Zoom level must be between 0 and 23");

      if (width <= 0 || height <= 0)
        throw new ArgumentOutOfRangeException("Dimensions must be positive numbers");

      if (width > 4096 || height > 4096)
        throw new ArgumentOutOfRangeException("Maximum dimension is 4096 pixels");
    }

    private string BuildMapUrl(Extent extent, int width, int height)
    {
      var parameters = new Dictionary<string, string>
      {
        { "bbox", $"{extent.XMin},{extent.YMin},{extent.XMax},{extent.YMax}" },
        { "bboxSR", "102100" }, // Web Mercator WKID
        { "size", $"{width},{height}" },
        { "imageSR", "102100" },
        { "format", "jpg" },
        { "f", "image" }
      };

      var queryString = string.Join("&", parameters.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
      return $"{BaseUrl}?{queryString}";
    }

    private (double x, double y) LatLongToWebMercator(double lat, double lon)
    {
      const double earthRadius = 6378137.0; // Earth's radius in meters

      var x = lon * Math.PI / 180 * earthRadius;
      var y = Math.Log(Math.Tan((90 + lat) * Math.PI / 360)) * earthRadius;

      return (x, y);
    }

    private Extent CalculateExtent(double centerX, double centerY, int zoomLevel, int width, int height)
    {
      // Calculate the ground resolution at the center latitude
      var resolution = 156543.03392 * Math.Pow(2, -zoomLevel); // meters per pixel

      // Calculate the width and height in meters
      var widthInMeters  = width * resolution;
      var heightInMeters = height * resolution;

      return new Extent
      {
        XMin = centerX - widthInMeters / 2,
        XMax = centerX + widthInMeters / 2,
        YMin = centerY - heightInMeters / 2,
        YMax = centerY + heightInMeters / 2
      };
    }

    #endregion

    private class Extent
    {
      #region Properties & Fields - Public

      public double XMin { get; set; }
      public double XMax { get; set; }
      public double YMin { get; set; }
      public double YMax { get; set; }

      #endregion
    }
  }
}
