using CombinedActorInfo;
using Newtonsoft.Json;

namespace cai2json
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length < 1 || string.IsNullOrEmpty(args[0]))
            {
                Console.WriteLine("Must specify path to file");
                return;
            }

            if (!File.Exists(args[0]))
            {
                Console.WriteLine("File does not exist");
                return;
            }

            if (args[0].EndsWith(".json"))
            {
                try
                {
                    var json = File.ReadAllText(args[0]);
                    var test = CombinedActorInfoFile.FromJson(json);
                    var outFile = args[0][..^5];
                    outFile = args.Length > 1 ? args[1] : outFile;

                    Console.WriteLine(outFile);

                    if (File.Exists(outFile))
                    {
                        if (File.Exists(outFile + ".bak"))
                        {
                            File.Delete(outFile + ".bak");
                        }
                        File.Move(outFile, outFile + ".bak");
                    }
                    test.Save(outFile);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            else
            {
                var fil = CombinedActorInfoFile.Open(args[0]);
                var json = JsonConvert.SerializeObject(fil.CombinedActor, Formatting.Indented, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
                File.WriteAllText(args[0] + ".json", json);
            }
        }
    }
}
