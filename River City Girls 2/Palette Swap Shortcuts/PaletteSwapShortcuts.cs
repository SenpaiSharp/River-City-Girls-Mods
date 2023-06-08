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
    /// Shortcut for Palette Quick Slots (Player 1 Keyboard Only)
    /// </summary>
    public class PaletteSlotShortcut: Shortcut
    {
        #region Fields
        public int index;
        #endregion

        #region Constructor
        /// <summary>
        /// A Shortcut for player 1 keyboard to swap to a specific index.
        /// </summary>
        /// <param name="key">Key to trigger the shortcut.</param>
        /// <param name="index">Index of the palette array to switch to.</param>
        public PaletteSlotShortcut(KeyIdentifiers key, int index)
            : base(0, ControllerType.Keyboard, (int)key, false)
        {
            this.index = index;
        }
        #endregion

        #region Functions
        /// <summary>
        /// Changes the palette index of the current Player 1 character.
        /// </summary>
        /// <param name="simulation">not used</param>
        /// <param name="player">Controlling player of the character whose palette is being changed.</param>
        public override void Call(SimulationIteration simulation, PlayerControllerEntity player)
        {
            ColorTools.ChangeCharacterPalette((player.Player(simulation) as PlayerEntity).ClassName, index);
        } 
        #endregion
    }

    /// <summary>
    /// Shortcut for cycling, supporting low and high array ranges.
    /// </summary>
    public abstract class CycleShortcut : Shortcut
    {
        #region Properties
        /// <summary>
        /// Lowest index of the palatte array that should be used.
        /// </summary>
        public int CustomLowRange { get; set; }

        /// <summary>
        /// Highest index of the palatte array that should be used.
        /// </summary>
        public int CustomHighRange { get; set; }
        #endregion

        #region Constructor
        /// <param name="playerNum">Index of controller player.</param>
        /// <param name="controllerType">Controller type for this shortcut.</param>
        /// <param name="elementIdentifierID">Trigger ID of button or key.</param>
        /// <param name="doubleTap">Require trigger to be double tapped to activate.</param>
        /// <param name="lowRange">Lowest index of the palette array that should be used.</param>
        /// <param name="highRange">Highest index of the palette array that should be used.</param>
        public CycleShortcut(int playerNum, ControllerType controllerType, int elementIdentifierID,
            bool doubleTap = false, int lowRange = -1, int highRange = -1)
            : base(playerNum, controllerType, elementIdentifierID, doubleTap)
        {
            CustomLowRange = lowRange;
            CustomHighRange = highRange;
        }
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
            var player = entity.Player(simulation) as PlayerEntity;

            if (player != null) // Pass name to cycle.
            {
                Cycle(player.ClassName);
            }
        }

        /// <summary>
        /// Calls ColorTools to cycle the character named.
        /// </summary>
        /// <param name="characterName">Name of the character whose palette is changing.</param>
        public abstract void Cycle(string characterName); 
        #endregion
    }

    /// <summary>
    /// Shortcut for cycling forward, supporting low and high array ranges.
    /// </summary>
    public class CycleForwardShortcut : CycleShortcut
    {
        /// <summary>
        /// Forward Shortcut
        /// </summary>
        /// <param name="playerNum">Index of controlling player.</param>
        /// <param name="controllerType">Type of controller being used.</param>
        /// <param name="elementIdentifierID">Trigger ID of button or key.</param>
        /// <param name="doubleTap">Require trigger to be double tapped to activate.</param>
        /// <param name="lowRange">Lowest index of the palette array that should be used.</param>
        /// <param name="highRange">Highest index of the palette array that should be used.</param>
        public CycleForwardShortcut(int playerNum, ControllerType controllerType, int elementIdentifierID,
            bool doubleTap = false, int lowRange = -1, int highRange = -1)
            : base(playerNum, controllerType, elementIdentifierID, doubleTap, lowRange, highRange) { }

        /// <summary>
        /// Calls for a forward cycle.
        /// </summary>
        /// <param name="characterName">Name of character whose palette is being cycled.</param>
        public override void Cycle(string characterName)
        {
            ColorTools.IterateCharacterPaletteForward(characterName, CustomLowRange, CustomHighRange);
        }
    }

    /// <summary>
    /// Shortcut for cycling backward, supporting low and high array ranges.
    /// </summary>
    public class CycleBackwardShortcut : CycleShortcut
    {
        /// <summary>
        /// Backward Shortcut
        /// </summary>
        /// <param name="playerNum">Index of controlling player.</param>
        /// <param name="controllerType">Type of controller being used.</param>
        /// <param name="elementIdentifierID">Trigger ID of button or key.</param>
        /// <param name="doubleTap">Require trigger to be double tapped to activate.</param>
        /// <param name="lowRange">Lowest index of the palette array that should be used.</param>
        /// <param name="highRange">Highest index of the palette array that should be used.</param>
        public CycleBackwardShortcut(int playerNum, ControllerType controllerType, int elementIdentifierID,
            bool doubleTap = false, int lowRange = -1, int highRange = -1)
            : base(playerNum, controllerType, elementIdentifierID, doubleTap, lowRange, highRange) { }

        /// <summary>
        /// Calls for a backward cycle.
        /// </summary>
        /// <param name="characterName">Name of character whose palette is being cycled.</param>
        public override void Cycle(string characterName)
        {
            ColorTools.IterateCharacterPaletteBackward(characterName, CustomLowRange, CustomHighRange);
        }
    }

    /// <summary>
    /// Container of a forward and backward swap shortcut assigned to specific player index and control type.
    /// </summary>
    public class PlayerShortcuts
    {
        #region Formatted Strings (const)
        const string identifier = "PalettePlayer{0}{1}{2}";
        const string display = "Player {0} Swap {1} {2}";
        const string warning = " (Does not apply to analog stick directions.)";
        const string tapidentifier = "PalettePlayer{0}DoubleTap";
        const string tapdisplay = "Require Double Tap for Player {0} {1}?";
        const string highlowidentifier = "PalettePlayer{0}{1}Custom{2}Palette";
        const string highlowdisplay = "Custom Range for Palette Cycling: {0} Index Player {1} ({2}) (set to -1 to disable)";
        #endregion

        #region Melon Preferences
        MelonPreferences_Entry up;
        MelonPreferences_Entry down;
        MelonPreferences_Entry<bool> tap;
        MelonPreferences_Entry<int> low;
        MelonPreferences_Entry<int> high;
        #endregion

        #region Fields
        ControllerType controller;
        int index;
        #endregion

        #region Shortcuts
        CycleBackwardShortcut backwardShortcut;
        CycleForwardShortcut forwardShortcut;
        #endregion

        #region Constructor
        /// <summary>
        /// Instanace for a player of a controller type.
        /// </summary>
        /// <param name="playerIndex">Index of player.</param>
        /// <param name="controllerType">Type of controller player is using.</param>
        public PlayerShortcuts(int playerIndex, ControllerType controllerType)
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
        /// <param name="upID">Element ID of the controller trigger for swapping up palette.</param>
        /// <param name="downID">Element ID of the controller trigger for swapping up palette.</param>
        /// <param name="doubleTap">Whether triggers need to be double tapped to activate.</param>
        /// <param name="lowIndex">The lowest part of the palette array that should be accessed by this shortcut.</param>
        /// <param name="highIndex">The highest part of the palette array that should be accessed by this shortcut.</param>
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
                string dtapdisplay = string.Format(tapdisplay, index + 1, "Swap Keys");
                string lowIndexID = string.Format(highlowidentifier, index + 1, "Low", "Key");
                string lowIndexDisplay = string.Format(highlowdisplay, "Low", index + 1, "Keyboard");
                string highIndexID = string.Format(highlowidentifier, index + 1, "High", "Key");
                string highIndexDisplay = string.Format(highlowdisplay, "High", index + 1, "Keyboard");

                // Create categories.
                up = category.CreateEntry<KeyIdentifiers>(upidentifier, (KeyIdentifiers)upID, updisplay);
                down = category.CreateEntry<KeyIdentifiers>(downidentifier, (KeyIdentifiers)downID, downdisplay);
                tap = category.CreateEntry<bool>(dtapIdentifier, doubleTap, dtapdisplay);
                low = category.CreateEntry<int>(lowIndexID, lowIndex, lowIndexDisplay);
                low.IsHidden = true;
                high = category.CreateEntry<int>(highIndexID, highIndex, highIndexDisplay);
                high.IsHidden = true;

                // Create shortcut
                backwardShortcut = new CycleBackwardShortcut(index, controller, (int)((KeyEntry)up).Value, tap.Value, low.Value, high.Value);
                forwardShortcut = new CycleForwardShortcut(index, controller, (int)((KeyEntry)down).Value, tap.Value, low.Value, high.Value);
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
                string lowIndexID = string.Format(highlowidentifier, index + 1, "Low", "Button");
                string lowIndexDisplay = string.Format(highlowidentifier, "Low", index + 1, "Gamepad");
                string highIndexID = string.Format(highlowidentifier, index + 1, "High", "Button");
                string highIndexDisplay = string.Format(highlowidentifier, "High", index + 1, "Gamepad");

                // Create categories.
                up = category.CreateEntry<JoystickIdentifiers>(upidentifier, (JoystickIdentifiers)upID, updisplay);
                down = category.CreateEntry<JoystickIdentifiers>(downidentifier, (JoystickIdentifiers)downID, downdisplay);
                tap = category.CreateEntry<bool>(dtapIdentifier, doubleTap, dtapdisplay);
                low = category.CreateEntry<int>(lowIndexID, lowIndex, lowIndexDisplay);
                low.IsHidden = true;
                high = category.CreateEntry<int>(highIndexID, highIndex, highIndexDisplay);
                high.IsHidden = true;

                // Create shortcut
                backwardShortcut = new CycleBackwardShortcut(index, controller, (int)((ButtonEntry)up).Value, tap.Value, low.Value, high.Value);
                forwardShortcut = new CycleForwardShortcut(index, controller, (int)((ButtonEntry)down).Value, tap.Value, low.Value, high.Value);
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
            backwardShortcut.CustomLowRange = low.Value;
            backwardShortcut.CustomHighRange = high.Value;
            forwardShortcut.CustomLowRange = low.Value;
            forwardShortcut.CustomHighRange = high.Value;

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
    /// Shortcut manager for palette swapping.
    /// </summary>
    public class PaletteSwapShortcuts : MelonMod
    {
        #region Fields
        PlayerShortcuts keyboardPlayer;
        PlayerShortcuts[] gamepadPlayers = new PlayerShortcuts[4];
        PaletteSlotShortcut[] slotShortcuts = new PaletteSlotShortcut[4];
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
            var mainCategory = MelonPreferences.CreateCategory("PaletteSwapShortcuts", "Palettes Keyboard Shortcuts");
            mainCategory.SetFilePath("UserData/Shortcuts.cfg");

            var controllerCategory = MelonPreferences.CreateCategory("Palettes Controller Shortcuts");
            controllerCategory.SetFilePath("UserData/Shortcuts.cfg", false);

            // Create Quick Slot Shortucts and preferences for Keyboard
            for (int i = 0; i < slotShortcuts.Length; i++)
            {
                slotKeys[i] = mainCategory.CreateEntry<KeyIdentifiers>(string.Format("PaletteQuickSlotKey{0}", i), KeyIdentifiers.F1 + i, string.Format("Quick Slot {0} Key", i));
                slotIndexes[i] = mainCategory.CreateEntry<int>(string.Format("PaletteQuickSlotIndex{0}", i), 0 + i, string.Format("Quick Slot {0} Palette Index", i));
                slotShortcuts[i] = new PaletteSlotShortcut(slotKeys[i].Value, slotIndexes[i].Value);

                if (slotShortcuts[i].ElementIdentifierID != 0)
                {
                    ShortcutsManager.AddShortcut(slotShortcuts[i]);
                }
            }

            // Create Swap Key Shortcuts and preferences for Keyboard
            keyboardPlayer = new PlayerShortcuts(0, ControllerType.Keyboard);
            keyboardPlayer.Setup(mainCategory, (int)KeyIdentifiers.Page_Up, (int)KeyIdentifiers.Page_Down, false, -1, -1);

            // Create Swap Button Shortcuts and preferences for Gamepads
            for (int i = 0; i < gamepadPlayers.Length; i++)
            {
                gamepadPlayers[i] = new PlayerShortcuts(i, ControllerType.Joystick);
                gamepadPlayers[i].Setup(controllerCategory, (int)JoystickIdentifiers.None, (int)JoystickIdentifiers.Left_Stick_Button, true, -1, -1);
            }

            // Register all preferences to refresh on any value change.
            for (int i = 0; i < mainCategory.Entries.Count; i++)
            {
                mainCategory.Entries[i].OnEntryValueChangedUntyped.Subscribe(UpdateShortcuts);
            }
            for (int i = 0; i < controllerCategory.Entries.Count; i++)
            {
                controllerCategory.Entries[i].OnEntryValueChangedUntyped.Subscribe(UpdateShortcuts);
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
