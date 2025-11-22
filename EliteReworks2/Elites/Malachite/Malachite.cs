using BepInEx.Configuration;
using EliteReworks2.Elites.Malachite.Components;
using EliteReworks2.Modules;
using R2API;
using RoR2;
using RoR2.Projectile;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using static RoR2.CharacterBody;

namespace EliteReworks2.Elites.Malachite
{
    public class Malachite : TweakBase<Malachite>
    {
        public override string ConfigCategoryString => "T2 - Malachite";

        public override string ConfigOptionName => "Enable Module";

        public override string ConfigDescriptionString => "Enable changes related to Malachite Elites.";

        public static float spikeDamage = 48f;
        public static float spikeDamageBoss = 64f;

        public static float healthBoostCoefficient;
        public static float damageBoostCoefficient;

        public static bool antiHealAura;

        protected override void ReadConfig(ConfigFile config)
        {
            base.ReadConfig(config);
            healthBoostCoefficient = config.Bind<float>(new ConfigDefinition(ConfigCategoryString, "Stats - Health Multiplier"), 16f, new ConfigDescription("Health multiplier for this Elite Type. (Vanilla = 18)")).Value;
            damageBoostCoefficient = config.Bind<float>(new ConfigDefinition(ConfigCategoryString, "Stats - Damage Multiplier"), 4f, new ConfigDescription("Damage multiplier for this Elite Type. (Vanilla = 6)")).Value;
            antiHealAura = config.Bind<bool>(new ConfigDefinition(ConfigCategoryString, "Rework - Antiheal Aura"), true, new ConfigDescription("Malachite Elites gradually apply antiheal to nearby enemies.")).Value;
        }

        protected override void ApplyChanges()
        {
            base.ApplyChanges();
            ModifyStats();
            Assets.Init();
            On.RoR2.CharacterBody.UpdateAffixPoison += UpdateAffixPoisonEliteReworks;
            if (antiHealAura)
            {
                On.RoR2.CharacterBody.OnClientBuffsChanged += CharacterBody_OnClientBuffsChanged;
            }
        }

        private void CharacterBody_OnClientBuffsChanged(On.RoR2.CharacterBody.orig_OnClientBuffsChanged orig, CharacterBody self)
        {
            orig(self);
            if (NetworkServer.active && self.HasBuff(RoR2Content.Buffs.AffixPoison))
            {
                var component = self.GetComponent<AntiHealAuraServer>();
                if (!component)
                {
                    component = self.gameObject.AddComponent<AntiHealAuraServer>();
                    component.characterBody = self;
                }
            }
        }

        private void UpdateAffixPoisonEliteReworks(On.RoR2.CharacterBody.orig_UpdateAffixPoison orig, CharacterBody self, float deltaTime)
        {
            if (!self.itemAvailability.hasAffixPoison || self.HasBuff(Common.Buffs.DisablePassiveEffect.buffDef))
            {
                self.poisonballTimer = 0f;
                return;
            }

            self.poisonballTimer += deltaTime;
            if (self.poisonballTimer >= 6f)
            {
                self.poisonballTimer -= 6f;

                int spikeCount = 3 + (int)self.radius;
                Vector3 up = Vector3.up;
                float num2 = 360f / (float)spikeCount;
                Vector3 normalized = Vector3.ProjectOnPlane(self.transform.forward, up).normalized;
                Vector3 point = Vector3.RotateTowards(up, normalized, 0.43633232f, float.PositiveInfinity);
                for (int i = 0; i < spikeCount; i++)
                {
                    Vector3 forward = Quaternion.AngleAxis(num2 * (float)i, up) * point;
                    float damage = EliteReworks2Utils.GetAmbientLevelScaledDamage(self.isChampion ? spikeDamageBoss : spikeDamage);
                    
                    ProjectileManager.instance.FireProjectile(Assets.Projectiles.MalachiteOrbModded, self.corePosition, Util.QuaternionSafeLookRotation(forward), self.gameObject, damage, 0f, self.RollCrit(), DamageColorIndex.Default, null, -1f);
                }
            }
        }

        private void ModifyStats()
        {
            EliteDef eliteDef = Addressables.LoadAssetAsync<EliteDef>("RoR2/Base/ElitePoison/edPoison.asset").WaitForCompletion();
            eliteDef.healthBoostCoefficient = healthBoostCoefficient;
            eliteDef.damageBoostCoefficient = damageBoostCoefficient;
        }

        public static class Assets
        {
            public static class Projectiles
            {
                public static GameObject MalachiteOrbModded;
                public static GameObject MalachiteStakeModded;
            }

            public static class DamageTypes
            {
                public static DamageAPI.ModdedDamageType MalachiteSpike;
            }

            public static class Buffs
            {
                public static BuffDef MalachiteBuildup;
            }

            public static class NetworkObjects
            {
                public static GameObject AntiHealAuraIndicator;
            }

            internal static void Init()
            {
                DamageTypes.MalachiteSpike = DamageAPI.ReserveDamageType();
                On.RoR2.GlobalEventManager.ServerDamageDealt += MalachiteSpike;
                CreateMalachiteBuildup();
                CreateAntiHealAuraIndicator();
                CreateMalachiteStakeModded();
                CreateMalachiteOrbModded();
            }

