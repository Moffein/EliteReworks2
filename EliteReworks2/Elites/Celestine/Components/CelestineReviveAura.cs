using EntityStates.AffixVoid;
using RoR2;
using RoR2.Orbs;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;

namespace EliteReworks2.Elites.Celestine.Components
{
    public class CelestineReviveAura : MonoBehaviour
    {
        public static float detachRadius = 45f; //Static since it's used in revive hook
        public float wardRadius = 30f;
        public float refreshTime = 0.4f;
        public float ghostPingTime = 1f;  //Sends an orb to ghost locations
        public int maxAttachedGhosts = 5;
        public int championBonusAttachedGhosts = 3;

        public CharacterBody characterBody;

        private float stopwatch = 0f;
        private float ghostPingStopwatch = 0f;

        public List<CharacterBody> attachedGhosts = new List<CharacterBody>();
        public List<CharacterBody> attachedAliveMonsters = new List<CharacterBody>();

        public bool PassiveIsActive()
        {
            return characterBody && !characterBody.HasBuff(Common.Buffs.DisablePassiveEffect);
        }

        public int GetMaxGhosts()
        {
            return (characterBody && characterBody.isChampion) ? maxAttachedGhosts + championBonusAttachedGhosts : maxAttachedGhosts;
        }

        public int GetCurrentAttached()
        {
            return attachedGhosts.Count + attachedAliveMonsters.Count;
        }

        private void Start()
        {
            if (!characterBody) characterBody = GetComponent<CharacterBody>();

            if (!characterBody)
            {
                Destroy(this);
                return;
            }
        }

        private void FixedUpdate()
        {
            if (!characterBody || !characterBody.HasBuff(RoR2Content.Buffs.AffixHaunted) || !(characterBody.healthComponent && characterBody.healthComponent.alive))
            {
                Destroy(this);
                return;
            }

            if (!NetworkServer.active) return;

            attachedGhosts = ValidateBodyList(attachedGhosts);
            attachedAliveMonsters = ValidateBodyList(attachedAliveMonsters);

            stopwatch += Time.fixedDeltaTime;
            if (!PassiveIsActive())
            {
                stopwatch = 0f;
                ClearAttachedAliveMonstersServer();
            }
            if (stopwatch >= refreshTime)
            {
                stopwatch -= refreshTime;
                UpdateAttachedMonstersServer();
            }

            //Update ghost pings at the very end
            ghostPingStopwatch += Time.fixedDeltaTime;
            if (ghostPingStopwatch >= ghostPingTime)
            {
                ghostPingStopwatch -= ghostPingTime;
                SendGhostOrbsServer();
            }
        }

        private void OnDestroy()
        {
            if (NetworkServer.active)
            {
                ClearAttachedAliveMonstersServer();
                foreach (CharacterBody cb in attachedGhosts)
                {
                    if (cb && cb.master)
                    {
                        cb.master.TrueKill();
                    }
                }
            }
        }

        private void UpdateAttachedMonstersServer()
        {
            if (!NetworkServer.active || !characterBody || !characterBody.teamComponent) return;

            //Detach far monsters
            float detachSqr = detachRadius * detachRadius;
            List<CharacterBody> toDetach = new List<CharacterBody>();
            foreach (CharacterBody cb in attachedAliveMonsters)
            {
                if ((cb.corePosition-transform.position).sqrMagnitude > detachSqr)
                {
                    toDetach.Add(cb);
                }
            }
            foreach(CharacterBody cb in toDetach)
            {
                attachedAliveMonsters.Remove(cb);
                cb.ClearTimedBuffs(Celestine.Assets.Buffs.CelestineReviveBuff);
            }

            int slotsRemaining = GetMaxGhosts() - GetCurrentAttached();
            if (slotsRemaining > 0)
            {
                //Seek out new minions
                float radiusSquare = wardRadius * wardRadius;
                TeamIndex ti = characterBody.teamComponent.teamIndex;

                //Prioritize buffing the strongest monsters
                var teamMembers = TeamComponent.GetTeamMembers(ti).ToArray();
                Array.Sort(teamMembers, (t1, t2) =>
                {
                    float t1Health = Mathf.NegativeInfinity;
                    if (t1 && t1.body)
                    {
                        t1Health = t1.body.maxHealth;
                    }

                    float t2Health = Mathf.NegativeInfinity;
                    if (t2 && t2.body)
                    {
                        t2Health = t2.body.maxHealth;
                    }

                    if (t1Health == t2Health)
                    {
                        return 0;
                    }
                    else if (t1Health > t2Health)
                    {
                        //Higher max health goes at the start of the list
                        return -1;
                    }
                    else
                    {
                        return 1;
                    }
                });

                foreach (TeamComponent tc in teamMembers)
                {
                    if (tc.body
                        && (tc.body.bodyFlags & CharacterBody.BodyFlags.Masterless) != CharacterBody.BodyFlags.Masterless
                        && !tc.body.disablingHurtBoxes
                        && tc.body.healthComponent && tc.body.healthComponent.alive
                        && !tc.body.HasBuff(RoR2Content.Buffs.AffixHaunted) && !tc.body.HasBuff(Celestine.Assets.Buffs.CelestineReviveBuff)
                        && !attachedGhosts.Contains(tc.body) && !attachedAliveMonsters.Contains(tc.body))
                    {
                        float squareDist = (transform.position - tc.body.corePosition).sqrMagnitude;
                        if (squareDist <= radiusSquare)
                        {
                            attachedAliveMonsters.Add(tc.body);

                            slotsRemaining--;
                            if (slotsRemaining <= 0)
                            {
                                break;
                            }
                        }
                    }
                }
            }

            //Refresh timed buff on attached alive monsters
            foreach (CharacterBody body in attachedAliveMonsters)
            {
                body.AddTimedBuff(Celestine.Assets.Buffs.CelestineReviveBuff, refreshTime + 0.2f);
            }
        }

        private void ClearAttachedAliveMonstersServer()
        {
            if (!NetworkServer.active) return;
            foreach(CharacterBody body in attachedAliveMonsters)
            {
                body.ClearTimedBuffs(Celestine.Assets.Buffs.CelestineReviveBuff);
            }
            attachedAliveMonsters.Clear();
        }

        private void SendGhostOrbsServer()
        {
            if (!NetworkServer.active || !characterBody || !characterBody.mainHurtBox) return;
            int indexInGroup = 0;
            foreach (CharacterBody body in attachedGhosts)
            {
                if (!body) continue;
                OrbManager.instance.AddOrb(new EliteReworksCelestineOrb
                {
                    origin = body.corePosition,
                    target = characterBody.mainHurtBox,
                    timeToArrive = 0.5f + indexInGroup * 0.1f,
                    scale = 0.5f
                });
                indexInGroup++;
            }

            foreach (CharacterBody body in attachedAliveMonsters)
            {
                if (!body) continue;
                OrbManager.instance.AddOrb(new EliteReworksCelestineOrb
                {
                    origin = body.corePosition,
                    target = characterBody.mainHurtBox,
                    timeToArrive = 0.5f + indexInGroup * 0.1f,
                    scale = 0.5f
                });
                indexInGroup++;
            }
        }

        private List<CharacterBody> ValidateBodyList(List<CharacterBody> bodyList)
        {
           return bodyList.Where(body => body && body.healthComponent && body.healthComponent.alive).ToList();
        }
    }
}
