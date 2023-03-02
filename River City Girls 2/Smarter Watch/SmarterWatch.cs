using MelonLoader;
using HarmonyLib; 
using RCG.Rollback.Components;
using Tools.BinaryRollback; 

namespace RCG2Mods
{
    #region MelonLoader
    /// <summary>
    /// This Mod for RCG2 always applies the effects of the Smart Watch, so long as it has been purchased, regardless if it is equipped.
    /// </summary>
    public class SmarterWatch : MelonMod
    {
        #region DataMap Hash Values (May change with RCG2 updates)
        internal const int BuffNameHash = 1137501490;
        internal const int SmartWatchNameHash = -847583387;
        #endregion

        #region Functions
        /// <summary>
        /// Gets Character Data from the ControllerEntity and PlayerEntity.
        /// </summary>
        internal static SaveSlotEntity.CharacterData GetCharacterData(PlayerControllerEntity __instance, PlayerEntity Player)
        {
            return __instance.SaveSlot.GetCharacterData(Player.m_baseData.CharacterDescription.NameHash, true);
        }
        #endregion
    } 
    #endregion

    #region Harmony Patches
    /// <summary>
    /// Harmony PlayerControllerEntity.SetEquipment Patch that allows for the removal of the Smart Watch without debuffing the player.
    /// </summary>
    [HarmonyPatch(typeof(PlayerControllerEntity))]
    [HarmonyPatch("SetEquipment", typeof(byte), typeof(int), typeof(SimulationIteration), typeof(PlayerEntity))]
    class SetEquipmentPatch1
    {
        #region Prefixes
        /// <summary>
        /// Will catch if the slot being replaced contains the Smart Watch. Continues to original SetEquipment method.
        /// </summary>
        /// <param name="__instance">The PlayerControllerEntity calling SetEquipment()</param>
        /// <param name="slot">The Accessories slot being replaced or set.</param>
        /// <param name="hash">NameHash of the item being put into the slot.</param>
        /// <param name="iteration">Active Iteration</param>
        /// <param name="Player">The controlling player who is having their equipment set.</param>
        /// <returns>Always returns True to (hopefully) avoid Mod incompatibilites, allowing for other Prefixes.</returns>
        static bool Prefix(PlayerControllerEntity __instance, byte slot, int hash, SimulationIteration iteration, PlayerEntity Player)
        {
            // Apparently happens, judging from the game code.
            if (Player == null)
            {
                return true;
            }

            // Check if the slot being set already has the Smart Watch.
            if (slot == 0 && __instance.m_playerData.m_equipmentOne == SmarterWatch.SmartWatchNameHash)
            {
                // Remove the Watch before it gets to the main method, avoiding RemoveInstance calls that remove the Buff.
                __instance.m_playerData.m_equipmentOne = 0;

                // Just to be safe, edit the save slot data too.
                SaveSlotEntity.CharacterData characterData = SmarterWatch.GetCharacterData(__instance, Player);
                characterData.m_equipSlot1 = 0;
            }
            else if (__instance.m_playerData.m_equipmentTwo == SmarterWatch.SmartWatchNameHash) // Same as above but with slot 2.
            {
                __instance.m_playerData.m_equipmentTwo = 0;
                SaveSlotEntity.CharacterData characterData = SmarterWatch.GetCharacterData(__instance, Player);
                characterData.m_equipSlot2 = 0;
            }

            return true;
        }
        #endregion
    }

    /// <summary>
    /// Harmony PlayerControllerEntity.LoadCharacterDataEntity Patch that buffs the player with the effect of the Smart Watch on character loadup, so long as the Smart Watch is owned.
    /// </summary>
    [HarmonyPatch(typeof(PlayerControllerEntity))]
    [HarmonyPatch("LoadCharacterDataToPlayerEntity", typeof(SimulationIteration), typeof(PlayerEntity))]
    class LoadCharacterDataToPlayerEntityPatch1
    {
        #region Postfixes
        /// <summary>
        /// Will apply the Smart Watch buff after all other loading is done.
        /// </summary>
        /// <param name="__instance">The PlayerControllerEntity that called SetEquipment()</param>
        /// <param name="iteration">Active Iteration</param>
        /// <param name="player">Player to buff.</param>
        private static void Postfix(ref PlayerControllerEntity __instance, ref SimulationIteration iteration, PlayerEntity player)
        {
            // Check and make sure the watch has been acquired.
            if (__instance.SaveSlot.m_data.m_singletonInventory.Contains(SmarterWatch.SmartWatchNameHash))
            {
                // Find the definition and add it.
                var buff = BuffSystem.BuffDefinition.Find(SmarterWatch.BuffNameHash);
                player.AddBuff(buff, iteration, player);
            }
        } 
        #endregion
    } 
    #endregion
}