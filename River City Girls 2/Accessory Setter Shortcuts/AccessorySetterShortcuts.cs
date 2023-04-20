using System;
using MelonLoader;
using Rewired;
using Tools.BinaryRollback;
using RCG.Rollback.Components;
using KeyEntry = MelonLoader.MelonPreferences_Entry<RCG2Mods.KeyIdentifiers>;
using ButtonEntry = MelonLoader.MelonPreferences_Entry<RCG2Mods.JoystickIdentifiers>;

namespace RCG2Mods
{ 
    /// <summary>
    /// Shortcut for cycling, supporting low and high array ranges.
    /// </summary>
    internal abstract class CycleSetShortcut : Shortcut
    {
        #region Constructor
        /// <param name="playerNum">Index of controller player.</param>
        /// <param name="controllerType">Controller type for this shortcut.</param>
        /// <param name="elementIdentifierID">Trigger ID of button or key.</param>
        /// <param name="doubleTap">Require trigger to be double tapped to activate.</param>
        /// <param name="lowRange">Lowest index of the accessory array that should be used.</param>
        /// <param name="highRange">Highest index of the accessory array that should be used.</param>
        public CycleSetShortcut(int playerNum, ControllerType controllerType, int elementIdentifierID,
            bool doubleTap = false, int lowRange = -1, int highRange = -1)
            : base(playerNum, controllerType, elementIdentifierID, doubleTap)
        { }
        #endregion

        #region Functions
        /// <summary>
        /// Call Handler that calls cycle if a character name chan be determined.
        /// </summary>
        /// <param name="simulation">not used</param>
        /// <param name="entity">Entity controller the player character.</param>
        public override void Call(SimulationIteration simulation, PlayerControllerEntity entity)
        {
            // Get player entity.
            var player = entity.Player as PlayerEntity;

            if (player != null) // Pass name to cycle.
            {
                Cycle(simulation, entity);
            }
        }

        /// <summary>
        /// Calls AccessorySetter to cycle the character named.
        /// </summary> 
        public abstract void Cycle(SimulationIteration simulation, PlayerControllerEntity player);
        #endregion
    }

    /// <summary>
    /// Shortcut for cycling forward, supporting low and high array ranges.
    /// </summary>
    internal class CycleSetForwardShortcut : CycleSetShortcut
    {
        /// <summary>
        /// Forward Shortcut
        /// </summary>
        /// <param name="playerNum">Index of controlling player.</param>
        /// <param name="controllerType">Type of controller being used.</param>
        /// <param name="elementIdentifierID">Trigger ID of button or key.</param>
        /// <param name="doubleTap">Require trigger to be double tapped to activate.</param> 
        public CycleSetForwardShortcut(int playerNum, ControllerType controllerType, int elementIdentifierID,
            bool doubleTap = false)
            : base(playerNum, controllerType, elementIdentifierID, doubleTap) { }

        /// <summary>
        /// Calls for a forward cycle.
        /// </summary> 
        public override void Cycle(SimulationIteration simulation, PlayerControllerEntity player)
        {
            AccessorySetter.CyclePlayerAccessories(simulation, player);
        }
    }

    /// <summary>
    /// Shortcut for cycling backward, supporting low and high array ranges.
    /// </summary>
    internal class CycleSetBackwardShortcut : CycleSetShortcut
    {
        /// <summary>
        /// Backward Shortcut
        /// </summary>
        /// <param name="playerNum">Index of controlling player.</param>
        /// <param name="controllerType">Type of controller being used.</param>
        /// <param name="elementIdentifierID">Trigger ID of button or key.</param>
        /// <param name="doubleTap">Require trigger to be double tapped to activate.</param>
        public CycleSetBackwardShortcut(int playerNum, ControllerType controllerType, int elementIdentifierID,
            bool doubleTap = false)
            : base(playerNum, controllerType, elementIdentifierID, doubleTap) { }

