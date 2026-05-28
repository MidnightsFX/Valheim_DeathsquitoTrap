using BepInEx.Configuration;
using DvergerSecretDefenses;
using HarmonyLib;
using Jotunn;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Piece;

namespace DvergerSecretDefenses.common {
    public static class JotunnPiece {

        public class LoadedGameObjects {
            public GameObject Prefab { get; set; }
            public Sprite Sprite { get; set; }
            public GameObject ScenePrefab { get; set; }
        }

        public class PieceCost {
            public string prefab { get; set; }
            public int amount { get; set; }
            public bool refundable { get; set; } = true;
        }

        public class PieceConfigs {
            public ConfigEntry<bool> Enabled { get; set; }
            public ConfigEntry<bool> RequiresWorkbench { get; set; }
            public ConfigEntry<string> Workbench { get; set; }
            public ConfigEntry<string> PieceCategory { get; set; }
            public ConfigEntry<string> PieceCost { get; set; }
            public List<PieceCost> UpdatedCost { get; set; } = new List<PieceCost>();
        }

        public class JotunnBuildPiece {
            public string Name { get; set; }
            public bool Enabled { get; set; } = true;
            public string Prefab { get; set; }
            public string Sprite { get; set; }
            public Piece.PieceCategory Category { get; set; } = Piece.PieceCategory.Misc;
            public string Workbench { get; set; } = "piece_workbench";
            public List<PieceCost> PieceCost { get; set; } = new List<PieceCost>();

            // Populated by in-game related runtime objects
            public LoadedGameObjects Objs { get; set; }
            public PieceConfigs Cfgs { get; set; }
        }

        static List<JotunnBuildPiece> BuildPieces = new List<JotunnBuildPiece>();

        public static void RegisterJotunnPiece(JotunnBuildPiece jbuildpiece) {

            LoadedGameObjects LGos = new LoadedGameObjects();

            // Set asset references
            LGos.Prefab = DvergerSecretDefenses.EmbeddedResourceBundle.LoadAsset<GameObject>($"Assets/Custom/Pieces/{jbuildpiece.Prefab}.prefab");
            LGos.Sprite = DvergerSecretDefenses.EmbeddedResourceBundle.LoadAsset<Sprite>($"Assets/Custom/Icons/{jbuildpiece.Sprite}.png");
            jbuildpiece.Objs = LGos;
            jbuildpiece.Cfgs = new PieceConfigs();

            InitialPieceSetup(jbuildpiece);

            BuildPieces.Add(jbuildpiece);

            // Find and register this prefab in the scene, for in-place updates.
            void SetScenePrefab() {
                IEnumerable<GameObject> scene_parents = Resources.FindObjectsOfTypeAll<GameObject>().Where(obj => obj.name == jbuildpiece.Prefab);
                if (ValConfig.EnableDebugMode.Value) { Logger.LogInfo($"Found {jbuildpiece.Prefab} scene parent objects: {scene_parents.Count()}"); }
                jbuildpiece.Objs.ScenePrefab = scene_parents.First();
            }
            PrefabManager.OnPrefabsRegistered += SetScenePrefab;
        }

