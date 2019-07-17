using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static RootFinder.Program;

namespace RootFinder.Data
{
    [Serializable]
    public class LineEntry
    {
        internal int LineIndex { get; set; }
        // Same as StartTime in T18
        internal int Ticks { get; set; }
        internal int Latency { get; set; }
        internal int ParentThreadId { get; set; }
        internal int ManagedThreadIdOriginal { get; set; }
        internal int ManagedThreadId { get; set; }
        internal int RequestId { get; set; }
        internal int Objid { get; set; }
        internal string Type { get; set; }
        internal string Caller { get; set; }
        internal string Callee { get; set; }
        internal int IlOffset { get; set; }
        internal string Exception { get; set; }
        internal ReturnVal ReturnValue { get; set; }
        internal string FileName { get; set; }
        internal Program.TorchVersion TVersion { get; set; }
        internal string Line { get; set; }
        internal int LineNumber { get; set; }
        internal int SequenceNumber { get; set; }

        public XElement ToXml()
        {
            XElement node = new XElement("Line");

            node.SetAttributeValue("lineId", GetLineId());
            node.SetAttributeValue("epoch", GetEpoch());

            node.SetAttributeValue("lineIndex", LineIndex);
            node.SetAttributeValue("startTime", Ticks);
            node.SetAttributeValue("ticks", Latency);
            if (TVersion == Program.TorchVersion.T18)
            {
                node.SetAttributeValue("pid", ParentThreadId);
            }
            node.SetAttributeValue("normalizeTid", ManagedThreadId);
            node.SetAttributeValue("original", ManagedThreadIdOriginal);
            node.SetAttributeValue("ptid", RequestId);
            node.SetAttributeValue("objid", Objid);
            node.SetAttributeValue("type", Type);
            node.SetAttributeValue("caller", Caller);
            node.SetAttributeValue("callee", Callee);
            node.SetAttributeValue("offset", IlOffset);
            node.SetAttributeValue("exception", Exception);
            node.SetAttributeValue("returnNull", ReturnValue);
            node.SetAttributeValue("fileName", FileName);
            node.SetAttributeValue("lineNumber", LineNumber);
            node.SetAttributeValue("seqNumber", SequenceNumber);

            return node;
        }

        public LineEntry ContainsKeyword(string keyword)
        {
            return Callee.Contains(keyword) ? new LineEntry(Line, LineIndex, FileName, TVersion) : null;
        }

        public string GetLineId()
        {
            return $"{IlOffset}:{ManagedThreadId}:{Caller}:{Callee}";
        }

        public string GetEpoch()
        {
            return $"{GetLineId()}:{SequenceNumber}";
        }

        public LineEntry(string line, int lineIndex, string fileName, Program.TorchVersion tVersion)
        {
            Line = line;
            TVersion = tVersion;
            LineIndex = lineIndex;
            var columns = line.Split(';');
            if (TVersion == Program.TorchVersion.T19 || TVersion == Program.TorchVersion.T20)
            {
                Ticks = ParseInt(columns[0]);
                ManagedThreadIdOriginal = ParseInt(columns[2]);
                Objid = ParseInt(columns[4]);
                Type = columns[5];
                Caller = columns[6];
                Callee = columns[7];

                Latency = ParseInt(columns[12]);
                ParentThreadId = ParseInt(columns[8]);
                RequestId = ParseInt(columns[9]);

                if (columns.Length > 10)
                    IlOffset = ParseInt(columns[10]);
                if (columns.Length > 11)
                    LineNumber = ParseInt(columns[11]);
                if (columns.Length > 14)
                    Exception = columns[14];

                if (columns.Length > 13)
                {
                    ReturnValue = new ReturnVal(columns[13]);
                }
            }
            else
            {
                Ticks = ParseInt(columns[0]);
                Latency = ParseInt(columns[1]);
                ParentThreadId = ParseInt(columns[2]);
                ManagedThreadIdOriginal = ParseInt(columns[3]);
                RequestId = ParseInt(columns[4]);
                Objid = ParseInt(columns[5]);
                Type = columns[6];
                Caller = columns[7];
                Callee = columns[8];

                if (columns.Length > 9)
                    IlOffset = ParseInt(columns[9]);
                if (columns.Length > 10)
                    Exception = columns[10];

                if (columns.Length > 11)
                {
                    ReturnValue = new ReturnVal(columns[11]);
                }
            }
            FileName = fileName;
        }

        public bool CompareCallerCallee(LineEntry entry)
        {
            return Callee.Equals(entry.Callee) && Caller.Equals(entry.Caller);
        }

        internal int ParseInt(string value)
        {
            int retVal = -1;
            Int32.TryParse(value, out retVal);
            return retVal;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is LineEntry))
                return false;
            var entryObj = (LineEntry)obj;
            return entryObj.Caller.Equals(Caller) && entryObj.Callee.Equals(Callee);
        }

        public override int GetHashCode()
        {
            return Caller.GetHashCode() + Callee.GetHashCode();
        }
    }
}
