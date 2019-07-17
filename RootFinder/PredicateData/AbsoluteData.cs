using RootFinder.Data;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace RootFinder.PredicateData
{
    class AbsoluteData : CollisionLog
    {
        internal string AbsoluteVal { get; set; }
        public AbsoluteData(Predicate.Predicate.PredicateType type, string epoch, string absoluteVal) : base(type, epoch)
        {
            AbsoluteVal = absoluteVal;
        }

        internal override PredicateLine GetPredicateLine()
        {
            var line = CurrentVals.First();
            bool isEqual = AbsoluteVal.Equals(line.ReturnValue);
            return (new PredicateLine(Type, Epoch, isEqual, line));
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
