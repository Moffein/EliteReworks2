using BepInEx.Configuration;
using EliteReworks2.Modules;
using HG;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RoR2;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace EliteReworks2.Elites.Glacial
{
    public class Glacial : TweakBase<Glacial>
    {
        public override string ConfigCategoryString => "T1 - Glacial";

        public override string ConfigOptionName => "Enable Module";

        public override string ConfigDescriptionString => "Enable changes related to Glacial Elites.";

        public static float healthBoostCoefficient;
        public static float damageBoostCoefficient;

        public static float healthBoostCoefficientHonor;
        public static float damageBoostCoefficientHonor;
        public static bool reworkOnHit;

        protected override void ReadConfig(ConfigFile config)
        {
            base.ReadConfig(config);
            healthBoostCoefficient = config.Bind<float>(new ConfigDefinition(ConfigCategoryString, "Stats - Health Multiplier"), 3f, new ConfigDescription("Health multiplier for this Elite Type. (Vanilla = 4)")).Value;
            damageBoostCoefficient = config.Bind<float>(new ConfigDefinition(ConfigCategoryString, "Stats - Damage Multiplier"), 1.5f, new ConfigDescription("Damage multiplier for this Elite Type. (Vanilla = 2)")).Value;

            healthBoostCoefficientHonor = config.Bind<float>(new ConfigDefinition(ConfigCategoryString, "Stats (Honor) - Health Multiplier"), 2.5f, new ConfigDescription("Health multiplier for this Elite Type when Honor is enabled. (Vanilla = 2.5)")).Value;
            damageBoostCoefficientHonor = config.Bind<float>(new ConfigDefinition(ConfigCategoryString, "Stats (Honor) - Damage Multiplier"), 1.5f, new ConfigDescription("Damage multiplier for this Elite Type when Honor is enabled. (Vanilla = 1.5)")).Value;

            reworkOnHit = config.Bind<bool>(new ConfigDefinition(ConfigCategoryString, "Frost Explosion on Hit"), true, new ConfigDescription("Glacial Elites create a slowing non-damaging frost explosion on hit.")).Value;
        }

        protected override void ApplyChanges()
        {
            base.ApplyChanges();
            ModifyStats();
            ReworkOnHitEffect();
            Assets.Init();
        }

        private void ModifyStats()
        {
            EliteDef eliteDef = Addressables.LoadAssetAsync<EliteDef>("RoR2/Base/EliteIce/edIce.asset").WaitForCompletion();
            eliteDef.healthBoostCoefficient = healthBoostCoefficient;
            eliteDef.damageBoostCoefficient = damageBoostCoefficient;

            EliteDef eliteDefHonor = Addressables.LoadAssetAsync<EliteDef>("RoR2/Base/EliteIce/edIceHonor.asset").WaitForCompletion();
            eliteDefHonor.healthBoostCoefficient = healthBoostCoefficientHonor;
            eliteDefHonor.damageBoostCoefficient = damageBoostCoefficientHonor;
        }

        private void ReworkOnHitEffect()
        {
            if (!reworkOnHit) return;
            IL.RoR2.GlobalEventManager.ProcessHitEnemy += RemoveVanillaOnHit;
            On.RoR2.GlobalEventManager.OnHitAll += FrostExplosionOnHit;
        }

        private void RemoveVanillaOnHit(MonoMod.Cil.ILContext il)
        {
            ILCursor c = new ILCursor(il);
            if (c.TryGotoNext(MoveType.After,
                 x => x.MatchLdsfld(typeof(RoR2Content.Buffs), "AffixWhite"),
                 x => x.MatchCallvirt<CharacterBody>("HasBuff")
                ))
            {
                c.EmitDelegate<Func<bool, bool>>(orig => false);
            }
            else
            {
                Debug.LogError("EliteReworks: AffixBlue RemoveVanillaOnHit IL hook failed.");
            }
        }

        private void FrostExplosionOnHit(On.RoR2.GlobalEventManager.orig_OnHitAll orig, GlobalEventManager self, DamageInfo damageInfo, GameObject hitObject)
        {
            orig(self, damageInfo, hitObject);
            if (!NetworkServer.active || !damageInfo.attacker) return;

            CharacterBody attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
            if (!attackerBody || !attackerBody.HasBuff(RoR2Content.Buffs.AffixWhite) || !attackerBody.teamComponent) return;

            float slowDuration = 1f + damageInfo.procCoefficient * 2f;
            EliteReworks2Utils.DebuffSphere(ModCompat.zetAspectsLoaded ? Assets.Buffs.Slow80Alt.buffIndex : RoR2Content.Buffs.Slow80.buffIndex, attackerBody.teamComponent.teamIndex,
                                damageInfo.position, 4f, slowDuration,
                                Assets.Effects.GlacialOnHitExplosion, null, false, true, Assets.NetworkSoundEvents.SlowApplied);
        }

        public static class Assets
        {
            public static class NetworkSoundEvents
            {
                public static NetworkSoundEventDef SlowApplied;
            }

            public static class Effects
            {
                public static GameObject GlacialOnHitExplosion;
            }

            public static class Buffs
            {
                public static BuffDef Slow80Alt;    //Used if ZetAspects is installed
            }

            internal static void Init()
            {
                if (!NetworkSoundEvents.SlowApplied) NetworkSoundEvents.SlowApplied = EliteReworks2Utils.BuildNetworkSound("Play_mage_m2_iceSpear_shoot");
                BuildGlacialOnHitExplosion();
                BuildSlow80Alt();
            }

            private static void BuildSlow80Alt()
            {
                if (Buffs.Slow80Alt) return;

                BuffDef orig = Addressables.LoadAssetAsync<BuffDef>("RoR2/Base/Common/bdSlow80.asset").WaitForCompletion();

                BuffDef buffDef = ScriptableObject.CreateInstance<BuffDef>();
                buffDef.isDebuff = true;
                buffDef.isHidden = false;
                buffDef.isDOT = false;
                buffDef.isCooldown = false;
                buffDef.iconSprite = orig.iconSprite;
                buffDef.buffColor = orig.buffColor;
                (buffDef as ScriptableObject).name = "MoffeinEliteReworks_Slow80Alt";
                PluginContentPack.buffDefs.Add(buffDef);
                Buffs.Slow80Alt = buffDef;

                RecalculateStatsAPI.GetStatCoefficients += Slow80AltStats;
                IL.RoR2.CharacterModel.UpdateOverlays += Slow80AltOverlay;
            }

            private static void Slow80AltStats(CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
            {
                if (sender.HasBuff(Buffs.Slow80Alt) && !sender.HasBuff(RoR2Content.Buffs.Slow80))
                {
                    args.moveSpeedReductionMultAdd += 0.8f;
                }
            }

            private static void Slow80AltOverlay(ILContext il)
            {
                ILCursor c = new ILCursor(il);
                if (c.TryGotoNext(
                     x => x.MatchLdsfld(typeof(RoR2Content.Buffs), "Slow80")
                    ))
                {
                    c.Index += 2;
                    c.Emit(OpCodes.Ldarg_0);
                    c.EmitDelegate<Func<bool, CharacterModel, bool>>((hasBuff, self) =>
                    {
                        return hasBuff || (self.body.HasBuff(Buffs.Slow80Alt));
                    });
                }
                else
                {
                    Debug.LogError("EliteReworks: Slow80AltOverlay IL hook failed.");
                }
            }

            private static void BuildGlacialOnHitExplosion()
            {
                if (Assets.Effects.GlacialOnHitExplosion) return;
                GameObject effect = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/ElementalRings/IceRingExplosion.prefab").WaitForCompletion().InstantiateClone("MoffeinEliteReworks_GlacialOnHitExplosionEffect", false);
                UnityEngine.Object.Destroy(effect.GetComponent<ShakeEmitter>());
                EffectComponent ec = effect.GetComponent<EffectComponent>();
                ec.soundName = "";
                ec.applyScale = false;

                ParticleSystemRenderer[] ps = effect.GetComponentsInChildren<ParticleSystemRenderer>();
                foreach (ParticleSystemRenderer p in ps)
                {
                    switch (p.name)
                    {
                        case "IceMesh":
                            UnityEngine.Object.Destroy(p);
                            break;
                        default:
                            break;
                    }
                }
                PluginContentPack.effectDefs.Add(new EffectDef(effect));
                Effects.GlacialOnHitExplosion = effect;
            }
        }
    }
}
