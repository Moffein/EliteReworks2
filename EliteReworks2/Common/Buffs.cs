using EliteReworks2.Modules;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EliteReworks2.Common
{
    public static class Buffs
    {
        public static BuffDef DisablePassiveEffect;

        internal static void Init()
        {
            CreateDisablePassiveEffect();
        }

        #region DisablePassiveEffect setup
        private static void CreateDisablePassiveEffect()
        {
            if (DisablePassiveEffect) return;

            BuffDef buffDef = ScriptableObject.CreateInstance<BuffDef>();
            buffDef.isDebuff = false;
            buffDef.isHidden = true;
            buffDef.isDOT = false;
            buffDef.isCooldown = false;
            buffDef.ignoreGrowthNectar = true;
            buffDef.canStack = false;
            buffDef.flags = BuffDef.Flags.ExcludeFromNoxiousThorns;
            (buffDef as ScriptableObject).name = "MoffeinEliteReworks_DisablePassiveEffect";
            PluginContentPack.buffDefs.Add(buffDef);
            DisablePassiveEffect = buffDef;

            On.EntityStates.StunState.OnEnter += StunState_OnEnter;
            On.EntityStates.StunState.OnExit += StunState_OnExit;
            On.EntityStates.ShockState.OnEnter += ShockState_OnEnter;
            On.EntityStates.ShockState.OnExit += ShockState_OnExit;
            On.EntityStates.FrozenState.OnEnter += FrozenState_OnEnter;
            On.EntityStates.FrozenState.OnExit += FrozenState_OnExit;
            On.EntityStates.Drifter.Bag.BaggedObject.OnEnter += BaggedObject_OnEnter;
            On.EntityStates.Drifter.Bag.BaggedObject.OnExit += BaggedObject_OnExit;
        }

        private static void StunState_OnEnter(On.EntityStates.StunState.orig_OnEnter orig, EntityStates.StunState self)
        {
            orig(self);
            if (NetworkServer.active && self.characterBody) self.characterBody.AddBuff(DisablePassiveEffect);
        }

        private static void StunState_OnExit(On.EntityStates.StunState.orig_OnExit orig, EntityStates.StunState self)
        {
            orig(self);
            if (NetworkServer.active && self.characterBody && self.characterBody.HasBuff(DisablePassiveEffect))
            {
                self.characterBody.RemoveBuff(DisablePassiveEffect);
                if (!self.characterBody.HasBuff(DisablePassiveEffect)) self.characterBody.AddTimedBuff(DisablePassiveEffect, 0.5f);
            }
        }

        private static void ShockState_OnEnter(On.EntityStates.ShockState.orig_OnEnter orig, EntityStates.ShockState self)
        {
            orig(self);
            if (NetworkServer.active && self.characterBody) self.characterBody.AddBuff(DisablePassiveEffect);
        }

        private static void ShockState_OnExit(On.EntityStates.ShockState.orig_OnExit orig, EntityStates.ShockState self)
        {
            orig(self);
            if (NetworkServer.active && self.characterBody && self.characterBody.HasBuff(DisablePassiveEffect))
            {
                self.characterBody.RemoveBuff(DisablePassiveEffect);
                if (!self.characterBody.HasBuff(DisablePassiveEffect)) self.characterBody.AddTimedBuff(DisablePassiveEffect, 0.5f);
            }
        }

        private static void FrozenState_OnEnter(On.EntityStates.FrozenState.orig_OnEnter orig, EntityStates.FrozenState self)
        {
            orig(self);
            if (NetworkServer.active && self.characterBody) self.characterBody.AddBuff(DisablePassiveEffect);
        }

        private static void FrozenState_OnExit(On.EntityStates.FrozenState.orig_OnExit orig, EntityStates.FrozenState self)
        {
            orig(self);
            if (NetworkServer.active && self.characterBody && self.characterBody.HasBuff(DisablePassiveEffect))
            {
                self.characterBody.RemoveBuff(DisablePassiveEffect);
                if (!self.characterBody.HasBuff(DisablePassiveEffect)) self.characterBody.AddTimedBuff(DisablePassiveEffect, 0.5f);
            }
        }

        private static void BaggedObject_OnEnter(On.EntityStates.Drifter.Bag.BaggedObject.orig_OnEnter orig, EntityStates.Drifter.Bag.BaggedObject self)
        {
            orig(self);
            if (NetworkServer.active && self.targetBody)
            {
                self.targetBody.AddBuff(DisablePassiveEffect);
            }
        }

        private static void BaggedObject_OnExit(On.EntityStates.Drifter.Bag.BaggedObject.orig_OnExit orig, EntityStates.Drifter.Bag.BaggedObject self)
        {
            orig(self);
            if (NetworkServer.active && self.targetBody && self.targetBody.HasBuff(DisablePassiveEffect))
            {
                self.targetBody.RemoveBuff(DisablePassiveEffect);
                if (!self.targetBody.HasBuff(DisablePassiveEffect)) self.targetBody.AddTimedBuff(DisablePassiveEffect, 0.5f);
            }
        }
        #endregion
    }
}
