using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EliteReworks2.Elites.Malachite.Components
{
    public class AntiHealAuraServer : MonoBehaviour
    {
        public static float wardRadius = 22f;
        public static float refreshTime = 2.5f / 5f;  //2.5 seconds divided by 5 stacks for full buildup
        public static float buffDuration = 1f;

        public CharacterBody characterBody;

        private float stopwatch = 0f;
        private GameObject indicator;

        private void Start()
        {
            if (!characterBody) characterBody = GetComponent<CharacterBody>();

            if (!characterBody || !NetworkServer.active)
            {
                Destroy(this);
            }

            UpdateIndicatorServer(true);
        }

        private void FixedUpdate()
        {
            if (!NetworkServer.active || !characterBody || ! characterBody.HasBuff(RoR2Content.Buffs.AffixPoison) || !(characterBody.healthComponent && characterBody.healthComponent.alive))
            {
                Destroy(this);
                return;
            }

            stopwatch += Time.fixedDeltaTime;
            if (stopwatch >= refreshTime)
            {
                stopwatch -= refreshTime;
                EliteReworks2Utils.BuffSphere(Malachite.Assets.Buffs.MalachiteBuildup, characterBody.teamComponent ? characterBody.teamComponent.teamIndex : TeamIndex.None, transform.position, wardRadius, buffDuration, true);
            }
        }

        private void OnDestroy()
        {
            if (NetworkServer.active && indicator)
            {
                Destroy(indicator);
                indicator = null;
            }
        }

        public void UpdateIndicatorServer(bool wardActive)
        {
            if (!NetworkServer.active) return;
            if (indicator != wardActive)
            {
                if (wardActive)
                {
                    indicator = UnityEngine.Object.Instantiate<GameObject>(Malachite.Assets.NetworkObjects.AntiHealAuraIndicator);
                    indicator.GetComponent<NetworkedBodyAttachment>().AttachToGameObjectAndSpawn(gameObject);
                }
                else
                {
                    Destroy(indicator);
                    indicator = null;
                }
            }
        }
    }
}
