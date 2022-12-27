﻿using System.CommandLine;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json.Serialization;
using EVA.SDK.Generator.V2.Exceptions;
using EVA.SDK.Generator.V2.Helpers;

namespace EVA.SDK.Generator.V2.Commands.Update;

public class UpdateCommand
{
  public static void Register(Command command)
  {
    var updateCommand = new Command("update");
    command.Add(updateCommand);

    var option = new Option<bool>(name: "--wait") { IsHidden = true }.WithDefault(false);
    updateCommand.AddOption(option);

    updateCommand.SetHandler(async wait =>
    {
      // Locations
      var currentExeLocation = Environment.ProcessPath;

      if (wait)
      {
        var newExeLocation = Path.Combine(Path.GetDirectoryName(currentExeLocation), Path.GetFileName(currentExeLocation)[4..]);

        // Try 10 times
        foreach (var _ in Enumerable.Range(0, 10))
        {
          try
          {
            if (File.Exists(newExeLocation)) File.Delete(newExeLocation);
            File.Move(currentExeLocation, newExeLocation);
            Console.WriteLine("Update completed!");
            break;
          }
          catch
          {
            // Ignore
          }

          await Task.Delay(TimeSpan.FromMilliseconds(250));
        }
      }
      else
      {
        var newExeLocation = Path.Combine(Path.GetDirectoryName(currentExeLocation), "new-" + Path.GetFileName(currentExeLocation));

        // Find current version
        var version = Assembly.GetEntryAssembly().GetName().Version.ToString(3);
        Console.WriteLine("Current version: {0}", version);

        // Find latest version
        var response = await HttpHelpers.GetJson(HttpConstants.LatestReleaseTage, JsonContext.Default.LatestResponse);
        var latestVersion = response.TagName;
        if (!latestVersion.StartsWith(HttpConstants.TagPrefix))
        {
          throw new SdkException("Could not determine latest version, update cancelled");
        }

        latestVersion = latestVersion[(HttpConstants.TagPrefix.Length)..];
        Console.WriteLine("Latest version: {0}", latestVersion);

        // Check for up-to-date
        if (version == latestVersion)
        {
          Console.WriteLine("Application already up-to-date");
          return;
        }

        Console.WriteLine("Trying to update {0} -> {1}", version, latestVersion);

        // Find the correct asset
        var expectedAssetName = Path.GetFileName(currentExeLocation);
        var asset = response.Assets.FirstOrDefault(a => a.Name == expectedAssetName);
        if (asset == null)
        {
          throw new SdkException($"Cannot find asset: {expectedAssetName}");
        }

        // Download the asset
        await HttpHelpers.GetToFile(asset.BrowserDownloadUrl, newExeLocation);

        // Start child process for replacement
        Process.Start(new ProcessStartInfo
        {
          WorkingDirectory = Directory.GetCurrentDirectory(),
          FileName = newExeLocation,
          Arguments = "update --wait"
        });
      }
    }, option);
  }

  public class LatestResponse
  {
    [JsonPropertyName("assets")] public List<Asset> Assets { get; set; }

    [JsonPropertyName("tag_name")] public string TagName { get; set; }

    public class Asset
    {
      [JsonPropertyName("name")] public string Name { get; set; }

      [JsonPropertyName("browser_download_url")]
      public string BrowserDownloadUrl { get; set; }
    }
  }
}