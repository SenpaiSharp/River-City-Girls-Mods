using System;
using System.Collections.Generic;
using MelonLoader;
using Rewired;
using Tools.BinaryRollback;
using RCG.Rollback.Components;

namespace RCG2Mods
{
    /// <summary>
    /// Rewired Keyboard Codes. Set up for MelonLoaderPrefereces.
    /// </summary>
    public enum KeyIdentifiers
    {
        None = 0,
        A = 1,
        B = 2,
        C = 3,
        D = 4,
        E = 5,
        F = 6,
        G = 7,
        H = 8,
        I = 9,
        J = 10,
        K = 11,
        L = 12,
        M = 13,
        N = 14,
        O = 15,
        P = 16,
        Q = 17,
        R = 18,
        S = 19,
        T = 20,
        U = 21,
        V = 22,
        W = 23,
        X = 24,
        Y = 25,
        Z = 26,
        Keypad_0 = 37,
        Keypad_1 = 38,
        Keypad_2 = 39,
        Keypad_3 = 40,
        Keypad_4 = 41,
        Keypad_5 = 42,
        Keypad_6 = 43,
        Keypad_7 = 44,
        Keypad_8 = 45,
        Keypad_9 = 46,
        Space = 54,
        Backspace = 55,
        Tab = 56,
        Clear = 57,
        Return = 58,
        Pause = 59,
        ESC = 60,
        Back_Quote = 87,
        Delete = 88,
        Up_Arrow = 89,
        Down_Arrow = 90,
        Right_Arrow = 91,
        Left_Arrow = 92,
        Insert = 93,
        Home = 94,
        End = 95,
        Page_Up = 96,
        Page_Down = 97,
        F1 = 98,
        F2 = 99,
        F3 = 100,
        F4 = 101,
        F5 = 102,
        F6 = 103,
        F7 = 104,
        F8 = 105,
        F9 = 106,
        F10 = 107,
        F11 = 108,
        F12 = 109,
        F13 = 110,
        F14 = 111,
        F15 = 112,
        Numlock = 113,
        Caps_Lock = 114,
        Scroll_Lock = 115,
        Right_Shift = 116,
        Left_Shift = 117,
        Right_Control = 118,
        Left_Control = 119,
        Right_Alt = 120,
        Left_Alt = 121,
        Right_Command = 122,
        Left_Command = 123,
        AltGr = 126,
        Help = 127,
        SysReq = 129,
        Break = 130,
        Menu = 131,
        NumRow0 = 27,
        NumRow1 = 28,
        NumRow2 = 29,
        NumRow3 = 30,
        NumRow4 = 31,
        NumRow5 = 32,
        NumRow6 = 33,
        NumRow7 = 34,
        NumRow8 = 35,
        NumRow9 = 36,
        Keypad_Del = 47,
        Keypad_Divide = 48,
        Keypad_Multiply = 49,
        Keypad_Subtract = 50,
        Keypad_Add = 51,
        Keypad_Enter = 52,
        Keypad_Equals = 53,
    }

    /// <summary>
    /// Rewired Controller Codes. Set up for MelonLoaderPrefereces.
    /// </summary>
    public enum JoystickIdentifiers
    {
        None = 0, // Technically this is Left Stick X but we don't use that.
        Left_Trigger = 4,
        Right_Trigger = 5,
        A = 6,
        B = 7,
        X = 8,
        Y = 9,
        Left_Bumper = 10,
        Right_Bumper = 11,
        Back = 12,
        Start = 13,
        Left_Stick_Button = 14,
        Right_Stick_Button = 15,
        D_Pad_Up = 16,
        D_Pad_Right = 17,
        D_Pad_Down = 18,
        D_Pad_Left = 19,
        Left_Stick_Up = 101, // These are custom.
        Left_Stick_Down = 102,
        Left_Stick_Left = 103,
        Left_Stick_Right = 104,
        Right_Stick_Up = 105,
        Right_Stick_Down = 106,
        Right_Stick_Left = 107,
        Right_Stick_Right = 108
    }

    /// <summary>
    /// A derivable class that contains the information needed to attach, activate and process a shortcut command for River City Girls 2.
    /// </summary>
    public abstract class Shortcut
    {
        #region Properties
        /// <summary>
        /// Controlling Player's Index
        /// </summary>
        public int PlayerNum { get; }

        /// <summary>
        /// Keyboard, Joystic, etc . . .
        /// </summary>
        public ControllerType ControllerType { get; }