        /// <summary>
        /// Calls for a backward cycle.
        /// </summary> 
        public override void Cycle(SimulationIteration simulation, PlayerControllerEntity player)
        {
            AccessorySetter.CyclePlayerAccessories(simulation, player, true);
        }
    }

    /// <summary>
    /// Container of a forward and backward swap shortcut assigned to specific player index and control type.
    /// </summary>
    internal class PlayerAccessoryShortcuts
    {
        #region Formatted Strings (const)
        const string identifier = "AccessoryPlayer{0}{1}{2}";
        const string display = "Player {0} Swap {1} {2}";
        const string warning = " (Does not apply to analog stick directions.)";
        const string tapidentifier = "AccessoryPlayer{0}DoubleTap";
        const string tapdisplay = "Require Double Tap for Player {0} {1}?";
        #endregion

        #region Melon Preferences
        MelonPreferences_Entry up;
        MelonPreferences_Entry down;
        MelonPreferences_Entry<bool> tap; 
        #endregion

        #region Fields
        ControllerType controller;
        int index;
        #endregion

        #region Shortcuts
        CycleSetBackwardShortcut backwardShortcut;
        CycleSetForwardShortcut forwardShortcut;
        #endregion

        #region Constructor
        /// <summary>
        /// Instanace for a player of a controller type.
        /// </summary>
        /// <param name="playerIndex">Index of player.</param>
        /// <param name="controllerType">Type of controller player is using.</param>
        public PlayerAccessoryShortcuts(int playerIndex, ControllerType controllerType)
        {
            index = playerIndex;
            controller = controllerType;
        }
        #endregion

        #region Functions
        /// <summary>
        /// Sets up the shortcuts for this Player.
        /// </summary>
        /// <param name="category">MelonPreference category preferences will be created for.</param>
        /// <param name="upID">Element ID of the controller trigger for swapping up acccessory set.</param>
        /// <param name="downID">Element ID of the controller trigger for swapping up accessory set.</param>
        /// <param name="doubleTap">Whether triggers need to be double tapped to activate.</param>
        /// <param name="lowIndex">The lowest part of the accessory array that should be accessed by this shortcut.</param>
        /// <param name="highIndex">The highest part of the accessory array that should be accessed by this shortcut.</param>
        public void Setup(MelonPreferences_Category category, int upID, int downID, bool doubleTap = false, int lowIndex = -1, int highIndex = -1)
        {
            if (controller == ControllerType.Keyboard)
            {
                // Create final strings from our preformatted strings based on this player index.
                string upidentifier = string.Format(identifier, index + 1, "Up", "Key");
                string downidentifier = string.Format(identifier, index + 1, "Down", "Key");
                string updisplay = string.Format(display, index + 1, "Up", "Key");
                string downdisplay = string.Format(display, index + 1, "Down", "Key");
                string dtapIdentifier = string.Format(tapidentifier, index + 1);
                string dtapdisplay = string.Format(tapdisplay, index + 1, "Keys");

                // Create categories.
                up = category.CreateEntry<KeyIdentifiers>(upidentifier, (KeyIdentifiers)upID, updisplay);
                down = category.CreateEntry<KeyIdentifiers>(downidentifier, (KeyIdentifiers)downID, downdisplay);
                tap = category.CreateEntry<bool>(dtapIdentifier, doubleTap, dtapdisplay); 

                // Create shortcut
                backwardShortcut = new CycleSetBackwardShortcut(index, controller, (int)((KeyEntry)up).Value, tap.Value);
                forwardShortcut = new CycleSetForwardShortcut(index, controller, (int)((KeyEntry)down).Value, tap.Value);
            }
            else if (controller == ControllerType.Joystick)
            {
                // Create final strings from our preformatted strings based on this player index.
                string upidentifier = string.Format(identifier, index + 1, "Up", "Button");
                string downidentifier = string.Format(identifier, index + 1, "Down", "Button");
                string updisplay = string.Format(display, index + 1, "Up", "Button");
                string downdisplay = string.Format(display, index + 1, "Down", "Button");
                string dtapIdentifier = string.Format(tapidentifier, index + 1);
                string dtapdisplay = string.Format(tapdisplay, index + 1, "Buttons") + warning;

                // Create categories.
                up = category.CreateEntry<JoystickIdentifiers>(upidentifier, (JoystickIdentifiers)upID, updisplay);
                down = category.CreateEntry<JoystickIdentifiers>(downidentifier, (JoystickIdentifiers)downID, downdisplay);
                tap = category.CreateEntry<bool>(dtapIdentifier, doubleTap, dtapdisplay); 

                // Create shortcut
                backwardShortcut = new CycleSetBackwardShortcut(index, controller, (int)((ButtonEntry)up).Value, tap.Value);
                forwardShortcut = new CycleSetForwardShortcut(index, controller, (int)((ButtonEntry)down).Value, tap.Value);
            }

            // 0 do not need be checked in the manager, add or remove on this basis.
            if (backwardShortcut.ElementIdentifierID != 0)
            {
                ShortcutsManager.AddShortcut(backwardShortcut);
            }
            if (forwardShortcut.ElementIdentifierID != 0)
            {
                ShortcutsManager.AddShortcut(forwardShortcut);
            }
        }

