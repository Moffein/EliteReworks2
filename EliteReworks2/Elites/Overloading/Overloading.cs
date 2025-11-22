using BepInEx.Configuration;
using EliteReworks2.Elites.Overloading.Components;
using EliteReworks2.Modules;
using MonoMod.Cil;
using R2API;
using RoR2;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace EliteReworks2.Elites.Overloading
{
    public class Overloading : TweakBase<Overloading>
    {
        public override string ConfigCategoryString => "T1 - Overloading";

        public override string ConfigOptionName => "Enable Module";

        public override string ConfigDescriptionString => "Enable changes related to Overloading Elites.";

        public static float zapDamage;
        public static float zapDamageBoss;

        public static float healthBoostCoefficient;
        public static float damageBoostCoefficient;

        public static float healthBoostCoefficientHonor;
        public static float damageBoostCoefficientHonor;

        public static bool reworkOnHit;
        public static bool removeShield;
        public static bool reduceShield;
        public static bool passiveLightning;

        protected override void ReadConfig(ConfigFile config)
        {
            base.ReadConfig(config);
            healthBoostCoefficient = config.Bind<float>(new ConfigDefinition(ConfigCategoryString, "Stats - Health Multiplier"), 3f, new ConfigDescription("Health multiplier for this Elite Type. (Vanilla = 4)")).Value;
            damageBoostCoefficient = config.Bind<float>(new ConfigDefinition(ConfigCategoryString, "Stats - Damage Multiplier"), 1.5f, new ConfigDescription("Damage multiplier for this Elite Type. (Vanilla = 2)")).Value;

            healthBoostCoefficientHonor = config.Bind<float>(new ConfigDefinition(ConfigCategoryString, "Stats (Honor) - Health Multiplier"), 2.5f, new ConfigDescription("Health multiplier for this Elite Type when Honor is enabled. (Vanilla = 2.5)")).Value;
            damageBoostCoefficientHonor = config.Bind<float>(new ConfigDefinition(ConfigCategoryString, "Stats (Honor) - Damage Multiplier"), 1.5f, new ConfigDescription("Damage multiplier for this Elite Type when Honor is enabled. (Vanilla = 1.5)")).Value;
        
            reworkOnHit = config.Bind<bool>(new ConfigDefinition(ConfigCategoryString, "Bigger Overloading Bombs"), true, new ConfigDescription("Overloading bombs have a much larger blast radius.")).Value;
            removeShield = config.Bind<bool>(new ConfigDefinition(ConfigCategoryString, "Shield - Remove Shield"), true, new ConfigDescription("Removes passive shield.")).Value;
            reduceShield = config.Bind<bool>(new ConfigDefinition(ConfigCategoryString, "Shield - Reduce Shield"), false, new ConfigDescription("Requires Remove Shield = false. Reduces passive shield from 50% to 25%.")).Value;
        
            passiveLightning = config.Bind<bool>(new ConfigDefinition(ConfigCategoryString, "Passive Lightning"), true, new ConfigDescription("Overloading Elites periodically fire lightning bombs around themselves.")).Value;
        }

        protected override void ApplyChanges()
        {
            base.ApplyChanges();
            ModifyStats();
            Assets.Init();
            ReworkOnHitEffect();
            ModifyShield();
            PassiveLightning();
        }

        private void ModifyStats()
        {
            EliteDef eliteDef = Addressables.LoadAssetAsync<EliteDef>("RoR2/Base/EliteLightning/edLightning.asset").WaitForCompletion();
            eliteDef.healthBoostCoefficient = healthBoostCoefficient;
            eliteDef.damageBoostCoefficient = damageBoostCoefficient;

            EliteDef eliteDefHonor = Addressables.LoadAssetAsync<EliteDef>("RoR2/Base/EliteLightning/edLightningHonor.asset").WaitForCompletion();
            eliteDefHonor.healthBoostCoefficient = healthBoostCoefficientHonor;
            eliteDefHonor.damageBoostCoefficient = damageBoostCoefficientHonor;
        }

        #region onhit
        private void ReworkOnHitEffect()
        {
            if (!reworkOnHit) return;
            IL.RoR2.GlobalEventManager.OnHitAllProcess += RemoveVanillaOnHit;
            On.RoR2.GlobalEventManager.OnHitAllProcess += BiggerLightningBombOnHit;
        }

        private void RemoveVanillaOnHit(MonoMod.Cil.ILContext il)
        {
            ILCursor c = new ILCursor(il);
            if (c.TryGotoNext(MoveType.After,
                 x => x.MatchLdsfld(typeof(RoR2Content.Buffs), "AffixBlue"),
                 x => x.MatchCallvirt<CharacterBody>("HasBuff")
                ))
            {
                c.EmitDelegate<Func<bool, bool>>(orig => false);
            }
            else
            {
                Debug.LogError("EliteReworks: AffixBlue RemoveVanillaOverloadingOnHit IL hook failed.");
            }
        }

        private void BiggerLightningBombOnHit(On.RoR2.GlobalEventManager.orig_OnHitAllProcess orig, GlobalEventManager self, DamageInfo damageInfo, GameObject hitObject)
        {
            orig(self, damageInfo, hitObject);
            if (NetworkServer.active && damageInfo.procCoefficient > 0f && damageInfo.attacker)
            {
                CharacterBody attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
                if (attackerBody && attackerBody.HasBuff(RoR2Content.Buffs.AffixBlue))
                {
                    ProjectileManager.instance.FireProjectile(Assets.Projectiles.OnHitLightningBomb, damageInfo.position, Quaternion.identity, damageInfo.attacker, damageInfo.damage * 0.5f, 0f, damageInfo.crit, DamageColorIndex.Item, null, -1f);
                }
            }
        }
        #endregion

        #region shield
        private void ModifyShield()
        {
            if (!removeShield && !reduceShield) return;
            if (removeShield)
            {
                IL.RoR2.CharacterBody.RecalculateStats += RemoveShields;
                return;
            }
            IL.RoR2.CharacterBody.RecalculateStats += ReduceShields;
        }

        private static void RemoveShields(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            if (c.TryGotoNext(MoveType.After,
                 x => x.MatchLdsfld(typeof(RoR2Content.Buffs), "AffixBlue"),
                 x => x.MatchCall<CharacterBody>("HasBuff")
                ))
            {
                c.EmitDelegate<Func<bool, bool>>(orig => false);
            }
            else
            {
                Debug.LogError("EliteReworks: AffixBlue RemoveShields IL hook failed.");
            }
        }

        private static void ReduceShields(ILContext il)
        {
            bool error = true;
            ILCursor c = new ILCursor(il);
            if (c.TryGotoNext(
                 x => x.MatchLdsfld(typeof(RoR2Content.Buffs), "AffixBlue")
                ) &&
                c.TryGotoNext(MoveType.After, x => x.MatchLdcR4(0.5f)))
            {
                //Lower health reduction
                c.EmitDelegate<Func<float, float>>(orig => orig * 0.5f);

                if (c.TryGotoNext(
                x => x.MatchCall(typeof(RoR2.CharacterBody), "get_maxHealth"),
                x => x.MatchAdd()))
                {
                    c.Index++;
                    c.EmitDelegate<Func<float, float>>(shieldToAdd =>
                    {
                        return shieldToAdd / 3f;
                    });
                    error = false;
                }
            }

            if (error) Debug.LogError("EliteReworks: AffixBlue ReduceShields IL hook failed.");
        }
        #endregion

        #region passive lightning
        private void PassiveLightning()
        {
            if (!passiveLightning) return;
            On.RoR2.CharacterBody.OnClientBuffsChanged += CharacterBody_OnClientBuffsChanged;
        }

        private void CharacterBody_OnClientBuffsChanged(On.RoR2.CharacterBody.orig_OnClientBuffsChanged orig, CharacterBody self)
        {
            orig(self);
            if (self.HasBuff(RoR2Content.Buffs.AffixBlue))
            {
                var component = self.GetComponent<PassiveLightningComponent>();
                if (!component)
                {
                    component = self.gameObject.AddComponent<PassiveLightningComponent>();
                    component.characterBody = self;
                }
            }
        }
        #endregion

        public static class Assets
        {
            public static class Effects
            {
                public static GameObject LightningQuiet;
                public static GameObject LightningOnHit;
            }

            public static class Projectiles
            {
                public static GameObject OnHitLightningBomb;
                public static GameObject PassiveLightningBomb;
                public static GameObject PassiveLightningBombBoss;
            }

            public static class NetworkSoundEvents
            {
                public static NetworkSoundEventDef PassiveTrigger;
                public static NetworkSoundEventDef PassiveTriggerBoss;
            }

            internal static void Init()
            {
                if (!NetworkSoundEvents.PassiveTrigger) NetworkSoundEvents.PassiveTrigger = EliteReworks2Utils.BuildNetworkSound("Play_EliteReworks_Lightning");
                if (!NetworkSoundEvents.PassiveTriggerBoss) NetworkSoundEvents.PassiveTriggerBoss = EliteReworks2Utils.BuildNetworkSound("Play_titanboss_shift_shoot");
                BuildLightningEffectQuiet();
                BuildLightningEffectOnHit();
                BuildPassiveLightningBomb();
                BuildPassiveLightningBombBoss();
                BuildOnHitLightningBomb();
            }

            private static void BuildLightningEffectQuiet()
            {
                if (Effects.LightningQuiet) return;
                GameObject effectPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/EliteLightning/LightningStakeNova.prefab").WaitForCompletion().InstantiateClone("MoffeinEliteReworks_OverloadingLightningQuiteEffect", false);
                EffectComponent ec = effectPrefab.GetComponent<EffectComponent>();
                ec.soundName = "Play_item_proc_chain_lightning";
                PluginContentPack.effectDefs.Add(new EffectDef(effectPrefab));
                Effects.LightningQuiet = effectPrefab;
            }

            private static void BuildLightningEffectOnHit()
            {
                if (Effects.LightningOnHit) return;
                GameObject effectPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/EliteLightning/LightningStakeNova.prefab").WaitForCompletion().InstantiateClone("MoffeinEliteReworks_OverloadingLightningOnHitEffect", false);
                EffectComponent ec = effectPrefab.GetComponent<EffectComponent>();
                ec.soundName = "Play_mage_m1_impact_lightning";
                PluginContentPack.effectDefs.Add(new EffectDef(effectPrefab));
                Effects.LightningOnHit = effectPrefab;
            }

            private static void BuildPassiveLightningBomb()
            {
                if (Projectiles.PassiveLightningBomb) return;
                GameObject projectile = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/ElectricWorm/ElectricWormSeekerProjectile.prefab").WaitForCompletion().InstantiateClone("MoffeinEliteReworks_OverloadinPassiveProjectile", true);

                ProjectileController pc = projectile.GetComponent<ProjectileController>();
                pc.procCoefficient = 0f;

                AkEvent[] ae = projectile.GetComponentsInChildren<AkEvent>();
                foreach (AkEvent a in ae)
                {
                    UnityEngine.Object.Destroy(a);
                }

                UnityEngine.Object.Destroy(projectile.GetComponent<AkGameObj>());

                ProjectileImpactExplosion pie = projectile.GetComponent<ProjectileImpactExplosion>();
                pie.blastProcCoefficient = 0f;
                pie.blastRadius = 5f;
                pie.destroyOnEnemy = false;
                pie.blastAttackerFiltering = AttackerFiltering.NeverHitSelf;
                pie.falloffModel = BlastAttack.FalloffModel.None;
                pie.impactEffect = Effects.LightningQuiet;

                PluginContentPack.projectilePrefabs.Add(projectile);
                Projectiles.PassiveLightningBomb = projectile;
            }

            private static void BuildPassiveLightningBombBoss()
            {
                if (Projectiles.PassiveLightningBombBoss) return;
                GameObject projectile = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/ElectricWorm/ElectricOrbProjectile.prefab").WaitForCompletion().InstantiateClone("MoffeinEliteReworks_OverloadinPassiveBossProjectile", true);

                ProjectileController pc = projectile.GetComponent<ProjectileController>();
                pc.procCoefficient = 0f;

                AkEvent[] ae = projectile.GetComponentsInChildren<AkEvent>();
                foreach (AkEvent a in ae)
                {
                    UnityEngine.Object.Destroy(a);
                }

                UnityEngine.Object.Destroy(projectile.GetComponent<AkGameObj>());

                ProjectileImpactExplosion pie = projectile.GetComponent<ProjectileImpactExplosion>();
                pie.blastProcCoefficient = 0f;
                pie.blastRadius = 7f;
                //pie.explosionEffect = BuildLightningEffect();
                //pie.impactEffect = BuildLightningEffect();
                pie.destroyOnEnemy = false;
                pie.blastAttackerFiltering = AttackerFiltering.NeverHitSelf;
                pie.falloffModel = BlastAttack.FalloffModel.None;
                pie.fireChildren = false;

                PluginContentPack.projectilePrefabs.Add(projectile);
                Projectiles.PassiveLightningBombBoss = projectile;
            }

            private static void BuildOnHitLightningBomb()
            {
                GameObject projectile = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/EliteLightning/LightningStake.prefab").WaitForCompletion().InstantiateClone("MoffeinEliteReworks_OverloadingStakeProjectile", true);
                ProjectileImpactExplosion pie = projectile.GetComponent<ProjectileImpactExplosion>();
                pie.blastRadius = 7f;
                pie.falloffModel = BlastAttack.FalloffModel.None;
                pie.impactEffect = Effects.LightningOnHit;

                PluginContentPack.projectilePrefabs.Add(projectile);
                Projectiles.OnHitLightningBomb = projectile;
            }
        }
    }
}
