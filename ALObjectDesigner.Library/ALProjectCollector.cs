using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ALObjectDesigner.Library
{
    public class ALProjectCollector
    {
        public ALProjectCollector()
        {
        }

        public static IEnumerable<FileInfo> GetProjectInfo(List<string> wkspcePaths)
        {
            var result = new List<FileInfo>();
            foreach (var path in wkspcePaths)
            {
                var files = Directory
                    .GetDirectories(path)
                    .SelectMany(s => Directory.GetFiles(s, "app.json", SearchOption.AllDirectories))
                    .Select(s => new FileInfo(s));

                var baseFile = Directory
                    .GetFiles(path, "app.json", SearchOption.AllDirectories)
                    .Select(s => new FileInfo(s));

                result.AddRange(baseFile);
                result.AddRange(files);
            }

            return result;
        }

        public static IEnumerable<ALProject> Discover(List<string> wkspcePaths)
        {
            var paths = ALProjectCollector.GetProjectInfo(wkspcePaths).Select(s => s.FullName);

            var projects = paths.Select(s => {
                var fileContent = File.ReadAllText(s);
                var result = JsonConvert.DeserializeObject<ALProject>(fileContent);
                result.FilePath = s;
                return result;
            });

            return projects.ToList();
        }
    }
}
