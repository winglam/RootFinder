using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogParser.Data
{
    class CalleeLists
    {
        internal HashSet<string> FailingLogsCallees { get; set; }
        internal HashSet<string> PassingLogsCallees { get; set; }
        internal HashSet<string> UnionLogsCallees { get; set; }
        internal HashSet<string> IntersectionLogsCallees { get; set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("[Intersect]:");
            IntersectionLogsCallees.ToList().ForEach(s => sb.AppendLine("  " + s));
            sb.AppendLine("[Union]:");
            UnionLogsCallees.ToList().ForEach(s => sb.AppendLine("  " + s));
            sb.AppendLine("[Failing]:");
            FailingLogsCallees.ToList().ForEach(s => sb.AppendLine("  " + s));
            sb.AppendLine("[Passing]:");
            PassingLogsCallees.ToList().ForEach(s => sb.AppendLine("  " + s));

            return sb.ToString();
        }
    }
}
