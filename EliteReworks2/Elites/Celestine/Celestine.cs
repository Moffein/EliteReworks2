using BepInEx.Configuration;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace EliteReworks2.Elites.Celestine
{
    public class Celestine : TweakBase<Celestine>
    {
        public override string ConfigCategoryString => "T2 - Celestine";

        public override string ConfigOptionName => "Enable Module";

        public override string ConfigDescriptionString => "Enable changes related to Celestine Elites.";

        public static float healthBoostCoefficient;
        public static float damageBoostCoefficient;

        public static bool disableBubble;

        protected override void ReadConfig(ConfigFile config)
        {
            base.ReadConfig(config);
            healthBoostCoefficient = config.Bind<float>(new ConfigDefinition(ConfigCategoryString, "Stats - Health Multiplier"), 16f, new ConfigDescription("Health multiplier for this Elite Type. (Vanilla = 18)")).Value;
            damageBoostCoefficient = config.Bind<float>(new ConfigDefinition(ConfigCategoryString, "Stats - Damage Multiplier"), 4f, new ConfigDescription("Damage multiplier for this Elite Type. (Vanilla = 6)")).Value;
        }

        protected override void ApplyChanges()
        {
            base.ApplyChanges();
            ModifyStats();
            if (!disableBubble)
            {
                IL.RoR2.CharacterBody.AffixHauntedBehavior.FixedUpdate += DisableBubbleOnStun;
            }
        }

        private void ModifyStats()
        {
            EliteDef eliteDef = Addressables.LoadAssetAsync<EliteDef>("RoR2/Base/EliteHaunted/edHaunted.asset").WaitForCompletion();
            eliteDef.healthBoostCoefficient = healthBoostCoefficient;
            eliteDef.damageBoostCoefficient = damageBoostCoefficient;
        }

        private void DisableBubbleOnStun(MonoMod.Cil.ILContext il)
        {
            ILCursor c = new ILCursor(il);
            if (c.TryGotoNext(MoveType.After, x => x.MatchLdfld(typeof(CharacterBody.ItemBehavior), "stack")))
            {
                c.Emit(OpCodes.Ldarg_0);    //self
                c.EmitDelegate<Func<int, CharacterBody.AffixHauntedBehavior, int>>((stack, self) =>
                {
                    if (self.body && self.body.HasBuff(Common.Buffs.DisablePassiveEffect))
                    {
                        return 0;
                    }
                    return stack;
                });
            }
            else
            {
                Debug.LogError("EliteReworks: Celestine DisableBubbleOnStun IL hook failed.");
            }
        }

    }
}
