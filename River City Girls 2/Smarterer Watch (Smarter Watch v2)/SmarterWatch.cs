using System;
using System.Collections.Generic;
using HarmonyLib;
using MelonLoader;
using Tools.BinaryRollback;
using Tools.BinaryPackets;
using RCG.Rollback.Components;
using PropertySerializer;
using RCG.UI.Screens;
using RCG.ResourceMaps;

namespace RCG2Mods
{    
    #region MelonLoader
    /// <summary>
    /// This Mod for RCG2 always applies the effects of the Smart Watch, so long as it has been purchased, regardless if it is equipped. Music Player equipping is also optional.
    /// </summary>
    internal class SmarterWatch : MelonMod
    {
        #region DataMap Hash Values (May change with RCG2 updates)
        internal const int SmartWatchHash = -847583387;
        internal const int MusicPlayerHash = 945610029;
        #endregion

        #region Finalize Bools
        //TODO: May replace these with an event system, some day. Mixed feelings.
        static internal bool JustBoughtWatch = false;
        static internal bool SwapNextFinalize = false;
        static internal bool ReverseSwapNextFinalize = false;
        #endregion

        #region Player Fields
        static internal List<(int Slot1, int Slot2)> EquipedItems;
        static internal SaveSlotEntity SaveBoughtOn;
        #endregion

        #region MelonLoader Preferences
        static MelonPreferences_Category prefCategory;
        static internal MelonPreferences_Entry<bool> IncludeMusicPlayer;
        public override void OnInitializeMelon()
        {
            prefCategory = MelonPreferences.CreateCategory("Smarter Watch");
            prefCategory.SetFilePath("UserData/SmarterWatch.cfg");
            IncludeMusicPlayer = prefCategory.CreateEntry<bool>(
                identifier: "EquipMusicPlayer",
                default_value: false,
                display_name: "Equip Music Player on store visits?");
        }
        #endregion
    } 
    #endregion

    #region Harmony Patches
    /// <summary>
    /// Harmony EquipmentData.OnItemAquired Patch that handles the set up of player buying the Smart Watch in store.
    /// </summary>
    [HarmonyPatch(typeof(EquipmentData))]
    [HarmonyPatch("OnItemAquired", typeof(SimulationIteration), typeof(PlayerEntity))]
    internal class OnItemAquiredPatch1
    {
        //TODO: 99% sure all these probably could be turned into a Postfixes, I'm just 1% too lazy right now. Check back when I have a more convienant save file.

        /// <summary>
        /// Will catch if the item bought is the Smart Watch and if so, set data to run on the next Finalize.
        /// </summary>
        /// <param name="__instance">The item being bought.</param>
        /// <param name="ent">The player buying it.</param>
        /// <returns>Always returns true to allow the method to continue.</returns>
        static bool Prefix(EquipmentData __instance, PlayerEntity ent)
        {
            if (ent == null)
            {
                return true;
            }

            if (__instance.NameHash == SmarterWatch.SmartWatchHash)
            {
                SmarterWatch.JustBoughtWatch = true;

                // Get the save slot of the current player, this might (as in, I don't know) factor into online play.
                var player = ent.Controller as PlayerControllerEntity;
                SmarterWatch.SaveBoughtOn = player.SaveSlot;
            }

            //Continue
            return true;
        }
    }

    /// <summary>
    /// Harmony UI_Shop.OnOpen Patch that handles the player entering a shop already owning the Smart Watch/Music Player.
    /// </summary>
    [HarmonyPatch(typeof(UI_Shop))]
    [HarmonyPatch("OnOpen", typeof(Container))]
    internal class OnOpenPatch1
    {
        /// <summary>
        /// Let's Finalize Patch know to swap on next frame.
        /// </summary>
        /// <returns>Always returns true.</returns>
        static bool Prefix()
        {
            SmarterWatch.SwapNextFinalize = true;
            return true;
        }
    }

    /// <summary>
    /// Harmony UI_Shop.OnClose Patch that handles the player entering a shop already owning the Smart Watch/Music Player.
    /// </summary>
    [HarmonyPatch(typeof(UI_Shop))]
    [HarmonyPatch("OnClose")]
    internal class OnClosePatch1
    {
        /// <summary>
        /// Let's Finalize Patch know to swap back on next frame.
        /// </summary>
        /// <returns>Always returns true.</returns>
        static bool Prefix()
        {
            SmarterWatch.ReverseSwapNextFinalize = true;
            return true;
        }
    }

