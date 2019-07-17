using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using RootFinder.Data;

namespace RootFinder.PredicateData
{
    [Serializable]
    class PredicateLine
    {
        public Predicate.Predicate.PredicateType Type { get; set; }
        public string Epoch { get; set; }
        public bool DidCollide { get; set; }
        public LineEntry Line { get; set; }

        public PredicateLine(Predicate.Predicate.PredicateType type, string epoch, bool didCollide, LineEntry line)
        {
            Type = type;
            Epoch = epoch;
            DidCollide = didCollide;
            Line = line;
        }

        public XElement ToXml()
        {
            XElement node = new XElement("PredicateLine");

            node.SetAttributeValue("type", Type);
            node.SetAttributeValue("epoch", Epoch);
            //node.SetAttributeValue("value", DidCollide);

            node.Add(Line.ToXml());

            return node;
        }
    }
}
