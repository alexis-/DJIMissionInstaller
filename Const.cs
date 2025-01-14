namespace DJI_Mission_Installer;

using System.IO;

public static class Const
{
  public const string WaypointFolder = @"Android\data\dji.go.v5\files\waypoint";
  public const string WaypointPreviewFolder = @"Android\data\dji.go.v5\files\waypoint\map_preview";
  public const string TempFolderName = "DJI_Mission_Installer";

  public static string TempPath => Path.Combine(Path.GetTempPath(), TempFolderName);
}