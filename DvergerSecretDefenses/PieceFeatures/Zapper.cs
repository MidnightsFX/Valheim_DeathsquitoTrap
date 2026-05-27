using DvergerSecretDefenses.Common;
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
        private HitData lightningHit;

        // May need to be Zsynced
        private static List<Character> LuringTargets = new List<Character>();

        public void Awake() {
            selfTarget = this.GetComponent<StaticTarget>();
            lightningEffect = PrefabManager.Instance.GetPrefab("fx_chainlightning_spread");

            lightningHit = new HitData() {};
            lightningHit.m_damage.m_lightning = 150f;
        }

        public void Update() {
            if (LuringTargets.Count > 0) {
                // Remove dead targets
                LuringTargets = LuringTargets.Where(x => x != null).ToList();

                foreach (var target in LuringTargets) {
                    if (target == null) { continue; }
                    // Kill nearby tracked creatures with lightning
                    float distance = Vector3.Distance(this.transform.position, target.gameObject.transform.position);
                    Logger.LogDebug($"{target.name} distance {distance}");
                    if (distance < 10f) {
                        Logger.LogDebug($"Shock-killer at {target.transform.position}");
                        GameObject.Instantiate(lightningEffect, target.transform.position, Quaternion.identity);
                        target.Damage(lightningHit);
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
                    if (character.GetFaction() != Character.Faction.PlainsMonsters) { continue; }
                    if (Utils.GetPrefabName(character.gameObject) != "Deathsquito") { continue; }
                    if (LuringTargets.Contains(character)) { continue; } // Don't need to re-add already tracked luring creatures

                    MonsterAI mai = character.GetComponent<MonsterAI>();
                    if (selfTarget == null) {
                        selfTarget = this.GetComponent<StaticTarget>();
                    }
                    mai.m_targetStatic = selfTarget;
                    mai.m_lastKnownTargetPos = this.transform.position;
                    mai.m_beenAtLastPos = false;
                }
            }
        }
    }
}
