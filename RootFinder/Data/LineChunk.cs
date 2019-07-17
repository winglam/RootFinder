using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RootFinder.Data
{
    [Serializable]
    public class LineChunk
    {
        internal FileProp FileProp { get; set; }
        internal LineEntry StartLine { get; set; }
        internal List<LineEntry> Entries { get; set; }
        internal HashSet<LineEntry> UniqueEntries { get; set; }
        internal List<CompareLineChunkResult> ChunkToResult { get; set; }

        public XElement ToXml()
        {
            XElement ChunkNode = new XElement("Chunk");
            ChunkNode.Add(StartLine.ToXml());

            foreach (var line in ChunkToResult)
            {
                ChunkNode.Add(line.ToXml());
            }

            ChunkNode.SetAttributeValue("chunksCompared", ChunkToResult.Count);

            bool allSameSize = true;
            bool allSameMethods = true;
            bool allSameSequence = true;

            foreach (var compareResult in ChunkToResult)
            {
                if (allSameSize)
                    allSameSize = compareResult.SameSize;
                if (allSameSequence)
                    allSameSequence = compareResult.SameSequence;
                if (allSameMethods)
                    allSameMethods = compareResult.SameMethods;
            }

            ChunkNode.SetAttributeValue("allSameSize", allSameSize);
            ChunkNode.SetAttributeValue("allSameMethods", allSameMethods);
            ChunkNode.SetAttributeValue("allSameSequence", allSameSequence);

            return ChunkNode;
        }

        public LineChunk(LineEntry startLine, string fileName, bool isPassing)
        {
            StartLine = startLine;
            UniqueEntries = new HashSet<LineEntry>();
            FileProp = new FileProp(fileName, isPassing);
            Entries = new List<LineEntry>();

            ChunkToResult = new List<CompareLineChunkResult>();
        }

        public void AddCompareResult(CompareLineChunkResult value)
        {
            ChunkToResult.Add(value);
        }

        public bool CompareCallerCalleeStartLine(LineChunk chunk)
        {
            return StartLine.CompareCallerCallee(chunk.StartLine);
        }

        public void AddEntry(LineEntry entry)
        {
            Entries.Add(entry);
            UniqueEntries.Add(entry);
        }

        public LineChunk ContainsKeyword(string keyword, bool isPassing)
        {
            var lines = new List<LineEntry>();
            var startLineContainsKeywordLine = StartLine.ContainsKeyword(keyword);
            if (startLineContainsKeywordLine != null)
            {
                lines.Add(startLineContainsKeywordLine);
            }

            foreach (var line in Entries)
            {
                var containsKeywordLine = line.ContainsKeyword(keyword);
                if (containsKeywordLine != null)
                {
                    lines.Add(containsKeywordLine);
                }
            }

            if (lines.Count() > 0)
            {
                var firstLine = lines.First();
                var retChunk = new LineChunk(firstLine, firstLine.FileName, isPassing);
                retChunk.Entries = lines.GetRange(1, lines.Count() - 1);
                return retChunk;
            }

            return null;
        }
    }
}
