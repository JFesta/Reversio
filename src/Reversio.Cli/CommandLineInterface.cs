/*
 * Copyright 2018 Jacopo Festa 
 * This file is part of Reversio.
 * Reversio is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * Reversio is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
 * GNU General Public License for more details.
 * You should have received a copy of the GNU General Public License
 * along with Reversio. If not, see <http://www.gnu.org/licenses/>.
*/

using Reversio.Core;
using Reversio.Core.Logging;
using Reversio.Core.Settings;
using Reversio.Core.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Reversio.Cli
{
    public class CommandLineInterface
    {
        private bool _verbose = false;
        private List<string> _settingPaths;

        private List<Job> _jobs;

        public void Execute(string[] args)
        {
            if (!args.Any() || args.First().Equals("help", StringComparison.InvariantCultureIgnoreCase))
            {
                Help();
                return;
            }
            if (args.Contains("-e", StringComparer.InvariantCultureIgnoreCase) || args.Contains("--example", StringComparer.InvariantCultureIgnoreCase))
            {
                GenerateExampleFile();
                return;
            }

            ParseArgs(args);
            Init();

            try
            {
                if (!LoadJobs()) return;
                LaunchJobs();
            }
            catch (Exception err)
            {
                if (_verbose)
                    Log.Error(err.ToString());
                else
                    Log.Error(err.Message);
            }
        }

        private void ParseArgs(string[] args)
        {
            _settingPaths = new List<string>();
            foreach (var arg in args)
            {
                if (ParseArg(arg))
                    _settingPaths.Add(arg);
            }
        }

        private bool ParseArg(string arg)
        {
            if (arg.Equals("-v", StringComparison.InvariantCultureIgnoreCase) || arg.Equals("--verbose", StringComparison.InvariantCultureIgnoreCase))
            {
                _verbose = true;
                return false;
            }
            return true;
        }

        private void Init()
        {
            Log.Logger = new ConsoleLogger(_verbose ? 1 : 3, _verbose);
        }

        private bool LoadJobs()
        {
            if (_settingPaths == null || !_settingPaths.Any())
            {
                Log.Warning("No json settings path specified");
                return false;
            }

            Log.Information(String.Format("Parsing {0} setting files", _settingPaths.Count()));
            foreach (var inputPath in _settingPaths)
            {
                var path = Path.GetFullPath(inputPath);
                Log.Debug(String.Format("Parsing file {0}", path));
                var settings = Newtonsoft.Json.JsonConvert.DeserializeObject<JsonSettings>(File.ReadAllText(path));

                _jobs = new List<Job>();
                int i = 1;
                Log.Debug(String.Format("Parsing job #{0}", i));
                foreach (var jsonJob in settings.Jobs)
                {
                    int state = 0;
                    bool preDbContext = false;
                    var job = new Job()
                    {
                        SettingsFile = path,
                        WorkingDirectory = Path.GetDirectoryName(path),
                        Provider = jsonJob.Provider,
                        ConnectionString = jsonJob.ConnectionString
                    };
                    foreach (var step in jsonJob.Steps)
                    {
                        Log.Debug(String.Format("Parsing step: {0}", step.StepType));
                        if (state == -1)
                        {
                            Log.Warning("Invalid step: no step is allowed after DbContext");
                            return false;
                        }
                        switch (step.StepType.ToLowerInvariant())
                        {
                            case "load":
                                if (state != 0 && state != 1)
                                {
                                    Log.Warning("Invalid Load Step: unallowed position in step flow");
                                    return false;
                                }
                                var loadStep = new LoadStep()
                                {
                                    Name = step.Name,
                                    EntityTypes = step.EntityTypes.Select(s => s?.ToLowerInvariant()),
                                    Schemas = step.Schemas?.Select(s => s?.ToLowerInvariant()),
                                    Exclude = step.Exclude.Convert()
                                };
                                if (!ValidateStep(loadStep))
                                    return false;
                                job.Steps.Add(loadStep);
                                state = 1;
                                break;
                            case "pocogenerate":
                                if (state != 1)
                                {
                                    Log.Warning("Invalid PocoGenerate Step: unallowed position in step flow");
                                    return false;
                                }
                                var pocoGenerateStep = new PocoGenerateStep()
                                {
                                    Name = step.Name,
                                    Namespace = step.Namespace,
                                    ClassAccessModifier = step.ClassAccessModifier,
                                    ClassPartial = step.ClassPartial,
                                    VirtualNavigationProperties = step.VirtualNavigationProperties,
                                    Usings = step.Usings,
                                    Extends = step.Extends,
                                    ClassNameForcePascalCase = step.ClassNameForcePascalCase,
                                    ClassNameReplace = step.ClassNameReplace.Select(c => c.Convert()),
                                    PropertyNameForcePascalCase = step.PropertyNameForcePascalCase,
                                    PropertyNameReplace = step.PropertyNameReplace.Select(c => c.Convert()),
                                    PropertyNullableIfDefaultAndNotPk = step.PropertyNullableIfDefaultAndNotPk
                                };
                                if (!ValidateStep(pocoGenerateStep))
                                    return false;
                                job.Steps.Add(pocoGenerateStep);
                                state = 2;
                                break;
                            case "pocowrite":
                                if (state != 2)
                                {
                                    Log.Warning("Invalid PocoWrite Step: unallowed position in step flow");
                                    return false;
                                }
                                var pocoWriteStep = new PocoWriteStep()
                                {
                                    Name = step.Name,
                                    OutputPath = step.OutputPath,
                                    CleanFolder = step.CleanFolder,
                                    PocosExclude = step.Exclude.Convert()
                                };
                                if (!ValidateStep(pocoWriteStep))
                                    return false;
                                job.Steps.Add(pocoWriteStep);
                                state = 0;
                                preDbContext = true;
                                break;
                            case "dbcontext":
                                if (state != 0 || !preDbContext)
                                {
                                    Log.Warning("Invalid DbContext Step: unallowed position in step flow");
                                    return false;
                                }
                                var dbContextStep = new DbContextStep()
                                {
                                    Name = step.Name,
                                    OutputPath = step.OutputPath,
                                    StubOutputPath = step.StubOutputPath,
                                    Namespace = step.Namespace,
                                    ClassName = step.ClassName,
                                    ClassAbstract = step.ClassAbstract,
                                    Extends = step.Extends,
                                    IncludeIndices = step.IncludeIndices,
                                    IncludeOnModelCreatingStub = step.IncludeOnModelCreatingStub,
                                    IncludeOptionalStubs = step.IncludeOptionalStubs,
                                    IncludeViews = step.IncludeViews,
                                    IncludeTablesWithoutPK = step.IncludeTablesWithoutPK
                                };
                                if (!ValidateStep(dbContextStep))
                                    return false;
                                job.Steps.Add(dbContextStep);
                                state = -1;
                                break;
                        }
                    }
                    if (!ValidateJob(job))
                        return false;

                    _jobs.Add(job);
                    i++;
                }
            }
            Log.Information(String.Format("Loaded {0} jobs", _settingPaths.Count()));
            return true;
        }

        private bool ValidateJob(Job job)
        {
            //ConnectionString
            if (String.IsNullOrWhiteSpace(job.ConnectionString))
            {
                Log.Warning("Invalid job: empty ConnectionString");
                return false;
            }
            return true;
        }

        private bool ValidateStep(LoadStep step)
        {
            if (step.EntityTypes == null || !step.EntityTypes.Any())
            {
                Log.Warning("Invalid Load step: at least one Entity Type is mandatory");
                return false;
            }
            return true;
        }

        private bool ValidateStep(PocoGenerateStep step)
        {
            if (String.IsNullOrWhiteSpace(step.Namespace))
            {
                Log.Warning("Invalid PocoWrite step: empty Namespace");
                return false;
            }
            return true;
        }

        private bool ValidateStep(PocoWriteStep step)
        {
            if (String.IsNullOrWhiteSpace(step.OutputPath))
            {
                Log.Warning("Invalid PocoWrite step: empty OutputPath");
                return false;
            }
            return true;
        }

        private bool ValidateStep(DbContextStep step)
        {
            if (String.IsNullOrWhiteSpace(step.OutputPath))
            {
                Log.Warning("Invalid DbContextStep step: empty OutputPath");
                return false;
            }
            if (String.IsNullOrWhiteSpace(step.ClassName))
            {
                Log.Warning("Invalid DbContextStep step: empty ClassName");
                return false;
            }
            if (String.IsNullOrWhiteSpace(step.Namespace))
            {
                Log.Warning("Invalid DbContextStep step: empty Namespace");
                return false;
            }
            return true;
        }

        private bool LaunchJobs()
        {
            int i = 1;
            foreach (var job in _jobs)
            {
                Log.Information(String.Format("Launching job #{0} from settings file {1}", i, job.SettingsFile));
                Directory.SetCurrentDirectory(job.WorkingDirectory);
                var engine = new ReversioEngine(job);
                engine.Execute();
                i++;
            }
            return true;
        }

        private void Help()
        {
            Console.WriteLine("Reversio v. {0} - Entity Framework Core Reverse-Engineering Tool ({1})", 
                Assembly.GetEntryAssembly().GetName().Version, InfoText.ProjectUrl);
            Console.WriteLine("{0} - Licensed under the GNU Lesser General Public License v3.0 (https://github.com/JFesta/Reversio/blob/master/LICENSE)", InfoText.Copyright);
            Console.WriteLine("This program executes processing jobs defined in one or more json-formatted settings files");
            Console.WriteLine("Usage:");
            Console.WriteLine("\tReverse engineering:");
            Console.WriteLine("\t\tReversio.exe [options] settings-path...");
            Console.WriteLine("\tGenerate an example json settings file:");
            Console.WriteLine("\t\tReversio.exe {-e|--example}");
            Console.WriteLine("options:");
            Console.WriteLine("\t-v, --verbose: prints out more detailed processing informations");
        }

        private void GenerateExampleFile(string path = null)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = assembly.GetName().Name + "." + "reversio.json";

            if (String.IsNullOrWhiteSpace(path))
                path = Path.Combine(Environment.CurrentDirectory, "reversio.json");
            else if (Directory.Exists(path))
                path = Path.Combine(path, "reversio.json");

            using (var stream = assembly.GetManifestResourceStream(resourceName))
            using (var reader = new StreamReader(stream))
            {
                File.WriteAllText(path, reader.ReadToEnd());
            }
            Console.WriteLine("Generated example file to {0}", path);
            return;
        }
    }
}
