using System;
using Newtonsoft.Json;
using System.IO;
using System.Linq;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using ALObjectParser.Library;

namespace ALObjectDesigner.Library
{
    public class ALObjectCollector
    {
        public List<string> WorkspacePath { get; set; }

        public string[] Types { 
            get {
                return new string[] {
                    "Tables",
                    "Pages",
                    "Reports",
                    "Codeunits",
                    "Queries",
                    "XmlPorts",
                    "Profiles",
                    "PageExtensions",
                    "PageCustomizations",
                    "TableExtensions",
                    "ControlAddIns",
                    "EnumTypes",
                    "DotNetPackages"
                };
            } 
            private set { } }

        public Dictionary<ALObjectParser.ALObjectType, string> ALTypeMap { 
            get {
                var result = new Dictionary<ALObjectParser.ALObjectType, string>();
                result.Add(ALObjectParser.ALObjectType.table, "Tables");
                result.Add(ALObjectParser.ALObjectType.page, "Pages");
                result.Add(ALObjectParser.ALObjectType.report, "Reports");
                result.Add(ALObjectParser.ALObjectType.codeunit, "Codeunits");
                result.Add(ALObjectParser.ALObjectType.query, "Queries");
                result.Add(ALObjectParser.ALObjectType.xmlport, "XmlPorts");
                result.Add(ALObjectParser.ALObjectType.profile, "Profiles");
                result.Add(ALObjectParser.ALObjectType.pageextension, "PageExtensions");
                result.Add(ALObjectParser.ALObjectType.pagecustomization, "PageCustomizations");
                result.Add(ALObjectParser.ALObjectType.tableextension, "TableExtensions");
                result.Add(ALObjectParser.ALObjectType.controladdin, "ControlAddIns");
                result.Add(ALObjectParser.ALObjectType.@enum, "EnumTypes");
                result.Add(ALObjectParser.ALObjectType.dotnet, "DotNetPackages");

                return result;
            } private set { } }

        public ALObjectCollector()
        {
        }

        public ALObjectCollector(List<string> wkspcePaths)
        {
            WorkspacePath = wkspcePaths;
        }

        public async Task<ICollection<CollectorItem>> Discover()
        {
            var result = await Discover(WorkspacePath);

            return result;
        }

        public async Task<ICollection<CollectorItem>> Discover(List<string> wkspcePaths)
        {
            var result = new List<CollectorItem>();

            var symbols = await DiscoverSymbols(wkspcePaths);

            if (symbols.Count() == 0)
            {
                return result;
            }

            var symbolRes = symbols
                .SelectMany(s =>
                {
                    List<dynamic> elems = new List<dynamic>();
                    elems.AddRange(s.Tables);
                    elems.AddRange(s.Pages);
                    elems.AddRange(s.TableExtensions);
                    elems.AddRange(s.PageExtensions);
                    elems.AddRange(s.PageCustomizations);
                    elems.AddRange(s.Codeunits);
                    elems.AddRange(s.Reports);
                    elems.AddRange(s.XmlPorts);
                    elems.AddRange(s.Queries);
                    elems.AddRange(s.ControlAddIns);
                    elems.AddRange(s.EnumTypes);
                    elems.AddRange(s.DotNetPackages);
                    elems.AddRange(s.Profiles);

                    var items = elems.Select(item => new CollectorItem
                    {
                        TypeId = item.Type,
                        Id = item.Id,
                        Type = $"{item.Type}",
                        Publisher = s.Publisher,
                        Version = s.Version.ToString(),
                        //Symbol = item,
                        FsPath = "",
                        Name = item.Name,
                        Application = s.Name,
                        CanExecute = (new string[] { "Table", "Page", "PageExtension", "TableExtension", "PageCustomization", "Report" }).Contains($"{item.Type}"),
                        CanDesign = (new string[] { "Table", "Page" }).Contains($"{item.Type}"),
                        CanCreatePage = (new string[] { "Table", "TableExtension" }).Contains($"{item.Type}"),
                        EventName = "not_an_event",
                        IsEvent = false,
                        SymbolData = new SymbolData
                        {
                            Index = item.Id,
                            Path = s.Path,
                            Type = item.Type
                        }
                    });

                    var events = elems
                    .Where(w =>
                        (w as ALObject)
                        .Methods
                        .SelectMany(a => a.Attributes)
                        .Where(at => at.Name.ToLower().Contains("event"))
                        .Any()
                    )
                    .SelectMany(item =>
                    {
                        var eventItem = (item as ALObject).Methods
                        .Where(ew => ew.Attributes.Where(at => at.Name.ToLower().Contains("event")).Any())
                        .Select(e =>
                        new CollectorItem
                        {
                            TypeId = item.Type,
                            Id = item.Id,
                            Type = $"{item.Type}",
                            Publisher = s.Publisher,
                            Version = s.Version.ToString(),
                            //Symbol = item,
                            FsPath = "",
                            Name = item.Name,
                            Application = s.Name,
                            CanExecute = (new string[] { "Table", "Page", "PageExtension", "TableExtension", "PageCustomization", "Report" }).Contains($"{item.Type}"),
                            CanDesign = (new string[] { "Table", "Page" }).Contains($"{item.Type}"),
                            CanCreatePage = (new string[] { "Table", "TableExtension" }).Contains($"{item.Type}"),
                            EventName = e.Name,
                            EventParameters = e.Parameters,
                            IsEvent = true
                        });

                        return eventItem;
                    });

                    if (events != null)
                        items = items.Concat(events);

                    return items;
                })
                .AsQueryable();

            var localSymbols = DiscoverLocalFiles(wkspcePaths);
            symbolRes = symbolRes
                .Distinct(new CollectorItemComparer())
                .Except(localSymbols, new CollectorItemComparer());

            symbolRes = symbolRes.Concat(localSymbols);

            return symbolRes.ToList();
        }