    /// <summary>
    /// Harmony SimulationIteration.Finalize Patch that swaps Accessories in and out just before the main BinaryPacket is written.
    /// </summary>
    [HarmonyPatch(typeof(SimulationIteration))]
    [HarmonyPatch("Finalize", typeof(BinaryPacket))]
    internal class FinalizePatch1
    {
        /// <summary>
        /// Will catch the MainSimulation iterator and runs code on possible conditions of the Smart Watch/Music Player.
        /// </summary>
        /// <param name="__instance">the MainSimulation iterator</param>
        /// <returns>Always returns true.</returns>
        static bool Prefix(SimulationIteration __instance)
        {
            #region If the watch was bought during this store session.
            if (SmarterWatch.JustBoughtWatch)
            {
                // Make sure this is m_iterator
                if (__instance.IterationTag == "MainSimulation")
                {
                    // Set this so not run more than once.
                    SmarterWatch.JustBoughtWatch = false;

                    // Get the active players.
                    var players = new List<PlayerControllerEntity>();
                    __instance.GetEntities<PlayerControllerEntity>(players);

                    // Check each player to see if they are part of the Save Slot that bought the watch.
                    // They might not be if this is used online. If it even works online!
                    //TODO: Find out if this works online.
                    for (int i = 0; i < players.Count; i++)
                    {
                        if (SmarterWatch.SaveBoughtOn == players[i].SaveSlot)
                        {
                            // Put the Watch in the first slot.
                            players[i].SetEquipment(0, SmarterWatch.SmartWatchHash, __instance);
                        }
                    }

                    // No longer needed, null just to be safe since it's a static field that .
                    SmarterWatch.SaveBoughtOn = null;
                }
            }
            #endregion

            #region If the Smart Watch/Music Player are owned when entering the store.
            if (SmarterWatch.SwapNextFinalize)
            {
                if (__instance.IterationTag == "MainSimulation")
                {
                    SmarterWatch.SwapNextFinalize = false;

                    // Stores the equipment worn on entering the store.
                    SmarterWatch.EquipedItems = new List<(int Slot1, int Slot2)>();

                    var players = new List<PlayerControllerEntity>();
                    __instance.GetEntities<PlayerControllerEntity>(players);

                    for (int i = 0; i < players.Count; i++)
                    {
                        var playerData = players[i].m_playerData;

                        // Record each player's equipment. We store everybody to keep things simple with the list iterations.
                        SmarterWatch.EquipedItems.Add((playerData.m_equipmentOne, playerData.m_equipmentTwo));

                        // We set either the Smart Watch/Music Player or empty each slot.
                        // This keeps things simple and ensures we don't end up with edge cases where two of the same items end up equiped.
                        var inventory = players[i].SaveSlot.m_data.m_singletonInventory;
                        int firstSlot = inventory.Contains(SmarterWatch.SmartWatchHash) ? SmarterWatch.SmartWatchHash : 0;
                        bool useMusicPlayer = (SmarterWatch.IncludeMusicPlayer.Value && inventory.Contains(SmarterWatch.MusicPlayerHash));
                        int secondSlot = useMusicPlayer ? SmarterWatch.MusicPlayerHash : 0;

                        // Set
                        players[i].SetEquipment(0, firstSlot, __instance);
                        players[i].SetEquipment(1, secondSlot, __instance);
                    }
                }
            }
            #endregion

            #region If the player is leaving the store.
            else if (SmarterWatch.ReverseSwapNextFinalize)
            {
                if (__instance.IterationTag == "MainSimulation")
                {
                    SmarterWatch.ReverseSwapNextFinalize = false;

                    var players = new List<PlayerControllerEntity>();

                    __instance.GetEntities<PlayerControllerEntity>(players);

                    // Reequip what each player had on entering the store.
                    for (int i = 0; i < players.Count; i++)
                    {
                        players[i].SetEquipment(0, SmarterWatch.EquipedItems[i].Slot1, __instance);
                        players[i].SetEquipment(1, SmarterWatch.EquipedItems[i].Slot2, __instance);
                    }
                }
            }
            #endregion

            // Continue to the main method and hope this sticks!
            return true;
        }
    } 
    #endregion
}
