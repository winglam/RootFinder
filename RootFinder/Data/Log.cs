using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using RootFinder.Data;

namespace RootFinder
{
    [Serializable]
    public class Log
    {
        internal List<LineChunk> LineChunks { get; set; }
        // Contains comparison result of this chunk with it's counterpart chunk in other logs
        internal string FileName { get; set; }
        // Contains Logs that have different number of Chunks as this current Log
        internal HashSet<Log> DiffLogs { get; set; }
        internal HashSet<Log> LogsCompared { get; set; }

        public Log(List<LineChunk> lineChunks, string fileName)
        {
            FileName = fileName;
            LineChunks = lineChunks;
            DiffLogs = new HashSet<Log>();
            LogsCompared = new HashSet<Log>();
        }

        public void AddDiffLog(Log diff)
        {
            DiffLogs.Add(diff);
        }

        public void AddLogCompared(Log log)
        {
            LogsCompared.Add(log);
        }

        public Log ContainsKeyword(string keyword, bool isPassing)
        {
            var chunks = new List<LineChunk>();
            foreach (var chunk in LineChunks)
            {
                var containsKeywordChunk = chunk.ContainsKeyword(keyword, isPassing);
                if (containsKeywordChunk != null)
                {
                    chunks.Add(containsKeywordChunk);
                }
            }
            if (chunks.Count != 0)
            {
                return new Log(chunks, chunks.First().FileProp.FileName);
            }
            return null;
        }

        public List<LineEntry> GetAllLines()
        {
            var lines = new List<LineEntry>();
            foreach (var chunk in LineChunks)
            {
                lines.Add(chunk.StartLine);
                foreach(var entry in chunk.Entries)
                {
                    lines.Add(entry);
                }
            }
            return lines;
        }

        public List<string> GetAllCallee()
        {
            var callees = new List<string>();
            foreach (var line in GetAllLines())
            {
                callees.Add(line.Callee);
            }
            return callees;
        }

        public List<KeyValuePair<string, int>> GetAllCalleeTime()
        {
            var callees = new List<KeyValuePair<string, int>>();
            foreach (var line in GetAllLines())
            {
                callees.Add(new KeyValuePair<string, int>(line.Callee, line.Latency));
            }
            return callees;
        }

        public List<KeyValuePair<string, ReturnVal>> GetAllCalleeRetVal()
        {
            var callees = new List<KeyValuePair<string, ReturnVal>>();
            foreach (var line in GetAllLines())
            {
                callees.Add(new KeyValuePair<string, ReturnVal>(line.Callee, line.ReturnValue));
            }
            return callees;
        }

        public XElement ToXml()
        {
            XElement logNode = new XElement("Log");

            foreach (var lineChunk in this.LineChunks)
            {
                logNode.Add(lineChunk.ToXml());
            }

            logNode.SetAttributeValue("logsCompared", LogsCompared.Count);
            logNode.SetAttributeValue("diffLogs", DiffLogs.Count);

            logNode.SetAttributeValue("fileName", FileName);

            return logNode;
        }
    }
}
