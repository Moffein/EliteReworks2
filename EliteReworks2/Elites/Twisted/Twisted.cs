using BepInEx.Configuration;
using EliteReworks2.Modules;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RoR2;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace EliteReworks2.Elites.Twisted
{
    public class Twisted : TweakBase<Twisted>
    {
        public override string ConfigCategoryString => "T2 - Twisted";

        public override string ConfigOptionName => "Enable Module";

        public override string ConfigDescriptionString => "Enable changes related to Twisted Elites.";

        public static float healthBoostCoefficient;
        public static float damageBoostCoefficient;

        public static float passiveBodyDamage = 48f;    //Lemurian with 4x damage
        public static float passiveBodyDamageBoss = 64f;    //Imp Overlord with 4x damage

        public static bool projectileIsUnshootable;

        protected override void ReadConfig(ConfigFile config)
        {
            base.ReadConfig(config);
            healthBoostCoefficient = config.Bind<float>(new ConfigDefinition(ConfigCategoryString, "Stats - Health Multiplier"), 16f, new ConfigDescription("Health multiplier for this Elite Type. (Vanilla = 18)")).Value;
            damageBoostCoefficient = config.Bind<float>(new ConfigDefinition(ConfigCategoryString, "Stats - Damage Multiplier"), 4f, new ConfigDescription("Damage multiplier for this Elite Type. (Vanilla = 6)")).Value;
            projectileIsUnshootable = config.Bind<bool>(new ConfigDefinition(ConfigCategoryString, "Projectile is Unshootable"), true, new ConfigDescription("Twisted projectile cannot be destroyed.")).Value;
        }

        protected override void ApplyChanges()
        {
            base.ApplyChanges();
            ModifyStats();
            Assets.Init();
            ModifyAttachment();
            IL.RoR2.AffixBeadAttachment.FireProjectile += NormalizeDamage;
            MakeProjectileUnshootable();
        }

        private void ModifyStats()
        {
            EliteDef eliteDef = Addressables.LoadAssetAsync<EliteDef>("RoR2/DLC2/Elites/EliteBead/edBead.asset").WaitForCompletion();
            eliteDef.healthBoostCoefficient = healthBoostCoefficient;
            eliteDef.damageBoostCoefficient = damageBoostCoefficient;
        }

        private void ModifyAttachment()
        {
            GameObject attachment = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC2/Elites/EliteBead/AffixBeadBodyAttachment.prefab").WaitForCompletion();
            AffixBeadAttachment aba = attachment.GetComponent<AffixBeadAttachment>();
            aba.damageCooldown = 0.01f; //Cooldown should only stop same-frame attacks, and not rapid hits.

            aba.fireDelay = 1.5f;   //vanilla is 2f
            aba.cooldownAfterFiring = 6f;   //vanilla is 10f
            aba.maxAllies = 10; //vanilla is 5
        }

        private void NormalizeDamage(MonoMod.Cil.ILContext il)
        {
            ILCursor c = new ILCursor(il);

            if (c.TryGotoNext(MoveType.After, x => x.MatchCallvirt<CharacterBody>("get_damage")))
            {
                c.Emit(OpCodes.Ldarg_0);    //Self
                c.EmitDelegate<Func<float, AffixBeadAttachment, float>>((damage, self) =>
                {
                    CharacterBody body = null;
                    if (self.networkedBodyAttachment) body = self.networkedBodyAttachment.attachedBody;

                    if (body)
                    {
                        //Player
                        if (body.isPlayerControlled || (body.teamComponent && body.teamComponent.teamIndex == TeamIndex.Player))
                        {
                            return damage;
                        }
                        else
                        {
                            return EliteReworks2Utils.GetAmbientLevelScaledDamage(body.isChampion ? passiveBodyDamageBoss : passiveBodyDamage);
                        }
                    }

                    //Fallback if No Body
                    return EliteReworks2Utils.GetAmbientLevelScaledDamage(passiveBodyDamage);
                });
            }
            else
            {
                Debug.LogError("EliteReworks: AffixBead NormalizeDamage Player IL hook failed.");
            }

            if (c.TryGotoNext(MoveType.After, x => x.MatchCallvirt<CharacterBody>("get_damage")))
            {
                c.Emit(OpCodes.Ldarg_0);    //Self
                c.EmitDelegate<Func<float, AffixBeadAttachment, float>>((damage, self) =>
                {
                    CharacterBody body = null;
                    if (self.networkedBodyAttachment) body = self.networkedBodyAttachment.attachedBody;

                    if (body)
                    {
                        //Player
                        if (body.isPlayerControlled || (body.teamComponent && body.teamComponent.teamIndex == TeamIndex.Player))
                        {
                            return damage;
                        }
                        else
                        {
                            return EliteReworks2Utils.GetAmbientLevelScaledDamage(body.isChampion ? passiveBodyDamageBoss : passiveBodyDamage);
                        }
                    }

                    //Fallback if No Body
                    return EliteReworks2Utils.GetAmbientLevelScaledDamage(passiveBodyDamage);
                });
            }
            else
            {
                Debug.LogError("EliteReworks: AffixBead NormalizeDamage IL hook failed.");
            }
        }

        private void MakeProjectileUnshootable()
        {
            if (!projectileIsUnshootable) return;
            GameObject attachment = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC2/Elites/EliteBead/AffixBeadBodyAttachment.prefab").WaitForCompletion();
            AffixBeadAttachment aba = attachment.GetComponent<AffixBeadAttachment>();
            aba.projectilePrefab = Assets.Projectiles.UnshootableEliteBeadProjectile;
        }

        public static class Assets
        {
            public static class Projectiles
            {
                public static GameObject UnshootableEliteBeadProjectile;
            }

            internal static void Init()
            {
                CreateUnshootableEliteBeadProjectile();
            }

            private static void CreateUnshootableEliteBeadProjectile()
            {
                if (Projectiles.UnshootableEliteBeadProjectile) return;

                GameObject projectile = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC2/Elites/EliteBead/BeadProjectileTrackingBomb.prefab").WaitForCompletion().InstantiateClone("MoffeinEliteReworks_UnshootableTwistedProjectile", true); ;

                HurtBox[] hurtboxes = projectile.GetComponentsInChildren<HurtBox>();
                foreach (HurtBox hb in hurtboxes)
                {
                    hb.enabled = false;
                }

                HealthComponent hc = projectile.GetComponent<HealthComponent>();
                hc.dontShowHealthbar = true;

                PluginContentPack.projectilePrefabs.Add(projectile);
                Assets.Projectiles.UnshootableEliteBeadProjectile = projectile;
            }
        }
    }
}
