using System.Collections.Generic;
using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.OpenTelemetry.Web;

public static class ModuleConstants
{
    public static class Security
    {
        public static class Permissions
        {
            public const string Access = "open-telemetry:access";
            public const string Create = "open-telemetry:create";
            public const string Read = "open-telemetry:read";
            public const string Update = "open-telemetry:update";
            public const string Delete = "open-telemetry:delete";

            public static string[] AllPermissions { get; } =
            [
                Access,
                Create,
                Read,
                Update,
                Delete,
            ];
        }
    }

    public static class Settings
    {
        public static class General
        {
            public static SettingDescriptor OpenTelemetryEnabled { get; } = new()
            {
                Name = "OpenTelemetry.Enabled",
                GroupName = "OpenTelemetry|General",
                ValueType = SettingValueType.Boolean,
                DefaultValue = false,
            };

            public static IEnumerable<SettingDescriptor> AllGeneralSettings
            {
                get
                {
                    yield return OpenTelemetryEnabled;
                }
            }
        }

        public static IEnumerable<SettingDescriptor> AllSettings
        {
            get
            {
                return General.AllGeneralSettings;
            }
        }
    }
}