            private static void CreateMalachiteOrbModded()
            {
                if (Projectiles.MalachiteOrbModded) return;
                if (!Projectiles.MalachiteStakeModded) CreateMalachiteStakeModded();

                GameObject projectilePrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/ElitePoison/PoisonOrbProjectile.prefab").WaitForCompletion().InstantiateClone("MoffeinEliteReworks_MalachiteOrbModded", true);
                ProjectileDamage pd = projectilePrefab.GetComponent<ProjectileDamage>();
                pd.damageType = DamageType.NonLethal;
                pd.damageType.AddModdedDamageType(DamageTypes.MalachiteSpike);

                PluginContentPack.projectilePrefabs.Add(projectilePrefab);
                Projectiles.MalachiteOrbModded = projectilePrefab;
                projectilePrefab.GetComponent<ProjectileImpactExplosion>().childrenProjectilePrefab = Projectiles.MalachiteStakeModded;

            }

            private static void CreateMalachiteStakeModded()
            {
                if (Projectiles.MalachiteStakeModded) return;

                GameObject projectilePrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/ElitePoison/PoisonStakeProjectile.prefab").WaitForCompletion().InstantiateClone("MoffeinEliteReworks_MalachiteStakeModded", true);
                ProjectileDamage pd = projectilePrefab.GetComponent<ProjectileDamage>();
                pd.damageType = DamageType.NonLethal;
                pd.damageType.AddModdedDamageType(DamageTypes.MalachiteSpike);

                PluginContentPack.projectilePrefabs.Add(projectilePrefab);
                Projectiles.MalachiteStakeModded = projectilePrefab;

            }


            private static void MalachiteSpike(On.RoR2.GlobalEventManager.orig_ServerDamageDealt orig, DamageReport damageReport)
            {
                orig(damageReport);
                if (damageReport.victimBody && !damageReport.damageInfo.rejected && damageReport.damageInfo.procCoefficient > 0f && damageReport.damageInfo.HasModdedDamageType(DamageTypes.MalachiteSpike))
                {
                    damageReport.victimBody.AddTimedBuff(RoR2Content.Buffs.HealingDisabled, 8f);
                }
            }

            private static void CreateMalachiteBuildup()
            {
                if (Buffs.MalachiteBuildup) return;
                BuffDef buffDef = ScriptableObject.CreateInstance<BuffDef>();
                buffDef.isDebuff = true;
                buffDef.isHidden = false;
                buffDef.isDOT = false;
                buffDef.isCooldown = false;
                buffDef.canStack = true;
                buffDef.buffColor = new Color(0.3f, 0.3f, 0.3f);
                buffDef.iconSprite = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/ElitePoison/texBuffHealingDisabledIcon.tif").WaitForCompletion();
                (buffDef as ScriptableObject).name = "MoffeinEliteReworks_MalachiteBuildup";
                PluginContentPack.buffDefs.Add(buffDef);
                Buffs.MalachiteBuildup = buffDef;

                On.RoR2.HealthComponent.Heal += ReduceHealing;
                On.RoR2.CharacterBody.AddTimedBuff_BuffDef_float += MalachiteBuildupBehavior;
            }

            private static float ReduceHealing(On.RoR2.HealthComponent.orig_Heal orig, HealthComponent self, float amount, ProcChainMask procChainMask, bool nonRegen)
            {
                if (self.body)
                {
                    int count = self.body.GetBuffCount(Buffs.MalachiteBuildup);
                    amount *= 1f - Mathf.Min(1f, 0.2f * count);
                }
                return orig(self, amount, procChainMask, nonRegen);
            }

            private static void MalachiteBuildupBehavior(On.RoR2.CharacterBody.orig_AddTimedBuff_BuffDef_float orig, CharacterBody self, BuffDef buffDef, float duration)
            {
                if (buffDef == Buffs.MalachiteBuildup)
                {
                    //If already HealingDisabled, extend it and make sure there are no remaining MalachiteBuildup
                    if (self.HasBuff(RoR2Content.Buffs.HealingDisabled))
                    {
                        self.ClearTimedBuffs(Buffs.MalachiteBuildup);
                        orig(self, RoR2Content.Buffs.HealingDisabled, duration);
                        return;
                    }

                    int currentBuffCount = self.GetBuffCount(Buffs.MalachiteBuildup);
                    if (currentBuffCount < 4)
                    {
                        //Refresh stack duration when building up
                        if (currentBuffCount > 0)
                        {
                            foreach(TimedBuff t in self.timedBuffs)
                            {
                                if (t.buffIndex != Buffs.MalachiteBuildup.buffIndex) continue;
                                if (t.timer < duration) t.timer = duration;
                            }
                        }
                    }
                    else
                    {
                        //Convert to real HealingDisabled at 5 stacks
                        self.ClearTimedBuffs(Buffs.MalachiteBuildup);
                        orig(self, RoR2Content.Buffs.HealingDisabled, duration);
                        return;
                    }
                }
                else if (buffDef == RoR2Content.Buffs.HealingDisabled)
                {
                    self.ClearTimedBuffs(Buffs.MalachiteBuildup);
                }

                orig(self, buffDef, duration);
            }

            private static void CreateAntiHealAuraIndicator()
            {
                if (NetworkObjects.AntiHealAuraIndicator) return;

                GameObject indicator = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/NearbyDamageBonus/NearbyDamageBonusIndicator.prefab").WaitForCompletion().InstantiateClone("MoffeinEliteReworks_PoisonIndicator", true);
                indicator.transform.localScale = AntiHealAuraServer.wardRadius / 12.8f * Vector3.one;
                PrefabAPI.RegisterNetworkPrefab(indicator);

                NetworkObjects.AntiHealAuraIndicator = indicator;
            }
        }
    }
}
