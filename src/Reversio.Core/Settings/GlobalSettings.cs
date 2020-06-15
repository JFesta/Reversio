using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Reversio.Core.Settings
{
    public class GlobalSettings
    {
        public const string DefaultNetCoreVersion = "3.1";
        private static Regex _versionRegex = new Regex("([0-9]+)(\\.([0-9]+))?(\\.([0-9]+))?(\\.([0-9]+))?");

        public string NetCoreVersion { get; set; }
        public bool ExcludeInfoText { get; set; }

        public bool IsVersionAtLeast(int major)
        {
            var parsedVersion = ParseNetCoreVersion(NetCoreVersion ?? DefaultNetCoreVersion);
            return (parsedVersion.Item1 >= major);
        }

        public bool IsVersionAtLeast(int major, int minor)
        {
            var parsedVersion = ParseNetCoreVersion(NetCoreVersion ?? DefaultNetCoreVersion);
            return (parsedVersion.Item1 >= major && parsedVersion.Item2 >= minor);
        }

        public bool IsNetCore3()
        {
            return (NetCoreVersion == null || NetCoreVersion.StartsWith("3."));
        }

        public bool IsNetCore2()
        {
            return (NetCoreVersion != null && NetCoreVersion.StartsWith("2."));
        }

        private Tuple<int, int> ParseNetCoreVersion(string netCoreVersion)
        {
            var match = _versionRegex.Match(netCoreVersion);
            if (!match.Success)
            {
                if (netCoreVersion == DefaultNetCoreVersion)
                    throw new Exception("Cannot parse version");
                else
                    return ParseNetCoreVersion(DefaultNetCoreVersion);
            }
            int major = 0, minor = 0;
            major = Int32.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
            if (match.Groups.Count >= 4)
                minor = Int32.Parse(match.Groups[3].Value, System.Globalization.CultureInfo.InvariantCulture);

            return new Tuple<int, int>(major, minor);
        }
    }
}
