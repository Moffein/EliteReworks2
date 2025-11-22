using EliteReworks2.Modules;
using RoR2;
using RoR2.Projectile;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace EliteReworks2
{
    public static class EliteReworks2Utils
    {
        public static float GetAmbientLevelDamageScalar()
        {
            float mult = 1f;
            if (Run.instance)
            {
                mult += 0.2f * (Run.instance.ambientLevelFloor - 1f);
            }
            return mult;
        }

        public static float GetAmbientLevelScaledDamage(float damage)
        {
            return damage * GetAmbientLevelDamageScalar();
        }

        public static NetworkSoundEventDef BuildNetworkSound(string eventName)
        {
            NetworkSoundEventDef toReturn = ScriptableObject.CreateInstance<NetworkSoundEventDef>();
            toReturn.eventName = eventName;
            (toReturn as UnityEngine.Object).name = "MoffeinEliteReworks_NSE_"+eventName;
            PluginContentPack.networkSoundEventDefs.Add(toReturn);
            return toReturn;
        }

        public static int BuffSphere(BuffDef buff, float buffDuration, Vector3 position, float radius, TeamIndex teamIndex)
        {
            int hitCount = 0;

            float radiusSqr = radius * radius;

            foreach (CharacterBody body in CharacterBody.instancesList)
            {
                if (!body) continue;
                if (body.teamComponent && body.teamComponent.teamIndex != teamIndex) continue;

                float distanceSqr = (body.corePosition - position).sqrMagnitude;
                if (distanceSqr > radiusSqr) continue;

                float duration = buffDuration;
                body.AddTimedBuff(buff, duration);
                hitCount++;
            }
            return hitCount;
        }

        public static void DebuffSphere(BuffIndex buff, TeamIndex team, Vector3 position, float radius, float debuffDuration, GameObject effect, GameObject hitEffect, bool ignoreImmunity, bool falloff, NetworkSoundEventDef buffSound)
        {
            if (!NetworkServer.active)
            {
                return;
            }

            if (effect != null)
            {
                EffectManager.SpawnEffect(effect, new EffectData
                {
                    origin = position,
                    scale = radius
                }, true);
            }
            float radiusHalfwaySqr = radius * radius * 0.25f;
            List<HealthComponent> hcList = new List<HealthComponent>();
            Collider[] array = Physics.OverlapSphere(position, radius, LayerIndex.entityPrecise.mask);
            for (int i = 0; i < array.Length; i++)
            {
                HurtBox hurtBox = array[i].GetComponent<HurtBox>();
                if (hurtBox)
                {
                    HealthComponent healthComponent = hurtBox.healthComponent;
                    if (healthComponent && healthComponent.body && !hcList.Contains(healthComponent))
                    {
                        hcList.Add(healthComponent);
                        if (healthComponent.body.teamComponent && healthComponent.body.teamComponent.teamIndex != team)
                        {
                            if (ignoreImmunity || (!healthComponent.body.HasBuff(RoR2Content.Buffs.Immune) && !healthComponent.body.HasBuff(RoR2Content.Buffs.HiddenInvincibility)))
                            {
                                float effectiveness = 1f;
                                if (falloff)
                                {
                                    float distSqr = (position - hurtBox.collider.ClosestPoint(position)).sqrMagnitude;
                                    if (distSqr > radiusHalfwaySqr)  //Reduce effectiveness when over half the radius away
                                    {
                                        effectiveness *= 0.5f;  //0.25 is vanilla sweetspot
                                    }
                                }
                                bool alreadyHasBuff = healthComponent.body.HasBuff(buff);
                                healthComponent.body.AddTimedBuff(buff, effectiveness * debuffDuration);
                                if (!alreadyHasBuff)
                                {
                                    if (hitEffect != null)
                                    {
                                        EffectManager.SpawnEffect(hitEffect, new EffectData
                                        {
                                            origin = healthComponent.body.corePosition
                                        }, true);
                                    }
                                    if (buffSound != null)
                                    {
                                        EffectManager.SimpleSoundEffect(buffSound.index, healthComponent.body.corePosition, true);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
