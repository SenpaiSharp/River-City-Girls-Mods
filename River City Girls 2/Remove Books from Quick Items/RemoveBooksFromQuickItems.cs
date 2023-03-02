using System;
using HarmonyLib;
using MelonLoader;
using RCG.ResourceMaps;
using RCG.Rollback.Components;
using RCG.UI.Screens.Widgets;
using System.Collections.Generic;
using Tools.BinaryRollback;

namespace RCG2Mods
{
    #region MelonLoader
    /// <summary>
    /// This Mod for RCG2 removes any Books in your inventory from the Quick Items Widget.
    /// </summary>
    public class RemoveBooksFromQuickItems : MelonMod
    {
        #region Hacks
        //HACK: Would like to remove the checks involving this. I think Harmony has better ways of doing this, I just haven't learned them yet.
        /// <summary>
        /// Determines if this mod should check for books, since most times it fires off is not related to our checks.
        /// </summary>
        internal static bool CatchAddEnabled = false;
        #endregion

        #region DataMap Hash Values (May change with RCG2 updates)
        internal const int JoysofToysHash = 440075120;
        internal const int JockJournal = -1802835302;
        internal const int Thrillhouse = 1937253779;
        internal const int Funstruction = 1883562947;
        #endregion
    }
    #endregion

    #region Harmony Patches
    /// <summary>
    /// Harmony System.Collections.Generic.List Patch that catches Books and prevents them from being edded.
    /// </summary>
    [HarmonyPatch(typeof(List<InventoryData>))]
    [HarmonyPatch("Add", typeof(InventoryData))]
    class AddPatch1
    {
        //HACK:  I don't really like this class. I don't know enough about Harmony yet and anything involving Generics is bit unpredictable and unreliable. For the purposes of this mod, it seems to work but I would like to change this out once I'm more knowledable.

        #region Harmony Fixes
        /// <summary>
        /// Checks if the item being added is a Book and prevents it from adding to the list.
        /// </summary>
        /// <param name="__instance">List of Inventory being queued up.</param>
        /// <param name="item">The Inventory being added to the list.</param>
        /// <returns>False if the item is a book, preventing the List.Add fucntion from being called. Otherwise True to allow.</returns>
        static bool Prefix(List<InventoryData> __instance, InventoryData item)
        {
            // Check if we're checking for Books during the Quick Items widget functions.
            if (RemoveBooksFromQuickItems.CatchAddEnabled)
            {
                // For some reason, the very first time the game checks for Quick Item Inventory, the list comes back with some errenous objects. Make sure this an object we want.
                var inv = item as ConsumableData;
                if (inv != null)
                {
                    // Check the item against the known book hashes. If it's one of them, we don't want to add.

                    return (inv.NameHash != RemoveBooksFromQuickItems.Funstruction)
                        && (inv.NameHash != RemoveBooksFromQuickItems.JockJournal) 
                        && (inv.NameHash != RemoveBooksFromQuickItems.JoysofToysHash)
                        && (inv.NameHash != RemoveBooksFromQuickItems.Thrillhouse);
                  
                }
            }

            // Run List.Add as normal.
            return true;
        }
        #endregion
    }

    /// <summary>
    /// Harmony Game_Player_QuickItem.UpdateIteration Patch that controls when this mod should be active.
    /// </summary>
    [HarmonyPatch(typeof(UI_Game_Player_QuickItem))]
    [HarmonyPatch("UpdateIteration", typeof(PlayerControllerEntity), typeof(SimulationIteration))]
    class UpdateIterationPatch1
    {
        #region Harmony Fixes
        /// <summary>
        /// Enables CatchAdd
        /// </summary>
        /// <returns>Always returns true to allow List.Add to run.</returns>
        static bool Prefix()
        {
            return RemoveBooksFromQuickItems.CatchAddEnabled = true;
        }

        /// <summary>
        /// Disables CatchAdd
        /// </summary>
        static void Postfix()
        {
            RemoveBooksFromQuickItems.CatchAddEnabled = false;
        }
        #endregion
    }

    /// <summary>
    /// Harmony Game_Player_QuickItem.AnimateCycle Patch that controls when this mod should be active.
    /// </summary>
    [HarmonyPatch(typeof(UI_Game_Player_QuickItem))]
    [HarmonyPatch("AnimateCycle", typeof(bool), typeof(PlayerControllerEntity))]
    class AnimateCyclePatch1
    {
        #region Harmony Fixes
        /// <summary>
        /// Enables CatchAdd
        /// </summary>
        /// <returns>Always returns true to allow List.Add to run.</returns>
        static bool Prefix()
        {
            return RemoveBooksFromQuickItems.CatchAddEnabled = true;
        }

        /// <summary>
        /// Disables CatchAdd
        /// </summary>
        static void Postfix(UI_Game_Player_QuickItem __instance)
        {
            RemoveBooksFromQuickItems.CatchAddEnabled = false;
        }
        #endregion
    }

    /// <summary>
    /// Harmony Game_Player_QuickItem.UseItem Patch that controls when this mod should be active.
    /// </summary>
    [HarmonyPatch(typeof(UI_Game_Player_QuickItem))]
    [HarmonyPatch("UseItem", typeof(PlayerControllerEntity), typeof(PlayerEntity), typeof(SimulationIteration))]
    class UseItemPatch1
    {
        #region Harmony Fixes
        /// <summary>
        /// Enables CatchAdd
        /// </summary>
        /// <returns>Always returns true to allow List.Add to run.</returns>
        static bool Prefix()
        {
            return RemoveBooksFromQuickItems.CatchAddEnabled = true;
        }

        /// <summary>
        /// Disables CatchAdd
        /// </summary>
        static void Postfix()
        {
            RemoveBooksFromQuickItems.CatchAddEnabled = false;
        }
        #endregion
    } 
    #endregion
}
