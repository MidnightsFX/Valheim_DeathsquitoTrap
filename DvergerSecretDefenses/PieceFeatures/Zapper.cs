using DvergerSecretDefenses.Common;
using Jotunn;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static UnityEngine.UI.Image;

namespace DvergerSecretDefenses.PieceFeatures {
    public class Zapper : MonoBehaviour {

        private double NextCheckTime;
        private StaticTarget selfTarget;
        private GameObject lightningEffect;
        private GameObject lightningProjectile;
        private HitData lightningHit;
        private GameObject shotSource;
        private GameObject thunderStone;
        public ZNetView m_nview;

        // May need to be Zsynced
        private static List<Character> LuringTargets = new List<Character>();

        public void Awake() {
            selfTarget = this.GetComponent<StaticTarget>();
            lightningEffect = PrefabManager.Instance.GetPrefab("fx_chainlightning_spread");
            lightningProjectile = PrefabManager.Instance.GetPrefab("staff_lightning_projectile");

            lightningHit = new HitData() {};
            lightningHit.m_damage.m_lightning = 150f;
            lightningHit.m_toolTier = 1;
            lightningHit.m_pushForce = 30f;
            lightningHit.m_backstabBonus = 2;
            lightningHit.m_staggerMultiplier = 2;
            lightningHit.m_blockable = true;
            lightningHit.m_dodgeable = true;
            lightningHit.m_skill = Skills.SkillType.ElementalMagic;
            lightningHit.m_itemWorldLevel = (byte)Game.m_worldLevel;
            lightningHit.m_hitType = HitData.HitType.Turret;

            m_nview = this.GetComponent<ZNetView>();

            shotSource = this.transform.Find("DvergerSecretDefenses/Shooter").gameObject;
            thunderStone = this.transform.Find("DvergerSecretDefenses/ThunderRock").gameObject;
        }

        public void Update() {
            // This is to prevent the building piece from activating before it is placed
            if (!this.m_nview.IsValid()) { return; }
                

            if (LuringTargets.Count > 0) {
                // Remove dead targets
                LuringTargets = LuringTargets.Where(x => x != null && x.GetZDOID() != null && x.GetZDOID().ID != 0L).ToList();

                foreach (var target in LuringTargets) {
                    if (target == null) { continue; }
                    // Kill nearby tracked creatures with lightning
                    float distance = Vector3.Distance(this.transform.position, target.gameObject.transform.position);
                    Logger.LogDebug($"{target.name} distance {distance}");
                    if (distance < 10f) {
                        Logger.LogDebug($"Shock-killer at {target.transform.position}");
                        // shoot projectile?
                        ShootProjectile(target.transform.position);

                        // Instakill?
                        //GameObject.Instantiate(lightningEffect, target.transform.position, Quaternion.identity);
                        //target.Damage(lightningHit);
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
                    if (mai.IsAggravatable()) { continue; }

                    Logger.LogDebug($"Luring {character} nearby.");
                    if (selfTarget == null) {
                        selfTarget = this.GetComponent<StaticTarget>();
                    }
                    mai.m_targetStatic = selfTarget;
                    mai.m_lastKnownTargetPos = this.transform.position;
                    mai.m_beenAtLastPos = false;
                    mai.SetAggravated(true, BaseAI.AggravatedReason.Building);
                    LuringTargets.Add(character);
                }
            }
        }

        public void ShootProjectile(Vector3 target, float speed = 5f) {
            // Visual at the thunderstone level
            UnityEngine.GameObject.Instantiate(lightningEffect, thunderStone.transform.position, thunderStone.transform.rotation);

            // Shot, spawned above the tower
            GameObject shot = UnityEngine.Object.Instantiate<GameObject>(lightningProjectile, shotSource.transform.position, shotSource.transform.rotation);
            Vector3 velocity = (target - shotSource.transform.position).normalized * speed;

            shot.GetComponent<IProjectile>()?.Setup((Character)null, velocity, 1f, lightningHit, (ItemDrop.ItemData)null, null);
        }
    }
}
