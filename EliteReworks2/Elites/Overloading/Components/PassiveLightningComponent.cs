using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace EliteReworks2.Elites.Overloading.Components
{
    public class PassiveLightningComponent : MonoBehaviour
    {
        public float lightingDelay = 6f;

        public float damageCoefficient = 3f;    //Used by players + player team
        public float flatDamageNPC = 36f;
        public float flatDamageNPCBoss = 57.6f; //+60% damage, mimics Imp Overlord vs Imp

        public CharacterBody characterBody;

        private float timer = 0f;

        private void Start()
        {
            if (!characterBody) characterBody = GetComponent<CharacterBody>();

            if (!characterBody)
            {
                Destroy(this);
            }
        }

        private void FixedUpdate()
        {
            if (!characterBody || !characterBody.HasBuff(RoR2Content.Buffs.AffixBlue) || !(characterBody.healthComponent && characterBody.healthComponent.alive))
            {
                Destroy(this);
                return;
            }

            if (characterBody.HasBuff(Common.Buffs.DisablePassiveEffect.buffDef))
            {
                timer = 0f;
            }
            else
            {
                timer += Time.fixedDeltaTime;
                if (timer >= lightingDelay)
                {
                    timer -= lightingDelay;
                    FireLightningAuthority();
                }
            }
        }

        public void FireLightningAuthority()
        {
            if (!characterBody.hasEffectiveAuthority) return;

            float damage = characterBody.damage * damageCoefficient;
            if (!characterBody.isPlayerControlled && !(characterBody.teamComponent && characterBody.teamComponent.teamIndex == TeamIndex.Player))
            {
                if (characterBody.isChampion)
                {
                    damage = EliteReworks2Utils.GetAmbientLevelScaledDamage(flatDamageNPC);
                }
                else
                {
                    damage = EliteReworks2Utils.GetAmbientLevelScaledDamage(flatDamageNPCBoss);
                }
            }

            FireMeatballs(gameObject, characterBody.isChampion, damage, characterBody.RollCrit(),
                Vector3.up, characterBody.corePosition + Vector3.up, characterBody.transform.forward,
                5, 20f, 400f, characterBody.isChampion ? 25f : 20f);
        }

        //Copypasted from Magma Worm
        public void FireMeatballs(GameObject attacker, bool isChampion, float damage, bool crit,
            Vector3 impactNormal, Vector3 impactPosition, Vector3 forward,
            int meatballCount, float meatballAngle, float meatballForce, float velocity)
        {
            EffectManager.SimpleSoundEffect((isChampion ? Overloading.Assets.NetworkSoundEvents.PassiveTriggerBoss : Overloading.Assets.NetworkSoundEvents.PassiveTrigger).index, impactPosition, true);

            float num = 360f / (float)meatballCount;
            float randomOffset = UnityEngine.Random.Range(0f, 360f);
            Vector3 normalized = Vector3.ProjectOnPlane(forward, impactNormal).normalized;
            Vector3 point = Vector3.zero;
            point = Vector3.RotateTowards(impactNormal, normalized, meatballAngle * 0.0174532924f, float.PositiveInfinity);
            bool spawnedClose = false;
            for (int i = 0; i < meatballCount; i++)
            {
                Vector3 forward2 = Quaternion.AngleAxis(randomOffset + num * (float)i, impactNormal) * point;
                GameObject projectile = (isChampion ? Overloading.Assets.Projectiles.PassiveLightningBombBoss : Overloading.Assets.Projectiles.PassiveLightningBomb);
                ProjectileManager.instance.FireProjectile(projectile, impactPosition, RoR2.Util.QuaternionSafeLookRotation(forward2),
                    attacker, damage, meatballForce, crit, DamageColorIndex.Default, null, velocity);
            }
        }
    }
}
