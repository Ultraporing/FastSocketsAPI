//-----------------------------------------------------------------------
// <copyright file="Config.cs" company="Tobias Lenz">
//     Copyright Tobias Lenz. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace FastSockets
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Config File Reader
    /// </summary>
    public static class Config
    {
        /// <summary>
        /// The settings
        /// </summary>
        private static Dictionary<string, object> _settings = new Dictionary<string, object>();

        /// <summary>
        /// Reads the configuration from a provided string. Every command has to be terminated by a semicolon ';'.
        /// </summary>
        public static bool ReadConfigRaw(string config)
        {
            try
            {
                IEnumerable<string> lines = config.Split(';').Select(tag => tag.Trim()).Where(tag => !string.IsNullOrEmpty(tag));


                foreach (string line in lines)
                {
                    string[] keyVal = line.Replace(" ", string.Empty).Split('=');

                    if (keyVal.Length != 2)
                    {
                        ConsoleLogger.WriteErrorToLog("Invalid number of values in (" + line + ")");
                        return false;
                    }

                    if (_settings.ContainsKey(keyVal[0]))
                    {
                        _settings[keyVal[0]] = keyVal[1];
                    }
                    else
                    {
                        _settings.Add(keyVal[0], keyVal[1]);
                    }
                }
            }
            catch (Exception e)
            {
                ConsoleLogger.WriteErrorToLog("EXCEPTION(" + e.Message + ")");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Reads the configuration.
        /// </summary>
        /// <param name="configFile">The configuration file.</param>
        /// <returns>Returns if read was successful</returns>
        public static bool ReadConfig(string configFile)
        {
            try
            {
                string[] lines = File.ReadAllLines(configFile, Encoding.UTF8);

                foreach (string line in lines)
                {
                    string[] keyVal = line.Replace(" ", string.Empty).Split('=');

                    if (keyVal.Length != 2)
                    {
                        ConsoleLogger.WriteErrorToLog("Invalid number of values in (" + line + ")");
                        return false;
                    }

                    if (_settings.ContainsKey(keyVal[0]))
                    {
                        _settings[keyVal[0]] = keyVal[1];
                    }
                    else
                    {
                        _settings.Add(keyVal[0], keyVal[1]);
                    }              
                }
            }
            catch (Exception e)
            {
                ConsoleLogger.WriteErrorToLog("EXCEPTION(" + e.Message + ")");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <typeparam name="T">Type of value to get.</typeparam>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns>Returns if get was successful</returns>
        public static bool GetValue<T>(string key, out T value) where T : IComparable<T>
        {
            object val = null;
            if (_settings.TryGetValue(key, out val))
            {
                try
                {
                    T v = (T)Convert.ChangeType(val, typeof(T));
                    value = v;
                    return true;
                }
                catch (Exception e)
                {
                    ConsoleLogger.WriteErrorToLog("KEY(" + key + ") EXCEPTION(" + e.Message + ")");
                    value = default(T);
                    return false;
                }
            }
            
            ConsoleLogger.WriteErrorToLog("Key does not exist");
            value = default(T);
            return false;
        }
    }
}
