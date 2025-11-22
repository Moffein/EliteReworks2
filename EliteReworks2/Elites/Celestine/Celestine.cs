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

        public static bool useRework;
        public static bool makeBubbleClearer = true;

        protected override void ReadConfig(ConfigFile config)
        {
            base.ReadConfig(config);
            healthBoostCoefficient = config.Bind<float>(new ConfigDefinition(ConfigCategoryString, "Stats - Health Multiplier"), 16f, new ConfigDescription("Health multiplier for this Elite Type. (Vanilla = 18)")).Value;
            damageBoostCoefficient = config.Bind<float>(new ConfigDefinition(ConfigCategoryString, "Stats - Damage Multiplier"), 4f, new ConfigDescription("Damage multiplier for this Elite Type. (Vanilla = 6)")).Value;
            makeBubbleClearer = config.Bind<bool>(new ConfigDefinition(ConfigCategoryString, "Less Obstructive Bubble"), true, new ConfigDescription("Celestine Bubble only shows indicator visuals on the ground.")).Value;
        }

        protected override void ApplyChanges()
        {
            base.ApplyChanges();
            ModifyStats();
            Assets.Init();
            if (!useRework)
            {
                IL.RoR2.CharacterBody.AffixHauntedBehavior.FixedUpdate += DisableBubbleOnStun;
                MakeBubbleClearer();
            }
            else
            {
                On.RoR2.CharacterBody.AffixHauntedBehavior.FixedUpdate += PreventBubbleFromEverSpawning;
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

        private void MakeBubbleClearer()
        {
            if (!makeBubbleClearer) return;
            GameObject ward = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/EliteHaunted/AffixHauntedWard.prefab").WaitForCompletion();
            MeshRenderer[] renderers = ward.GetComponentsInChildren<MeshRenderer>();
            foreach (MeshRenderer mr in renderers)
            {
                if (mr.gameObject.name.Equals("IndicatorSphere"))
                {
                    mr.material = Assets.Materials.CelestineIndicatorNoBubble;
                    break;
                }
            }
        }

        private void PreventBubbleFromEverSpawning(On.RoR2.CharacterBody.AffixHauntedBehavior.orig_FixedUpdate orig, CharacterBody.AffixHauntedBehavior self)
        {
            return;
        }

        public static class Assets
        {
            public static class Materials
            {
                public static Material CelestineIndicatorNoBubble;
            }

            internal static void Init()
            {
                CreateCelestineIndicatorNoBubble();
            }

            private static void CreateCelestineIndicatorNoBubble()
            {
                if (Materials.CelestineIndicatorNoBubble) return;
                Material original = Addressables.LoadAssetAsync<Material>("RoR2/Base/EliteHaunted/matHauntedEliteAreaIndicator.mat").WaitForCompletion();

                Material mat = new Material(original);
                mat.SetFloat("_RimStrength", 0f);
                Materials.CelestineIndicatorNoBubble = mat;
            }
        }
    }
}
