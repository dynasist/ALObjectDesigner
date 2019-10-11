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

        public ALObjectCollector()
        { }

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

            result = symbols
                .SelectMany(s => {
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
                        EventName = "not_an_event"
                    })
                    .ToList();

                    var events = elems
                    .Where(w => 
                        (w as ALObject)
                        .Methods
                        .SelectMany(a => a.Attributes)
                        .Where(at => at.Name.ToLower().Contains("event"))
                        .Any()
                    )
                    .SelectMany(item => {
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
                    })
                    .ToList();

                    if (events != null)
                        items.AddRange(events);

                    return items;
                })
                .ToList();

            return result;
        }

        public async Task<ICollection<SymbolReference>> DiscoverSymbols(List<string> wkspcePaths)
        {
            var symbolPaths = GetSymbolPaths(wkspcePaths);

            if (symbolPaths.Count() == 0)
            {
                return new List<SymbolReference>();
            }

            var symbolTasks = symbolPaths.AsQueryable().Select(s => GetSymbolReference(s.FullName));

            var result = await Task.WhenAll<SymbolReference>(symbolTasks);

            return result;
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
                    symbolRef.Tables = symbolRef.Tables.Select(x => { x.Type = ALObjectParser.ALObjectType.table; return x; }).ToList();
                    symbolRef.TableExtensions = symbolRef.TableExtensions.Select(x => { x.Type = ALObjectParser.ALObjectType.tableextension; return x; }).ToList();
                    symbolRef.Pages = symbolRef.Pages.Select(x => { x.Type = ALObjectParser.ALObjectType.page; return x; }).ToList();
                    symbolRef.PageCustomizations = symbolRef.PageCustomizations.Select(x => { x.Type = ALObjectParser.ALObjectType.pagecustomization; return x; }).ToList();
                    symbolRef.PageExtensions = symbolRef.PageExtensions.Select(x => { x.Type = ALObjectParser.ALObjectType.pageextension; return x; }).ToList();
                    symbolRef.Reports = symbolRef.Reports.Select(x => { x.Type = ALObjectParser.ALObjectType.report; return x; }).ToList();
                    symbolRef.Codeunits = symbolRef.Codeunits.Select(x => { x.Type = ALObjectParser.ALObjectType.codeunit; return x; }).ToList();
                    symbolRef.XmlPorts = symbolRef.XmlPorts.Select(x => { x.Type = ALObjectParser.ALObjectType.xmlport; return x; }).ToList();
                    symbolRef.Queries = symbolRef.Queries.Select(x => { x.Type = ALObjectParser.ALObjectType.query; return x; }).ToList();
                    symbolRef.EnumTypes = symbolRef.EnumTypes.Select(x => { x.Type = ALObjectParser.ALObjectType.@enum; return x; }).ToList();
                    symbolRef.ControlAddIns = symbolRef.ControlAddIns.Select(x => { x.Type = ALObjectParser.ALObjectType.controladdin; return x; }).ToList();
                    symbolRef.DotNetPackages = symbolRef.DotNetPackages.Select(x => { x.Type = ALObjectParser.ALObjectType.dotnet; return x; }).ToList();
                    symbolRef.Profiles = symbolRef.Profiles.Select(x => { x.Type = ALObjectParser.ALObjectType.profile; return x; }).ToList();
                }
            }

            return Task.FromResult(symbolRef);
        }

    }
}
