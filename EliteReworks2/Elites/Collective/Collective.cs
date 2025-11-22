using BepInEx.Configuration;
using EliteReworks2.Elites.Collective.Components;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace EliteReworks2.Elites.Collective
{
    public class Collective : TweakBase<Collective>
    {
        public override string ConfigCategoryString => "T2 - Collective";

        public override string ConfigOptionName => "Enable Module";

        public override string ConfigDescriptionString => "Enable changes related to Collective Elites.";

        public static float healthBoostCoefficient;
        public static float damageBoostCoefficient;

        public static float healthBoostCoefficientCC;
        public static float damageBoostCoefficientCC;

        public static bool stunDisablesShield;
        public static bool useRework = false;   //TODO: FIGURE SOMETHING OUT

        protected override void ReadConfig(ConfigFile config)
        {
            base.ReadConfig(config);
            healthBoostCoefficient = config.Bind<float>(new ConfigDefinition(ConfigCategoryString, "Stats - Health Multiplier"), 16f, new ConfigDescription("Health multiplier for this Elite Type. (Vanilla = 16)")).Value;
            damageBoostCoefficient = config.Bind<float>(new ConfigDefinition(ConfigCategoryString, "Stats - Damage Multiplier"), 4f, new ConfigDescription("Damage multiplier for this Elite Type. (Vanilla = 4)")).Value;

            healthBoostCoefficientCC = config.Bind<float>(new ConfigDefinition(ConfigCategoryString, "Stats (Conduit Canyon) - Health Multiplier"), 3f, new ConfigDescription("Health multiplier for this Elite Type on Conduit Canyon. (Vanilla = 4)")).Value;
            damageBoostCoefficientCC = config.Bind<float>(new ConfigDefinition(ConfigCategoryString, "Stats (Conduit Canyon) - Damage Multiplier"), 1.5f, new ConfigDescription("Damage multiplier for this Elite Type on Conduit Canyon. (Vanilla = 2)")).Value;
        
            stunDisablesShield = config.Bind<bool>(new ConfigDefinition(ConfigCategoryString, "Shield - Disabled by Stun"), true, new ConfigDescription("Stun and similar effects disable Collective shields.")).Value;
        }

        protected override void ApplyChanges()
        {
            base.ApplyChanges();
            ModifyStats();
            if (!useRework)
            {
                StunnableShield();
            }
        }

        private void ModifyStats()
        {
            EliteDef eliteDef = Addressables.LoadAssetAsync<EliteDef>("RoR2/DLC3/Collective/edCollective.asset").WaitForCompletion();
            eliteDef.healthBoostCoefficient = healthBoostCoefficient;
            eliteDef.damageBoostCoefficient = damageBoostCoefficient;

            EliteDef eliteDefConduitCanyon = Addressables.LoadAssetAsync<EliteDef>("RoR2/DLC3/Collective/edCollectiveWeak.asset").WaitForCompletion();
            eliteDefConduitCanyon.healthBoostCoefficient = healthBoostCoefficientCC;
            eliteDefConduitCanyon.damageBoostCoefficient = damageBoostCoefficientCC;
        }

        private void StunnableShield()
        {
            if (!stunDisablesShield) return;
            On.RoR2.CharacterBody.OnClientBuffsChanged += CharacterBody_OnClientBuffsChanged;
        }

        private void CharacterBody_OnClientBuffsChanged(On.RoR2.CharacterBody.orig_OnClientBuffsChanged orig, CharacterBody self)
        {
            orig(self);
            if (NetworkServer.active && self.HasBuff(DLC3Content.Buffs.EliteCollective))
            {
                var component = self.GetComponent<CollectiveShieldManagerServer>();
                if (!component)
                {
                    component = self.gameObject.AddComponent<CollectiveShieldManagerServer>();
                    component.characterBody = self;
                }
            }
        }
    }
}
