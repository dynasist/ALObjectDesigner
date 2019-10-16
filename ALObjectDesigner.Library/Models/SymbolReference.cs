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
            Tables = new List<ALTable>();
            Codeunits = new List<ALCodeunit>();
            Pages = new List<ALPage>();
            PageCustomizations = new List<ALPageCustomization>();
            PageExtensions = new List<ALPageExtension>();
            TableExtensions = new List<ALTableExtension>();
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
        public IEnumerable<ALTable> Tables { get; set; }
        public IEnumerable<ALCodeunit> Codeunits { get; set; }
        public IEnumerable<ALPage> Pages { get; set; }
        public IEnumerable<ALPageExtension> PageExtensions { get; set; }
        public IEnumerable<ALPageCustomization> PageCustomizations { get; set; }
        public IEnumerable<ALTableExtension> TableExtensions { get; set; }
        public IEnumerable<ALObject> Reports { get; set; }
        public IEnumerable<ALObject> XmlPorts { get; set; }
        public IEnumerable<ALObject> Queries { get; set; }
        public IEnumerable<ALObject> Profiles { get; set; }
        public IEnumerable<ALObject> ControlAddIns { get; set; }
        public IEnumerable<ALObject> EnumTypes { get; set; }
        public IEnumerable<ALObject> DotNetPackages { get; set; }
        public string Path { get; set; }
    }
}
