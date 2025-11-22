using BepInEx.Configuration;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RoR2;
using System;
using UnityEngine;
using UnityEngine.Networking;

namespace EliteReworks2.Elites.Voidtouched
{
    public class Voidtouched : TweakBase<Voidtouched>
    {
        public override string ConfigCategoryString => "Voidtouched";

        public override string ConfigOptionName => "Enable Module";

        public override string ConfigDescriptionString => "Enable changes related to Voidtouched Elites.";

        public static bool useRework;
        public static bool reworkBuffNullify;

        public float collapseBodyDamageBase = 12f*0.7f;  //Lemurian
        public float collapseBossBodyDamageBase = 16f * 0.7f;  //Imp Overlord

        protected override void ReadConfig(ConfigFile config)
        {
            base.ReadConfig(config);
            useRework = config.Bind<bool>(new ConfigDefinition(ConfigCategoryString, "Rework - Nullify on Hit"), true, new ConfigDescription("Attacks apply Nullify instead of Collapse.")).Value;
            reworkBuffNullify = config.Bind<bool>(new ConfigDefinition(ConfigCategoryString, "Rework - Buff Nullify"), true, new ConfigDescription("Requires Rework - Nullify on Hit. Nullify only takes 2 stacks to root.")).Value;
        }

        protected override void ApplyChanges()
        {
            base.ApplyChanges();
            if (useRework)
            {
                IL.RoR2.GlobalEventManager.ProcessHitEnemy += RemoveVanillaVoidEliteCollapse;
                if (reworkBuffNullify) On.RoR2.CharacterBody.AddTimedBuff_BuffDef_float += BuffNullify;
                On.RoR2.GlobalEventManager.ServerDamageDealt += GlobalEventManager_ServerDamageDealt;
                RecalculateStatsAPI.GetStatCoefficients += AdjustVanillaVoidEliteStats;
            }
            else
            {
                IL.RoR2.GlobalEventManager.ProcessHitEnemy += NormalizeCollapseDamage;
            }
        }

        private void GlobalEventManager_ServerDamageDealt(On.RoR2.GlobalEventManager.orig_ServerDamageDealt orig, DamageReport damageReport)
        {
            orig(damageReport);
            if (!damageReport.damageInfo.rejected && damageReport.damageInfo.procCoefficient > 0f
                && damageReport.attackerBody && damageReport.attackerBody.HasBuff(DLC1Content.Buffs.EliteVoid)
                 && damageReport.victimBody && !damageReport.victimBody.HasBuff(RoR2Content.Buffs.Nullified))
            {
                if (Util.CheckRoll(100f * damageReport.damageInfo.procCoefficient, damageReport.attackerBody.master))
                {
                    damageReport.victimBody.AddTimedBuff(RoR2Content.Buffs.NullifyStack.buffIndex, 8f * damageReport.damageInfo.procCoefficient);
                }
            }
        }

        private static void RemoveVanillaVoidEliteCollapse(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            if (c.TryGotoNext(MoveType.After,
                 x => x.MatchLdsfld(typeof(DLC1Content.Buffs), "EliteVoid"),
                 x => x.MatchCallvirt<CharacterBody>("HasBuff")
                ))
            {
                c.EmitDelegate<Func<bool, bool>>(orig => false);
            }
            else
            {
                Debug.LogError("EliteReworks: RemoveVanillaVoidEliteCollapse IL hook failed.");
            }
        }

        private static void BuffNullify(On.RoR2.CharacterBody.orig_AddTimedBuff_BuffDef_float orig, CharacterBody self, BuffDef buffDef, float duration)
        {
            orig(self, buffDef, duration);
            if (NetworkServer.active && buffDef == RoR2Content.Buffs.NullifyStack && !self.HasBuff(RoR2Content.Buffs.Nullified))
            {
                int nullifyCount = self.GetBuffCount(buffDef);
                if (nullifyCount >= 2)
                {
                    self.ClearTimedBuffs(buffDef);
                    self.AddTimedBuff(RoR2Content.Buffs.Nullified, 3f);
                }
            }
        }

        private static void AdjustVanillaVoidEliteStats(CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
        {
            if (sender.HasBuff(DLC1Content.Buffs.EliteVoid))
            {
                args.damageMultAdd += 1.3f; //2f total
            }
        }

        private void NormalizeCollapseDamage(MonoMod.Cil.ILContext il)
        {
            ILCursor c = new ILCursor(il);
            if (c.TryGotoNext(x => x.MatchLdsfld(typeof(DLC1Content.Buffs), "EliteVoid"))
                && c.TryGotoNext(MoveType.After, x => x.MatchLdcR4(1f)))
            {
                c.Emit(OpCodes.Ldarg_1);//damageInfo
                c.EmitDelegate<Func<float, DamageInfo, float>>((damageMult, damageInfo) =>
                {
                    //Normalize damage for NPCs
                    if (damageInfo.attacker)
                    {
                        CharacterBody attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
                        if (attackerBody)
                        {
                            if (attackerBody.isPlayerControlled || (attackerBody.teamComponent && attackerBody.teamComponent.teamIndex == TeamIndex.Player))
                            {
                                //Players just use the raw value
                                return damageMult;
                            }
                            else
                            {
                                //Enemies need to scale
                                float ambientDamageStat = EliteReworks2Utils.GetAmbientLevelScaledDamage(attackerBody.isChampion ? collapseBossBodyDamageBase : collapseBodyDamageBase);
                                return damageMult * (ambientDamageStat / attackerBody.damage);
                            }
                        }
                    }

                    //If no attacker, DoTController will just make it do 0 damage
                    return damageMult;
                });
            }
            else
            {
                Debug.LogError("EliteReworks: NormalizeCollapseDamage IL hook failed.");
            }
        }
    }
}
