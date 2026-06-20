using DvergerSecretDefenses.Common;
using Jotunn;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DvergerSecretDefenses.PieceFeatures {
    public class Zapper : MonoBehaviour {

        private double NextCheckTime;
        private StaticTarget selfTarget;
        private GameObject lightningEffect;
        private GameObject lightningProjectile;
        private HitData lightningHit;
        public ZNetView m_nview;
        public GameObject m_shotSource;
        public GameObject m_thunderstone;

        // May need to be Zsynced
        private List<Character> LuringTargets = new List<Character>();
        private double NextAvailableShotTime = 0f;

        public void Awake() {
            selfTarget = this.GetComponent<StaticTarget>();
            lightningEffect = PrefabManager.Instance.GetPrefab("fx_chainlightning_spread");
            lightningProjectile = PrefabManager.Instance.GetPrefab("staff_lightning_projectile");

            lightningHit = new HitData() {};
            lightningHit.m_damage.m_lightning = ValConfig.ZapperDamage.Value;
            lightningHit.m_toolTier = 1;
            lightningHit.m_pushForce = 30f;
            lightningHit.m_backstabBonus = 2;
            lightningHit.m_staggerMultiplier = 2;
            lightningHit.m_blockable = true;
            lightningHit.m_dodgeable = true;
            lightningHit.m_skill = Skills.SkillType.ElementalMagic;
            lightningHit.m_itemWorldLevel = (byte)Game.m_worldLevel;
            lightningHit.m_hitType = HitData.HitType.Turret;
        }

        public void Update() {
            // This is to prevent the building piece from activating before it is placed
            // And only runs the actual luring and damage loops inside whoever is the Zowner
            if (m_nview.IsValid() == false || m_nview.IsOwner() == false) { return; }


            if (LuringTargets.Count > 0) {
                // Drop dead targets and any that have wandered out of lure range, restoring
                // their vanilla AI so they don't stay frozen-alerted on our static target.
                LuringTargets = LuringTargets.Where(target => {
                    if (target == null || target.GetZDOID() == null || target.GetZDOID().ID == 0L) { return false; }
                    if (Vector3.Distance(this.transform.position, target.transform.position) > ValConfig.ZapperLureRange.Value) {
                        ReleaseLureTarget(target);
                        return false;
                    }
                    return true;
                }).ToList();

                foreach (var target in LuringTargets) {
                    if (target == null) { continue; }

                    // Re-assert the lure every tick. MonsterAI.UpdateTarget would otherwise
                    // clear m_targetStatic within 2-6s; re-applying keeps the squito pathing here.
                    MonsterAI mai = target.GetComponent<MonsterAI>();
                    if (mai != null) {
                        ClaimCreature(target);
                        ForceLureTarget(mai);
                    }

                    // Kill nearby tracked creatures with lightning
                    float distance = Vector3.Distance(this.transform.position, target.gameObject.transform.position);
                    //Logger.LogDebug($"{target.name} distance {distance}");
                    if (distance < ValConfig.ZapperShotRange.Value) {
                        if (ZNet.instance.GetTimeSeconds() < NextAvailableShotTime) { continue; }

                        Logger.LogDebug($"Shock-killer at {target}");
                        // Visual at the thunderstone level
                        Instantiate(lightningEffect, m_thunderstone.transform.position, m_thunderstone.transform.rotation);

                        // Insta-damage a target, never miss.
                        Instantiate(lightningEffect, target.transform.position, Quaternion.identity);
                        target.Damage(lightningHit);

                        NextAvailableShotTime = ZNet.instance.GetTimeSeconds() + ValConfig.ZapperShotInterval.Value;
                    }
                }
            }

            // Aquire far out lurable targets
            if (ZNet.instance.GetTimeSeconds() >= NextCheckTime) {
                // Set the next scan time
                NextCheckTime = ZNet.instance.GetTimeSeconds() + ValConfig.ZapperScanInterval.Value;


                // Scan for nearby squittos
                List<Character> nearbyCharas = CommonUtils.GetCharactersInRange(this.transform.position, ValConfig.ZapperLureRange.Value);

                foreach (Character character in nearbyCharas) {
                    Logger.LogDebug($"Checking {nearbyCharas.Count} nearby.");
                    if (character.GetFaction() != Character.Faction.PlainsMonsters) { continue; }
                    if (Utils.GetPrefabName(character.gameObject) != "Deathsquito") { continue; }
                    if (LuringTargets.Contains(character)) { continue; } // Don't need to re-add already tracked luring creatures

                    MonsterAI mai = character.GetComponent<MonsterAI>();
                    if (mai == null) { continue; }

                    Logger.LogDebug($"Luring {character} nearby.");
                    if (selfTarget == null) {
                        selfTarget = this.GetComponent<StaticTarget>();
                    }

                    // Claim the creature's ZDO so our machine runs its AI
                    ClaimCreature(character);
                    ForceLureTarget(mai);
                    LuringTargets.Add(character);
                }
            }
        }

        // Forces a monster AI to run towards this object
        private void ForceLureTarget(MonsterAI mai) {
            if (selfTarget == null) {
                selfTarget = this.GetComponent<StaticTarget>();
            }
            mai.m_targetStatic = selfTarget;
            mai.m_targetCreature = null;            // stop it chasing players while lured
            mai.m_lastKnownTargetPos = this.transform.position;
            mai.m_beenAtLastPos = false;
            mai.SetAlerted(true);                   // self-guarded; makes MoveTo run instead of walk
            mai.m_updateTargetTimer = 9999f;        // block UpdateTarget from clearing the assignment
        }

        // Reset creature AI if its outside of luring range
        private void ReleaseLureTarget(Character character) {
            if (character == null) { return; }
            MonsterAI mai = character.GetComponent<MonsterAI>();
            if (mai == null) { return; }
            mai.m_updateTargetTimer = 0f;
            if (mai.m_targetStatic == selfTarget) { mai.m_targetStatic = null; }
        }

        // Claim ownership over a creature
        // Creature AI should run on the same zowner as the zapper to ensure that the AI changes take place
        private void ClaimCreature(Character character) {
            ZNetView cnview = character.GetComponent<ZNetView>();
            if (cnview != null && cnview.IsValid() && !cnview.IsOwner()) {
                cnview.ClaimOwnership();
            }
        }

        public void ShootProjectile(Vector3 target, float speed = 50f) {

            // Shot, spawned above the tower
            GameObject shot = UnityEngine.Object.Instantiate<GameObject>(lightningProjectile, m_shotSource.transform.position, m_shotSource.transform.rotation);
            Vector3 velocity = (target - m_shotSource.transform.position).normalized * speed;

            shot.GetComponent<IProjectile>()?.Setup((Character)null, velocity, 1f, lightningHit, (ItemDrop.ItemData)null, null);
        }
    }
}
