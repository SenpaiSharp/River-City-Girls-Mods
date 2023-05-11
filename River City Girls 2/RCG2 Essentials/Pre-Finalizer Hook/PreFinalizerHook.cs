using System;
using HarmonyLib;
using System.Reflection; 
using Tools.BinaryRollback;
using System.Collections.Generic;

namespace RCG2Mods
{
    /// <summary>
    /// A UserLib for RCG2 that patches a hook before Simulation.Finalize is called that can be Subscribed to. It will run any any subscription for one frame and reset. Not an ideal function for something expected to run every single frame. This is the currently best known way to changes to stick to an entity without (Rollback?) interference.
    /// </summary>
    public static class PreFinalizerHook
    {
        #region Fields
        internal const string mainSimTag = "MainSimulation";
        internal static bool initialized;
        internal static Action<SimulationIteration> prefinalizers;
        internal static List<Action<SimulationIteration>> nextFrameFinalizers;
        #endregion

        #region Construction and Initializing
        /// <summary>
        /// Constructor
        /// </summary>
        static PreFinalizerHook()
        {
            //Cheaper than an anonymous () every frame.
            prefinalizers = EmptyCall;

            nextFrameFinalizers = new List<Action<SimulationIteration>>();
        }

        /// <summary>
        /// Checks if ran once and if not, runs our Patch.
        /// </summary>
        public static void Initialize()
        {
            if (initialized)
            {
                return;
            }
            initialized = true;

            PatchFinalize();
        }

        /// <summary>
        /// Patches the finalize method to add our hook.
        /// </summary>
        internal static void PatchFinalize()
        {
            // Get the finalize method.
            // I don't know, it didn't like me getting the method directly for whatever reason.
            MethodInfo[] methods = typeof(SimulationIteration).GetMethods(BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo finalizeMethod = null;
            for (int i = 0; i < methods.Length; i++)
            {
                if (methods[i].Name == "Finalize")
                {
                    finalizeMethod = methods[i];
                    break;
                }
            }

            // The patching.
            HarmonyLib.Harmony harmony = new HarmonyLib.Harmony("RCG2Mods.PreFinalizerHook");
            PatchProcessor patchProcessor = harmony.CreateProcessor(finalizeMethod);
            HarmonyMethod harmonyMethod = new HarmonyMethod(typeof(PreFinalizerHook), "PreFinalize");
            patchProcessor.AddPrefix(harmonyMethod);
            patchProcessor.Patch(); 
        }
        #endregion

        #region Functions
        /// <summary>
        /// Subscribes a () that will be called once on the next Finalize call. Only one subscription per instance () is allowed.
        /// </summary>
        /// <param name="action">The () to be performed.</param>
        public static void Subscribe(Action<SimulationIteration> action)
        {
            // Remove the action first. This will do nothing if it's not already there but will ensure we only have it once if it is.
            prefinalizers -= action;

            // Add
            prefinalizers += action;
        }

        public static void SubscribeSubsequentFrame(Action<SimulationIteration> action)
        {
            nextFrameFinalizers.Add(action);
        }

        /// <summary>
        /// Does nothing.
        /// </summary>
        /// <param name="iter"></param>
        internal static void EmptyCall(SimulationIteration iter) { }

        /// <summary>
        /// Runs just before Finalize is called and Invokes any subscribers.
        /// </summary>
        /// <param name="__instance">The SimulationIteration that is calling Finalize.</param>
        /// <returns>Always returns true, so that Finalize will run.</returns>
        private static bool PreFinalize(SimulationIteration __instance)
        {
            // Check if this is the m_iterator, it is the only one we care about.
            if (__instance.IterationTag == mainSimTag)
            {
                // Invoke
                prefinalizers.Invoke(__instance);

                // Reset for next frame;
                prefinalizers = EmptyCall;

                if (nextFrameFinalizers.Count > 0)
                {
                    for (int i = 0; i < nextFrameFinalizers.Count; i++)
                    {
                        Subscribe(nextFrameFinalizers[i]);
                    }
                    nextFrameFinalizers.Clear();
                }
            }

            // Done
            return true;
        } 
        #endregion
    }
}
