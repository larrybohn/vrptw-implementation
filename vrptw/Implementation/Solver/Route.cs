using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Implementation.Solver
{
    class Route
    {
        public int Vehicle { get; set; }
        public List<int> Customers { get; set; } //excluding the depot

        public List<int> ExtendWithDepots()
        {
            var l = Customers.ToArray().ToList();
            l.Insert(0, 0);
            l.Add(0);
            return l;
        }
    }
}
