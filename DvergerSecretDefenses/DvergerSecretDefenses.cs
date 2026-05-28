using BepInEx;
using BepInEx.Logging;
using DvergerSecretDefenses.common;
using DvergerSecretDefenses.Common;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using PlayFab.Internal;
using System.Collections.Generic;
using UnityEngine;

namespace DvergerSecretDefenses
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    internal class DvergerSecretDefenses : BaseUnityPlugin
    {
        public const string PluginGUID = "com.jotunn.jotunnmodstub";
        public const string PluginName = "DvergerSecretDefenses";
        public const string PluginVersion = "0.0.1";

        internal static ManualLogSource Log;
        internal ValConfig cfg;

        // Use this class to add your own localization to the game
        // https://valheim-modding.github.io/Jotunn/tutorials/localization.html
        public static CustomLocalization Localization = LocalizationManager.Instance.GetLocalization();
        public static AssetBundle EmbeddedResourceBundle;

        public void Awake()
        {
            Log = this.Logger;
            cfg = new ValConfig(Config);

            EmbeddedResourceBundle = AssetUtils.LoadAssetBundleFromResources("DvergerSecretDefenses.Assets.zapper_assets", typeof(DvergerSecretDefenses).Assembly);

            LocalizationLoader.AddLocalizations();
            AddPieces();
        }

        public void AddPieces() {
            JotunnPiece.JotunnBuildPiece DS_Electric = new JotunnPiece.JotunnBuildPiece();
            DS_Electric.Name = "Dverger Thundercage";
            DS_Electric.Prefab = "DS_ElectricTrap";
            DS_Electric.Sprite = "DS_ElectricTrap";
            DS_Electric.Workbench = "forge";
            DS_Electric.Category = Piece.PieceCategory.Misc;
            DS_Electric.PieceCost = new List<JotunnPiece.PieceCost>() {
                { new JotunnPiece.PieceCost() { prefab = "BlackMarble", amount = 25, refundable = true } },
                { new JotunnPiece.PieceCost() { prefab = "Copper", amount = 12, refundable = true } },
                { new JotunnPiece.PieceCost() { prefab = "BlackMetal", amount = 12, refundable = true } },
                { new JotunnPiece.PieceCost() { prefab = "Thunderstone", amount = 8, refundable = true } }
            };
            JotunnPiece.RegisterJotunnPiece(DS_Electric);
        }
    }
}