using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
namespace ConfigConvertor
{
    class Program
    {
        static void Main(string[] args)
        {
            // az account set --subscription a909e367-fe60-4605-b7b0-b0286868316c
            // az functionapp config appsettings list --name uecstream-uks-submission-func --resource-group uec-stream-rg
            // az functionapp config appsettings list --name ueccontrol-uks-submission-func --resource-group uec-control-rg
            // az webapp config connection-string list --name ueccontrol-uks-submission-func --resource-group uec-control-rg

            var json = File.ReadAllText(@"C:\shared\config.txt");

            var typeOfConversion = "appSettings"; //other is appSettings/connectionStrings
            var enclosingObjecyKey = "";
            if (typeOfConversion == "appSettings")
            {
                enclosingObjecyKey = "Values";
            }
            else
            {
                enclosingObjecyKey = "ConnectionStrings";
            }
            var pattern = @"\""(?:\\.|[^\""\\\\])*\"""; ;
            Regex regexp = new Regex(pattern,   RegexOptions.IgnorePatternWhitespace | RegexOptions.IgnoreCase);
            var matchCollection = regexp.Matches(json).Cast<Match>()
                .Select(m => m.Value)
                .ToArray();

            var allMatches = new List<string>();
            var iterator = 0;
            foreach (var match in matchCollection)
            {
                string key = match.ToString();
                var keyItem = match.ToString().Replace("\"", "").Replace(@"\\n", "").Replace("\\", "\"");
                if (keyItem == "name" || keyItem == "Custom" || keyItem == "type" || keyItem == "slotSetting" || keyItem == "value")
                    continue;

                allMatches.Add(keyItem);
                iterator++;
            }

            var keys = new List<string>();
            iterator = 0;
            foreach (var match in allMatches)
            {
                string key = match.ToString();
                if (iterator % 2 == 0)
                    keys.Add(match.ToString());

                iterator++;
            }

            var values = new List<string>();
            iterator = 0;
            foreach (var match in allMatches)
            {
                string key = match.ToString();
                if (iterator % 2 != 0)
                    values.Add(match.ToString());
                
                iterator++;
            }

            var dict = new Dictionary<string, string>();

            for (int i = 0; i < values.Count(); i++)
            {
                dict.Add(keys[i], values[i]);
            }

            var tuple = new List<Tuple<string, string, string, string>>();

            foreach (var item in dict)
            {
                var items = item.Key.Split(":");
                if (items.Length == 1)
                {
                    //no :colon then append Values
                    var items2 = new List<string>();
                    items2.Add(enclosingObjecyKey);
                    items2.AddRange(items);
                    items = items2.ToArray();
                }

                tuple.Add(new Tuple<string, string, string, string>(items[0].Trim(),items.Length> 1 ? items[1].Trim(): string.Empty, items.Length > 2 ? items[2].Trim() : string.Empty, item.Value.Trim()));
            }

            var groups = tuple.GroupBy(x => x.Item1);
            var root = JObject.Parse("{}");
            if (enclosingObjecyKey == "Values")
            {
                root.Add("IsEncrypted", new JValue(false));
            }

            foreach (var group in groups)
            {
                var tupleItem = tuple.Where(x => x.Item1 == group.Key);
                root.Add(group.Key, FormatTupleItem(tupleItem) );
            }

            var rootString = root.ToString().Replace(@"{{", @"{").Replace(@"}}", "}");

            Console.WriteLine(rootString);

        }

        private static JToken FormatTupleItem(IEnumerable<Tuple<string, string, string, string>> tupleItems)
        {
            JObject jObj = new JObject();
            var jArr = new JArray();
            bool isArray = false;
            foreach (var item in tupleItems)
            {
                if (!string.IsNullOrWhiteSpace(item.Item3))
                {
                    jArr.Add(item.Item4);
                    isArray = true;
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(item.Item2))
                    {
                        var jValue = new JValue(CheckForBoolean(item.Item4));
                        return jValue;
                    }

                    jObj.Add(item.Item2, CheckForBoolean(item.Item4));
                }
            }

            if (isArray)
            {
                return jArr;
            }

            return jObj;

        }

        private static JValue CheckForBoolean(string item)
        {
            if (item == "true" || item == "false")
                return new JValue(Convert.ToBoolean(item));

            return new JValue(item);
        }
    }
}
