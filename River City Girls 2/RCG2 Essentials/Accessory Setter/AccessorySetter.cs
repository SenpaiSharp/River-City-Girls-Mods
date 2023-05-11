using System;
using System.Collections.Generic;
using RCG.Rollback.Components;
using Tools.BinaryRollback;
using MelonLoader;
using UnityEngine;
using TextFloater;
using RCG;

namespace RCG2Mods
{
    internal enum Accesories
    {
        Empty = 0,
        Keep_Same = 1,
        Athletic_Bottom,
        Athletic_Top,
        Bomb_Bottom,
        Bomb_Bra,
        Bunny_Button,
        Choker,
        Clown_Makeup,
        Coachs_Hat,
        Coin_Purse,
        Deco_Nails,
        Eyeshadow,
        Fake_Tattoos,
        False_Lashes,
        Fishnet_Top,
        Frilly_Bottom,
        Frilly_Bra,
        Frost_Sigil,
        Gamer_Guide,
        Glass_Mouthgaurd,
        Goth_Shirt,
        Gym_Shorts,
        Heart_Earrings,
        Heart_Reactor,
        Knife_Earrings,
        Lantern,
        Lockpick_Kit,
        Love_Lettter,
        Makeup_Kit,
        Military_Belt,
        Multitool,
        Muscle_Charm,
        Music_Player,
        Padlock_Bra,
        Pepper_Spray,
        Power_Mitten,
        Ribbon_Bra,
        Scissors,
        Sign_Necklace,
        Smart_Watch,
        Speed_Runners,
        Spiked_Knuckles,
        Tank_Button,
        Teen_Magazine,
        Trianons_Trinket,
        Tuna_Shirt,
        Vampire_Teeth,
        Virtual_Pet,
        Witch_Bottle,
        Wolf_Locket,
        Wool_Socks,
        Wrestling_Singlet
    }

    internal static class AccessoriesExtensions
    {
        public static string GetName(this Accesories accessory)
        {
            switch (accessory)
            {
                case Accesories.Trianons_Trinket:
                    {
                        return "Trianon's Trinket";
                    }
                case Accesories.Coachs_Hat:
                    {
                        return "Coach's Hat";
                    }
                case Accesories.Empty:
                case Accesories.Keep_Same:
                    {
                        return string.Empty;
                    }
                default:
                    {
                        return accessory.ToString().Replace("_", " ");
                    }
            }
        }
    }

    /// <summary>
    /// A Category for Accessory Sets.
    /// </summary>
    internal class CategorySet
    {
        #region Preferences
        internal MelonPreferences_Category category;
        internal MelonPreferences_Entry<int> setCount;
        #endregion

        #region Constructor and Initialization
        /// <summary>
        /// Create a new CategorySet
        /// </summary>
        /// <param name="player">The player this category is for. 0 to create one default set for everyone.</param>
        public CategorySet(int player)
        {
            // Format based on player.
            string display = (player == 0) ? "Accessory Sets" : string.Format("Accessory Sets Player {0}", player);
            string identifier = (player == 0) ? "AccessorySetterPlayer1Sets" : string.Format("AccessorySetterPlayer{0}Sets", player);

            // Create
            category = MelonPreferences.CreateCategory(identifier, display);
            category.SetFilePath("UserData/AccessorySetter.cfg");

            // Make our options
            setCount = category.CreateEntry(
                identifier: "AccessoryCycleSetsCount",
                default_value: 3,
                display_name: "How many accessory sets should be in the cycle? (Requires Restart)");

            for (int i = 0; i < setCount.Value; i++)
            {
                CreateEntrySet(i);
            }
        }

        /// <summary>
        /// Creates Set Pair entry options.
        /// </summary>
        /// <param name="setNumber">The number of pairs to make</param>
        private void CreateEntrySet(int setNumber)
        {
            // Format our strings.
            string identifier1 = string.Format("Set{0}Slot{1}", setNumber, 1);
            string identifier2 = string.Format("Set{0}Slot{1}", setNumber, 2);
            string identifierTextOverride = string.Format("Set{0}TextOverride", setNumber);
            string identifierColorOverride = string.Format("Set{0}ColorOverride", setNumber);
            string identifierPaletteSwitch = string.Format("Set{0}SwitchPalette", setNumber);

            string display1 = string.Format("Set: {0} Slot: {1}", setNumber, 1);
            string display2 = string.Format("Set: {0} Slot: {1}", setNumber, 2);
            string displayIdentifierTextOverride = string.Format("Set {0} Text Override", setNumber);
            string displayColorOverride = string.Format("Set {0} Color Override", setNumber);
            string displayPaletteSwitch = string.Format("Set {0} Switch to Palette (-1 to disable)", setNumber);

            // Add our options.
            category.CreateEntry<Accesories>(identifier1, Accesories.Empty, display1);
            category.CreateEntry<Accesories>(identifier2, Accesories.Empty, display2);

            // Advanced Options
            category.CreateEntry<string>(identifierTextOverride, string.Empty, displayIdentifierTextOverride, true);
            category.CreateEntry<Color>(identifierColorOverride, Color.clear, displayColorOverride, true);
            category.CreateEntry<int>(identifierPaletteSwitch, -1, displayPaletteSwitch, true);
        }
        #endregion
    }