        /// <summary>
        /// Reassigns all shortcuts their connected preference values.
        /// </summary>
        public void Refresh()
        {
            // Update element identifiers based on keyboard or joystick types.
            if (controller == ControllerType.Keyboard)
            {
                backwardShortcut.ElementIdentifierID = (int)((KeyEntry)up).Value;
                forwardShortcut.ElementIdentifierID = (int)((KeyEntry)down).Value;
            }
            else if (controller == ControllerType.Joystick)
            {
                backwardShortcut.ElementIdentifierID = (int)((ButtonEntry)up).Value;
                forwardShortcut.ElementIdentifierID = (int)((ButtonEntry)down).Value;
            }

            // Update all other elements.
            backwardShortcut.RequireDoubleTap = tap.Value;
            forwardShortcut.RequireDoubleTap = tap.Value; 

            // Add or Remove to the manager, with 0 indicating no trigger.
            if (backwardShortcut.ElementIdentifierID != 0)
            {
                ShortcutsManager.AddShortcut(backwardShortcut);
            }
            else
            {
                ShortcutsManager.RemoveShortcut(backwardShortcut);
            }
            if (forwardShortcut.ElementIdentifierID != 0)
            {
                ShortcutsManager.AddShortcut(forwardShortcut);
            }
            else
            {
                ShortcutsManager.RemoveShortcut(forwardShortcut);
            }
        }
        #endregion
    }

    /// <summary>
    /// Shortcut for Accessory Quick Slots (Player 1 Keyboard Only)
    /// </summary>
    internal class AccesorySlotShortcut : Shortcut
    {
        #region Fields  
        public int index;
        #endregion

        #region Constructor
        /// <summary>
        /// A Shortcut for player 1 keyboard to swap to a specific index.
        /// </summary>
        /// <param name="key">Key to trigger the shortcut.</param>
        /// <param name="index">Index of the accessory array to switch to.</param>
        public AccesorySlotShortcut(KeyIdentifiers key, int index)
            : base(0, ControllerType.Keyboard, (int)key, false)
        {
            this.index = index;
        }
        #endregion

        #region Functions
        /// <summary>
        /// Changes the accessory index of the current Player 1 character.
        /// </summary>
        /// <param name="simulation">not used</param>
        /// <param name="player">Controlling player of the character whose accessory is being changed.</param>
        public override void Call(SimulationIteration simulation, PlayerControllerEntity player)
        {
            AccessorySetter.SetPlayerAccessories(simulation, player, index);
        }
        #endregion
    }

    /// <summary>
    /// Shortcut manager for accessory swapping.
    /// </summary>
    internal class AccessorySetterShortcuts : MelonMod
    {
        #region Fields
        PlayerAccessoryShortcuts keyboardPlayer;
        PlayerAccessoryShortcuts[] gamepadPlayers = new PlayerAccessoryShortcuts[4];
        AccesorySlotShortcut[] slotShortcuts = new AccesorySlotShortcut[4];
        MelonPreferences_Entry<KeyIdentifiers>[] slotKeys = new KeyEntry[4];
        MelonPreferences_Entry<int>[] slotIndexes = new MelonPreferences_Entry<int>[4];
        #endregion

