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

using Reversio.Core.Entities;
using Reversio.Core.Settings;
using Reversio.Core.SqlEngine;
using Reversio.Core.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Reversio.Core
{
    public class ReversioEngine
    {
        public Job _job;
        private List<Flow> _flows = new List<Flow>();

        private class Flow
        {
            public HashSet<Table> Entities { get; set; } = new HashSet<Table>();
            public List<Poco> Pocos { get; set; } = new List<Poco>();
            public bool Open { get; set; } = true;
        }

        public ReversioEngine(Job job)
        {
            _job = job;
        }

        public void Execute()
        {
            var sqlEngine = GetEngine();
            var pocoEngine = new PocoEngine(sqlEngine);
            var currentFlow = new Flow();
            //bool last = false;
            foreach (var step in _job.Steps)
            {
                switch (step)
                {
                    case LoadStep load:
                        if (!currentFlow.Open)
                            throw new Exception("Invalid step order. Cannot execute Load step: flow closed");
                        Log.Information(String.Format("Executing Load step \"{0}\" - Entity Types: {1} - Schemas: {2}", 
                            load.Name, String.Join(",", load.EntityTypes), String.Join(",", load.Schemas)));
                        currentFlow.Entities.UnionWith(sqlEngine.Load(load));
                        break;

                    case PocoGenerateStep pocoGenerate:
                        if (!currentFlow.Open)
                            throw new Exception("Invalid step order. Cannot execute PocoGenerate step: flow closed");
                        Log.Information(String.Format("Executing PocoGenerate step \"{0}\"", pocoGenerate.Name));
                        var newPocos = pocoEngine.GeneratePocos(currentFlow.Entities, pocoGenerate);
                        if (currentFlow.Pocos.Any())
                        {
                            foreach (var poco in newPocos)
                            {
                                pocoEngine.ResolvePocoName(currentFlow.Pocos, poco);
                            }
                        }
                        currentFlow.Pocos.AddRange(newPocos);
                        currentFlow.Open = false;
                        break;

                    case PocoWriteStep pocoWrite:
                        if (currentFlow.Open)
                            throw new Exception("Invalid step order. Cannot execute write step: flow open");
                        Log.Information(String.Format("Executing PocoWrite step \"{0}\" to output path {1}", 
                            pocoWrite.Name, pocoWrite.OutputPath));
                        pocoEngine.WritePocos(currentFlow.Pocos, pocoWrite);
                        _flows.Add(currentFlow);
                        currentFlow = new Flow();
                        break;

                    case DbContextStep dbContext:
                        Log.Information(String.Format("Executing DbContext step \"{0}\" to output path {1}", 
                            dbContext.Name, dbContext.OutputPath));
                        var dbContextEngine = new DbContextEngine(dbContext, sqlEngine);
                        dbContextEngine.WriteDbContext(_flows.SelectMany(f => f.Pocos));
                        //last = true;
                        break;
                }
                //if (last)
                //    break;
            }
        }

        private ISqlEngine GetEngine()
        {
            if (_job.Provider.Equals(SqlServerEngine.Provider, StringComparison.InvariantCultureIgnoreCase))
                return new SqlServerEngine(_job.ConnectionString);
            else
                throw new Exception("Data provider not supported: " + _job.Provider);
        }
    }
}
