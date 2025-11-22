using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EliteReworks2.Elites.Collective.Components
{
    //Destroys and recreates the collective shield based on stun state
    public class CollectiveShieldManagerServer : MonoBehaviour
    {
        public CharacterBody characterBody;
        public AffixCollectiveBehavior affixCollectiveBehavior;

        private void Start()
        {
            if (!characterBody) characterBody = GetComponent<CharacterBody>();

            if (!characterBody || !NetworkServer.active)
            {
                Destroy(this);
                return;
            }
        }

        private void FixedUpdate()
        {
            if (!characterBody || !characterBody.HasBuff(DLC3Content.Buffs.EliteCollective) || !NetworkServer.active)
            {
                Destroy(this);
                return;
            }

            if (!affixCollectiveBehavior)
            {
                affixCollectiveBehavior = GetComponent<AffixCollectiveBehavior>();
            }

            if (affixCollectiveBehavior)
            {
                if (characterBody.HasBuff(Common.Buffs.DisablePassiveEffect))
                {
                    if (affixCollectiveBehavior._attachmentInstance)
                    {
                        Destroy(affixCollectiveBehavior._attachmentInstance);
                        affixCollectiveBehavior._attachmentInstance = null;
                    }
                }
                else
                {
                    if (!affixCollectiveBehavior._attachmentInstance)
                    {
                        affixCollectiveBehavior._attachmentInstance = UnityEngine.Object.Instantiate<GameObject>(AffixCollectiveBehavior._attachmentPrefab);
                        affixCollectiveBehavior._attachmentInstance.GetComponent<NetworkedBodyAttachment>().AttachToGameObjectAndSpawn(base.gameObject, null);
                    }
                }
            }
        }
    }
}