        /// <summary>
        /// The key, button or stick direction to call this shortcut.
        /// </summary>
        public int ElementIdentifierID { get; set; }

        /// <summary>
        /// Determines if the activation requires a timely double tap to activate. Does not apply to analog stick triggers.
        /// </summary>
        public bool RequireDoubleTap { get; set; }
        #endregion

        #region Constructor
        /// <summary>
        /// Instance of a Shortcut with minimum required information. 
        /// </summary>
        /// <param name="playerNum">Index of player this shortcut is for.</param>
        /// <param name="controllerType">Type of controller the player is expected to be using.</param>
        /// <param name="elementIdentifierID">ID code of the key, button or stick direction to call this shortcut.</param>
        public Shortcut(int playerNum, ControllerType controllerType, int elementIdentifierID, bool doubleTap = false)
        {
            PlayerNum = playerNum;
            ControllerType = controllerType;
            ElementIdentifierID = elementIdentifierID;
            RequireDoubleTap = doubleTap;
        }
        #endregion

        #region Functions
        /// <summary>
        /// The action that should take place when this shortcut is called.
        /// </summary>
        /// <param name="simulation">Simulation Iteration being updated this frame.</param>
        /// <param name="player">The player this shortcut applies to.</param>
        abstract public void Call(SimulationIteration simulation, PlayerControllerEntity player);
        #endregion
    }

    /// <summary>
    /// Manager that can check shortcuts for activity and call them when activated.
    /// </summary>
    public class ShortcutsManager : MelonPlugin
    {
        #region Shortcut Lists
        /// <summary>
        /// Shortcuts that are checked for activity every frame.
        /// </summary>
        internal static List<Shortcut> Shortcuts = new List<Shortcut>();

        /// <summary>
        /// Shortcuts that are active this frame.
        /// </summary>
        internal static List<Shortcut> ActiveShortcuts = new List<Shortcut>();
        #endregion

        #region Analog Stick Calibrations
        const float CounterDeadzone = .35f;
        const float MinThreshold = .5f;
        #endregion

        #region Functions
        /// <summary>
        /// Adds a shortcut to be checked for activity each update, if it hasn't already been added.
        /// </summary>
        /// <param name="shortcut">Shortcut to add.</param>
        public static void AddShortcut(Shortcut shortcut)
        {
            // Make sure it's not already here, so it doesn't get checked multiple times to the same result.
            if (!Shortcuts.Contains(shortcut))
            {
                Shortcuts.Add(shortcut);
            }
        }

        /// <summary>
        /// Removes a shortcut from the activity checking, if it's already on the queue.
        /// </summary>
        /// <param name="shortcut"></param>
        public static void RemoveShortcut(Shortcut shortcut)
        {
            // Does nothing if not there, that's fine.
            Shortcuts.Remove(shortcut);
        }