        private static void InitialPieceSetup(JotunnBuildPiece jbuildpiece) {
            // Set where the recipe can be crafted
            void RequiredBench_SettingChanged(object sender, EventArgs e) {
                if (jbuildpiece.Cfgs.RequiresWorkbench.Value == false || string.IsNullOrEmpty(jbuildpiece.Cfgs.Workbench.Value) || jbuildpiece.Cfgs.Workbench.Value.ToLower() == "none") {
                    Logger.LogInfo("Setting required crafting station to none.");
                    jbuildpiece.Objs.ScenePrefab.GetComponent<Piece>().m_craftingStation = null;
                    return;
                }

                CraftingStation craftable_at = PrefabManager.Instance.GetPrefab(jbuildpiece.Cfgs.Workbench.Value)?.GetComponent<CraftingStation>();
                if (craftable_at == null) {
                    Logger.LogWarning($"Required crafting station does not exist or does not have a crafting station componet, check your prefab name ({jbuildpiece.Cfgs.Workbench.Value}).");
                    return;
                }

                if (ValConfig.EnableDebugMode.Value == true) { Logger.LogInfo($"Setting crafting station to {jbuildpiece.Cfgs.Workbench.Value}."); }
                jbuildpiece.Objs.ScenePrefab.GetComponent<Piece>().m_craftingStation = craftable_at;
            }
            jbuildpiece.Cfgs.Workbench = ValConfig.BindServerConfig($"{jbuildpiece.Name}", $"Workbench", jbuildpiece.Workbench, $"The table required to allow building this piece, eg: 'forge', 'piece_workbench', 'blackforge', 'piece_artisanstation'.");
            jbuildpiece.Cfgs.Workbench.SettingChanged += RequiredBench_SettingChanged;

            // Lambda Crafting cost change
            void BuildRecipeChanged_SettingChanged(object sender, EventArgs e) {
                if (sender.GetType() == typeof(ConfigEntry<string>)) {
                    ConfigEntry<string> sendEntry = (ConfigEntry<string>)sender;
                    if (ValConfig.EnableDebugMode.Value == true) { Logger.LogInfo($"Recieved new piece config {sendEntry.Value}"); }
                    // return if its an invalid change
                    if (PieceRecipeConfigUpdater(jbuildpiece, sendEntry.Value) == false) { return; }
                }

                List<RequirementConfig> recipe = new List<RequirementConfig>();
                if (ValConfig.EnableDebugMode.Value == true) { Logger.LogInfo("Validating and building requirementsConfig"); }
                foreach (var entry in jbuildpiece.Cfgs.UpdatedCost) {
                    if (PrefabManager.Instance.GetPrefab(entry.prefab) == null) {
                        if (ValConfig.EnableDebugMode.Value == true) { Logger.LogInfo($"{entry.prefab} is not a valid prefab, skipping recipe update."); }
                        return;
                    }
                    if (ValConfig.EnableDebugMode.Value == true) { Logger.LogInfo($"Checking entry {entry.prefab} amount:{entry.amount} refund?:{entry.refundable}"); }
                    recipe.Add(new RequirementConfig { Item = entry.prefab, Amount = entry.amount, Recover = entry.refundable });
                }
                if (jbuildpiece.Cfgs.Enabled.Value) {
                    if (ValConfig.EnableDebugMode.Value == true) { Logger.LogInfo("Updating Piece."); }
                    List<Piece.Requirement> newRequirements = new List<Piece.Requirement>();
                    foreach (var recipe_entry in recipe) {
                        //recipe_entry.FixReferences();
                        Piece.Requirement piece_req = new Piece.Requirement();
                        piece_req.m_resItem = PrefabManager.Instance.GetPrefab(recipe_entry.Item.Replace("JVLmock_", ""))?.GetComponent<ItemDrop>();
                        piece_req.m_amount = recipe_entry.Amount;
                        piece_req.m_recover = recipe_entry.Recover;
                        newRequirements.Add(piece_req);
                    }
                    if (ValConfig.EnableDebugMode.Value == true) { Logger.LogInfo($"Fixed mock requirements {newRequirements.Count}."); }
                    jbuildpiece.Objs.ScenePrefab.GetComponent<Piece>().m_resources = newRequirements.ToArray();
                    if (ValConfig.EnableDebugMode.Value == true) { Logger.LogInfo($"New requirements set {jbuildpiece.Objs.ScenePrefab.GetComponent<Piece>().m_resources}."); }
                } else {
                    // Set this piece not craftable
                    jbuildpiece.Objs.ScenePrefab.GetComponent<Piece>().m_enabled = false;
                }
            }

            // Setup enable/disable
            jbuildpiece.Cfgs.Enabled = ValConfig.BindServerConfig($"{jbuildpiece.Name}", $"Enabled", jbuildpiece.Enabled, $"Enable/Disable the {jbuildpiece.Name}.");
            jbuildpiece.Cfgs.Enabled.SettingChanged += BuildRecipeChanged_SettingChanged;
            // Setup piece category
            jbuildpiece.Cfgs.PieceCategory = ValConfig.BindServerConfig($"{jbuildpiece.Name}", $"Piece Category", jbuildpiece.Category.ToString(), "Piece category for building.", PieceCategories.GetAcceptableValueList());
            void CraftingCategory_SettingChanged(object sender, EventArgs e) {
                jbuildpiece.Objs.ScenePrefab.GetComponent<Piece>().m_category = (PieceCategory)Enum.Parse(typeof(PieceCategory), PieceCategories.GetInternalName(jbuildpiece.Cfgs.PieceCategory.Value));
            }
            jbuildpiece.Cfgs.PieceCategory.SettingChanged += CraftingCategory_SettingChanged;

            // Build out the internal default recipe
            List<string> raw_recipe_default = new List<string>();
            foreach (var entry in jbuildpiece.PieceCost) { raw_recipe_default.Add($"{entry.prefab},{entry.amount},{entry.refundable}"); }
            string recipe_cfg_default = string.Join("|", raw_recipe_default);
            // Wire up the config and on-change for piece costs
            jbuildpiece.Cfgs.PieceCost = ValConfig.BindServerConfig($"{jbuildpiece.Name}", $"Building Cost", recipe_cfg_default, $"Cost to build. Find item ids: https://valheim.fandom.com/wiki/Item_IDs Format: resouce_id,amount,refund eg: Wood,8,true|LeatherScraps,4,false", advanced: true);
            if (PieceRecipeConfigUpdater(jbuildpiece, jbuildpiece.Cfgs.PieceCost.Value, false) == false) {
                Logger.LogWarning($"{jbuildpiece.Name} has an invalid piece cost. The default will be used instead.");
                PieceRecipeConfigUpdater(jbuildpiece, recipe_cfg_default, false);
            }


            jbuildpiece.Cfgs.PieceCost.SettingChanged += BuildRecipeChanged_SettingChanged;
            List<RequirementConfig> recipe = new List<RequirementConfig>();
            foreach (var entry in jbuildpiece.Cfgs.UpdatedCost) {
                recipe.Add(new RequirementConfig { Item = entry.prefab, Amount = entry.amount, Recover = entry.refundable });
            }

            // Build the jotunn piece definition
            PieceConfig piececfg = new PieceConfig() {
                CraftingStation = jbuildpiece.Cfgs.Workbench.Value,
                PieceTable = PieceTables.Hammer,
                Category = jbuildpiece.Cfgs.PieceCategory.Value,
                Icon = jbuildpiece.Objs.Sprite,
                Requirements = recipe.ToArray()
            };
            // Add the updated piece to the piece manager
            PieceManager.Instance.AddPiece(new CustomPiece(jbuildpiece.Objs.Prefab, fixReference: true, piececfg));
        }

