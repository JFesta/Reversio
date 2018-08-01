using System;
using System.Collections.Generic;
using System.Text;

namespace Reversio.Core.Entities
{
    public class Table
    {
        private string _name;
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
                SetIdentifier();
            }
        }

        private string _schema;
        public string Schema
        {
            get
            {
                return _schema;
            }
            set
            {
                _schema = value;
                SetIdentifier();
            }
        }

        public string Catalog { get; set; }
        public string Type { get; set; }

        public string Identifier { get; private set; }

        public IDictionary<string, Column> Columns { get; set; }
        public List<Index> Indices { get; set; }
        public Index Pk { get; set; }
        public List<ForeignKey> Dependencies { get; set; }
        public List<ForeignKey> DependenciesFrom { get; set; }

        public Poco Poco { get; set; }
        
        private void SetIdentifier()
        {
            Identifier = _schema + "." + _name;
        }

        public override string ToString()
        {
            return Identifier;
        }
    }
}
