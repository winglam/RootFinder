using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RootFinder.Data
{
    [Serializable]
    public class CompareLineChunkResult
    {
        internal LineChunk P2 { get; set; }
        internal LineChunk P1 { get; set; }
        internal bool SameSize { get; set; }
        internal bool SameSequence { get; set; }
        internal bool SameMethods { get; set; }
        internal bool SameStartLineCallerCallee { get; set; }

        public CompareLineChunkResult(LineChunk p1, LineChunk p2)
        {
            P1 = p1 ?? throw new ArgumentNullException("p1 is not allowed to be null");

            if (p2 != null)
            {
                P2 = p2;

                SameSequence = P1.Entries.Equals(P2.Entries);
                SameSize = P1.Entries.Count == P2.Entries.Count;
                SameMethods = P1.UniqueEntries.Equals(P2.UniqueEntries);
                SameStartLineCallerCallee = P1.CompareCallerCalleeStartLine(P2);
            }
        }

        public XElement ToXml()
        {
            XElement resultNode = new XElement("CompareResult");

            resultNode.Add(P1.ToXml());

            if (P2 != null)
            {
                resultNode.Add(P2.ToXml());
                resultNode.SetAttributeValue("sameStartLineCallerCallee", SameStartLineCallerCallee);
                resultNode.SetAttributeValue("sameSize", SameSize);
                resultNode.SetAttributeValue("sameSequence", SameSequence);
                resultNode.SetAttributeValue("sameMethods", SameMethods);
                resultNode.SetAttributeValue("p2FileName", P2.FileProp.FileName);
            }
            else
            {
                resultNode.SetAttributeValue("p2IsNull", true);
            }
            return resultNode;
        }
    }
}
