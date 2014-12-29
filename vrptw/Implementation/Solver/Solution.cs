using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Implementation.Solver
{
    class Solution
    {
        public List<Route> Routes { get; set; }

        public Solution Clone()
        {
            Solution newSolution = new Solution();

            newSolution.Routes = new List<Route>(this.Routes.Count);

            foreach (Route r in this.Routes)
            {
                newSolution.Routes.Add(new Route
                {
                    Vehicle = r.Vehicle,
                    Customers = r.Customers.ToArray().ToList()
                });
            }

            return newSolution;
        }

        public string ToString() {
            StringBuilder presentation = new StringBuilder();
            presentation.AppendLine(this.Routes.Count.ToString());

            foreach (Route r in Routes)
            {
                presentation.Append(r.Vehicle.ToString());
                var v = r.ExtendWithDepots();
                for (int i = 0; i < v.Count; ++i)
                {
                    presentation.AppendFormat(" {0}", v[i]);
                }
                presentation.AppendLine();
            }

            return presentation.ToString();
        }
    }
}