    /// <summary>
    /// Class that can Cycle through sets of accessories.
    /// </summary>
    public class AccessorySetter
    {
        #region Preferences
        static internal MelonPreferences_Category mainCategory;
        static internal MelonPreferences_Entry<bool> showText;
        static internal MelonPreferences_Entry<float> textSize;
        static internal MelonPreferences_Entry<float> boxSize;
        static internal MelonPreferences_Entry<Color> textColor;
        static internal MelonPreferences_Entry<bool> force;
        static internal MelonPreferences_Entry<bool> perPlayerSets;

        static CategorySet defaultSet;
        static CategorySet player2Set;
        static CategorySet player3Set;
        static CategorySet player4set;

        // Set once, to force a reboot and to prevent chaos.
        static bool perPlayer;
        #endregion

        #region Fields
        /// <summary>
        /// The current set each player is on.
        /// </summary>
        static internal int[] SetIndexes = new int[4];

        /// <summary>
        /// Quick reference for Accessories and their Hashes. Might change in future RCG2 updates. Not using an Enum because it affects sorting in MelonPreferencesManager.
        /// </summary>
        static internal Dictionary<Accesories, int> valuePairs = new Dictionary<Accesories, int>
        {
            { Accesories.Empty, 0 },
            { Accesories.Keep_Same, 1 },
            { Accesories.Athletic_Bottom , -1571602576 },
            { Accesories.Athletic_Top , -1880945162 },
            { Accesories.Bomb_Bottom , 885376415 },
            { Accesories.Bomb_Bra , 1928124976 },
            { Accesories.Bunny_Button , -593737404 },
            { Accesories.Choker , -71191246 },
            { Accesories.Clown_Makeup , -1108851171 },
            { Accesories.Coachs_Hat , -2049268358 },
            { Accesories.Coin_Purse , -2063825853 },
            { Accesories.Deco_Nails , 574185668 },
            { Accesories.Eyeshadow , 1532303300 },
            { Accesories.Fake_Tattoos , -866699062 },
            { Accesories.False_Lashes , -248285514 },
            { Accesories.Fishnet_Top , -1208443992 },
            { Accesories.Frilly_Bottom , 1810095161 },
            { Accesories.Frilly_Bra , 1206450084 },
            { Accesories.Frost_Sigil , -45505303 },
            { Accesories.Gamer_Guide , -62252525 },
            { Accesories.Glass_Mouthgaurd , -347800326 },
            { Accesories.Goth_Shirt , -214056367 },
            { Accesories.Gym_Shorts , -1818552554 },
            { Accesories.Heart_Earrings , 836932906 },
            { Accesories.Heart_Reactor , 459213802 },
            { Accesories.Knife_Earrings , 357950378 },
            { Accesories.Lantern , -1268862382 },
            { Accesories.Lockpick_Kit , 424944271 },
            { Accesories.Love_Lettter , 1889439154 },
            { Accesories.Makeup_Kit , -380400030 },
            { Accesories.Military_Belt , 915212178 },
            { Accesories.Multitool , -404042986 },
            { Accesories.Muscle_Charm , -1242294733 },
            { Accesories.Music_Player , 945610029 },
            { Accesories.Padlock_Bra , 1499822631 },
            { Accesories.Pepper_Spray , 881494768 },
            { Accesories.Power_Mitten , -915324928 },
            { Accesories.Ribbon_Bra , -1901771230 },
            { Accesories.Scissors , 33828966 },
            { Accesories.Sign_Necklace , 266344232 },
            { Accesories.Smart_Watch , -847583387 },
            { Accesories.Speed_Runners , 977409536 },
            { Accesories.Spiked_Knuckles , -829580351 },
            { Accesories.Tank_Button , 2031456163 },
            { Accesories.Teen_Magazine , 617188822 },
            { Accesories.Trianons_Trinket , 2051535481 },
            { Accesories.Tuna_Shirt , 167792012 },
            { Accesories.Vampire_Teeth , 1084126562 },
            { Accesories.Virtual_Pet , 609057255 },
            { Accesories.Witch_Bottle , 36887812 },
            { Accesories.Wolf_Locket , -524969944 },
            { Accesories.Wool_Socks , 225808166 },
            { Accesories.Wrestling_Singlet , 655791321 }
        };

