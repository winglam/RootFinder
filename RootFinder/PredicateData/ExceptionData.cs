using RootFinder.Data;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace RootFinder.PredicateData
{
    class ExceptionData : CollisionLog
    {
        internal string ExceptionVal { get; set; }
        public ExceptionData(Predicate.Predicate.PredicateType type, string epoch, string exceptionVal) : base(type, epoch)
        {
            ExceptionVal = exceptionVal;
        }

        internal override PredicateLine GetPredicateLine()
        {
            var line = CurrentVals.First();
            bool isEqual = ExceptionVal.Equals(line.Exception);
            return (new PredicateLine(Type, Epoch, isEqual, line));
        }
        internal override XElement ToXml()
        {
            var passingNodes = new XElement("Log");

            foreach (var line in CurrentVals)
            {
                var lineNode = new XElement("PredicateLine");
                lineNode.SetAttributeValue("SequenceNumber", line.SequenceNumber);
                lineNode.SetAttributeValue("ExceptionVal", line.Exception);
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
