using System.Collections.Generic;
using Vintagestory.API.Common;


    public class ConfigSettings
    {
        public static ConfigSettings Loaded { get; set; } = new ConfigSettings();
        public int LOG_LIMIT_MB { get; set; } = 10;

        public string PATH { get; set; } = "/home/container/data/Logs/";
        public int BACKUP_FREQ_MINS { get; set; } = 5;

        public List<string> LOG_FILES = new List<string>
            { "server-audit.txt", "server-main.txt",/* rest of elements */ };

    }