        #region Initialization
        /// <summary>
        /// Initializes preferences and shortcuts.
        /// </summary>
        public override void OnInitializeMelon()
        {
            // Create Categories
            var mainCategory = MelonPreferences.CreateCategory("AccessorySetterShortcuts", "Accessory Keyboard Shortcuts");
            mainCategory.SetFilePath("UserData/Shortcuts.cfg");

            var controllerCategory = MelonPreferences.CreateCategory("AccessorySetterShortcutsControllers", "Accessory Controller Shortcuts");
            controllerCategory.SetFilePath("UserData/Shortcuts.cfg");

            // Create Quick Slot Shortucts and preferences for Keyboard
            for (int i = 0; i < slotShortcuts.Length; i++)
            {
                slotKeys[i] = mainCategory.CreateEntry<KeyIdentifiers>(string.Format("AccessoryQuickSlotKey{0}", i), (i == slotIndexes.Length -1)? KeyIdentifiers.NumRow0: KeyIdentifiers.NumRow7 + i, string.Format("Quick Slot {0} Key", i));
                slotIndexes[i] = mainCategory.CreateEntry<int>(string.Format("AccessoryQuickSlotIndex{0}", i), 0 + i, string.Format("Quick Slot {0} Accessory Index", i + 1));
                slotShortcuts[i] = new AccesorySlotShortcut(slotKeys[i].Value, slotIndexes[i].Value);

                if (slotShortcuts[i].ElementIdentifierID != 0)
                {
                    ShortcutsManager.AddShortcut(slotShortcuts[i]);
                }
            }

            // Create Swap Key Shortcuts and preferences for Keyboard
            keyboardPlayer = new PlayerAccessoryShortcuts(0, ControllerType.Keyboard);
            keyboardPlayer.Setup(mainCategory, (int)KeyIdentifiers.Insert, (int)KeyIdentifiers.Delete, false, -1, -1);

            // Create Swap Button Shortcuts and preferences for Gamepads
            for (int i = 0; i < gamepadPlayers.Length; i++)
            {
                gamepadPlayers[i] = new PlayerAccessoryShortcuts(i, ControllerType.Joystick);
                gamepadPlayers[i].Setup(controllerCategory, (int)JoystickIdentifiers.Right_Stick_Up, (int)JoystickIdentifiers.Right_Stick_Down, false, -1, -1);
            }

            // Register all preferences to refresh on any value change.
            for (int i = 0; i < mainCategory.Entries.Count; i++)
            {
                mainCategory.Entries[i].OnEntryValueChangedUntyped.Subscribe(UpdateShortcuts);
            }
        }
        #endregion

        #region Event Handling
        /// <summary>
        /// Refreshes all shortcuts to catch any preference changes.
        /// </summary>
        /// <param name="o">Old value, not used.</param>
        /// <param name="n">New value, not used</param>
        public void UpdateShortcuts(object o, object n)
        {
            // Refresh Keyboard Swaps
            keyboardPlayer.Refresh();

            // Refresh Gamepad Swaps
            for (int i = 0; i < gamepadPlayers.Length; i++)
            {
                gamepadPlayers[i].Refresh();
            }

            // Refresh Keyboard Quick Keys
            for (int i = 0; i < slotShortcuts.Length; i++)
            {
                slotShortcuts[i].ElementIdentifierID = (int)slotKeys[i].Value;
                slotShortcuts[i].index = slotIndexes[i].Value;

                if (slotShortcuts[i].ElementIdentifierID != 0)
                {
                    ShortcutsManager.AddShortcut(slotShortcuts[i]);
                }
                else
                {
                    ShortcutsManager.RemoveShortcut(slotShortcuts[i]);
                }
            }
        }
        #endregion
    }
}