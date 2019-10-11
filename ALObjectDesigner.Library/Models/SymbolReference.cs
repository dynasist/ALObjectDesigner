using ALObjectParser.Library;
using System;
using System.Collections.Generic;
using System.Text;

namespace ALObjectDesigner.Library
{
    public class SymbolReference
    {
        public SymbolReference()
        {
            Tables = new List<ALObject>();
            Codeunits = new List<ALObject>();
            Pages = new List<ALObject>();
            PageCustomizations = new List<ALObject>();
            PageExtensions = new List<ALObject>();
            TableExtensions = new List<ALObject>();
            Reports = new List<ALObject>();
            XmlPorts = new List<ALObject>();
            Queries = new List<ALObject>();
            Profiles = new List<ALObject>();
            EnumTypes = new List<ALObject>();
            DotNetPackages = new List<ALObject>();
            ControlAddIns = new List<ALObject>();
        }

        public Guid AppId { get; set; }
        public string Name { get; set; }
        public string Publisher { get; set; }
        public Version Version { get; set; }
        public ICollection<ALObject> Tables { get; set; }
        public ICollection<ALObject> Codeunits { get; set; }
        public ICollection<ALObject> Pages { get; set; }
        public ICollection<ALObject> PageExtensions { get; set; }
        public ICollection<ALObject> PageCustomizations { get; set; }
        public ICollection<ALObject> TableExtensions { get; set; }
        public ICollection<ALObject> Reports { get; set; }
        public ICollection<ALObject> XmlPorts { get; set; }
        public ICollection<ALObject> Queries { get; set; }
        public ICollection<ALObject> Profiles { get; set; }
        public ICollection<ALObject> ControlAddIns { get; set; }
        public ICollection<ALObject> EnumTypes { get; set; }
        public ICollection<ALObject> DotNetPackages { get; set; }
    }
}
