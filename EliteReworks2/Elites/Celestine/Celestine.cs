using BepInEx.Configuration;
using EliteReworks2.Common.Components;
using EliteReworks2.Elites.Celestine.Components;
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
        public static bool makeBubbleClearer;

        protected override void ReadConfig(ConfigFile config)
        {
            base.ReadConfig(config);
            healthBoostCoefficient = config.Bind<float>(new ConfigDefinition(ConfigCategoryString, "Stats - Health Multiplier"), 16f, new ConfigDescription("Health multiplier for this Elite Type. (Vanilla = 18)")).Value;
            damageBoostCoefficient = config.Bind<float>(new ConfigDefinition(ConfigCategoryString, "Stats - Damage Multiplier"), 4f, new ConfigDescription("Damage multiplier for this Elite Type. (Vanilla = 6)")).Value;
            makeBubbleClearer = config.Bind<bool>(new ConfigDefinition(ConfigCategoryString, "Less Obstructive Bubble"), true, new ConfigDescription("Celestine Bubble only shows indicator visuals on the ground.")).Value;
            useRework = config.Bind<bool>(new ConfigDefinition(ConfigCategoryString, "Rework - Ghost Revive"), true, new ConfigDescription("Reworks Celestine Elites to revive fallen enemies as ghosts.")).Value;
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
                On.RoR2.CharacterBody.OnClientBuffsChanged += CharacterBody_OnClientBuffsChanged;
            }
        }

        private void CharacterBody_OnClientBuffsChanged(On.RoR2.CharacterBody.orig_OnClientBuffsChanged orig, CharacterBody self)
        {
            orig(self);
            if (self.HasBuff(RoR2Content.Buffs.AffixHaunted))
            {
                var component = self.GetComponent<CelestineReviveAura>();
                if (!component)
                {
                    component = self.gameObject.AddComponent<CelestineReviveAura>();
                    component.characterBody = self;
                }
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

        public static void AttemptSpawnGhost(CharacterBody sourceBody, Vector3 position, float radius)
        {
            if (!sourceBody || (sourceBody.bodyFlags & CharacterBody.BodyFlags.Masterless) == CharacterBody.BodyFlags.Masterless) return;

            float radiusSquare = radius * radius;
            float squareDist;

            if (sourceBody.teamComponent)
            {
                TeamIndex ti = sourceBody.teamComponent.teamIndex;
                System.Collections.ObjectModel.ReadOnlyCollection<TeamComponent> teamMembers = TeamComponent.GetTeamMembers(ti);
                if (teamMembers == null) return;
                foreach (TeamComponent tc in teamMembers)
                {
                    if (tc.body && !tc.body.bodyFlags.HasFlag(CharacterBody.BodyFlags.Masterless) && tc.body.HasBuff(RoR2Content.Buffs.AffixHaunted) && tc.body.healthComponent && tc.body.healthComponent.alive)
                    {
                        squareDist = (position - tc.body.corePosition).sqrMagnitude;
                        if (squareDist <= radiusSquare)
                        {
                            CelestineReviveAura ahr = tc.body.GetComponent<CelestineReviveAura>();
                            if (ahr && ahr.PassiveIsActive() && ahr.attachedGhosts.Count < ahr.GetMaxGhosts())
                            {
                                CharacterBody ghostBody = Util.TryToCreateGhost(sourceBody, tc.body, 60 + (tc.body.isChampion ? 30 : 0));
                                if (ghostBody)
                                {
                                    if (ghostBody.master && ghostBody.master.inventory)
                                    {
                                        int boostDamageCount = ghostBody.master.inventory.GetItemCountPermanent(RoR2Content.Items.BoostDamage);
                                        if (boostDamageCount > 0)
                                        {
                                            ghostBody.master.inventory.RemoveItemPermanent(RoR2Content.Items.BoostDamage, ghostBody.master.inventory.GetItemCountPermanent(RoR2Content.Items.BoostDamage));
                                        }
                                        ghostBody.master.inventory.GiveItemPermanent(RoR2Content.Items.BoostDamage, 5);
                                        ghostBody.master.inventory.SetEquipmentIndex(EquipmentIndex.None, true);
                                    }
                                    ghostBody.AddBuff(Assets.Buffs.CelestineReviveBuff);
                                    ahr.attachedGhosts.Add(ghostBody);
                                }
                                break;
                            }
                        }
                    }
                }
            }
        }

        public static class Assets
        {
            public static class NonCatalogEffects
            {
                public static GameObject CelestineMarkerGreen;
                public static GameObject CelestineMarkerRed;
            }

            public static class Effects
            {
                public static GameObject CelestineOrb;
            }

            public static class Materials
            {
                public static Material CelestineIndicatorNoBubble;
                public static Material CelestineBuffMarkerMat;
                public static Material CelestineOwnerMarkerMat;
            }

            public static class Buffs
            {
                public static BuffDef CelestineReviveBuff;
            }

            internal static void Init()
            {
                CreateCelestineIndicatorNoBubble();
                CreateCelestineMarkerGreenMat();
                CreateCelestineMarkerRedMat();
                CreateCelestineOrbEffect();

                CreateCelestineMarkerGreen();
                CreateCelestineMarkerRed();
                CreateReviveBuff();
                On.RoR2.CharacterBody.UpdateAllTemporaryVisualEffects += CharacterBody_UpdateAllTemporaryVisualEffects;
            }

            private static void CreateCelestineOrbEffect()
            {
                GameObject effect = Addressables.LoadAssetAsync<GameObject>("RoR2/Junk/EliteHaunted/HauntOrbEffect.prefab").WaitForCompletion().InstantiateClone("MoffeinEliteReworks_CelestineOrbEffect", false);

                effect.GetComponent<EffectComponent>().applyScale = true;
                AkEvent[] events = effect.GetComponentsInChildren<AkEvent>();
                foreach (var ev in events)
                {
                    UnityEngine.Object.Destroy(ev);
                }
                AkGameObj[] gos = effect.GetComponentsInChildren<AkGameObj>();
                foreach (var go in gos)
                {
                    UnityEngine.Object.Destroy(go);
                }

                //effect.transform.Find("VFX").localScale = 0.5f * Vector3.one;

                PluginContentPack.effectDefs.Add(new EffectDef(effect));
                Effects.CelestineOrb = effect;
            }

            private static void CharacterBody_UpdateAllTemporaryVisualEffects(On.RoR2.CharacterBody.orig_UpdateAllTemporaryVisualEffects orig, CharacterBody self)
            {
                orig(self);

                var er = self.GetComponent<EliteReworksTemporaryVisualEffectInstances>();
                if (!er) er = self.gameObject.AddComponent<EliteReworksTemporaryVisualEffectInstances>();

                bool hasReviveBuff = self.HasBuff(Assets.Buffs.CelestineReviveBuff);
                if (!hasReviveBuff)
                {
                    self.UpdateSingleTemporaryVisualEffect(ref er.celestineBuffMarkerInstance, Assets.NonCatalogEffects.CelestineMarkerRed, self.radius, false);
                    self.UpdateSingleTemporaryVisualEffect(ref er.celestineOwnerMarkerInstance, Assets.NonCatalogEffects.CelestineMarkerGreen, self.radius, false);
                }
                else
                {
                    bool isGhost = self.inventory && self.inventory.GetItemCountPermanent(RoR2Content.Items.Ghost) > 0;
                    self.UpdateSingleTemporaryVisualEffect(ref er.celestineBuffMarkerInstance, Assets.NonCatalogEffects.CelestineMarkerRed, self.radius, isGhost);
                    self.UpdateSingleTemporaryVisualEffect(ref er.celestineOwnerMarkerInstance, Assets.NonCatalogEffects.CelestineMarkerGreen, self.radius, !isGhost);
                }
            }

            private static void CreateCelestineMarkerGreenMat()
            {
                if (Materials.CelestineBuffMarkerMat) return;
                Material original = Addressables.LoadAssetAsync<Material>("RoR2/Base/WardOnLevel/matWarbannerBuffBillboard.mat").WaitForCompletion();

                Material mat = new Material(original);
                mat.SetColor("_TintColor", new Color32(0, 200, 140, 255));

                Materials.CelestineBuffMarkerMat = mat;
            }

            private static void CreateCelestineMarkerRedMat()
            {
                if (Materials.CelestineOwnerMarkerMat) return;
                Material original = Addressables.LoadAssetAsync<Material>("RoR2/Base/WardOnLevel/matWarbannerBuffBillboard.mat").WaitForCompletion();

                Material mat = new Material(original);
                mat.SetColor("_TintColor", new Color32(200, 0, 40, 255));

                Materials.CelestineOwnerMarkerMat = mat;
            }

            private static void CreateCelestineIndicatorNoBubble()
            {
                if (Materials.CelestineIndicatorNoBubble) return;
                Material original = Addressables.LoadAssetAsync<Material>("RoR2/Base/EliteHaunted/matHauntedEliteAreaIndicator.mat").WaitForCompletion();

                Material mat = new Material(original);
                mat.SetFloat("_RimStrength", 0f);
                Materials.CelestineIndicatorNoBubble = mat;
            }

            private static void CreateCelestineMarkerGreen()
            {
                if (NonCatalogEffects.CelestineMarkerGreen) return;
                GameObject effect = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/WardOnLevel/WarbannerBuffEffect.prefab").WaitForCompletion().InstantiateClone("MoffeinEliteReworks_CelestineBuffEffect", false);

                Transform visual = effect.transform.Find("Visual");
                Transform flare = visual.Find("FlarePerst_Ps (1)");
                {
                    ParticleSystem ps = flare.GetComponent<ParticleSystem>();
                    var main = ps.main;
                    main.startColor = new ParticleSystem.MinMaxGradient(new Color32(0, 188, 131, 255), new Color32(102, 255, 179, 255));
                }
                Transform softGlow = visual.Find("SoftGlow");
                {
                    ParticleSystem ps = softGlow.GetComponent<ParticleSystem>();
                    ps.startColor = new Color32(0, 200, 140, 93);
                }
                Transform pulse = visual.Find("PulseEffect, Ring");
                {
                    ParticleSystemRenderer psr = pulse.GetComponent<ParticleSystemRenderer>();
                    psr.material = Materials.CelestineBuffMarkerMat;
                }

                //PluginContentPack.effectDefs.Add(new EffectDef(effect));
                NonCatalogEffects.CelestineMarkerGreen = effect;
            }

            private static void CreateCelestineMarkerRed()
            {
                if (NonCatalogEffects.CelestineMarkerRed) return;
                GameObject effect = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/WardOnLevel/WarbannerBuffEffect.prefab").WaitForCompletion().InstantiateClone("MoffeinEliteReworks_CelestineOwnerEffect", false);

                Transform visual = effect.transform.Find("Visual");
                Transform flare = visual.Find("FlarePerst_Ps (1)");
                {
                    ParticleSystem ps = flare.GetComponent<ParticleSystem>();
                    var main = ps.main;
                    main.startColor = new ParticleSystem.MinMaxGradient(new Color32(188, 0, 38, 255), new Color32(255, 90, 179, 255));
                }
                Transform softGlow = visual.Find("SoftGlow");
                {
                    ParticleSystem ps = softGlow.GetComponent<ParticleSystem>();
                    ps.startColor = new Color32(200, 0, 40, 93);
                }
                Transform pulse = visual.Find("PulseEffect, Ring");
                {
                    ParticleSystemRenderer psr = pulse.GetComponent<ParticleSystemRenderer>();
                    psr.material = Materials.CelestineOwnerMarkerMat;
                }

                //PluginContentPack.effectDefs.Add(new EffectDef(effect));
                NonCatalogEffects.CelestineMarkerRed = effect;
            }

            #region revive
            private static void CreateReviveBuff()
            {
                if (Buffs.CelestineReviveBuff) return;

                BuffDef buff = ScriptableObject.CreateInstance<BuffDef>();
                buff.buffColor = new Color(157f / 255f, 221f / 255f, 216f / 255f);
                buff.canStack = false;
                buff.isDebuff = false;
                buff.isCooldown = false;
                buff.iconSprite = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/DeathMark/texBuffDeathMarkIcon.tif").WaitForCompletion();
                (buff as ScriptableObject).name = "MoffeinEliteReworks_HauntedReviveBuff";
                PluginContentPack.buffDefs.Add(buff);
                Buffs.CelestineReviveBuff = buff;

                //These hooks are complicated, don't bother setting them up if the rework isn't being used.
                if (!useRework) return;
                On.EntityStates.Gup.BaseSplitDeath.OnEnter += FixGhostGupSplit;
                On.RoR2.GlobalEventManager.OnCharacterDeath += ReviveAsGhost;
            }

            private static void FixGhostGupSplit(On.EntityStates.Gup.BaseSplitDeath.orig_OnEnter orig, EntityStates.Gup.BaseSplitDeath self)
            {
                orig(self);
                if (NetworkServer.active && self.characterBody && self.characterBody.HasBuff(Assets.Buffs.CelestineReviveBuff))
                {
                    self.hasDied = true;
                    self.DestroyBodyAsapServer();
                }
            }

            private static void ReviveAsGhost(On.RoR2.GlobalEventManager.orig_OnCharacterDeath orig, GlobalEventManager self, DamageReport damageReport)
            {
                orig(self, damageReport);
                if (damageReport.victimBody
                    && damageReport.victimBody.HasBuff(Buffs.CelestineReviveBuff)
                    && !damageReport.victimBody.disablingHurtBoxes
                    && !damageReport.victimBody.HasBuff(RoR2Content.Buffs.AffixHaunted))
                {
                    AttemptSpawnGhost(damageReport.victimBody, damageReport.damageInfo.position, CelestineReviveAura.detachRadius * 1.1f);
                }
            }
            #endregion
        }
    }
}
