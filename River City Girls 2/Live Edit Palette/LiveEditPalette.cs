using System;
using MelonLoader;
using RCG.Rollback.Components;
using System.IO;
using UnityEngine;
using Tools.BinaryRollback;
using Rewired;
using TextFloater;

namespace RCG2Mods
{
    /// <summary>
    /// Shortcut to trigger file to palette refreshing.
    /// </summary>
    class LiveRefresh : Shortcut
    {
        #region Constructor
        /// <summary>
        /// A Keyboard exclusive shortcut for palette refreshing.
        /// </summary>
        /// <param name="key">Key to use</param>
        public LiveRefresh(int key)
            : base(0, ControllerType.Keyboard, key) { }
        #endregion

        #region Functions
        /// <summary>
        /// Either creates a editable png of the current palette or reloads changes of that file back into the game.
        /// </summary>
        /// <param name="simulation">Game's main simulation iterator.</param>
        /// <param name="entity">Player whose palette is being edited.</param>
        public override void Call(SimulationIteration simulation, PlayerControllerEntity entity)
        {
            // Get the current player entity.
            var player = entity.Player(simulation) as PlayerEntity;

            if (player != null)
            {
                // Use a png path specific for this character.
                string path = string.Format("./UserData/Palettes/{0}/-LiveEdit.png", player.ClassName);

                if (File.Exists(path)) // Reload the file into the game.
                {
                    // Load
                    player.Character.m_allVariations[0] = Palette.CreateTextureFromFile(path, player.ClassName);

                    // Give a notification that this worked, if the change is not immedietly noticable.
                    TextLib.PopText(simulation, entity.Player(simulation) as CombatEntity, "Palette Refreshed", Color.yellow, 30);
                }
                else // create the file.
                {
                    // Get a duplicate of the palette Texture, since the real one is not readable, then encode to PNG.
                    //TODO: Does this create memory leaks? It's not something people should be doing a bunch per-session but technically it might be.
                    byte[] png = player.Character.m_allVariations[0].Duplicate().EncodeToPNG();

                    // Load into a file stream and save.
                    using (MemoryStream ms = new MemoryStream(png))
                    {
                        using (FileStream fs = new FileStream(path, FileMode.Create))
                        {
                            ms.CopyTo(fs);
                        }
                    }

                    // Give a notification that this worked.
                    // TODO: Technically, it might not have!
                    TextLib.PopText(simulation, entity.Player(simulation) as CombatEntity, path + "-LiveEdit.png Created", Color.yellow, 30);
                }
            }
        }
        #endregion
    }

    /// <summary>
    /// Mod that allows a River City Girls 2 palette to be edited (out of game) in real time.
    /// </summary>
    public class LiveEditPalette : MelonMod
    {
        #region Fields
        private LiveRefresh refreshShortcut;
        #endregion

        /// <summary>
        /// Initializes preferences and adds our shortcut.
        /// </summary>
        public override void OnInitializeMelon()
        {
            // Create/Load config
            var PrefCategory = MelonPreferences.CreateCategory("LiveEditPalette", "Live Edit Palette");
            PrefCategory.SetFilePath("UserData/Shortcuts.cfg");

            // Create/Load current settings.
            var ent = PrefCategory.CreateEntry<KeyIdentifiers>("CreateReload", KeyIdentifiers.F8, "Create or Reload -LiveEdit.png Key");
            ent.OnEntryValueChanged.Subscribe(RefreshKeyChange);

            // Create the shortcut.
            refreshShortcut = new LiveRefresh((int)ent.Value);

            if (ent.Value != KeyIdentifiers.None) // Add the shortcut.
            {
                ShortcutsManager.AddShortcut(refreshShortcut);
            }
        }

        /// <summary>
        /// Replaces the key used for our main shortcut.
        /// </summary>
        /// <param name="oldK">ignored</param>
        /// <param name="newK">the key we're switching to.</param>
        private void RefreshKeyChange(KeyIdentifiers oldK, KeyIdentifiers newK)
        {
            // Set the key. ID
            refreshShortcut.ElementIdentifierID = (int)newK;

            if (newK == KeyIdentifiers.None) // remove the shortcut since we're not using it.
            {
                ShortcutsManager.RemoveShortcut(refreshShortcut);
            }
            else // Make sure the shortcut is added in.
            {
                ShortcutsManager.AddShortcut(refreshShortcut);
            }
        }
    }
}
