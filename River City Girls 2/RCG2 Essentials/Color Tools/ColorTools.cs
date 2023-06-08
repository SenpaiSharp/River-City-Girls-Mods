using System;
using System.Collections.Generic;
using System.Linq;
using RCG.Rollback.Components;
using MelonLoader;
using UnityEngine;
using Tools.BinaryRollback;
using HarmonyLib;
using RCG.Rollback.Mono;
using System.IO;
using RCG.UI.Screens.Widgets;
using Harmonyc = HarmonyLib.Harmony;

namespace RCG2Mods
{
    /// <summary>
    /// Contains a Texture formatted for specific character palettes and a look up name for adding to collections.
    /// </summary>
    public class Palette
    {
        #region Properties
        /// <summary>
        /// A 16x16 dot color Texture that is fed to the game's shader.
        /// </summary>
        public Texture2D Texture { get; }

        /// <summary>
        /// Name of this Palette.
        /// </summary>
        public string Name { get; }
        #endregion

        #region Static Creators
        /// <summary>
        /// Creates a Palette straight from a texture and name. Be sure the texture itself is properly formatte.d
        /// </summary>
        /// <param name="texture">The color palette.</param>
        /// <param name="paletteName">Name for looking up in a collection.</param>
        /// <returns></returns>
        public static Palette CreatePalette(Texture2D texture, string paletteName)
        {
            return new Palette(texture, paletteName);
        }

        /// <summary>
        /// Creates a Palette from a png, providing the file path of the png.
        /// </summary>
        /// <param name="pngPath">File path of our properly formatted png file.</param>
        /// <param name="paletteName">Name of this palette, for looking up.</param>
        /// <param name="characterName">Name of the character this is being loaded into.</param>
        /// <returns>A new palette.</returns>
        public static Palette CreatePalette(string pngPath, string paletteName, string characterName)
        {
            Texture2D texture = CreateTextureFromFile(pngPath, characterName);

            return new Palette(texture, paletteName);
        }

        /// <summary>
        /// Creates a Palette from a file path of a png, using folder and file name information to derive needed data.
        /// </summary>
        /// <param name="pngPath">Full path of the png.</param>
        /// <returns></returns>
        public static Palette CreatePalette(string pngPath)
        {
            string paletteName = Path.GetFileNameWithoutExtension(pngPath);
            string characterName = Path.GetFileName(Path.GetDirectoryName(pngPath));

            return Palette.CreatePalette(pngPath, paletteName, characterName);
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Constructs a Palette.
        /// </summary>
        /// <param name="texture">color palette data in the form of a texture</param>
        /// <param name="paletteName">name of this palette</param>
        private Palette(Texture2D texture, string paletteName)
        {
            Texture = texture;
            Name = paletteName;
        }
        #endregion

        #region Helper Tools
        /// <summary>
        /// Creates a formatted Texture2D suited to be a palette texture from a png file path.
        /// </summary>
        /// <param name="filePath">Location of the formatted png that will become the texture.</param>
        /// <param name="characterName">Name of the character that this Texture2D will be used for. Important for naming reasons.</param>
        /// <returns></returns>
        public static Texture2D CreateTextureFromFile(string filePath, string characterName)
        {
            // Create a blank texture of RBGA32 format.
            Texture2D create = new Texture2D(1, 1, TextureFormat.RGBA32, false);

            // Get .png file and load it into our texture.
            using (FileStream fs = new FileStream(filePath, FileMode.Open))
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    fs.CopyTo(ms);
                    create.LoadImage(ms.ToArray());
                }
            }

            // Set the name to DefaultPalette so the game knows to go back to it if poisoned or frozen.
            create.name = characterName + "DefaultPalette";

            // This texture represent absolute data, remove any distortion possibilities (just in case).
            create.anisoLevel = 1; //todo:check if this right
            create.filterMode = FilterMode.Point;
            create.wrapMode = TextureWrapMode.Clamp;
            create.wrapModeU = TextureWrapMode.Clamp;
            create.wrapModeV = TextureWrapMode.Clamp;
            create.wrapModeW = TextureWrapMode.Clamp;