        /// <summary>
        /// Check for Intialization, which only needs to run once.
        /// </summary>
        private static bool Initialized;
        #endregion

        #region Initializer
        static public void Initialize()
        {
            if (!Initialized)
            {
                // To not run this more than once.
                Initialized = true;

                // Create the main category.
                mainCategory = MelonPreferences.CreateCategory("Accessory Setter Options");
                mainCategory.SetFilePath("UserData/AccessorySetter.cfg");

                // Set up our options.
                force = mainCategory.CreateEntry<bool>(
                    identifier: "ForceAccessoryEquip",
                    default_value: true,
                    display_name: "Force Accesories to equip, even if not owned or doubled up?");

                showText = mainCategory.CreateEntry(
                    identifier: "ShowAnimatedText",
                    default_value: true,
                    display_name: "Show text on player when equipping?");

                textColor = mainCategory.CreateEntry(
                    identifier: "EquippTextColor",
                    default_value: Color.yellow,
                    display_name: "Equipped Text Color");

                perPlayerSets = mainCategory.CreateEntry<bool>(
                    identifier: "PerPlayerSets",
                    default_value: false,
                    display_name: "Use Per-Player Sets? (Requires Restart)");

                // Advanced Options
                textSize = mainCategory.CreateEntry<float>(
                    identifier: "TextSize",
                    default_value: 40f,
                    display_name: "Text Size",
                    is_hidden: true)
                    as MelonPreferences_Entry<float>;

                boxSize = mainCategory.CreateEntry<float>(
                    identifier: "TextBoxSize",
                    default_value: 2f,
                    display_name: "Text Bounding Box Size (Try adjusting for better formatting)",
                    is_hidden: true)
                    as MelonPreferences_Entry<float>;

                // Set this once, so regardless if the preference is changed, this stays set all session. Require a reboot.
                perPlayer = perPlayerSets.Value;

                if (perPlayer)
                {
                    defaultSet = new CategorySet(1);
                    player2Set = new CategorySet(2);
                    player3Set = new CategorySet(3);
                    player4set = new CategorySet(4);
                }
                else
                {
                    defaultSet = new CategorySet(0);
                }
            }
        }
        #endregion

        #region Functions
        /// <summary>
        /// Cycle a player's accessories.
        /// </summary>
        /// <param name="iter">Main SimulationIterator</param>
        /// <param name="player">Player we are cycling.</param>
        static public void CyclePlayerAccessories(SimulationIteration iter, PlayerControllerEntity player, bool reverse = false)
        { 
            int playerId = player.LocalID;
             
            // Advance or reverse our player's index, reset if necessary
            if (!reverse)
            {
                SetIndexes[playerId]++;

                if (SetIndexes[playerId] > defaultSet.setCount.Value)
                {
                    // We use 1 because this info gets passed to the person playing and starting with 0 probably feel weird to them.
                    SetIndexes[playerId] = 0;
                }
            }
            else
            {
                SetIndexes[playerId]--;

                if (SetIndexes[playerId] < 0)
                {
                    SetIndexes[playerId] = defaultSet.setCount.Value - 1;
                }
            }
            
            // Apply does the actual changing.
            Apply(iter, player);
        }

        /// <summary>
        /// Get the player set based on player ID.
        /// </summary>
        /// <param name="playerId">ID of the player the set we want belongs to.</param>
        /// <returns></returns>
        private static CategorySet GetCategory(int playerId)
        {
            if (perPlayer)
            {
                switch (playerId)
                {
                    default: return defaultSet;
                    case 2: return player2Set;
                    case 3: return player3Set;
                    case 4: return player4set;
                }
            }
            else // Only using default set for everyone.
            {
                return defaultSet;
            }
        }

        /// <summary>
        /// Set a player's accessories to the set by index.
        /// </summary>
        /// <param name="iter">Main Simulation</param>
        /// <param name="player">Player who is being adjusted.</param>
        /// <param name="setIndex">Index of the set that will be applied</param>
        static public void SetPlayerAccessories(SimulationIteration iter, PlayerControllerEntity player, int setIndex)
        {
            // Set our index.
            SetIndexes[player.LocalID] = Math.Abs(setIndex) % defaultSet.setCount.Value;

            // Apply does the actual changing.
            Apply(iter, player);
        }

