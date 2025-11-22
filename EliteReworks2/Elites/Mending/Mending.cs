using BepInEx.Configuration;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace EliteReworks2.Elites.Mending
{
    public class Mending : TweakBase<Mending>
    {
        public override string ConfigCategoryString => "T1 - Mending";

        public override string ConfigOptionName => "Enable Module";

        public override string ConfigDescriptionString => "Enable changes related to Mending Elites.";

        public static float healthBoostCoefficient;
        public static float damageBoostCoefficient;

        public static float healthBoostCoefficientHonor;
        public static float damageBoostCoefficientHonor;

        protected override void ReadConfig(ConfigFile config)
        {
            base.ReadConfig(config);
            healthBoostCoefficient = config.Bind<float>(new ConfigDefinition(ConfigCategoryString, "Stats - Health Multiplier"), 3f, new ConfigDescription("Health multiplier for this Elite Type. (Vanilla = 3)")).Value;
            damageBoostCoefficient = config.Bind<float>(new ConfigDefinition(ConfigCategoryString, "Stats - Damage Multiplier"), 1.5f, new ConfigDescription("Damage multiplier for this Elite Type. (Vanilla = 2)")).Value;

            healthBoostCoefficientHonor = config.Bind<float>(new ConfigDefinition(ConfigCategoryString, "Stats (Honor) - Health Multiplier"), 1.5f, new ConfigDescription("Health multiplier for this Elite Type when Honor is enabled. (Vanilla = 1.5)")).Value;
            damageBoostCoefficientHonor = config.Bind<float>(new ConfigDefinition(ConfigCategoryString, "Stats (Honor) - Damage Multiplier"), 1.5f, new ConfigDescription("Damage multiplier for this Elite Type when Honor is enabled. (Vanilla = 1.5)")).Value;
        }

        protected override void ApplyChanges()
        {
            base.ApplyChanges();
            ModifyStats();
            FixHealCore();
            On.RoR2.AffixEarthBehavior.FixedUpdate += AffixEarthBehavior_FixedUpdate;
        }

        private void FixHealCore()
        {
            GameObject healCore = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/EliteEarth/AffixEarthHealerBody.prefab").WaitForCompletion();
            CharacterBody body = healCore.GetComponent<CharacterBody>();
            body.levelMaxHealth = body.baseMaxHealth * 0.3f;
            body.levelDamage = body.baseDamage * 0.2f;
        }

        private void AffixEarthBehavior_FixedUpdate(On.RoR2.AffixEarthBehavior.orig_FixedUpdate orig, AffixEarthBehavior self)
        {
            if (NetworkServer.active && self.body && self.body.HasBuff(Common.Buffs.DisablePassiveEffect.buffDef) && self.affixEarthAttachment)
            {
                UnityEngine.Object.Destroy(self.affixEarthAttachment);
                self.affixEarthAttachment = null;
                return;
            }

            orig(self);
        }

        private void ModifyStats()
        {
            EliteDef eliteDef = Addressables.LoadAssetAsync<EliteDef>("RoR2/DLC1/EliteEarth/edEarth.asset").WaitForCompletion();
            eliteDef.healthBoostCoefficient = healthBoostCoefficient;
            eliteDef.damageBoostCoefficient = damageBoostCoefficient;

            EliteDef eliteDefHonor = Addressables.LoadAssetAsync<EliteDef>("RoR2/DLC1/EliteEarth/edEarthHonor.asset").WaitForCompletion();
            eliteDefHonor.healthBoostCoefficient = healthBoostCoefficientHonor;
            eliteDefHonor.damageBoostCoefficient = damageBoostCoefficientHonor;
        }
    }
}