        private static bool PieceRecipeConfigUpdater(JotunnBuildPiece jbuildpiece, string rawrecipe, bool during_runtime = true) {
            String[] RawRecipeEntries = rawrecipe.Split('|');
            // Logger.LogInfo($"{RawRecipeEntries.Length} {string.Join(", ", RawRecipeEntries)}");
            List<PieceCost> updated_pieceRecipe = new List<PieceCost>();
            // we only clear out the default recipe if there is recipe data provided, otherwise we will continue to use the default recipe
            // TODO: Add a sanity check to ensure that recipe formatting is correct
            if (RawRecipeEntries.Length >= 1) {
                foreach (String recipe_entry in RawRecipeEntries) {
                    //Logger.LogInfo($"{recipe_entry}");
                    String[] recipe_segments = recipe_entry.Split(',');
                    if (recipe_segments.Length != 3) {
                        Logger.LogWarning($"{recipe_entry} is invalid, it does not have enough segments. Proper format is: PREFABNAME,COST,REFUND_BOOL eg: Wood,8,false");
                        return false;
                    }
                    if (ValConfig.EnableDebugMode.Value == true) {
                        String split_segments = "";
                        foreach (String segment in recipe_segments) {
                            split_segments += $" {segment}";
                        }
                        //Logger.LogInfo($"recipe segments: {split_segments} from {recipe_entry}");
                    }
                    // Add a sanity check to ensure the prefab we are trying to use exists
                    // This can only happen during runtime after pieces are available otherwise it will cause errors
                    if (during_runtime) {
                        if (PrefabManager.Instance.GetPrefab(recipe_segments[0]) == null) {
                            Logger.LogWarning($"{recipe_segments[0]} is an invalid prefab and does not exist.");
                            return false;
                        }
                    }
                    if (recipe_segments[0].Length == 0 || recipe_segments[1].Length == 0 || recipe_segments[2].Length == 0) {
                        Logger.LogWarning($"{recipe_entry} is invalid, one segment does not have enough data. Proper format is: PREFABNAME,CRAFT_COST,REFUND_BOOL eg: Wood,8,false");
                        return false;
                    }
                    bool refund_flag_parse;
                    if (bool.TryParse(recipe_segments[2], out refund_flag_parse) == false) {
                        Logger.LogWarning($"{recipe_entry} is invalid, the REFUND_BOOL could not be parsed to (true/false). Proper format is: PREFABNAME,CRAFT_COST,REFUND_BOOL eg: Wood,8,false");
                        return false;
                    }

                    if (ValConfig.EnableDebugMode.Value == true) {
                        Logger.LogInfo($"prefab: {recipe_segments[0]} c:{recipe_segments[1]} u:{recipe_segments[2]}");
                    }
                    updated_pieceRecipe.Add(new PieceCost() { prefab = recipe_segments[0], amount = Int32.Parse(recipe_segments[1]), refundable = refund_flag_parse });
                }
                //Logger.LogInfo("Done parsing recipe");
                jbuildpiece.Cfgs.UpdatedCost.Clear();
                foreach (var entry in updated_pieceRecipe) { jbuildpiece.Cfgs.UpdatedCost.Add(entry); }
                //Logger.LogInfo("Set UpdatedRecipe");
                if (ValConfig.EnableDebugMode.Value == true) {
                    String recipe_string = "";
                    foreach(var entry in updated_pieceRecipe) {
                        recipe_string += $" {entry.prefab} c:{entry.amount} r:{entry.refundable}";
                    }
                    Logger.LogInfo($"Updated recipe:{recipe_string}");
                }
                return true;
            } else {
                Logger.LogWarning($"Invalid recipe: {rawrecipe}. defaults will be used. Check your prefab names.");
                jbuildpiece.Cfgs.UpdatedCost = jbuildpiece.PieceCost;

            }
            return false;
        }
    }
}