        /// <summary>
        /// Does the Cycling.
        /// </summary>
        /// <param name="iter">Main SimulationIterator</param>
        /// <param name="player">Player we are cycling.</param>
        /// <param name="category">The player's sets preferences category.</param>
        /// <param name="index">The player's current index.</param>
        /// <param name="accessory1">Slot 1 Accessory</param>
        /// <param name="accessory2">Slot 2 Accessory</param>
        private static void Apply(SimulationIteration iter, PlayerControllerEntity player)
        {
            CategorySet category = GetCategory(player.LocalID);

            int index = SetIndexes[player.LocalID];

            // Get our SaveSlot Inventory
            SingletonHashedInventory inventory = player.SaveSlot.m_data.m_singletonInventory;

            // Get the preference indicators.
            string slot1string = string.Format("Set{0}Slot{1}", index, 1);
            string slot2string = string.Format("Set{0}Slot{1}", index, 2);

            // Get our Accessories.
            Accesories accessory1 = category.category.GetEntry<Accesories>(slot1string).Value;
            Accesories accessory2 = category.category.GetEntry<Accesories>(slot2string).Value;

            // Get their Hashes.
            int slot1hash = valuePairs[accessory1];
            int slot2hash = valuePairs[accessory2];

            // Determine if we have those accessories or use them anyway if being forced to.
            slot1hash = (accessory1 == Accesories.Keep_Same || force.Value || inventory.Contains(slot1hash)) ? slot1hash : 0;
            slot2hash = (accessory2 == Accesories.Keep_Same || force.Value || inventory.Contains(slot2hash)) ? slot2hash : 0;

            // Set our out going accessories to Empty if necessary.
            if (slot1hash == 0) accessory1 = Accesories.Empty;
            if (slot2hash == 0) accessory2 = Accesories.Empty;

            // Set, unless the we're keeping the same.
            if (accessory1 != Accesories.Keep_Same)
                player.SetEquipment(0, slot1hash, iter);
            if (accessory2 != Accesories.Keep_Same)
                player.SetEquipment(1, slot2hash, iter);
            
            // Switch palette if necessary.
            string paletteSwitchEntry = string.Format("Set{0}SwitchPalette", index);
            int paletteSwitchValue = category.category.GetEntry<int>(paletteSwitchEntry).Value;

            if (paletteSwitchValue >= 0)
            {
                string name = (player.Player as PlayerEntity).Character.ClassName;
                ColorTools.ChangeCharacterPalette(name, paletteSwitchValue);
            }

            // Draw text if necessary.
            if (showText.Value)
            {
                // Draw
                MakeText(iter, player.Player as PlayerEntity, index, accessory1, accessory2, category);
            }
        }

        /// <summary>
        /// Creates AnimatedTextEntity
        /// </summary>
        /// <param name="iter">Main SimulatorIterator</param>
        /// <param name="entity">PlayerEntity that the text is coming from</param>
        /// <param name="index">Player's Set index</param>
        /// <param name="accessory1">Slot 1 Accessory</param>
        /// <param name="accessory2">Slot 2 Accessory</param>
        /// <param name="category">Player's Set category</param>
        private static void MakeText(SimulationIteration iter, PlayerEntity entity, int index, Accesories accessory1, Accesories accessory2, CategorySet category)
        {
            // Determine what color we are using, default or override.
            Color color = category.category.GetEntry<Color>(string.Format("Set{0}ColorOverride", index)).Value;
            if (color == Color.clear) color = textColor.Value;

            // Determine if we're using default text or override
            string overrideText = category.category.GetEntry<string>(string.Format("Set{0}TextOverride", index)).Value;

            if (string.IsNullOrEmpty(overrideText)) // Draw the names of both items in succession.
            {
                string name1 = accessory1.GetName();
                string name2 = accessory2.GetName();

                if (name1.Length > 0)
                {
                    TextLib.PopText(iter, entity, accessory1.GetName(), color, textSize.Value, boxSize.Value);
                }
                if (name2.Length > 0)
                {
                    TextLib.PopText(iter, entity, accessory2.GetName(), color, textSize.Value, boxSize.Value);
                }
            }
            else // draw the override
            {
                TextLib.PopText(iter, entity, overrideText, color, textSize.Value, boxSize.Value);
            }
        }
        #endregion
    }
}