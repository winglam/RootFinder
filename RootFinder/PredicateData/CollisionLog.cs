using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using RootFinder.Data;

namespace RootFinder.PredicateData
{
    [Serializable]
    internal abstract class CollisionLog
    {
        public List<LineEntry> CurrentVals { get; set; }
        public Predicate.Predicate.PredicateType Type { get; set; }
        public string Epoch { get; set; }
        public string File { get; set; }

        protected CollisionLog(Predicate.Predicate.PredicateType type, string epoch)
        {
            CurrentVals = new List<LineEntry>();
            Type = type;
            Epoch = epoch;
        }

        public void Add(LineEntry line)
        {
            CurrentVals.Add(line);
        }

        internal bool ValueOfAllPredicate()
        {
            return GetPredicateLine().DidCollide;
        }


        internal abstract PredicateLine GetPredicateLine();
        internal abstract XElement ToXml();
    }
}
