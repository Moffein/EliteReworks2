using BepInEx.Configuration;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace EliteReworks2.Elites.Blazing
{
    public class Blazing : TweakBase<Blazing>
    {
        public override string ConfigCategoryString => "T1 - Blazing";

        public override string ConfigOptionName => "Enable Module";

        public override string ConfigDescriptionString => "Enable changes related to Blazing Elites.";

        public static float fireTrailDamage = 24f;  //Lemurian
        public static float fireTrailDamageBoss = 32f;  //Imp Overlord

        public static float healthBoostCoefficient;
        public static float damageBoostCoefficient;

        public static float healthBoostCoefficientHonor;
        public static float damageBoostCoefficientHonor;

        protected override void ReadConfig(ConfigFile config)
        {
            base.ReadConfig(config);
            healthBoostCoefficient = config.Bind<float>(new ConfigDefinition(ConfigCategoryString, "Stats - Health Multiplier"), 3f, new ConfigDescription("Health multiplier for this Elite Type. (Vanilla = 4)")).Value;
            damageBoostCoefficient = config.Bind<float>(new ConfigDefinition(ConfigCategoryString, "Stats - Damage Multiplier"), 1.5f, new ConfigDescription("Damage multiplier for this Elite Type. (Vanilla = 2)")).Value;

            healthBoostCoefficientHonor = config.Bind<float>(new ConfigDefinition(ConfigCategoryString, "Stats (Honor) - Health Multiplier"), 2.5f, new ConfigDescription("Health multiplier for this Elite Type when Honor is enabled. (Vanilla = 2.5)")).Value;
            damageBoostCoefficientHonor = config.Bind<float>(new ConfigDefinition(ConfigCategoryString, "Stats (Honor) - Damage Multiplier"), 1.5f, new ConfigDescription("Damage multiplier for this Elite Type when Honor is enabled. (Vanilla = 1.5)")).Value;
        }

        protected override void ApplyChanges()
        {
            base.ApplyChanges();
            ModifyStats();
            On.RoR2.CharacterBody.UpdateFireTrail += ModifyFireTrail;
        }

        private void ModifyStats()
        {
            EliteDef eliteDef = Addressables.LoadAssetAsync<EliteDef>("RoR2/Base/EliteFire/edFire.asset").WaitForCompletion();
            eliteDef.healthBoostCoefficient = healthBoostCoefficient;
            eliteDef.damageBoostCoefficient = damageBoostCoefficient;

            EliteDef eliteDefHonor = Addressables.LoadAssetAsync<EliteDef>("RoR2/Base/EliteFire/edFireHonor.asset").WaitForCompletion();
            eliteDefHonor.healthBoostCoefficient = healthBoostCoefficientHonor;
            eliteDefHonor.damageBoostCoefficient = damageBoostCoefficientHonor;
        }

        private void ModifyFireTrail(On.RoR2.CharacterBody.orig_UpdateFireTrail orig, CharacterBody self)
        {
            //Disable on stun
            if (self.HasBuff(EliteReworks2.Common.Buffs.DisablePassiveEffect.buffDef) || !(self.healthComponent && self.healthComponent.alive))
            {
                if (self.fireTrail)
                {
                    UnityEngine.Object.Destroy(self.fireTrail.gameObject);
                    self.fireTrail = null;
                }
                return;
            }

            orig(self);

            if (self.fireTrail)
            {
                if (self.isChampion)
                {
                    self.fireTrail.damagePerSecond = EliteReworks2Utils.GetAmbientLevelScaledDamage(fireTrailDamageBoss) * 1.5f;
                }
                else
                {
                    self.fireTrail.damagePerSecond = EliteReworks2Utils.GetAmbientLevelScaledDamage(fireTrailDamage) * 1.5f;
                }
            }
        }
    }
}
