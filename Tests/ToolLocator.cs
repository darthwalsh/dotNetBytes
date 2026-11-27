using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Tests
{
  /// <summary>Pure helpers for picking the newest compiler install, split out so they can be unit tested.</summary>
  static class ToolLocator
  {
    // Compare numerically: as strings, "10.0.9" would sort above "10.0.10".
    // Prerelease ("10.0.100-rc.1") sorts below the release of the same number.
    static (Version Number, bool IsRelease) VersionKey(string version) =>
      (Version.Parse(version.Split('-')[0]), !version.Contains('-'));

    public static string NewestVersion(IEnumerable<string> versions) =>
      versions.OrderByDescending(VersionKey).First();

    /// <param name="listSdksOutput">Output of <c>dotnet --list-sdks</c>, e.g. <c>10.0.100 [/usr/local/share/dotnet/sdk]</c></param>
    public static (string Version, string SdkRoot) NewestSdk(string listSdksOutput, int major) {
      var sdks = listSdksOutput
        .Split('\n')
        .Select(line => Regex.Match(line.Trim(), @"^(?<version>\S+) \[(?<root>.+)\]$"))
        .Where(m => m.Success && m.Groups["version"].Value.StartsWith($"{major}."))
        .Select(m => (Version: m.Groups["version"].Value, SdkRoot: m.Groups["root"].Value))
        .ToList();
      if (sdks.Count == 0) {
        throw new InvalidOperationException($"No .NET {major} SDK in `dotnet --list-sdks` output: {listSdksOutput}");
      }

      var newest = NewestVersion(sdks.Select(sdk => sdk.Version));
      return sdks.First(sdk => sdk.Version == newest);
    }

    /// <summary>
    /// Visual Studio install folders are named by year through 2022 (2017, 2019, 2022), then by major version (18 for VS 2026).
    /// Returns the numeric ones newest-first, skipping siblings like "Installer" and "Shared".
    /// </summary>
    public static IEnumerable<string> VsVersionDirsNewestFirst(IEnumerable<string> versionDirs) =>
      versionDirs
        .Select(dir => (Dir: dir, Parsed: int.TryParse(Path.GetFileName(dir), out var number), Number: number))
        .Where(v => v.Parsed)
        .OrderByDescending(v => (IsMajorVersion: v.Number < 1000, v.Number))
        .Select(v => v.Dir);
  }
}
