using System;
using System.Collections.Generic;
using System.Text;

namespace EliteReworks2.Modules
{
    internal static class ModCompat
    {
        public static bool zetAspectsLoaded;
        internal static void Init()
        {
            zetAspectsLoaded = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.TPDespair.ZetAspects");
        }
    }
}
