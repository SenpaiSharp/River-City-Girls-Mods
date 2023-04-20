using MelonLoader;

namespace RCG2Mods
{
    /// <summary>
    /// Just initializes some libraries.
    /// </summary>
    public class LibTools : MelonPlugin
    {
        /* In retrospect, AccessoryCycler and PreFinalizerHooks should have loaded as plugins.
         * To keep update compatibility, meaning to keep people from having to do anything but overwrite old files
         * and avoid any conflicting libraries, this plugin will be used to extend plugin like functions to the UserLibs.
         * Long story short, for now, this is just a way to ensure they get initialized without a bunch of checking.*/

        public override void OnApplicationStarted()
        { 
            AccessorySetter.Initialize();
            PreFinalizerHook.Initialize();
        }
    }
}
