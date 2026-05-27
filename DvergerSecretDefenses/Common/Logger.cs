using BepInEx.Logging;
using System;


namespace DvergerSecretDefenses {
    internal class Logger {
        public static LogLevel Level = LogLevel.Info;

        public static void EnableDebugLogging(object sender, EventArgs e) {
            CheckEnableDebugLogging();
        }

        public static void CheckEnableDebugLogging() {
            if (ValConfig.EnableDebugMode.Value) {
                Level = LogLevel.Debug;
            } else {
                Level = LogLevel.Info;
            }
        }

        public static void SetDebugLogging(bool state) {
            if (state) {
                Level = LogLevel.Debug;
            } else {
                Level = LogLevel.Info;
            }
        }

        public static void LogDebug(string message) {
            if (Level >= LogLevel.Debug) {
                DvergerSecretDefenses.Log.LogInfo(message);
            }
        }
        public static void LogInfo(string message) {
            if (Level >= LogLevel.Info) {
                DvergerSecretDefenses.Log.LogInfo(message);
            }
        }

        public static void LogWarning(string message) {
            if (Level >= LogLevel.Warning) {
                DvergerSecretDefenses.Log.LogWarning(message);
            }
        }

        public static void LogError(string message) {
            if (Level >= LogLevel.Error) {
                DvergerSecretDefenses.Log.LogError(message);
            }
        }
    }
}
