﻿using System;
using System.IO;

namespace versioncontrol_ONI
{
    // reads changelog and updates AssemblyInfo and info.json with new version
    class Program
    {
        static int Main(string[] args)
        {
#if DEBUG
            args = new string[]
            {
                "-md",
                @"C:\Users\Fumihiko\Documents\Visual Studio Projects\ONI_Mod\CustomizeBuildings\Changelog.md",
                "-info",
                @"C:\Users\Fumihiko\Documents\Visual Studio Projects\ONI_Mod\CustomizeBuildings\mod_info.yaml",
                "-asbly",
                @"C:\Users\Fumihiko\Documents\Visual Studio Projects\ONI_Mod\CustomizeBuildings\Properties\AssemblyInfo.cs"
            };
#endif

            try
            {
                string path_log = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\..\LocalLow\Klei\Oxygen Not Included\Player.log";
                string path_md = null;
                string path_info = null;
                string path_assembly = null;
                string gameversion = null;
                string assemblyversion = null;
                int int_gameversion = 0;
                bool b_exp1 = false;

                // read args
                for (int i = 0; i+1 < args.Length; i++)
                {
                    switch (args[i])
                    {
                        case "-log":
                            path_log = args[i + 1];
                            break;
                        case "-md":
                            path_md = args[i + 1];
                            break;
                        case "-info":
                            path_info = args[i + 1];
                            break;
                        case "-asbly":
                            path_assembly = args[i + 1];
                            break;
                    }
                }

                // read current version from log
                using (var log = File.OpenText(path_log))
                {
                    //Console.WriteLine("Reading log file...");
                    int counter = 0;
                    while (!log.EndOfStream && counter++ < 50)
                    {
                        var line = log.ReadLine();
                        if (line.Contains("[INFO] Expansion1: True", StringComparison.Ordinal))
                        {
                            b_exp1 = true;
                            break;
                        }

                        int index = line.IndexOf("[INFO] release Build: ", StringComparison.Ordinal);
                        if (index < 0) index = line.IndexOf("[INFO] preview Build: ", StringComparison.Ordinal);
                        if (index >= 0)
                        {
                            index += 22;
                            gameversion = line[index..];
                            int.TryParse(gameversion[^6..], out int_gameversion);
                            Console.WriteLine($"gameversion is {gameversion}, parsed as build: {int_gameversion}");
                        }
                    }
                }

                // read assemblyversion and update gameversion to changelog
                if (path_md != null)
                {
                    //Console.WriteLine("Reading changelog...");
                    var changelog = File.ReadAllLines(path_md);
                    for (int i = 0; i < changelog.Length; i++)
                    {
                        if (changelog[i].StartsWith("## [", StringComparison.Ordinal))
                        {
                            changelog[i] = changelog[i].TrimEnd();
                            int index = changelog[i].LastIndexOf(']');
                            if (index > 5)
                            {
                                assemblyversion = changelog[i][4..index];
                                Console.WriteLine("read assembly version as: " + assemblyversion);
                                if (gameversion != null)
                                {
                                    changelog[i] = changelog[i][..(index+1)] + " " + gameversion;
                                }
                            }
                            break;
                        }
                    }
                    File.WriteAllLines(path_md, changelog);
                }

                // update version to mod_info.yaml
                if (path_info != null && int_gameversion != 0)
                {
                    var modinfo = new string[]
                    {
                        "supportedContent: " + (b_exp1 ? "EXPANSION1_ID" : "VANILLA_ID"),
                        "lastWorkingBuild: " + int_gameversion
                    };
                    File.WriteAllLines(path_info, modinfo);
                }

                // update assemblyversion to assembly
                if (path_assembly != null && assemblyversion != null)
                {
                    //Console.WriteLine("Reading assembly...");
                    var assembly = File.ReadAllLines(path_assembly);
                    for (int i = 0; i < assembly.Length; i++)
                    {
                        if (assembly[i].StartsWith("[assembly: AssemblyVersion(\"", StringComparison.Ordinal))
                            assembly[i] = "[assembly: AssemblyVersion(\"" + assemblyversion + "\")]";
                        else if (assembly[i].StartsWith("[assembly: AssemblyFileVersion(\"", StringComparison.Ordinal))
                            assembly[i] = "[assembly: AssemblyFileVersion(\"" + assemblyversion + "\")]";
                    }
                    File.WriteAllLines(path_assembly, assembly);
                }

                Console.WriteLine("versioncontrol done!");
                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return 1;
            }
        }
    }
}

