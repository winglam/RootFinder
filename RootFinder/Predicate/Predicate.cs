using RootFinder.Data;
using RootFinder.PredicateData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace RootFinder.Predicate
{
    public class Predicate
    {
        internal List<CollisionLogNode> Nodes { get; set; }
        public enum CollisionNodeStatus { InconsistentInPassing, InconsistentInFailing,
            ConsistentAndMatching, ConsistentButDifferent, ConsistentButDifferentMissingInFailing, ConsistentButDifferentMissingInPassing
        }

        public enum PredicateType
        {
            Relative, Absolute, Fast, Slow, Exception, Unrecognized
        }

        internal readonly Dictionary<CollisionNodeStatus, string> StatusToMessage = new Dictionary<CollisionNodeStatus, string>
        {
            { CollisionNodeStatus.InconsistentInPassing, "The return value of this Line sometimes collides with other Lines but not always in the Passing logs." },
            { CollisionNodeStatus.InconsistentInFailing, "The return value of this Line consistently collides (or not) with other Lines in the Passing logs, but the Failing logs do not always contain this Line." },
            { CollisionNodeStatus.ConsistentAndMatching, "The return value of this Line consistently collides (or not) in both the Passing and Failing logs." },
            { CollisionNodeStatus.ConsistentButDifferent,  "The return value of this Line consistently collides (or not) in the Passing logs but it is consistently different in the Failing logs."},
            { CollisionNodeStatus.ConsistentButDifferentMissingInPassing,  "The return value of this Line is consistent in the Failing logs but it is consistently missing in the Passing logs."},
            { CollisionNodeStatus.ConsistentButDifferentMissingInFailing,  "The return value of this Line is consistent in the Passing logs but it is consistently missing in the Failing logs."},
        };

        public class CollisionLogNode
        {
            public XElement Element { get; set; }
            public CollisionNodeStatus Status { get; set; }

            public CollisionLogNode(XElement element, CollisionNodeStatus status)
            {
                Element = element;
                Status = status;
            }
        }

        internal PredicateType Type { get; set; }

        internal Dictionary<string, Dictionary<string, ReturnVal>> BuildCsvFile(List<string> allEpochs, 
            Dictionary<string, List<CollisionLog>> logsData)
        {
            var allFiles = logsData.Values.SelectMany(v => v.Select(f => f.GetPredicateLine().Line.FileName));
            Console.WriteLine($"Building CSV files now. Total: {allFiles.Count()}");

            var fileToEpochValues = new Dictionary<string, Dictionary<string, ReturnVal>>();
            var x = 1;
            foreach (var file in allFiles)
            {
                Console.Write($"{x}|");
                x += 1;
                var fileLines = logsData.Values.SelectMany(list =>
                    list.Where(data => data.GetPredicateLine().Line.FileName.Equals(file)));
                var epochToValue = GetEpochToValueDictionary(file, fileLines, allEpochs);
                fileToEpochValues[file] = epochToValue;
            }

            return fileToEpochValues;
        }

        internal Dictionary<string, ReturnVal> GetEpochToValueDictionary(string file, IEnumerable<CollisionLog> passingData, 
            IEnumerable<string> epochData)
        {
            var epochToValue = new Dictionary<string, ReturnVal>();
            foreach (string s in epochData)
            {
                epochToValue[s] = new ReturnVal("undefined", ReturnVal.Type.Undefined);
            }

            foreach (var collisionData in passingData)
            {
                var predicateLine = collisionData.GetPredicateLine();
                var line = predicateLine.Line;
                if (line.FileName.Equals(file))
                {
                    epochToValue[collisionData.Epoch] = line.ReturnValue;
                }
            }

            return epochToValue;
        }
        public void FastSlowPredicate(List<Log> passingRandomLogs, List<Log> failingRandomLogs, PredicateType type, string outputDir, List<string> args)
        {
            Nodes = new List<CollisionLogNode>();

            // Get all latency for an epoch in passing/failing logs

            var passingLogsData = GetLinesForAllLineIds(passingRandomLogs, type, args, false);
            var failingLogsData = GetLinesForAllLineIds(failingRandomLogs, type, args, false);

            var passingKeys = passingLogsData.Keys.OrderBy(lineIndexEpoch => lineIndexEpoch.LineIndex);
            var failingKeys = failingLogsData.Keys.OrderBy(lineIndexEpoch => lineIndexEpoch.LineIndex);

            var passingTimeData = GetAllTimeData(passingKeys, passingLogsData);
            var failingTimeData = GetAllTimeData(failingKeys, failingLogsData);

            // for each epoch in passing that is not within bounds of failing, or vice-versa, or only exists in one or the other, output its results
            foreach (var key in passingTimeData.Keys)
            {
                if (!failingTimeData.ContainsKey(key))
                {
                    // Failing logs do not contain the line
                    Nodes.Add(GetCollisionNode(CollisionNodeStatus.InconsistentInPassing, key.Epoch,
                        passingLogsData[key], new HashSet<CollisionLog>()));
                }
                else
                {
                    var failingTimeDatum = failingTimeData[key];
                    var passingTimeDatum = passingTimeData[key];

                    if (AreTimeDataOutOfRange(passingTimeDatum, failingTimeDatum))
                    {
                        // Line is inconsistent in failing logs OR line is inconsistent in passing logs
                        Nodes.Add(GetCollisionNode(CollisionNodeStatus.ConsistentButDifferent, key.Epoch,
                            passingLogsData[key], failingLogsData[key]));
                    }
                    else
                    {
                        // Line is consistent in failing logs, line is consistent in passing logs
                        Nodes.Add(GetCollisionNode(CollisionNodeStatus.ConsistentAndMatching, key.Epoch,
                            passingLogsData[key], failingLogsData[key]));
                    }
                }
            }

            foreach (var key in failingTimeData.Keys)
            {
                // Lines that are not in passing but only in failing
                if (!passingTimeData.ContainsKey(key))
                {
                    // Passing logs do not contain the line
                    Nodes.Add(GetCollisionNode(CollisionNodeStatus.InconsistentInFailing, key.Epoch,
                        new HashSet<CollisionLog>(), failingLogsData[key]));
                }
            }
        }

        internal bool AreTimeDataOutOfRange(TimeData timeData1, TimeData timeData2)
        {
            return timeData1.Min >= timeData2.Max || timeData1.Max <= timeData2.Min;
        }

        private Dictionary<LineIndexEpoch, TimeData> GetAllTimeData(IOrderedEnumerable<LineIndexEpoch> passingKeys, Dictionary<LineIndexEpoch, HashSet<CollisionLog>> passingLogsData)
        {
            var latencyForEpochs = new Dictionary<LineIndexEpoch, TimeData>();
            foreach (var key in passingKeys)
            {
                var timeData = TimeForEpochs(passingLogsData[key]);

                latencyForEpochs[key] = timeData;
            }
            return latencyForEpochs;
        }

        private TimeData TimeForEpochs(HashSet<CollisionLog> hashSet)
        {
            var timeData = new TimeData();
            foreach (var collisionLog in hashSet)
            {
                foreach (var line in collisionLog.CurrentVals)
                {
                    timeData.AddTime(line.Latency);
                }
            }

            return timeData;
        }

        public class TimeData
        {
            internal int Min { get; set; }
            internal int Max { get; set; }

            internal List<int> Times { get; set; }

            internal int GetDuration()
            {
                return Max - Min;
            }

            public TimeData()
            {
                Times = new List<int>();
                Min = Int32.MaxValue;
                Max = Int32.MinValue;
            }

            public TimeData(int time)
            {
                Times = new List<int>();
                Min = time;
                Max = time;
                Times.Add(time);
            }

            public void AddTime(int time)
            {
                Min = Math.Min(Min, time);
                Max = Math.Max(Max, time);
                Times.Add(time);
            }
        }

        public Predicate(List<Log> passingRandomLogs, List<Log> failingRandomLogs, PredicateType type, string outputDir, List<string> args)
        {
            if (type == PredicateType.Relative)
            {
                RelativePredicate(passingRandomLogs, failingRandomLogs, type, outputDir, args);
            } else if (type == PredicateType.Slow || type == PredicateType.Fast)
            {
                FastSlowPredicate(passingRandomLogs, failingRandomLogs, type, outputDir, args);
            }
        }

        internal void RelativePredicate(List<Log> passingRandomLogs, List<Log> failingRandomLogs, PredicateType type, string outputDir, List<string> args)
        {
            Nodes = new List<CollisionLogNode>();

            var passingLogsData = GetLinesForAllLineIds(passingRandomLogs, type, args);
            var failingLogsData = GetLinesForAllLineIds(failingRandomLogs, type, args);

            var passingKeys = passingLogsData.Keys.OrderBy(lineIndexEpoch => lineIndexEpoch.LineIndex);

            foreach (var key in passingKeys)
            {
                var valueForAllPassingLogs = PatternSignatureForAllLogs(passingLogsData[key]);

                if (!failingLogsData.ContainsKey(key) && valueForAllPassingLogs == null)
                {
                    // Failing logs do not contain the line, line is inconsistent in passing logs
                    Nodes.Add(GetCollisionNode(CollisionNodeStatus.InconsistentInPassing, key.Epoch,
                        passingLogsData[key], new HashSet<CollisionLog>()));
                }
                else if (!failingLogsData.ContainsKey(key) && valueForAllPassingLogs != null)
                {
                    // Failing logs do not contain the line, line is consistent in passing logs
                    Nodes.Add(GetCollisionNode(CollisionNodeStatus.ConsistentButDifferentMissingInFailing, key.Epoch,
                        passingLogsData[key], new HashSet<CollisionLog>()));
                }
                else
                {
                    var valueForAllFailingLogs = PatternSignatureForAllLogs(failingLogsData[key]);
                    if (((valueForAllFailingLogs != null && valueForAllPassingLogs != null) && valueForAllPassingLogs.SequenceEqual(valueForAllFailingLogs)) || (valueForAllPassingLogs == null && valueForAllFailingLogs == null))
                    {
                        // Line is consistent in failing logs, line is consistent in passing logs
                        Nodes.Add(GetCollisionNode(CollisionNodeStatus.ConsistentAndMatching, key.Epoch,
                            passingLogsData[key], failingLogsData[key]));
                    }
                    else
                    {
                        // Line is inconsistent in failing logs OR line is inconsistent in passing logs
                        Nodes.Add(GetCollisionNode(CollisionNodeStatus.ConsistentButDifferent, key.Epoch,
                            passingLogsData[key], failingLogsData[key]));
                    }
                    failingLogsData.Remove(key);
                }
            }

            var failingKeys = failingLogsData.Keys.OrderBy(lineIndexEpoch => lineIndexEpoch.LineIndex);
            foreach (var key in failingKeys)
            {
                // Lines that are not in passing but only in failing
                var valueForAllFailingLogs = PatternSignatureForAllLogs(failingLogsData[key]);
                if (valueForAllFailingLogs != null)
                {
                    Nodes.Add(GetCollisionNode(CollisionNodeStatus.ConsistentButDifferentMissingInPassing, key.Epoch,
                        new HashSet<CollisionLog>(), failingLogsData[key]));
                }
                else
                {
                    Nodes.Add(GetCollisionNode(CollisionNodeStatus.InconsistentInFailing, key.Epoch,
                        new HashSet<CollisionLog>(), failingLogsData[key]));
                }
            }
        }

        //public Predicate(List<Log> passingRandomLogs, List<Log> failingRandomLogs, PredicateType type, string outputDir, List<string> args)
        //{
        //    Nodes = new List<CollisionLogNode>();

        //    var passingLogsData = GetDataForAllLogs(passingRandomLogs, type, outputDir, args);
        //    var failingLogsData = GetDataForAllLogs(failingRandomLogs, type, outputDir, args);

        //    //var allEpochs = failingLogsData.Keys.Union(passingLogsData.Keys);
        //    //var enumerable = allEpochs.ToList();
        //    //enumerable.Sort();
        //    //var failingFileToEpochValues = BuildCsvFile(enumerable, failingLogsData);
        //    //var passingFileToEpochValues = BuildCsvFile(enumerable, passingLogsData);
        //    //OutputCsv(failingFileToEpochValues, passingFileToEpochValues, outputDir, enumerable);


        //    foreach (var entry in passingLogsData)
        //    {
        //        var valueForAllPassingLogs = ValueForAllLogs(entry.Value);

        //        if (!failingLogsData.ContainsKey(entry.Key) && !valueForAllPassingLogs)
        //        {
        //            // Failing logs do not contain the line, line is inconsistent in passing logs
        //            Nodes.Add(GetCollisionNode(CollisionNodeStatus.InconsistentInPassing, entry.Key,
        //                GetAllPredicateLine(entry.Value), new List<PredicateLine>()));
        //        } 
        //        else if (!failingLogsData.ContainsKey(entry.Key) && valueForAllPassingLogs)
        //        {
        //            // Failing logs do not contain the line, line is consistent in passing logs
        //            Nodes.Add(GetCollisionNode(CollisionNodeStatus.ConsistentButDifferent, entry.Key,
        //                GetAllPredicateLine(entry.Value), new List<PredicateLine>()));
        //        }
        //        else
        //        {
        //            var valueForAllFailingLogs = ValueForAllLogs(failingLogsData[entry.Key]);
        //            if ((valueForAllFailingLogs && valueForAllPassingLogs) || (!valueForAllFailingLogs && !valueForAllPassingLogs))
        //            {
        //                // Line is consistent in failing logs, line is consistent in passing logs
        //                Nodes.Add(GetCollisionNode(CollisionNodeStatus.ConsistentAndMatching, entry.Key,
        //                    GetAllPredicateLine(entry.Value), GetAllPredicateLine(failingLogsData[entry.Key])));
        //            }
        //            else
        //            {
        //                // Line is inconsistent in failing logs OR line is inconsistent in passing logs
        //                Nodes.Add(GetCollisionNode(CollisionNodeStatus.ConsistentButDifferent, entry.Key,
        //                    GetAllPredicateLine(entry.Value), GetAllPredicateLine(failingLogsData[entry.Key])));
        //            }
        //            failingLogsData.Remove(entry.Key);
        //        }
        //    }

        //    foreach (var entry in failingLogsData)
        //    {
        //        // Lines that are not in passing but only in failing
        //        var valueForAllFailingLogs = ValueForAllLogs(entry.Value);
        //        if (valueForAllFailingLogs)
        //        {
        //            Nodes.Add(GetCollisionNode(CollisionNodeStatus.ConsistentButDifferent, entry.Key,
        //                new List<PredicateLine>(), GetAllPredicateLine(entry.Value)));
        //        }
        //        else
        //        {
        //            Nodes.Add(GetCollisionNode(CollisionNodeStatus.InconsistentInFailing, entry.Key,
        //                new List<PredicateLine>(), GetAllPredicateLine(entry.Value)));
        //        }
        //    }
        //}

        private void OutputCsv(Dictionary<string, Dictionary<string, ReturnVal>> failingFileToEpochValues,
            Dictionary<string, Dictionary<string, ReturnVal>> passingFileToEpochValues, string outputDir, List<string> epochList)
        {
            var sb = new StringBuilder();
            var epochToTypes = new Dictionary<string, ReturnVal.Type>();

            foreach (var epoch in epochList)
            {
                sb.Append(epoch);
                sb.Append(",");
            }
            sb.AppendLine("result");

            GetAllTypes(failingFileToEpochValues, epochToTypes);
            GetAllTypes(passingFileToEpochValues, epochToTypes);

            var typeSb = new StringBuilder();
            foreach (var epoch in epochList)
            {
                /**
                 * Possible outputs:
                 * String
                 * Int
                 * Bool 
                 * Inconsistent - this epoch's type is inconsistent, that is some logs say it is a type while other logs say it is another type
                 * Undefined - this epoch is always undefined
                 * NoTypeInfoFound - likely a bug in the code such that this epoch appears in epochList but not in dictionaries
                 */
                var gotValue = epochToTypes.TryGetValue(epoch, out var type);
                if (gotValue)
                {
                    if (Program.CsvFormat == Program.CSVFormat.BOOL_INT_ONLY)
                    {
                        if (type == ReturnVal.Type.BoolType || type == ReturnVal.Type.IntType)
                        {
                            typeSb.AppendLine($"var_{epoch} : {type}");
                        }
                    }
                    else
                    {
                        typeSb.AppendLine($"var_{epoch} : {type}");
                    }
                }
                else
                {
                    typeSb.AppendLine($"var_{epoch} : NoTypeInfoFound");
                }
            }

            OutputCsvHelper(failingFileToEpochValues, sb, "failing", epochToTypes);
            OutputCsvHelper(passingFileToEpochValues, sb, "passing", epochToTypes);

            File.WriteAllText(Path.Combine(outputDir, "PredicatesPerFile.csv"), sb.ToString());
            File.WriteAllText(Path.Combine(outputDir, "pre.names"), typeSb.ToString());
        }

        private static void GetAllTypes(Dictionary<string, Dictionary<string, ReturnVal>> fileToEpochValues, 
            Dictionary<string, ReturnVal.Type> epochToTypes)
        {
            foreach (var entry in fileToEpochValues)
            {
                var epochList = new List<string>(entry.Value.Keys);
                epochList.Sort();
                foreach (var epoch in epochList)
                {
                    var gotValue = epochToTypes.TryGetValue(epoch, out var currentType);
                    if (gotValue && currentType != entry.Value[epoch].ValueType)
                    {
                        epochToTypes[epoch] = ReturnVal.Type.Inconsistent;
                    }
                    else
                    {
                        epochToTypes[epoch] = entry.Value[epoch].ValueType;
                    }
                }
            }
        }

        private static void OutputCsvHelper(Dictionary<string, Dictionary<string, ReturnVal>> fileToEpochValues, 
            StringBuilder sb, string result, Dictionary<string, ReturnVal.Type> epochToTypes)
        {
            foreach (var entry in fileToEpochValues)
            {
                var epochList = new List<string>(entry.Value.Keys);
                epochList.Sort();
                foreach (var epoch in epochList)
                {
                    if (Program.CsvFormat == Program.CSVFormat.BOOL_INT_ONLY)
                    {
                        var gotValue = epochToTypes.TryGetValue(epoch, out var currentType);
                        if (!gotValue || (currentType != ReturnVal.Type.IntType &&
                                          currentType != ReturnVal.Type.BoolType)) continue;
                        sb.Append(entry.Value[epoch]);
                        sb.Append(",");
                    }
                    else
                    {
                        sb.Append(entry.Value[epoch]);
                        sb.Append(",");
                    }
                }
                sb.AppendLine(result);
            }
        }

        //internal bool SameValueForAllLines(HashSet<CollisionData> data)
        //{
        //    bool result = true;

        //    // Compare within logs whether the return values of this epoch is the same
        //    foreach (var datum in data)
        //    {
        //        var lines = datum.CurrentVals;
        //        var returnVal = lines.First().ReturnValue.Value;
        //        foreach (var line in lines)
        //        {
        //            var currentVal = line.ReturnValue.Value;
        //            result = result && returnVal.Equals(currentVal);
        //        }
        //    }
        //    //var lines = data.SelectMany(datum => datum.CurrentVals).Distinct();

        //    return result;
        //}

        // Returns null if all logs do not have the same pattern signature e.g., [true, false, true]
        internal List<bool> PatternSignatureForAllLogs(HashSet<CollisionLog> data)
        {
            bool result = true;

            if (data.Count == 0)
            {
                return null;
            }

            var firstDatum = data.First();
            var returnValList = CompareReturnValueOfLines(firstDatum.CurrentVals);            

            // Compare within logs whether the return values of this epoch is the same
            foreach (var datum in data)
            {
                var lines = datum.CurrentVals;
                var currentVal = CompareReturnValueOfLines(lines);
                result = result && returnValList.SequenceEqual(currentVal);
            }

            if (result)
            {
                return returnValList;
            } else
            {
                return null;
            }
        }

        internal List<bool> CompareReturnValueOfLines(List<LineEntry> lines)
        {
            var retList = new List<bool>();
            var previousVal = lines.First().ReturnValue.Value;
            foreach (var line in lines)
            {
                var currentVal = line.ReturnValue.Value;
                retList.Add(previousVal.Equals(currentVal));
                previousVal = currentVal;
            }
            return retList;
        }

        internal bool ValueForAllLogs(List<CollisionLog> logs)
        {
            bool result = true;
            foreach (var log in logs)
            {
                result = result && log.ValueOfAllPredicate();
            }

            return result;
        }

        internal List<PredicateLine> GetAllPredicateLine(IEnumerable<CollisionLog> logs)
        {
            var lines = new List<PredicateLine>();
            foreach (var log in logs)
            {
                lines.Add(log.GetPredicateLine());
            }

            return lines;
        }

        internal CollisionLogNode GetCollisionNode(CollisionNodeStatus status, string epoch, HashSet<CollisionLog> passingData, HashSet<CollisionLog> failingData)
        {
            return GetCollisionNode(status, epoch, passingData, failingData, null);
        }
        internal CollisionLogNode GetCollisionNode(CollisionNodeStatus status, string epoch, HashSet<CollisionLog> passingData, HashSet<CollisionLog> failingData, TimeData timeData)
        {
            var logNode = new XElement("CollisionNode");
            logNode.SetAttributeValue("type", status.ToString());

            var messageNode = new XElement("Message");
            messageNode.SetValue(StatusToMessage[status]);
            logNode.Add(messageNode);

            logNode.SetAttributeValue("epoch", epoch);

            var passingNodes = new XElement("PassingLogs");
            foreach (var file in passingData)
            {
                passingNodes.Add(file.ToXml());
            }
            passingNodes.SetAttributeValue("num", passingData.Count());
            logNode.Add(passingNodes);

            var failingNodes = new XElement("FailingLogs");
            foreach (var file in failingData)
            {
                failingNodes.Add(file.ToXml());
            }
            failingNodes.SetAttributeValue("num", failingData.Count());

            if (timeData != null)
            {
                logNode.SetAttributeValue("Min", timeData.Min);
                logNode.SetAttributeValue("Max", timeData.Max);
                logNode.SetAttributeValue("Duration", timeData.GetDuration());
            }

            logNode.Add(failingNodes);

            return new CollisionLogNode(logNode, status);
        }

        //internal static List<LineEntry> GetRandomLines(CollisionData passingLogUniqueRandomVals)
        //{
        //    var randomLines = new List<LineEntry>();
        //    foreach (var data in passingLogUniqueRandomVals.CurrentVals)
        //    {
        //        randomLines.Add(data);
        //    }
        //    return randomLines;
        //}

        public XElement ToXml()
        {
            var logNode = new XElement("Predicate");

            var consistentButDifferentNodes = new List<XElement>();
            var consistentButDifferentMissingInFailingNodes = new List<XElement>();
            var consistentButDifferentMissingInPassingNodes = new List<XElement>();
            var inconsistentInFailingNodes = new List<XElement>();
            var inconsistentInPassingNodes = new List<XElement>();
            var consistentAndMatchingNodes = new List<XElement>();

            foreach (var node in Nodes)
            {
                if (node.Status == CollisionNodeStatus.ConsistentButDifferent)
                {
                    consistentButDifferentNodes.Add(node.Element);
                } else if (node.Status == CollisionNodeStatus.ConsistentButDifferentMissingInFailing)
                {
                    consistentButDifferentMissingInFailingNodes.Add(node.Element);
                }
                else if (node.Status == CollisionNodeStatus.ConsistentButDifferentMissingInPassing)
                {
                    consistentButDifferentMissingInPassingNodes.Add(node.Element);
                }
                else if (node.Status == CollisionNodeStatus.InconsistentInFailing)
                {
                    inconsistentInFailingNodes.Add(node.Element);
                }
                else if (node.Status == CollisionNodeStatus.InconsistentInPassing)
                {
                    inconsistentInPassingNodes.Add(node.Element);
                }
                else if (node.Status == CollisionNodeStatus.ConsistentAndMatching)
                {
                    consistentAndMatchingNodes.Add(node.Element);
                }
            }
            if (consistentButDifferentNodes.Count != 0)
            {
                AddRankToCollisionNodes(consistentButDifferentNodes);
                logNode.Add(consistentButDifferentNodes);
            }
            if (consistentButDifferentMissingInFailingNodes.Count != 0)
            {
                AddRankToCollisionNodes(consistentButDifferentMissingInFailingNodes);
                logNode.Add(consistentButDifferentMissingInFailingNodes);
            }
            if (consistentButDifferentMissingInPassingNodes.Count != 0)
            {
                AddRankToCollisionNodes(consistentButDifferentMissingInPassingNodes);
                logNode.Add(consistentButDifferentMissingInPassingNodes);
            }
            if (inconsistentInPassingNodes.Count != 0)
            {
                AddRankToCollisionNodes(inconsistentInPassingNodes);
                logNode.Add(inconsistentInPassingNodes);
            }
            if (inconsistentInFailingNodes.Count != 0)
            {
                AddRankToCollisionNodes(inconsistentInFailingNodes);
                logNode.Add(inconsistentInFailingNodes);
            }
            if (consistentAndMatchingNodes.Count != 0)
            {
                AddRankToCollisionNodes(consistentAndMatchingNodes);
                logNode.Add(consistentAndMatchingNodes);
            }
            logNode.SetAttributeValue("type", Type);
            logNode.SetAttributeValue("totalPredicates", Nodes.Count);
            logNode.SetAttributeValue("numInconsistentInPassing", inconsistentInPassingNodes.Count);
            logNode.SetAttributeValue("numInconsistentInFailing", inconsistentInFailingNodes.Count);
            logNode.SetAttributeValue("numConsistentAndMatching", consistentAndMatchingNodes.Count);
            logNode.SetAttributeValue("numConsistentButDifferent", consistentButDifferentNodes.Count);
            logNode.SetAttributeValue("numConsistentButDifferentMissingInFailing", consistentButDifferentMissingInFailingNodes.Count);
            logNode.SetAttributeValue("numConsistentButDifferentMissingInPassing", consistentButDifferentMissingInPassingNodes.Count);

            return logNode;
        }

        internal void AddRankToCollisionNodes(List<XElement> nodes)
        {
            var counter = 1;
            foreach(var node in nodes)
            {
                node.SetAttributeValue("Rank", counter);
                counter += 1;
            }
        }

        //internal void LabelPredicates(Dictionary<string, bool> isAllPassingLogSame, Dictionary<string, CollisionData> passingData,
        //    Dictionary<string, CollisionData> failingData)
        //{
        //    foreach (var entry in isAllPassingLogSame)
        //    {
        //        var passingLogUniqueRandomVals = passingData[entry.Key];

        //        if (!entry.Value)
        //        {
        //            Nodes.Add(GetCollisionNode(CollisionNodeStatus.InconsistentInPassing, passingLogUniqueRandomVals.LineEntry,
        //                new List<LineEntry> { passingLogUniqueRandomVals.LineEntry }, new List<LineEntry>()));
        //            continue;
        //        }
        //        if (!failingData.ContainsKey(entry.Key))
        //        {
        //            Nodes.Add(GetCollisionNode(CollisionNodeStatus.InconsistentInFailing, passingLogUniqueRandomVals.LineEntry,
        //                GetRandomLines(passingLogUniqueRandomVals), new List<LineEntry>()));
        //        }
        //        else
        //        {
        //            var failingLogUniqueRandomVals = failingData[entry.Key];

        //            if (passingLogUniqueRandomVals.Equals(failingLogUniqueRandomVals))
        //            {
        //                Nodes.Add(GetCollisionNode(CollisionNodeStatus.ConsistentAndMatching, passingLogUniqueRandomVals.LineEntry,
        //                    new List<LineEntry> { passingLogUniqueRandomVals.LineEntry }, new List<LineEntry> { failingLogUniqueRandomVals.LineEntry }));
        //            }
        //            else
        //            {
        //                Nodes.Add(GetCollisionNode(CollisionNodeStatus.ConsistentButDifferent, passingLogUniqueRandomVals.LineEntry,
        //                    GetRandomLines(passingLogUniqueRandomVals), GetRandomLines(failingLogUniqueRandomVals)));
        //            }
        //        }
        //    }
        //}

        //internal Dictionary<string, bool> IsAllLogSame(List<Log> logs)
        //{
        //    var indexRetValSame = new Dictionary<string, bool>();
        //    var onePassingLines = logs.First().GetAllLines();
        //    var passingRandomData = GetData(onePassingLines);
        //    for (var i = 1; i < logs.Count; i++)
        //    {
        //        var currentRandomData = GetData(logs[i].GetAllLines());

        //        if (passingRandomData.Count != currentRandomData.Count)
        //        {
        //            throw new Exception("Two logs have different number of randoms: "
        //                + logs.First().FileName + " and " + logs[i].FileName);
        //        }

        //        foreach (var entry in passingRandomData)
        //        {
        //            if (!currentRandomData.ContainsKey(entry.Key))
        //            {
        //                throw new Exception("Index of randoms differ even though size is same: "
        //                    + logs.First().FileName + " and " + logs[i].FileName);
        //            }

        //            if (indexRetValSame.ContainsKey(entry.Key))
        //            {
        //                var containsVal = indexRetValSame.TryGetValue(entry.Key, out var currentRetValSame);

        //                indexRetValSame[entry.Key] = currentRandomData[entry.Key].Equals(entry.Value) && currentRetValSame;
        //            }
        //            else
        //            {
        //                indexRetValSame[entry.Key] = currentRandomData[entry.Key].Equals(entry.Value);
        //            }
        //        }

        //        //var dataEquals = passingRandomData.Count == currentRandomData.Count 
        //        //    && !passingRandomData.Except(currentRandomData).Any();
        //        //if (!dataEquals)
        //        //{
        //        //    return false;
        //        //}
        //    }
        //    return indexRetValSame;
        //}

        internal Dictionary<string, CollisionLog> GetData(List<LineEntry> entries, PredicateType type, List<string> args)
        {
            var lines = new List<LineEntry>();
            var epochToData = new Dictionary<string, CollisionLog>();
            foreach (var line in entries)
            {
                var epoch = line.GetLineId();

                var gotValue = epochToData.TryGetValue(epoch, out var currentVal);
                if (!gotValue)
                {
                    currentVal = CreateData(type, epoch, args);
                }

                if (type == PredicateType.Relative)
                {
                    lines.Add(line);
                    currentVal.CurrentVals = new List<LineEntry>(lines);
                }
                else
                {
                    currentVal.Add(line);
                }

                epochToData[epoch] = currentVal;
            }
            return epochToData;
        }

        internal CollisionLog CreateData(PredicateType type, string epoch, List<string> args)
        {
            CollisionLog currentVal;
            switch (type)
            {
                case PredicateType.Relative:
                    currentVal = new RelativeData(type, epoch);
                    break;
                case PredicateType.Absolute:
                    currentVal = new AbsoluteData(type, epoch, args.First());
                    break;
                case PredicateType.Exception:
                    currentVal = new ExceptionData(type, epoch, args.First());
                    break;
                case PredicateType.Fast:
                    currentVal = new FastData(type, epoch, int.Parse(args.First()));
                    break;
                case PredicateType.Slow:
                    currentVal = new SlowData(type, epoch);
                    break;
                default:
                    throw new SystemException("Unknown Predicate type : " + type);
            }
            return currentVal;
        }

        internal Dictionary<string, List<CollisionLog>> GetDataForAllLogs(List<Log> logs, PredicateType type, string outputDir, List<string> args)
        {
            // Used to keep track of what to output to predicateFiles/ directory
            var epochToData = new Dictionary<string, List<CollisionLog>>();

            foreach (var log in logs)
            {
                var oneDatum = GetData(log.GetAllLines(), type, args);
                var predicateLines = new List<PredicateLine>();


                foreach (var entry in oneDatum)
                {
                    var gotValue = epochToData.TryGetValue(entry.Key, out var currentVal);
                    if (!gotValue)
                    {
                        currentVal = new List<CollisionLog>();
                    }
                    currentVal.Add(entry.Value);
                    epochToData[entry.Key] = currentVal;

                    predicateLines.Add(entry.Value.GetPredicateLine());
                }

                predicateLines = predicateLines.OrderBy(obj => obj.Line.LineIndex).ToList();
                PredicateFileToXml(predicateLines, log.FileName, outputDir);
            }

            return epochToData;
        }

        internal class LineIndexEpoch
        {
            internal string Epoch { get; set; }
            internal int LineIndex { get; set; }

            internal LineIndexEpoch (string epoch, int lineIndex)
            {
                Epoch = epoch;
                LineIndex = lineIndex;
            }

            // LineIndex shouldn't be used for equal and hashcode since we aim to treat all lines 
            // with same signature as the same even if they have different LineIndex
            public override bool Equals(object obj)
            {
                var item = obj as LineIndexEpoch;

                if (item == null)
                {
                    return false;
                }

                return this.Epoch.Equals(item.Epoch);
            }

            public override int GetHashCode()
            {
                return Epoch.GetHashCode();
            }
        }

        internal Dictionary<LineIndexEpoch, HashSet<CollisionLog>> GetLinesForAllLineIds(List<Log> logs, PredicateType type, List<string> args)
        {
            return GetLinesForAllLineIds(logs, type, args, true);
        }

        internal Dictionary<LineIndexEpoch, HashSet<CollisionLog>> GetLinesForAllLineIds(List<Log> logs, PredicateType type, List<string> args, bool useLineId)
        {
            // Used to keep track of what lines are relevant to this epoch
            var epochToRelevantData = new Dictionary<LineIndexEpoch, HashSet<CollisionLog>>();
            foreach (var log in logs)
            {
                var lines = log.GetAllLines();

                foreach (var line in lines)
                {
                    string lineId;
                    if (useLineId)
                    {
                        lineId = line.GetLineId();
                    } else
                    {
                        lineId = line.GetEpoch();
                    }
                    var lineIndex = line.LineIndex;
                    var lineIndexEpoch = new LineIndexEpoch(lineId, lineIndex);

                    // Check whether this lineId was already found in one or more files
                    var gotValue = epochToRelevantData.TryGetValue(lineIndexEpoch, out var collisionData);
                    if (!gotValue)
                    {
                        collisionData = new HashSet<CollisionLog>();
                    }

                    // Get the specific file that contains this line or create a new data representing the file if none exists
                    var collisionDatum = collisionData.Where(datum => datum.File.Equals(log.FileName)).FirstOrDefault();
                    if (collisionDatum == null)
                    {
                        collisionDatum = CreateData(type, lineId, args);
                        collisionDatum.File = log.FileName;
                    }

                    // Add the line to the file
                    collisionDatum.Add(line);
                    // Add the file to the list of files that contain this epoch
                    collisionData.Add(collisionDatum);
                    // link this epoch to this list of files
                    epochToRelevantData[lineIndexEpoch] = collisionData;
                }
            }
            return epochToRelevantData;
        }

        internal void PredicateFileToXml(List<PredicateLine> lines, string fileName, string outputDir)
        {
            var node = new XElement("PredicateFile");

            foreach (var line in lines)
            {
                node.Add(line.ToXml());
            }

            node.SetAttributeValue("numLines", lines.Count);
            string directoryPath = Path.Combine(outputDir, "predicateFiles");
            Directory.CreateDirectory(directoryPath);
            string filePath = Path.Combine(directoryPath, Path.GetFileNameWithoutExtension(fileName) + ".xml");
            node.Save(filePath);
        }
    }
}
