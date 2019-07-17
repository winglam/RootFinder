using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LogParser.Data;
using RootFinder;
using RootFinder.Predicate;
using static RootFinder.Program;

namespace LogParser
{
    class Program
    {
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

            if (argsList.Contains("-torchVersion"))
            {
                var torchNum = Int32.Parse(argsList[argsList.IndexOf("-torchVersion") + 1]);
                if (torchNum == 18)
                {
                    TVersion = TorchVersion.T18;
                }
                else if (torchNum == 19)
                {
                    TVersion = TorchVersion.T19;
                }
                else if (torchNum == 20)
                {
                    TVersion = TorchVersion.T20;
                }
                else
                {
                    throw new Exception("Invalid -torchVersion argument.");
                }
            }

            Predicate.PredicateType type = Predicate.PredicateType.Unrecognized;
            if (argsList.Contains("-type"))
            {
                var passedType = Enum.Parse(typeof(Predicate.PredicateType), argsList[argsList.IndexOf("-type") + 1]);
                if (passedType.Equals(Predicate.PredicateType.Relative))
                {
                    type = Predicate.PredicateType.Relative;
                }
                else if (passedType.Equals(Predicate.PredicateType.Fast))
                {
                    type = Predicate.PredicateType.Fast;
                }
                else if (passedType.Equals(Predicate.PredicateType.Slow))
                {
                    type = Predicate.PredicateType.Slow;
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

            Console.WriteLine(DateTime.Now.ToString("HH:mm:ss tt"));

            var passingFiles = Directory.GetFiles(passingDir, "*.torchlog.decompressed", SearchOption.AllDirectories);
            var failingFiles = Directory.GetFiles(failingDir, "*.torchlog.decompressed", SearchOption.AllDirectories);

            var passingLogs = GetLogsFromAllRuns(passingFiles, null, true);
            var failingLogs = GetLogsFromAllRuns(failingFiles, null, false);

            if (type == Predicate.PredicateType.Fast || type == Predicate.PredicateType.Slow)
            {
                CheckAllSlowFast(passingLogs, failingLogs, outputDir);
            }
            else if (type == Predicate.PredicateType.Relative)
            {
                CheckAllRelative(passingLogs, failingLogs, outputDir);
            }

            Console.WriteLine(DateTime.Now.ToString("HH:mm:ss tt"));
        }


        internal static void CheckAllSlowFast(List<Log> passingLogs, List<Log> failingLogs, string outputDir)
        {
            var calleeLists = GetCalleeLists(passingLogs, failingLogs);
            File.WriteAllText(Path.Combine(outputDir, "CalleeList.txt"), calleeLists.ToString());

            RunPredicate(outputDir, passingLogs, failingLogs, "(Running all method configuration)", new List<string>(), Predicate.PredicateType.Slow, "Slow.xml");
        }


        internal static void CheckAllRelative(List<Log> passingLogs, List<Log> failingLogs, string outputDir)
        {
            var calleeLists = GetCalleeLists(passingLogs, failingLogs);
            File.WriteAllText(Path.Combine(outputDir, "CalleeList.txt"), calleeLists.ToString());

            RunPredicate(outputDir, passingLogs, failingLogs, "(Running all method configuration)", new List<string>(), Predicate.PredicateType.Relative, "Relative.xml");
        }

        internal static CalleeLists GetCalleeLists(List<Log> passingLogs, List<Log> failingLogs)
        {
            var calleeLists =
                new CalleeLists
                {
                    PassingLogsCallees = new HashSet<string>(passingLogs.SelectMany(l => l.GetAllCallee())),
                    FailingLogsCallees = new HashSet<string>(failingLogs.SelectMany(l => l.GetAllCallee()))
                };

            calleeLists.UnionLogsCallees = new HashSet<string>(calleeLists.PassingLogsCallees.Concat(calleeLists.FailingLogsCallees));
            calleeLists.IntersectionLogsCallees = new HashSet<string>(calleeLists.PassingLogsCallees.Intersect(calleeLists.FailingLogsCallees));

            return calleeLists;
        }
    }
}
