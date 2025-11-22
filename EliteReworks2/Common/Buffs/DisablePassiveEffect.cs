using EliteReworks2.Modules;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EliteReworks2.Common.Buffs
{
    //Elite passives check if the body has this buff, and voluntarily disable themselves if this is the case.
    public class DisablePassiveEffect
    {
        public static BuffDef buffDef;

        internal static void Init()
        {
            if (buffDef) return;

            buffDef = ScriptableObject.CreateInstance<BuffDef>();
            buffDef.isDebuff = false;
            buffDef.isHidden = true;
            buffDef.isDOT = false;
            buffDef.isCooldown = false;
            buffDef.ignoreGrowthNectar = true;
            buffDef.flags = BuffDef.Flags.ExcludeFromNoxiousThorns;
            (buffDef as ScriptableObject).name = "MoffeinEliteReworks_DisablePassiveEffect";
            PluginContentPack.buffDefs.Add(buffDef);

            On.EntityStates.StunState.OnEnter += StunState_OnEnter;
            On.EntityStates.StunState.OnExit += StunState_OnExit;
            On.EntityStates.ShockState.OnEnter += ShockState_OnEnter;
            On.EntityStates.ShockState.OnExit += ShockState_OnExit;
            On.EntityStates.FrozenState.OnEnter += FrozenState_OnEnter;
            On.EntityStates.FrozenState.OnExit += FrozenState_OnExit;
        }

        private static void StunState_OnEnter(On.EntityStates.StunState.orig_OnEnter orig, EntityStates.StunState self)
        {
            orig(self);
            if (NetworkServer.active && self.characterBody) self.characterBody.AddBuff(buffDef);
        }

        private static void StunState_OnExit(On.EntityStates.StunState.orig_OnExit orig, EntityStates.StunState self)
        {
            orig(self);
            if (NetworkServer.active && self.characterBody && self.characterBody.HasBuff(buffDef)) self.characterBody.RemoveBuff(buffDef);
        }

        private static void ShockState_OnEnter(On.EntityStates.ShockState.orig_OnEnter orig, EntityStates.ShockState self)
        {
            orig(self);
            if (NetworkServer.active && self.characterBody) self.characterBody.AddBuff(buffDef);
        }

        private static void ShockState_OnExit(On.EntityStates.ShockState.orig_OnExit orig, EntityStates.ShockState self)
        {
            orig(self);
            if (NetworkServer.active && self.characterBody && self.characterBody.HasBuff(buffDef)) self.characterBody.RemoveBuff(buffDef);
        }

        private static void FrozenState_OnEnter(On.EntityStates.FrozenState.orig_OnEnter orig, EntityStates.FrozenState self)
        {
            orig(self);
            if (NetworkServer.active && self.characterBody) self.characterBody.AddBuff(buffDef);
        }

        private static void FrozenState_OnExit(On.EntityStates.FrozenState.orig_OnExit orig, EntityStates.FrozenState self)
        {
            orig(self);
            if (NetworkServer.active && self.characterBody && self.characterBody.HasBuff(buffDef)) self.characterBody.RemoveBuff(buffDef);
        }
    }
}
