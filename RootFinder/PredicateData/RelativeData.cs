using RootFinder.Data;
using System.Collections.Generic;
using System.Xml.Linq;

namespace RootFinder.PredicateData
{
    internal class RelativeData : CollisionLog
    {
        public RelativeData(Predicate.Predicate.PredicateType type, string epoch) : base(type, epoch)
        {
        }

        internal override PredicateLine GetPredicateLine()
        {
            var returnVals = new HashSet<string>();
            PredicateLine lastLine = null;
            foreach (var line in CurrentVals)
            {
                bool isUnique = returnVals.Contains(line.ReturnValue.Value);
                returnVals.Add(line.ReturnValue.Value);
                lastLine = new PredicateLine(Type, Epoch, isUnique, line);
            }

            return lastLine;
        }

        internal override XElement ToXml()
        {
            var passingNodes = new XElement("Log");

            foreach (var line in CurrentVals)
            {
                var lineNode = new XElement("PredicateLine");
                lineNode.SetAttributeValue("SequenceNumber", line.SequenceNumber);
                lineNode.SetAttributeValue("ReturnVal", line.ReturnValue.Value);
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
