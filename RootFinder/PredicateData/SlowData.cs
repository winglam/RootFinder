using RootFinder.Data;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using static RootFinder.Predicate.Predicate;

namespace RootFinder.PredicateData
{
    class SlowData : CollisionLog
    {
        public SlowData(Predicate.Predicate.PredicateType type, string epoch) : base(type, epoch)
        {
        }

        internal override XElement ToXml()
        {
            var passingNodes = new XElement("Log");

            foreach (var line in CurrentVals)
            {
                var lineNode = new XElement("PredicateLine");
                lineNode.SetAttributeValue("SequenceNumber", line.SequenceNumber);
                lineNode.SetAttributeValue("Latency", line.Latency);
                lineNode.SetAttributeValue("LogLineIndex", line.LineIndex);
                lineNode.SetAttributeValue("Epoch", line.GetEpoch());
                passingNodes.Add(lineNode);
            }
            passingNodes.SetAttributeValue("FileName", File);
            passingNodes.SetAttributeValue("NumLines", CurrentVals.Count);

            return passingNodes;
        }

        internal override PredicateLine GetPredicateLine()
        {
            throw new System.NotImplementedException();
        }
    }
}
