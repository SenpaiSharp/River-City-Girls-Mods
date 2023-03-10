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

        #region Player Fields
        static internal List<(int Slot1, int Slot2)> EquipedItems;
        static internal SaveSlotEntity SaveBoughtOn;
        #endregion

        #region MelonLoader Preferences
        static MelonPreferences_Category prefCategory;
        static internal MelonPreferences_Entry<bool> IncludeMusicPlayer;
        public override void OnInitializeMelon()
        {
            prefCategory = MelonPreferences.CreateCategory("SmarterWatch");
            prefCategory.SetFilePath("UserData/SmarterWatch.cfg");
            IncludeMusicPlayer = prefCategory.CreateEntry<bool>(
                identifier: "EquipMusicPlayer",
                default_value: false,
                display_name: "Equip Music Player on store visits?");
        }
        #endregion

        #region Functions
        /// <summary>
        /// Equips the watch while in the store.
        /// </summary>
        /// <param name="iter">Main SimulationIterator</param>
        static internal void EquipAfterBuy(SimulationIteration iter)
        {
            // Get the active players.
            var players = new List<PlayerControllerEntity>();
            iter.GetEntities<PlayerControllerEntity>(players);

            // Check each player to see if they are part of the Save Slot that bought the watch.
            // They might not be if this is used online. If it even works online!
            //TODO: Find out if this works online.
            for (int i = 0; i < players.Count; i++)
            {
                if (SaveBoughtOn == players[i].SaveSlot)
                {
                    // Put the Watch in the first slot.
                    players[i].SetEquipment(0, SmartWatchHash, iter);
                }
            }
            // No longer needed, null just to be safe since it's a static field that.  
            SaveBoughtOn = null;
        }

        /// <summary>
        /// Makes equipment changes on entering the store.
        /// </summary>
        /// <param name="iter">Main SimulationIterator</param>
        static internal void Equip(SimulationIteration iter)
        {
            // Stores the equipment worn on entering the store.
            SmarterWatch.EquipedItems = new List<(int Slot1, int Slot2)>();

            var players = new List<PlayerControllerEntity>();
            iter.GetEntities<PlayerControllerEntity>(players);

            for (int i = 0; i < players.Count; i++)
            {
                var playerData = players[i].m_playerData;

                // Record each player's equipment. We store everybody to keep things simple with the list iterations.
                EquipedItems.Add((playerData.m_equipmentOne, playerData.m_equipmentTwo));

                // We set either the Smart Watch/Music Player or empty each slot.
                // This keeps things simple and ensures we don't end up with edge cases where two of the same items end up equiped.
                var inventory = players[i].SaveSlot.m_data.m_singletonInventory;

                int firstSlot = inventory.Contains(SmarterWatch.SmartWatchHash) ? SmarterWatch.SmartWatchHash : 0;
                bool useMusicPlayer = (SmarterWatch.IncludeMusicPlayer.Value && inventory.Contains(SmarterWatch.MusicPlayerHash));
                int secondSlot = useMusicPlayer ? SmarterWatch.MusicPlayerHash : 0;

                // Set
                players[i].SetEquipment(0, firstSlot, iter);
                players[i].SetEquipment(1, secondSlot, iter);
            }
        }

        /// <summary>
        /// Equips the equipment that was worn when entering the store.
        /// </summary>
        /// <param name="iter">Main SimulatorIterator</param>
        static internal void Unequip(SimulationIteration iter)
        {
            var players = new List<PlayerControllerEntity>();

            iter.GetEntities<PlayerControllerEntity>(players);

            // Reequip what each player had on entering the store.
            for (int i = 0; i < players.Count; i++)
            {
                players[i].SetEquipment(0, SmarterWatch.EquipedItems[i].Slot1, iter);
                players[i].SetEquipment(1, SmarterWatch.EquipedItems[i].Slot2, iter);
            }
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
                PreFinalizerHook.Subscribe(SmarterWatch.EquipAfterBuy);

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
            PreFinalizerHook.Subscribe(SmarterWatch.Equip);
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
            PreFinalizerHook.Subscribe(SmarterWatch.Unequip);
            return true;
        }
    }

  
    #endregion
}
