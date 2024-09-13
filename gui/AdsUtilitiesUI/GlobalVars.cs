﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace AdsUtilitiesUI
{
    internal static class GlobalVars
    {
        // Spezifischer Name und Pfad für die Konfigurationsdatei
        public static readonly string AppFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AdsUtilities");
        public static readonly string ConfigFileName = "cerhost_config.json";
        public static readonly string ConfigFilePath = Path.Combine(AppFolder, ConfigFileName);

    }
}