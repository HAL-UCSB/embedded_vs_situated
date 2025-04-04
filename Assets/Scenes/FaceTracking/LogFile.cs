using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;


namespace UnityEngine.XR.ARFoundation.Samples
{
    public static class LogFile
    {
        private const string EXTENSION = ".txt";
        private const string TIMESTAMP_FORMAT = "yyyyMMddHHmmssfff";

        private static readonly string TIMESTAMP = Timestamp();
        private static readonly Dictionary<string, StreamWriter> repository = new();
        private static readonly LogFileSchedueler scheduler = new GameObject("LogFileSchedueler").AddComponent<LogFileSchedueler>();

        public static void Log(string name, string message, bool verbose = false)
        {
            Write(name, message, false, verbose);
        }

        public static void Overwrite(string name, string message, bool verbose = false)
        {
            Write(name, message, true, verbose);
        }

        private static void Write(string name, string message, bool isOverride, bool verbose = false)
        {
            if (verbose)
                Debug.Log($"[{name}] {message}");

            if (!repository.TryGetValue(name, out StreamWriter writer))
            {
                writer = CreateWriter(name, verbose);
                repository[name] = writer;
            }
            if (isOverride)
                writer.Write(message);
            else
                writer.WriteLine(message);
        }

        private static StreamWriter CreateWriter(string name, bool verbose = false)
        {
            string path = Path.Join(Application.persistentDataPath, TIMESTAMP, name + EXTENSION);
            if (verbose)
                Debug.Log($"[{name}] {path}");
            var dir = Directory.GetParent(path);
            Directory.CreateDirectory(dir.FullName);
            File.Create(path).Dispose();
            return new StreamWriter(path);
        }

        public static Coroutine ScheduleLog(string name, Func<string> logFunction, float periodSeconds)
        {
            return scheduler.StartCoroutine(PeriodicLog_Coroutine(name, logFunction, periodSeconds));
        }

        private static IEnumerator PeriodicLog_Coroutine(string name, Func<string> logFunction, float periodSeconds)
        {
            while (true)
            {
                Log(name, logFunction());
                yield return new WaitForSeconds(periodSeconds);
            }
        }

        public static string Timestamp()
        {
            return DateTime.UtcNow.ToString(TIMESTAMP_FORMAT);
        }

        private sealed class LogFileSchedueler : MonoBehaviour
        {
            private void OnDestroy()
            {
                foreach (StreamWriter writer in repository.Values)
                {
                    writer.Flush();
                    writer.Close();
                }
            }
        }
    }
}