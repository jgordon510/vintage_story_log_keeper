using System.Collections.Generic;
using System.IO;
using System;
using System.Diagnostics;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using System.Text.RegularExpressions;
using System.Text;
using Microsoft.VisualBasic;
using System.Threading;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Drawing;

namespace audit_log_keeper
{
    public class audit_log_keeperModSystem : ModSystem
    {
        public ICoreAPI api;
        private ICoreServerAPI sapi;
        ConfigSettings config;
        private Dictionary<string, int> logLens;  //the lengths of each of the logs being tracked
        public override void Start(ICoreAPI api)
        {
            this.api = api;
        }
        public override void StartServerSide(ICoreServerAPI api)
        {
            sapi = api;
            logLens = new Dictionary<string, int>();

            config = sapi.LoadModConfig<ConfigSettings>("log_backup.json");
            if (config == null)
            {
                sapi.StoreModConfig(new ConfigSettings(), "log_backup.json");
                config = new ConfigSettings();
            }
            foreach (var fn in config.LOG_FILES)
            {
                logLens[fn] = 0; //zero the log line counts out
            }
            api.Event.RegisterGameTickListener(BackupLogs, 1000 * 60 * config.BACKUP_FREQ_MINS);
            BackupLogs(0);
        }


        public string[] WriteSafeReadAllLines(string path)
        {
            using (var csv = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var sr = new StreamReader(csv))
            {
                List<string> file = new List<string>();
                while (!sr.EndOfStream)
                {
                    file.Add(sr.ReadLine());
                }

                return file.ToArray();
            }
        }

        private void BackupLogs(float dt)
        {
            Stopwatch watch = Stopwatch.StartNew();
            try
            {
                foreach (var fn in config.LOG_FILES)
                {

                    string docPath = config.PATH + fn;
                    string noExt = fn.Remove(fn.Length - 4);
                    string bakPath = config.PATH + noExt + ".bak";

                    string[] lines = WriteSafeReadAllLines(docPath);
                    //api.Logger.Debug(lines.ToString());
                    if (lines.Length > logLens[fn])
                    {
                        int n = 3; // Number of elements you want to retrieve
                                   // Copy the last n elements to the new array

                        string[] keepLines = new string[lines.Length - logLens[fn]];
                        Array.Copy(lines, lines.Length - n, keepLines, 0, n);

                        using (StreamWriter w = File.AppendText(bakPath))
                        {
                            foreach (var line in keepLines)
                            {
                                w.WriteLine(line);
                            }

                        }
                        logLens[fn] = lines.Length;

                        int trimSize = config.LOG_LIMIT_MB * 1024 * 1024;
                        long size = new FileInfo(bakPath).Length;
                        if (size > trimSize)
                        {
                            //api.Logger.Debug("trimming....");
                            using (MemoryStream ms = new MemoryStream(trimSize))
                            {
                                using (FileStream s = new FileStream(bakPath, FileMode.Open, FileAccess.ReadWrite))
                                {
                                    s.Seek(-trimSize, SeekOrigin.End);
                                    s.CopyTo(ms);
                                    s.SetLength(trimSize);
                                    s.Position = 0;
                                    ms.Position = 0;
                                    ms.CopyTo(s);
                                }
                            }

                        }
                        //api.Logger.Debug("finishing up");
                    }
                }

                watch.Stop();

            }
            catch (Exception e)
            {
                sapi.Logger.Debug("Log Back up ERROR: " + e.Message);
            }
            finally
            {
                watch.Stop();
                api.Logger.Debug("Backed up all logs.  Total exec time: {0} ms.", watch.ElapsedMilliseconds.ToString());
            }
        }

    }


}
