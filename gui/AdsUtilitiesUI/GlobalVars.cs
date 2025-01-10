using System;
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
        // For config file in AppData
        public static readonly string AppFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AdsUtilities");
        public static readonly string CerhostConfigFileName = "cerhost_config.json";
        public static readonly string NetworkCardSelectionFileName = "network_card_selections.json";


    }
}
