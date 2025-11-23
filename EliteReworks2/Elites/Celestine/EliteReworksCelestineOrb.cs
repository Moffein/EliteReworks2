using RoR2;
using UnityEngine.AddressableAssets;
using UnityEngine;

namespace EliteReworks2.Elites.Celestine
{
    public class EliteReworksCelestineOrb : RoR2.Orbs.Orb
    {
        public float timeToArrive;
        public float scale = 1f;

        public override void Begin()
        {
            base.duration = this.timeToArrive + UnityEngine.Random.Range(0f, 0.4f);
            EffectData effectData = new EffectData
            {
                scale = this.scale,
                origin = this.origin,
                genericFloat = base.duration
            };
            effectData.SetHurtBoxReference(this.target);
            EffectManager.SpawnEffect(Celestine.Assets.Effects.CelestineOrb, effectData, true);
        }
    }
}