        public async Task<ICollection<SymbolReference>> DiscoverSymbols(List<string> wkspcePaths)
        {
            var symbolPaths = GetSymbolPaths(wkspcePaths);

            if (symbolPaths.Count() == 0)
            {
                return new List<SymbolReference>();
            }

            var symbolTasks = symbolPaths.AsQueryable().Select(s => GetSymbolReference(s.FullName)).AsQueryable();
            var result = await Task.WhenAll<SymbolReference>(symbolTasks);
            
            return result;
        }

        public ICollection<CollectorItem> DiscoverLocalFiles(List<string> wkspcePaths)
        {
            var projects = ALProjectCollector.Discover(wkspcePaths);
            var srcDirs = projects.Select(s => Directory.GetParent(s.FilePath).FullName).ToArray();
            var result = new List<CollectorItem>().AsQueryable();

            foreach (var project in projects)
            {
                var path = Directory.GetParent(project.FilePath).FullName;
                var localSymbols = Directory
                    .GetDirectories(path)
                    .SelectMany(s => Directory.GetFiles(s, "*.al", SearchOption.AllDirectories))
                    .SelectMany(item =>
                    {
                        var collectorItems = new List<CollectorItem>();
                        var alobjects = ALParser.Read(item);
                        foreach (var alobject in alobjects)
                        {

                            collectorItems.Add(new CollectorItem
                            {
                                TypeId = alobject.Type,
                                Id = alobject.Id,
                                Type = $"{alobject.Type}",
                                Publisher = project.publisher,
                                //Version = project.version
                                //Symbol = item,
                                FsPath = item,
                                Name = alobject.Name,
                                Application = project.name,
                                CanExecute = (new string[] { "Table", "Page", "PageExtension", "TableExtension", "PageCustomization", "Report" }).Contains($"{alobject.Type}"),
                                CanDesign = (new string[] { "Table", "Page" }).Contains($"{alobject.Type}"),
                                CanCreatePage = (new string[] { "Table", "TableExtension" }).Contains($"{alobject.Type}"),
                                EventName = "not_an_event",
                                IsEvent = false,
                                SymbolData = new SymbolData
                                {
                                    Index = alobject.Id,
                                    Path = item,
                                    Type = alobject.Type
                                }
                            });
                        }

                        return collectorItems;
                    });

                result = result.Concat(localSymbols);
            }

            return result.ToList();
        }