        /// <summary>
        /// Checks each shortcut to see if it's been activated the current game frame.
        /// </summary>
        public override void OnUpdate()
        {
            if (Shortcuts.Count > 0)
            {
                // Get active players.
                var players = ReInput.players.GetPlayers(false);

                for (int i = 0; i < players.Count; i++)
                {
                    var player = players[i];

                    // Figure out if we're on Keyboard or Joystick (Gamepad)
                    var controller = player.controllers.GetLastActiveController();

                    if (controller != null)
                    {
                        if (controller.type == ControllerType.Keyboard)
                        {
                            controller = player.controllers.Keyboard;
                        }
                        else if (controller.type == ControllerType.Joystick)
                        {
                            controller = player.controllers.Joysticks[0];
                        }

                        // Check player against each shortcut.
                        for (int s = 0; s < Shortcuts.Count; s++)
                        {
                            var shortcut = Shortcuts[s];

                            // Shortcuts expect specific player and specific control type.
                            if (shortcut.PlayerNum == player.id && shortcut.ControllerType == controller.type)
                            {
                                // Handle Analog Stick Directions as triggers
                                // TODO: These controls are experimental and almost certainly need tweaking.
                                // Rewired seems to claim this can be done in some sort of automatic way.
                                // But I haven't figured that out and am not sure it'll work as I want.
                                if (controller.type == ControllerType.Joystick && shortcut.ElementIdentifierID > 100)
                                {
                                    int axisIndex = -1;

                                    switch (shortcut.ElementIdentifierID)
                                    {
                                        // Left Stick = 0
                                        case 101:
                                        case 102:
                                        case 103:
                                        case 104:
                                            {
                                                axisIndex = 0;
                                                break;
                                            }
                                        // Right Stick = 1
                                        case 105:
                                        case 106:
                                        case 107:
                                        case 108:
                                            {
                                                axisIndex = 1;
                                                break;
                                            }
                                    }

                                    if (axisIndex >= 0)
                                    {
                                        // Get our exact axis to check
                                        var axis = (controller as Joystick).GetAxis2D(axisIndex);
                                        var axisPrev = (controller as Joystick).GetAxis2DPrev(axisIndex);
                                        var identifier = (JoystickIdentifiers)shortcut.ElementIdentifierID;

                                        float check = 0;
                                        float prevCheck = 0;

                                        switch (identifier)
                                        {
                                            case JoystickIdentifiers.Left_Stick_Up:
                                            case JoystickIdentifiers.Right_Stick_Up:
                                                {
                                                    //We check the other axis to prevent detecting incidental tilts.
                                                    if (axis.y <= 0 || Math.Abs(axis.x) > CounterDeadzone)
                                                        continue;
                                                    else
                                                    {
                                                        check = axis.y;
                                                        prevCheck = axisPrev.y;
                                                        break;
                                                    }
                                                }
                                            case JoystickIdentifiers.Left_Stick_Down:
                                            case JoystickIdentifiers.Right_Stick_Down:
                                                {
                                                    if (axis.y >= 0 || Math.Abs(axis.x) > CounterDeadzone)
                                                        continue;
                                                    else
                                                    {
                                                        check = axis.y;
                                                        prevCheck = axisPrev.y;
                                                        break;
                                                    }
                                                }
                                            case JoystickIdentifiers.Left_Stick_Left:
                                            case JoystickIdentifiers.Right_Stick_Left:
                                                {
                                                    if (axis.x >= 0 || Math.Abs(axis.y) > CounterDeadzone)
                                                        continue;
                                                    else
                                                    {
                                                        check = axis.x;
                                                        prevCheck = axisPrev.x;
                                                        break;
                                                    }
                                                }
                                            case JoystickIdentifiers.Left_Stick_Right:
                                            case JoystickIdentifiers.Right_Stick_Right:
                                                {
                                                    if (axis.x <= 0 || Math.Abs(axis.y) > CounterDeadzone)
                                                        continue;
                                                    else
                                                    {
                                                        check = axis.x;
                                                        prevCheck = axisPrev.x;
                                                        break;
                                                    }
                                                }
                                        }

                                        // Final checks include a minimum amount the stick is off center
                                        // and a comparison of where the stick was last frame.

                                        float finalPrev = Math.Abs(prevCheck);
                                        float finalCheck = Math.Abs(check);

                                        if (finalCheck > MinThreshold && finalPrev <= MinThreshold) // Hit
                                        {
                                            ActiveShortcuts.Add(shortcut);
                                        }
                                    }
                                }
                                // Handle Digital Buttons at triggers
                                // (u_u ) much simpler
                                else
                                {
                                    if (shortcut.RequireDoubleTap)
                                    {
                                        if (controller.GetButtonDoublePressDownById(shortcut.ElementIdentifierID))
                                        {
                                            ActiveShortcuts.Add(shortcut);
                                        }
                                    }
                                    else
                                    {
                                        if (controller.GetButtonDownById(shortcut.ElementIdentifierID))
                                        {
                                            ActiveShortcuts.Add(shortcut);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // Let the game know if we need to call any shortcuts at the end of this frame.
                    if (ActiveShortcuts.Count > 0)
                    {
                        PreFinalizerHook.Subscribe(CallShortcuts);
                    }
                }
            }
        }

        /// <summary>
        /// Calls each shortcut on the active shortcuts list.
        /// </summary>
        /// <param name="iteration">Simulation frame these shortcuts will activate on.</param>
        public void CallShortcuts(SimulationIteration iteration)
        {
            // Get our controlled characters.
            List<PlayerControllerEntity> playerControllerEntities = new List<PlayerControllerEntity>();
            iteration.GetEntities<PlayerControllerEntity>(playerControllerEntities);

            // pair the characters with their shortcuts.
            for (int s = 0; s < ActiveShortcuts.Count; s++)
            {
                for (int p = 0; p < playerControllerEntities.Count; p++)
                {
                    if (playerControllerEntities[p].LocalID == ActiveShortcuts[s].PlayerNum)
                    {
                        // Call and move to the next shortcut.
                        ActiveShortcuts[s].Call(iteration, playerControllerEntities[p]);
                        break;
                    } 
                } 
            }

            // This frame is done, prepare for next frame.
            ActiveShortcuts.Clear();
        } 
        #endregion
    }
}