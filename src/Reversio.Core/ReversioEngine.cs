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
            var sqlEngine = new SqlServerEngine(_job.ConnectionString);
            var pocoEngine = new PocoEngine(sqlEngine);
            var currentFlow = new Flow();
            bool last = false;
            foreach (var step in _job.Steps)
            {
                switch (step)
                {
                    case LoadStep load:
                        if (!currentFlow.Open)
                            throw new Exception("Invalid step order. Cannot execute Load step: flow closed");
                        Log.Information(String.Format("Executing Load step on schemas: ", String.Join(",",load.Schemas)));
                        currentFlow.Entities.UnionWith(sqlEngine.Load(load));
                        break;

                    case PocoGenerateStep pocoGenerate:
                        if (!currentFlow.Open)
                            throw new Exception("Invalid step order. Cannot execute PocoGenerate step: flow closed");
                        Log.Information(String.Format("Executing PocoGenerate step"));
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
                        Log.Information(String.Format("Executing PocoWrite step to output path {0}", pocoWrite.OutputPath));
                        pocoEngine.WritePocos(currentFlow.Pocos, pocoWrite);
                        _flows.Add(currentFlow);
                        currentFlow = new Flow();
                        break;

                    case DbContextStep dbContext:
                        Log.Information(String.Format("Executing DbContext step to output path {0}", dbContext.OutputPath));
                        var dbContextEngine = new DbContextEngine(dbContext, sqlEngine);
                        dbContextEngine.WriteDbContext(_flows.SelectMany(f => f.Pocos));
                        last = true;
                        break;
                }
                if (last)
                    break;
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
