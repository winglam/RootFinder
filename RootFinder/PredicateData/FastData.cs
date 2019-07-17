using RootFinder.Data;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace RootFinder.PredicateData
{
    class FastData : CollisionLog
    {
        internal int FastTime { get; set; }
        public FastData(Predicate.Predicate.PredicateType type, string epoch, int fastTime) : base(type, epoch)
        {
            FastTime = fastTime;
        }

        internal override PredicateLine GetPredicateLine()
        {
            var line = CurrentVals.First();
            bool isEqual = FastTime >= line.Latency;
            return (new PredicateLine(Type, Epoch, isEqual, line));
        }

        internal override XElement ToXml()
        {
            var passingNodes = new XElement("Log");

            foreach (var line in CurrentVals)
            {
                var lineNode = new XElement("PredicateLine");
                lineNode.SetAttributeValue("SequenceNumber", line.SequenceNumber);
                lineNode.SetAttributeValue("Timing", line.Ticks);
                lineNode.SetAttributeValue("LogLineIndex", line.LineIndex);
                lineNode.SetAttributeValue("Epoch", line.GetEpoch());
                passingNodes.Add(lineNode);
            }
            passingNodes.SetAttributeValue("FileName", File);
            passingNodes.SetAttributeValue("NumLines", CurrentVals.Count);

            return passingNodes;
        }
    }
}
