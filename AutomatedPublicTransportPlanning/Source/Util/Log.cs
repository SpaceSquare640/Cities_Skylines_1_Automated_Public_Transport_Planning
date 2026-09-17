using System;
using UnityEngine;

namespace AutomatedPublicTransportPlanning.Util
{
    /// <summary>
    /// Thin wrapper over Unity's logger so every line this mod writes carries the
    /// same prefix and can be found in the game's output log.
    /// </summary>
    public static class Log
    {
        private const string Prefix = "[APTP] ";

        public static void Info(string message)
        {
            Debug.Log(Prefix + message);
        }

        public static void Warning(string message)
        {
            Debug.LogWarning(Prefix + message);
        }

        public static void Error(string message)
        {
            Debug.LogError(Prefix + message);
        }

        /// <summary>
        /// Logs an exception without letting the logging itself throw. Mod code runs
        /// inside the game's loop, where an unhandled exception can take the session
        /// down with it.
        /// </summary>
        public static void Exception(string context, Exception e)
        {
            try
            {
                Debug.LogError(Prefix + context + ": " + e);
            }
            catch
            {
                // Nothing useful left to do; swallowing beats crashing the game.
            }
        }

        /// <summary>
        /// Runs an action, turning anything it throws into a log line.
        ///
        /// Every entry point the game can call into — button handlers, extension
        /// callbacks — goes through this. An exception escaping one of those lands in
        /// the game's own dispatcher, and whether that dispatcher swallows it or lets
        /// it bring the session down is not something worth finding out in a player's
        /// city.
        /// </summary>
        public static void Guard(string context, Action action)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                Exception(context, e);
            }
        }
    }
}