        public List<FileInfo> GetSymbolPaths(List<string> wkspcePaths)
        {
            var symbolPaths = new List<FileInfo>();
            foreach (var path in wkspcePaths)
            {
                var files = Directory
                    .GetDirectories(path, ".alpackages", SearchOption.AllDirectories)
                    .SelectMany(s => Directory.GetFiles(s, "*.app"))
                    .Select(s => new FileInfo(s))
                    .ToList();

                symbolPaths.AddRange(files);
            }

            symbolPaths = symbolPaths
                .Distinct(new FileInfoNameComparer())
                .ToList();

            return symbolPaths;
        }

        public Task<SymbolReference> GetSymbolReference(string symbolPath)
        {
            var symbolRef = new SymbolReference();

            using (Stream fs = File.OpenRead(symbolPath))
            using (var zf = new ZipFile(fs))
            {
                var zipEntry = zf.GetEntry("SymbolReference.json");

                using (var zipStream = new StreamReader(zf.GetInputStream(zipEntry)))
                {
                    var contents = zipStream.ReadToEnd();
                    symbolRef = JsonConvert.DeserializeObject<SymbolReference>(contents);

                    symbolRef.Tables = symbolRef.Tables.Select(x => { x.Type = ALObjectParser.ALObjectType.table; return x; });
                    symbolRef.TableExtensions = symbolRef.TableExtensions.Select(x => { x.Type = ALObjectParser.ALObjectType.tableextension; return x; });
                    symbolRef.Pages = symbolRef.Pages.Select(x => { x.Type = ALObjectParser.ALObjectType.page; return x; });
                    symbolRef.PageCustomizations = symbolRef.PageCustomizations.Select(x => { x.Type = ALObjectParser.ALObjectType.pagecustomization; return x; });
                    symbolRef.PageExtensions = symbolRef.PageExtensions.Select(x => { x.Type = ALObjectParser.ALObjectType.pageextension; return x; });
                    symbolRef.Reports = symbolRef.Reports.Select(x => { x.Type = ALObjectParser.ALObjectType.report; return x; });
                    symbolRef.Codeunits = symbolRef.Codeunits.Select(x => { x.Type = ALObjectParser.ALObjectType.codeunit; return x; });
                    symbolRef.XmlPorts = symbolRef.XmlPorts.Select(x => { x.Type = ALObjectParser.ALObjectType.xmlport; return x; });
                    symbolRef.Queries = symbolRef.Queries.Select(x => { x.Type = ALObjectParser.ALObjectType.query; return x; });
                    symbolRef.EnumTypes = symbolRef.EnumTypes.Select(x => { x.Type = ALObjectParser.ALObjectType.@enum; return x; });
                    symbolRef.ControlAddIns = symbolRef.ControlAddIns.Select(x => { x.Type = ALObjectParser.ALObjectType.controladdin; return x; });
                    symbolRef.DotNetPackages = symbolRef.DotNetPackages.Select(x => { x.Type = ALObjectParser.ALObjectType.dotnet; return x; });
                    symbolRef.Profiles = symbolRef.Profiles.Select(x => { x.Type = ALObjectParser.ALObjectType.profile; return x; });

                    symbolRef.Path = symbolPath;
                }
            }

            return Task.FromResult(symbolRef);
        }

        public async Task<IALObject> GetSymbolObject(SymbolData data)
        {
            FileInfo info = new FileInfo(data.Path);
            if (info.Extension == ".al")
            {
                var alobjects = ALParser.Read(data.Path);
                var alobject = alobjects.FirstOrDefault(f => f.Type == data.Type && f.Id == data.Index);

                return alobject;
            }

            var symbols = await GetSymbolReference(data.Path);
            var packType = ALTypeMap[data.Type];

            var objects = symbols.GetType().GetProperty(packType)?.GetValue(symbols) as IEnumerable<IALObject>;
            var result = objects.Where(w => w.Id == data.Index).FirstOrDefault();
            return result;
        }
    }
}
