using BepInEx.Configuration;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using RoR2.Audio;
using RoR2.Orbs;
using RoR2.Projectile;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace EliteReworks2.Elites.Gilded
{
    public class Gilded : TweakBase<Gilded>
    {
        public override string ConfigCategoryString => "T1 - Gilded";

        public override string ConfigOptionName => "Enable Module";

        public override string ConfigDescriptionString => "Enable changes related to Voidtouched Elites.";

        public static float healthBoostCoefficient;
        public static float damageBoostCoefficient;

        public static float healthBoostCoefficientHonor;
        public static float damageBoostCoefficientHonor;

        public static bool directSiphon;
        public static bool onlyKnockoutGoldFromPlayers;

        public static float playerDamageCoefficient = 2f;
        public static float passiveDamage = 24f;    //T1 Elite Lemurian
        public static float passiveDamageBoss = 32f;    //T1 Elite Imp Overlord

        protected override void ReadConfig(ConfigFile config)
        {
            base.ReadConfig(config);
            healthBoostCoefficient = config.Bind<float>(new ConfigDefinition(ConfigCategoryString, "Stats - Health Multiplier"), 4f, new ConfigDescription("Health multiplier for this Elite Type. (Vanilla = 5)")).Value;
            damageBoostCoefficient = config.Bind<float>(new ConfigDefinition(ConfigCategoryString, "Stats - Damage Multiplier"), 2f, new ConfigDescription("Damage multiplier for this Elite Type. (Vanilla = 2.5)")).Value;

            healthBoostCoefficientHonor = config.Bind<float>(new ConfigDefinition(ConfigCategoryString, "Stats (Honor) - Health Multiplier"), 3.5f, new ConfigDescription("Health multiplier for this Elite Type when Honor is enabled. (Vanilla = 3.5)")).Value;
            damageBoostCoefficientHonor = config.Bind<float>(new ConfigDefinition(ConfigCategoryString, "Stats (Honor) - Damage Multiplier"), 2f, new ConfigDescription("Damage multiplier for this Elite Type when Honor is enabled. (Vanilla = 2)")).Value;
        
            directSiphon = config.Bind<bool>(new ConfigDefinition(ConfigCategoryString, "Directly Steal Gold"), true, new ConfigDescription("Directly steal gold from players instead of spawning gold chunks.")).Value;
            onlyKnockoutGoldFromPlayers = config.Bind<bool>(new ConfigDefinition(ConfigCategoryString, "Only Knockout Gold from Players"), true, new ConfigDescription("Passive Spikes only knock gold chunks out of players, or when the equipment is used by a player.")).Value;
        }

        protected override void ApplyChanges()
        {
            base.ApplyChanges();
            ModifyStats();
            IL.RoR2.AffixAurelioniteBehavior.FireAurelioniteAttack += NormalizePassiveDamage;

            if (directSiphon)
            {
                On.RoR2.AffixAurelioniteBehavior.StealMoneyWithFX += AffixAurelioniteBehavior_StealMoneyWithFX;
            }

            if (onlyKnockoutGoldFromPlayers)
            {
                On.RoR2.Projectile.ProjectileKnockOutGold.KnockGoldFromVictim += ProjectileKnockOutGold_KnockGoldFromVictim;
            }
        }

        private void ProjectileKnockOutGold_KnockGoldFromVictim(On.RoR2.Projectile.ProjectileKnockOutGold.orig_KnockGoldFromVictim orig, RoR2.Projectile.ProjectileKnockOutGold self, HurtBox hurtbox)
        {
            bool shouldKnock = hurtbox && hurtbox.healthComponent && hurtbox.healthComponent.body && hurtbox.healthComponent.body.isPlayerControlled;
            //Check if this is being fired by a player
            if (!shouldKnock)
            {
                ProjectileController pc = self.GetComponent<ProjectileController>();
                if (pc && pc.owner)
                {
                    CharacterBody body = pc.owner.GetComponent<CharacterBody>();
                    if (body && body.isPlayerControlled)
                    {
                        shouldKnock = true;
                    }
                }
            }

            if (shouldKnock) orig(self, hurtbox);
        }

        private void ModifyStats()
        {
            EliteDef eliteDef = Addressables.LoadAssetAsync<EliteDef>("RoR2/DLC2/Elites/EliteAurelionite/edAurelionite.asset").WaitForCompletion();
            eliteDef.healthBoostCoefficient = healthBoostCoefficient;
            eliteDef.damageBoostCoefficient = damageBoostCoefficient;

            EliteDef eliteDefHonor = Addressables.LoadAssetAsync<EliteDef>("RoR2/DLC2/Elites/EliteAurelionite/edAurelioniteHonor.asset").WaitForCompletion();
            eliteDefHonor.healthBoostCoefficient = healthBoostCoefficientHonor;
            eliteDefHonor.damageBoostCoefficient = damageBoostCoefficientHonor;
        }

        private static void NormalizePassiveDamage(ILContext il)
        {
            bool error = true;
            ILCursor c = new ILCursor(il);
            if (c.TryGotoNext(MoveType.After, x => x.MatchCallvirt<CharacterBody>("get_damage")))
            {
                c.Emit(OpCodes.Ldarg_2);    //Body
                c.EmitDelegate<Func<float, CharacterBody, float>>((damage, body) =>
                {
                    if (body.isPlayerControlled || (body.teamComponent && body.teamComponent.teamIndex == TeamIndex.Player))
                    {
                        return body.damage * playerDamageCoefficient;
                    }
                    return EliteReworks2Utils.GetAmbientLevelScaledDamage(body.isChampion ? passiveDamageBoss : passiveDamage);
                });

                if (c.TryGotoNext(MoveType.After, x => x.MatchCallvirt<CharacterBody>("get_damage")))
                {
                    c.Emit(OpCodes.Ldarg_2);    //Body
                    c.EmitDelegate<Func<float, CharacterBody, float>>((damage, body) =>
                    {
                        if (body.isPlayerControlled || (body.teamComponent && body.teamComponent.teamIndex == TeamIndex.Player))
                        {
                            return body.damage * playerDamageCoefficient;
                        }
                        return EliteReworks2Utils.GetAmbientLevelScaledDamage(body.isChampion ? passiveDamageBoss : passiveDamage);
                    });
                    error = false;
                }
            }

            if (error)
            {
                Debug.LogError("EliteReworks: AffixGilded IL hook failed.");
            }
        }

        private static void AffixAurelioniteBehavior_StealMoneyWithFX(On.RoR2.AffixAurelioniteBehavior.orig_StealMoneyWithFX orig, AffixAurelioniteBehavior self, Transform victimTransform, int numberOfNuggetsToSpawn, CharacterBody body)
        {
            /*for (int i = 0; i < numberOfNuggetsToSpawn; i++)
            {
                PowerUpDropletController.FirePowerUpDropletRandomly(AffixAurelioniteBehavior.moneyPackPrefab, TeamIndex.Player, body.corePosition + Vector3.up * 5f, AffixAurelioniteBehavior.minimumRandomVelocity, AffixAurelioniteBehavior.maximumRandomVelocity, null);
            }*/

            //Calculate gold to steal based on numberOfNuggetsToSpawn
            //Create gold orb based on that amount
            if (self.body.mainHurtBox)
            {
                GoldOrb goldOrb = new GoldOrb();
                goldOrb.origin = victimTransform.position;
                goldOrb.target = self.body.mainHurtBox;
                int moneyToGrant = numberOfNuggetsToSpawn * 8;
                goldOrb.goldAmount = (uint)Run.instance.GetDifficultyScaledCost(moneyToGrant);
                OrbManager.instance.AddOrb(goldOrb);
            }

            EntitySoundManager.EmitSoundServer(AffixAurelioniteBehavior.stealGoldSFX.index, victimTransform.gameObject);
            EffectData effectData = new EffectData();
            effectData.origin = victimTransform.position;
            EffectManager.SpawnEffect(AffixAurelioniteBehavior.staticCoinEffect, effectData, true);
        }
    }
}
