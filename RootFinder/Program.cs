using RootFinder.Data;
using System;
using System.Collections.Generic;
using System.IO;

namespace RootFinder
{
    public class Program
    {

        public enum TorchVersion { T18, T19, T20 };

        public enum CSVFormat
        {
            ALL,
            BOOL_INT_ONLY,
            METHOD_ONLY
        };

        public static CSVFormat CsvFormat = CSVFormat.METHOD_ONLY;

        public static TorchVersion TVersion = TorchVersion.T20;
        public enum Keywords { Async, Network, Random, DateTime };
        internal static Dictionary<string, Keywords> StringToKeywords = new Dictionary<string, Keywords> {
            { "Async", Keywords.Async },
            { "System.DateTime", Keywords.DateTime },
            { "System.Random", Keywords.Random },
            { "System.Net.HttpWeb", Keywords.Network }
        };

        static void Main(string[] args)
        {
            List<string> argsList = new List<string>(args);
            if (argsList.Contains("-debugMode"))
            {
                System.Diagnostics.Debugger.Launch();
            }

            string passingDir = null;
            if (argsList.Contains("-passingLogsDir"))
            {
                passingDir = argsList[argsList.IndexOf("-passingLogsDir") + 1];
            }

            string failingDir = null;
            if (argsList.Contains("-failingLogsDir"))
            {
                failingDir = argsList[argsList.IndexOf("-failingLogsDir") + 1];
            }

            string methodName = null;
            if (argsList.Contains("-methodName"))
            {
                methodName = argsList[argsList.IndexOf("-methodName") + 1];
            }

            if (argsList.Contains("-torchVersion"))
            {
                var torchNum = Int32.Parse(argsList[argsList.IndexOf("-torchVersion") + 1]);
                if (torchNum == 18)
                {
                    TVersion = TorchVersion.T18;
                } else if (torchNum == 19)
                {
                    TVersion = TorchVersion.T19;
                } else if (torchNum == 20)
                {
                    TVersion = TorchVersion.T20;
                } else
                {
                    throw new Exception("Invalid -torchVersion argument.");
                }
            }

            if (argsList.Contains("-csvformat"))
            {
                var csvFormat = argsList[argsList.IndexOf("-csvformat") + 1];
                if (csvFormat.Equals("all", StringComparison.OrdinalIgnoreCase))
                {
                    CsvFormat = CSVFormat.ALL;
                }
                else if (csvFormat.Equals("bool_int_only", StringComparison.OrdinalIgnoreCase))
                {
                    CsvFormat = CSVFormat.BOOL_INT_ONLY;
                }
                else if (csvFormat.Equals("method_only", StringComparison.OrdinalIgnoreCase))
                {
                    CsvFormat = CSVFormat.METHOD_ONLY;
                }
                else
                {
                    throw new Exception("Invalid -csvformat argument.");
                }
            }

            Predicate.Predicate.PredicateType type = Predicate.Predicate.PredicateType.Unrecognized;
            if (argsList.Contains("-type"))
            {
                var passedType = Enum.Parse(typeof(Predicate.Predicate.PredicateType), argsList[argsList.IndexOf("-type") + 1]);
                if (passedType.Equals(Predicate.Predicate.PredicateType.Relative))
                {
                    type = Predicate.Predicate.PredicateType.Relative;
                }
                else if (passedType.Equals(Predicate.Predicate.PredicateType.Absolute))
                {
                    type = Predicate.Predicate.PredicateType.Absolute;
                }
                else if (passedType.Equals(Predicate.Predicate.PredicateType.Exception))
                {
                    type = Predicate.Predicate.PredicateType.Exception;
                }
                else if (passedType.Equals(Predicate.Predicate.PredicateType.Fast))
                {
                    type = Predicate.Predicate.PredicateType.Fast;
                }
                else if (passedType.Equals(Predicate.Predicate.PredicateType.Slow))
                {
                    type = Predicate.Predicate.PredicateType.Slow;
                }
                else
                {
                    throw new SystemException("Unrecognized type : " + passedType);
                }
            }

            string outputDir;
            if (argsList.Contains("-outputDir"))
            {
                outputDir = argsList[argsList.IndexOf("-outputDir") + 1];
                Directory.CreateDirectory(outputDir);
            }
            else
            {
                outputDir = Directory.GetCurrentDirectory();
            }

            var passingFiles = Directory.GetFiles(passingDir, "*.torchlog.decompressed*", SearchOption.AllDirectories);
            var failingFiles = Directory.GetFiles(failingDir, "*.torchlog.decompressed*", SearchOption.AllDirectories);

            var passingLogs = GetLogsFromAllRuns(passingFiles, methodName, true);
            var failingLogs = GetLogsFromAllRuns(failingFiles, methodName, false);


            switch (type)
            {
                case Predicate.Predicate.PredicateType.Relative:
                    RunPredicate(outputDir, passingLogs, failingLogs, methodName, new List<string>(), type, "Relative.xml");
                    break;
                case Predicate.Predicate.PredicateType.Absolute:
                    string absoluteVal = "";
                    if (argsList.Contains("-predicate_Val"))
                    {
                        absoluteVal = argsList[argsList.IndexOf("-predicate_Val") + 1];
                    }
                    RunPredicate(outputDir, passingLogs, failingLogs, methodName, new List<string> { absoluteVal }, type, "Absolute.xml");
                    break;
                case Predicate.Predicate.PredicateType.Exception:
                    string exceptionVal = "";
                    if (argsList.Contains("-predicate_Val"))
                    {
                        exceptionVal = argsList[argsList.IndexOf("-predicate_Val") + 1];
                    }
                    RunPredicate(outputDir, passingLogs, failingLogs, methodName, new List<string> { exceptionVal }, type, "Exception.xml");
                    break;
                case Predicate.Predicate.PredicateType.Fast:
                    string fastVal = "";
                    if (argsList.Contains("-predicate_Val"))
                    {
                        fastVal = argsList[argsList.IndexOf("-predicate_Val") + 1];
                    }
                    RunPredicate(outputDir, passingLogs, failingLogs, methodName, new List<string>{fastVal}, type, "Fast.xml");
                    break;
                case Predicate.Predicate.PredicateType.Slow:
                    string slowVal = "";
                    if (argsList.Contains("-predicate_Val"))
                    {
                        slowVal = argsList[argsList.IndexOf("-predicate_Val") + 1];
                    }
                    RunPredicate(outputDir, passingLogs, failingLogs, methodName, new List<string> { slowVal }, type, "Slow.xml");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static void RunPredicate(string outputDir, List<Log> passingLogs, List<Log> failingLogs, string methodName, List<string> args, Predicate.Predicate.PredicateType type, string outputFileName)
        {
            if (passingLogs.Count != 0 && failingLogs.Count != 0)
            {
                var preciate = new Predicate.Predicate(passingLogs, failingLogs, type, outputDir, args);
                preciate.ToXml().Save(Path.Combine(outputDir, outputFileName));
            }
            else
            {
                Console.WriteLine("Unable to find passing and failing logs containing the specified method name: " + methodName);
            }
        }

        public static List<Log> GetLogsFromAllRuns(string[] files, string methodName, bool isPassing)
        {
            List<Log> listLogs = new List<Log>();
            foreach(var fileName in files)
            {
                var log = ParseFileCallee(fileName, methodName, isPassing);
                AddSeqToLog(log);
                NormalizeTheadId(log);
                listLogs.Add(log);
            }
            return listLogs;
        }

        internal static Log ParseFileCallee(string file, string methodName, bool isPassing)
        {
            var entryToChunk = new List<LineChunk>();
            string[] lines = File.ReadAllLines(file);

            for (var i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith("#Header:"))
                {
                    continue;
                }
                var lineEntry = new LineEntry(lines[i], i, file, TVersion);

                if (methodName == null || CsvFormat.Equals(CSVFormat.METHOD_ONLY) && lineEntry.Callee.Contains(methodName) || CsvFormat.Equals(CSVFormat.ALL) || (CsvFormat.Equals(CSVFormat.BOOL_INT_ONLY) && !lineEntry.ReturnValue.ValueType.Equals(ReturnVal.Type.StringType)))
                {
                    LineChunk chunk = new LineChunk(lineEntry, file, isPassing);
                    entryToChunk.Add(chunk);
                }
            }
            return new Log(entryToChunk, file);
        }

        internal static void AddSeqToLog(Log log)
        {
            var epochToCount = new Dictionary<string, int>();

            foreach (var line in log.GetAllLines())
            {
                var epoch = line.GetLineId();
                var gotValue = epochToCount.TryGetValue(epoch, out var currentVal);
                if (!gotValue)
                {
                    currentVal = 1;
                }
                else
                {
                    currentVal = currentVal + 1;
                }
                epochToCount[epoch] = currentVal;
                line.SequenceNumber = currentVal;
            }
        }

        internal static void NormalizeTheadId(Log log)
        {
            var threadIdToNormalizeId = new Dictionary<int, int>();

            var currentThreadId = 0;
            foreach (var line in log.GetAllLines())
            {
                var threadId = line.ManagedThreadIdOriginal;
                var gotValue = threadIdToNormalizeId.TryGetValue(threadId, out var currentVal);
                if (!gotValue)
                {
                    currentThreadId += 1;
                    currentVal = currentThreadId;
                    threadIdToNormalizeId[threadId] = currentVal;
                }
                line.ManagedThreadId = currentVal;
            }
        }
    }
}