            // Set the texture to readonly for efficiency.
            create.Apply(false, true);

            return create;
        } 
        #endregion
    }

    /// <summary>
    /// A plugin that enables, loads and manages alternate palettes for the main playable characters of River City Girls 2.
    /// </summary>
    public class ColorTools : MelonPlugin
    {
        #region Static Fields
        /// <summary>
        /// Character Descriptions for each character.
        /// </summary>
        static internal Dictionary<string, List<CharacterDescription>> Descriptions;
        /// <summary>
        /// A dollection of palettes for each character.
        /// </summary>
        static internal Dictionary<string, List<Palette>> Palettes;
        /// <summary>
        /// Mod preferences
        /// </summary>
        static internal MelonPreferences_Category Preferences;
        /// <summary>
        /// Instance created by Melon.
        /// </summary>
        static internal ColorTools Instance;
        #endregion

        #region Fields
        /// <summary>
        /// Names of the 6 main Characters.
        /// </summary>
        internal readonly string[] CharacterNames = new string[]
        {
                "Misako",
                "Kyoko",
                "Kunio",
                "Riki",
                "Marian",
                "Provie"
        };

        /// <summary>
        /// Harmony instance just for this class.
        /// </summary>
        private Harmonyc harmony;
        #endregion

        #region Initializing, Loading and Patching
        /// <summary>
        /// Initializes, Loads and Patches everything for ready usage.
        /// </summary>
        public override void OnApplicationStarted()
        {
            // Initialize
            Instance = this;
            harmony = new Harmonyc("RCG2Mods.ColorTools");
            Descriptions = new Dictionary<string, List<CharacterDescription>>();

            // Load
            LoadPreferences();
            LoadPalettes();

            // Patch
            PatchCreate();
            PatchSelectSaveFile();
        }

        /// <summary>
        /// Creates/Loads preference entries.
        /// </summary>
        private void LoadPreferences()
        {
            // Create our .cfg
            Preferences = MelonPreferences.CreateCategory("Palettes");
            Preferences.SetFilePath("UserData/PaletteTools.cfg");

            // Create an index entry for each character.
            for (int i = 0; i < CharacterNames.Length; i++)
            {
                // Create entry.
                var pref = Preferences.CreateEntry<int>(CharacterNames[i] + "Index", 0, string.Format("Current {0} Palette", CharacterNames[i]), false);

                // Make sure nothing is negative.
                if (Preferences.GetEntry<int>(pref.Identifier).Value < 0)
                {
                    Preferences.GetEntry<int>(pref.Identifier).Value = 0;
                }

                // Subscribe so that changing the index will always call a refresh.
                pref.OnEntryValueChangedUntyped.Subscribe(IndexChange);
            }
        }

        /// <summary>
        /// Loads all palettes for each character from their respective folders.
        /// </summary>
        private void LoadPalettes()
        {
            Palettes = new Dictionary<string, List<Palette>>();

            for (int i = 0; i < CharacterNames.Length; i++)
            {
                string name = CharacterNames[i];

                // Create our list for our character.
                Palettes.Add(name, new List<Palette>() { null });

                // Get all files from our character's folder.
                string directory = string.Format("./UserData/Palettes/{0}", name);
                if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
                List<string> created = Directory.GetFiles(directory, "*.png", SearchOption.AllDirectories).ToList();
                created.Sort();

                // create and add from each file.
                for (int x = 0; x < created.Count; x++)
                {
                    // We are delibertly not loading files that start with this char for flexibility reasons.
                    string pngFileName = Path.GetFileName(created[x]);
                    if (pngFileName[0] == '-') continue;
                    
                    // get our palette texture from our filepath.
                    Palette palette = Palette.CreatePalette(created[x]);

                    // Add our palette.
                    Palettes[CharacterNames[i]].Add(palette);
                }
            }
        }
         
        /// <summary>
        /// Patches in the HookAnEntity into PlayerEntity.Create. 
        /// </summary>
        private void PatchCreate()
        {
            var create = AccessTools.Method(
                typeof(PlayerEntity),
                "Create",
                new Type[]
                {
                    typeof(SimulationIteration),
                    typeof(CharacterDescription),
                    typeof(SaveSlotEntity.CharacterData)
                });

            var hook = AccessTools.Method(
                this.GetType(),
                "HookAnEntity",
                new Type[]
                {
                    typeof(PlayerEntity),
                    typeof(CharacterDescription)
                });

            harmony.Patch(create, postfix: new HarmonyMethod(hook));
        }

        /// <summary>
        /// Patches in the HookAll method to HideOutSetup.FinalizeLevelLoad.
        /// </summary>
        private void PatchSelectSaveFile()
        {
            var onselect = AccessTools.Method( typeof(SaveFile_SaveSlot), "SelectSaveFile");

            var hook = AccessTools.Method(this.GetType(), "ClearDictionaries");

            harmony.Patch(onselect, postfix: new HarmonyMethod(hook));
        }
        #endregion

        #region Patches
        /// <summary>
        /// Resets our descriptions for a new game, which is necessary now in RCG2 1.0.2. thanks.
        /// </summary>
        private static void ClearDictionaries()
        {
            Descriptions.Clear();
        }

        /// <summary>
        /// Hooks a PlayerEntity on its creation.
        /// </summary>
        /// <param name="__result">the entity just created</param>
        /// <param name="description">The character description (the part we actually want).</param>
        /// <returns>The __result</returns>
        private static PlayerEntity HookAnEntity(PlayerEntity __result, CharacterDescription description)
        {
            // get the raw name of the character.
            string name = description.name.Replace("Description", "");

            if (!Descriptions.ContainsKey(name))
            {
                //Create an entry that will hold our descriptions for htis character
                Descriptions.Add(name, new List<CharacterDescription>() { description });

                //Set our palette list so that 0 is the default texture.
                Palettes[name][0] = Palette.CreatePalette(Descriptions[name][0].m_allVariations[0], "DefaultPalette");
            }

            // Avoid Duplications.
            Descriptions[name].Remove(description);

            // Add this description.
            Descriptions[name].Add(description);

            // Refresh colors.
            RefreshColors();

            // exit
            return __result;
        }
        #endregion

        #region Public Functions
        /// <summary>
        /// Sets a character's palette index and refreshes.
        /// </summary>
        /// <param name="characterName">Name of the hooked character.</param>
        /// <param name="index">Index of the preloaded Texture.</param>
        public static void ChangeCharacterPalette(string characterName, int index)
        {
            var entry = ((MelonPreferences_Entry<int>)Preferences.GetEntry(characterName + "Index"));

            if (entry != null) entry.Value = index;
        }

        /// <summary>
        /// Sets a character's palette index and refreshes.
        /// </summary>
        /// <param name="characterName">Name of the hooked character.</param>
        /// <param name="textureName">Name of the preloaded Texture.</param>
        public static void ChangeCharacterPalette(string characterName, string textureName)
        {
            // Find our palette
            List<Palette> palettes = Palettes[characterName];
            Palette palette = palettes.FirstOrDefault(x => x.Name == textureName);

            // Get our index
            int index = -1;
            if (palette != null) index = palettes.IndexOf(palette);

            // change
            ChangeCharacterPalette(characterName, index);
        }

        /// <summary>
        /// Switches to the next palette.
        /// </summary>
        /// <param name="characterName">Character to switch palette of.</param>
        /// <param name="lowRange">The lowest palette from loaded array that should be used.</param>
        /// <param name="highRange">The highest palette from loaded array that should be used.</param>
        public static void IterateCharacterPaletteForward(string characterName, int lowRange = -1, int highRange = -1)
        {
            // Get our current index
            var entry = Preferences.GetEntry<int>(characterName + "Index");

            int index = entry.Value;

            // iterate forward.
            index++;

            // Make sure we're in positive territory.
            if (index < 0)
            {
                index = 0;
            }

            // Fit within our ranges.
            if (lowRange >= 0 && lowRange > index)
            {
                index = lowRange;
            }
            if (highRange >= 0 && index > highRange)
            {
                index = lowRange;
            }

            // Set
            entry.Value = index; 
        }

        /// <summary>
        /// Switches to the previous palette.
        /// </summary>
        /// <param name="characterName"></param>
        /// <param name="lowRange"></param>
        /// <param name="highRange"></param>
        public static void IterateCharacterPaletteBackward(string characterName, int lowRange = -1, int highRange = -1)
        {
            // Get our current index

            var entry = Preferences.GetEntry<int>(characterName + "Index");

            int index = entry.Value;

            // iterate backward.
            index--;

            // Make sure we're in positive territory.
            if (index < 0)
            {
                index = Palettes[characterName].Count - 1; //Technically incorrect.
            }

            // Fit within our ranges.
            if (lowRange >= 0 && lowRange > index)
            {
                index = highRange;
            }
            if (highRange >= 0 && index > highRange)
            {
                index = highRange;
            }

            // Set
            entry.Value = index;
        }
        #endregion

        #region Private Functions
        /// <summary>
        /// Refreshes all character's textures to their current index.
        /// </summary>
        /// <param name="simulationIteration"></param>
        private static void RefreshColors()
        {
            var keys = Descriptions.Keys.ToList();

            for (int i = 0; i < keys.Count; i++)
            {
                string name = string.Format("{0}Index", keys[i]);
                var entry = Preferences.GetEntry<int>(name);

                List<Palette> palettes = Palettes[keys[i]];

                List<CharacterDescription> descriptions = Descriptions[keys[i]];

                // Update all descriptions that have been loaded. It's multiple per character to avoid any unpredictable desyncing of our character and the instance of their description, which happens a lot more than I'd like (0).
                for (int c = 0; c < descriptions.Count; c++)
                {
                    // Get our real index, using a modolo to avoid crashes on errant changes.
                    int index = entry.Value % Palettes[keys[i]].Count;

                    // Set the character's default to our new one.
                    descriptions[c].m_allVariations[0] = palettes[index].Texture;
                }    
            } 
        }
        #endregion

        #region Event Handling
        /// <summary>
        /// Catches Preferences changes. 
        /// </summary>
        /// <param name="old">old value</param>
        /// <param name="replace">new value</param>
        private void IndexChange(object old, object replace)
        {
            // If the value is negative, just reset everything (I'm being lazy).
            if ((int)replace < 0)
            {
                for (int i = 0; i < CharacterNames[i].Length; i++)
                {
                    Preferences.GetEntry<int>(CharacterNames[i] + "Index").Value = 0;
                }
                return;
            }

            // Refresh colors on frame.
            RefreshColors();
        }
        #endregion
    }

    /// <summary>
    /// Unity Texture2D Extensions 
    /// </summary>
    public static class Texture2DExtensions
    {
        /// <summary>
        /// Creates a new Texture2D from a source. It can recreate a Texture2D that is no longer readable and return a readable one.
        /// </summary>
        /// <param name="source">Texture being duplicated.</param>
        /// <returns>A new, readable texture duplicated from the source.</returns>
        public static Texture2D Duplicate(this Texture2D source)
        {
            RenderTexture renderTex = RenderTexture.GetTemporary(
                        source.width,
                        source.height,
                        0,
                        RenderTextureFormat.Default,
                        RenderTextureReadWrite.Linear);

            Graphics.Blit(source, renderTex);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = renderTex;
            Texture2D readableText = new Texture2D(source.width, source.height);
            readableText.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
            readableText.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTex);
            return readableText;
        }
    }
}
