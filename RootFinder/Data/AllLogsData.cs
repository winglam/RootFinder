using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RootFinder.Data
{
    [Serializable]
    internal class AllLogsData
    {
        internal List<Log> PassingLogs { get; set; }
        internal List<Log> FailingLogs { get; set; }
        // Contains the first failing chunks that differ from the PassingLogs. This field should only be set if AreAllLogsStartLinesSame is true for PassingLogs
        internal List<CompareLineChunkResult> FirstFailingChunks { get; set; }

        internal AllLogsData(List<Log> passingLogs, List<Log> failingLogs)
        {
            PassingLogs = new List<Log>(passingLogs);
            FailingLogs = new List<Log>(failingLogs);
        }

        internal bool ArePassingLogsSameSize()
        {
            return AreLogsSameSize(PassingLogs);
        }

        internal bool AreFailingLogsSameSize()
        {
            return AreLogsSameSize(FailingLogs);
        }

        internal List<Log> AllLogsContainKeyword(List<Log> logs, string keyword, bool isPassing)
        {
            var retLogs = new List<Log>();
            foreach (var log in logs)
            {
                var containsKeywordLog = log.ContainsKeyword(keyword, isPassing);
                if (containsKeywordLog != null)
                {
                    retLogs.Add(containsKeywordLog);
                }
            }
            return retLogs;
        }

        internal void SetFirstFailngChunks()
        {
            if (FirstFailingChunks != null)
            {
                return;
            }

            FirstFailingChunks = new List<CompareLineChunkResult>();
            var passingLog = PassingLogs.First();
            foreach (var failingLog in FailingLogs)
            {
                var compareResult = GetFirstDifferingLineChunk(passingLog, failingLog);
                if (compareResult != null)
                {
                    FirstFailingChunks.Add(compareResult);
                }
            }
        }

        internal CompareLineChunkResult GetFirstDifferingLineChunk(Log passingLog, Log failingLog)
        {
            for (var i = 0; i < passingLog.LineChunks.Count; i++)
            {
                var p1 = passingLog.LineChunks[i];

                if (i >= failingLog.LineChunks.Count)
                {
                    return new CompareLineChunkResult(p1, null);
                }

                var p2 = failingLog.LineChunks[i];
                if (!p1.CompareCallerCalleeStartLine(p2))
                {
                    return new CompareLineChunkResult(p1, p2);
                }
            }

            return null;
        }

        internal bool AreAllLogsStartLinesSame(List<Log> logs)
        {
            for (int i = logs.Count - 1; i >= 1; i--)
            {
                if (!AreLogsStartLinesSame(logs[i], logs[i - 1]))
                {
                    return false;
                }
            }
            return true;
        }

        internal bool AreLogsStartLinesSame(Log passingLog, Log failingLog)
        {
            for (int i = 0; i < passingLog.LineChunks.Count; i++)
            {
                var p1 = passingLog.LineChunks[i];
                var p2 = failingLog.LineChunks[i];
                if (!p1.CompareCallerCalleeStartLine(p2))
                {
                    return false;
                }
            }
            return true;
        }

        private bool AreLogsSameSize(List<Log> log)
        {
            for (int i = log.Count - 1; i >= 1; i--)
            {
                if (log[i].LineChunks.Count != log[i - 1].LineChunks.Count)
                {
                    return false;
                }
            }
            return true;
        }

        public XElement ToXml()
        {
            XElement logNode = new XElement("AllLogsData");

            if (FirstFailingChunks != null)
            {
                XElement compareResultsNode = new XElement("CompareResults");
                foreach (var compareResult in FirstFailingChunks)
                {
                    compareResultsNode.Add(compareResult.ToXml());
                }
                compareResultsNode.SetAttributeValue("count", FirstFailingChunks.Count);
                logNode.Add(compareResultsNode);
            }

            logNode.SetAttributeValue("passingLogsCount", PassingLogs.Count);
            logNode.SetAttributeValue("failingLogsCount", FailingLogs.Count);
            logNode.SetAttributeValue("arePassingLogsSameSize", ArePassingLogsSameSize());
            logNode.SetAttributeValue("areFailingLogsSameSize", AreFailingLogsSameSize());
            logNode.SetAttributeValue("areAllPassingLogsStartLinesSame", AreAllLogsStartLinesSame(PassingLogs));

            return logNode;
        }
    }
}
