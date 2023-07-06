using System.Text.RegularExpressions;
using CombinedActorInfo;
using ToolLib;

namespace caiextract
{
    public class Program
    {
        public static byte[] SaveMagic { get; set; } = {4, 3, 2, 1};
        public static ulong HashID = 2774999734;

        public static void Main(string[] args)
        {
            // args = new[] {"progress.sav", "-v", "-e2" , "-e12", "-e23"};
            // args = new[] {"progress.sav", "-w1", "-fAutoBuild1.cai" };

            var fileName = args[0];
            var verbose = args.Contains("-v");

            if (!File.Exists(fileName))
            {
                Console.WriteLine("File Does Not Exist");
                return;
            }

            var saveBytes = File.ReadAllBytes(fileName);

            var reader = new BinaryReader(new MemoryStream(saveBytes));
            if (!(reader.BaseStream.Position + SaveMagic.Length < reader.BaseStream.Length && reader.ReadBytes(SaveMagic.Length).SequenceEqual(SaveMagic)))
            {
                Console.WriteLine("Invalid Save File");
                return;
            }

            var extract = args.Where(c => Regex.IsMatch(c.Trim(), "-[eE]\\d+")).Distinct().Select(c => Convert.ToInt32(c[2..])).Where(c => c is < 31 and > 0).ToArray();
            var write = args.Where(c => Regex.IsMatch(c.Trim(), "-[wW]\\d+")).Distinct().Select(c => Convert.ToInt32(c[2..])).Where(c => c is < 31 and > 0).ToArray();
            var fileIn = args.Where(c => Regex.IsMatch(c.Trim(), "-[fF][\\w.-:\\\\]+")).Distinct().Select(c => c[2..]).FirstOrDefault();

            var isExtract = extract.Length > 0;
            var isWrite = write.Length > 0;

            if (isExtract && isWrite)
            {
                Console.WriteLine("Can Only Extract OR Write. Not Both At Once");
                return;
            }

            if (write.Length > 1)
            {
                Console.WriteLine("Can Only Write One CAI At A Time");
                return;
            }

            if (!isExtract && !isWrite)
            {
                verbose = true;
            }

            var dataOffset = reader.ReadInt32At(0x08);
            reader.BaseStream.Position = 0x28;
            var offset = 0;

            Console.WriteLine("Searching For Offset");
            while (reader.BaseStream.Position < dataOffset)
            {
                var hash = reader.ReadUInt32();
                offset = reader.ReadInt32();

                if (hash == HashID)
                {
                    Console.WriteLine($"Offset Found At: 0x{offset:X8}");
                    break;
                }

            }

            Console.WriteLine("");

            reader.BaseStream.Position = offset;
            if (isExtract)
            {
                var actorArray = new byte[30][];
                var entryCount = reader.ReadInt32();
                for (var i = 0; i < entryCount; i++)
                {
                    var length = reader.ReadInt32();
                    var caiData = reader.ReadBytes(length);
                    actorArray[i] = caiData;
                }

                foreach (var e in extract)
                {
                    Console.WriteLine($"*** AutoBuild #{e:D2} ***");
                    if (verbose)
                    {
                        Console.WriteLine("");
                        var cai = new CombinedActor(new BinaryReader(new MemoryStream(actorArray[e].Skip(6).ToArray())));
                        foreach (var actor in cai.Actors)
                        {
                            Console.WriteLine($"{actor.Matrix.Position.X,5:F2}X {actor.Matrix.Position.Y,5:F2}Y {actor.Matrix.Position.Z,5:F2}Z | {actor.Matrix.Rotation.X,8:F2}RotX {actor.Matrix.Rotation.Y,8:F2}RotY {actor.Matrix.Rotation.Z,8:F2}RotZ | {actor.Name}");
                        }

                        Console.WriteLine("");
                    }
                    File.WriteAllBytes($"AutoBuild{e}.cai", actorArray[e]);
                    Console.WriteLine($"File Written To: {Directory.GetCurrentDirectory()}\\AutoBuild{e}.cai");
                }

            } else if (isWrite)
            {

                if (isWrite && string.IsNullOrEmpty(fileIn))
                {
                    Console.WriteLine("Must Specify CAI File To Write with -f");
                    return;
                }

                if (!File.Exists(fileIn))
                {
                    Console.WriteLine("Input CAI File Does Not Exist");
                    return;
                }

                var entryBytes = File.ReadAllBytes(fileIn);

                var isTotkab = entryBytes.Take(8).SequenceEqual("TOTKAuto"u8.ToArray());
                if (isTotkab)
                {
                    Console.WriteLine($"Every Time You Use A TOTKAB File, I Say Mean Things To A Puppy");
                    entryBytes = entryBytes.Skip(48).ToArray();
                }

                var magicOk = entryBytes.Take(6).SequenceEqual("CmbAct"u8.ToArray());
                if (!magicOk)
                {
                    Console.WriteLine($"File {fileIn} Does Not Appear To Be A CAI File");
                    return;
                }

                var entryCount = reader.ReadInt32();
                for (var i = 0; i < entryCount; i++)
                {
                    var length = reader.ReadInt32();
                    if (i == write[0] - 1)
                    {
                        if (entryBytes.Length != length)
                        {
                            Console.WriteLine($"Byte Count Mismatch. Incoming: {entryBytes.Length} Expected: {length}");
                            Console.WriteLine("This Tool Does Not Currently Support File Size Changes");
                            return;
                        }

                        Console.WriteLine($"CAI Offset 0x{reader.BaseStream.Position}");
                        var byteStream = new MemoryStream(saveBytes);
                        var writer = new BinaryWriter(byteStream);
                        writer.Seek((int)reader.BaseStream.Position, SeekOrigin.Begin);
                        writer.Write(entryBytes);
                        writer.Flush();
                        File.WriteAllBytes("progress.sav.out", byteStream.ToArray());
                        Console.WriteLine($"File Written To: {Directory.GetCurrentDirectory()}\\progress.sav.out");
                        return;
                    }

                    var caiData = reader.ReadBytes(length);
                }

            }
            else
            {
                reader.BaseStream.Position = offset;
                var entryCount = reader.ReadInt32();
                for (var i = 0; i < entryCount; i++)
                {
                    var length = reader.ReadInt32();
                    var caiData = reader.ReadBytes(length);
                    var cai = new CombinedActor(new BinaryReader(new MemoryStream(caiData.Skip(6).ToArray())));
                    if (verbose)
                    {
                        Console.WriteLine($"*** AutoBuild #{i + 1:D2} ***");
                        foreach (var actor in cai.Actors)
                        {
                            Console.WriteLine($"{actor.Matrix.Position.X,5:F2}X {actor.Matrix.Position.Y,5:F2}Y {actor.Matrix.Position.Z,5:F2}Z | {actor.Matrix.Rotation.X,8:F2}RotX {actor.Matrix.Rotation.Y,8:F2}RotY {actor.Matrix.Rotation.Z,8:F2}RotZ | {actor.Name}");
                        }

                        Console.WriteLine("");
                    }
                }
            }
        }
    }
}
