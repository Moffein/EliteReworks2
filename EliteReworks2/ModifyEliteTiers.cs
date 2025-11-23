using BepInEx.Configuration;
using Facepunch.Steamworks;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using static RoR2.CombatDirector;

namespace EliteReworks2
{
    public static class ModifyEliteTiers
    {
        public static float tier1Cost;
        public static float tier1HonorCost;
        public static float tier2Cost;
        public static int tier2MinStages;

        internal static void Init(ConfigFile config)
        {
            tier1Cost = config.Bind<float>(new ConfigDefinition("Elite Tiers", "Tier 1 - Cost"), 4.5f, new ConfigDescription("Cost multiplier for this Elite tier. (Vanilla is 6)")).Value;
            tier1HonorCost = config.Bind<float>(new ConfigDefinition("Elite Tiers", "Tier 1 (Honor) - Cost"), 3.5f, new ConfigDescription("Cost multiplier for this Elite tier. (Vanilla is 3.5)")).Value;
            tier2Cost = config.Bind<float>(new ConfigDefinition("Elite Tiers", "Tier 2 - Cost"), 36f, new ConfigDescription("Cost multiplier for this Elite tier. (Vanilla is 36)")).Value;
            tier2MinStages = config.Bind<int>(new ConfigDefinition("Elite Tiers", "Tier 2 - Minimum Stages"), 5, new ConfigDescription("Minimum stage completions before this tier becomes available. (Vanilla is 5)")).Value;

            On.RoR2.CombatDirector.Init += CombatDirector_Init;
        }

        private static void CombatDirector_Init(On.RoR2.CombatDirector.orig_Init orig)
        {
            orig();

            EliteTierDef t1Tier = EliteAPI.VanillaEliteTiers[1];
            t1Tier.costMultiplier = tier1Cost;

            EliteTierDef t1HonorTier = EliteAPI.VanillaEliteTiers[2];
            t1HonorTier.costMultiplier = tier1HonorCost;

            EliteTierDef t1GildedHonorTier = EliteAPI.VanillaEliteTiers[3];
            t1GildedHonorTier.costMultiplier = tier1HonorCost;

            EliteTierDef t1GildedTier = EliteAPI.VanillaEliteTiers[4];
            t1GildedTier.costMultiplier = tier1Cost;

            EliteTierDef t2Tier = EliteAPI.VanillaEliteTiers[5];
            if (tier2MinStages != 5) t2Tier.isAvailable = (eliteRules) =>
            {
                return Run.instance && Run.instance.stageClearCount >= tier2MinStages;
            };
            t2Tier.costMultiplier = tier2Cost;
        }
    }
}
