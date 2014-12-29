using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CaseGenerator;
using System.Threading;
using System.Threading.Tasks;

namespace Implementation.Solver
{
    public class RoutingTaskSolver
    {
        private int n; //vertex count (including the depot)
        private int[,] t; //travel time between customers
        private double[,] d; //travel cost (= distance)
        private int[] demand;
        private int[] q; //capacities
        private int[] startTime;
        private int[] endTime;
        private int q0;
        private int penalty = 1;
        private const double EPS = 1e-2;
        private const double alpha = 0.1;
        private const double beta = 4.0;

        TaskInstance task;

        private static double Distance(int x1, int y1, int x2, int y2) {
            return Math.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2));
        }

        public RoutingTaskSolver(TaskInstance taskInstance)
        {
            task = taskInstance;
            n = taskInstance.Customers.Count;
            t = new int[n,n];
            d = new double[n,n];
            demand = new int[n];
            startTime = new int[n];
            endTime = new int [n];

            for (int i = 0; i < n-1; ++i)
            {
                for (int j = i + 1; j < n; ++j)
                {
                    int ind1 = taskInstance.Customers[i].Id;
                    int ind2 = taskInstance.Customers[j].Id;

                    d[ind1, ind2] = Distance(taskInstance.Customers[i].X, taskInstance.Customers[i].Y, taskInstance.Customers[j].X, taskInstance.Customers[j].Y);
                    t[ind1, ind2] = (int)d[ind1, ind2];
                    d[ind2, ind1] = d[ind1, ind2];
                    t[ind2, ind1] = t[ind1, ind2];
                }
            }

            foreach(Customer x in taskInstance.Customers) {
                demand[x.Id] = x.Demand;
                startTime[x.Id] = x.StartTime;
                endTime[x.Id] = x.EndTime;
            }
            
            q = taskInstance.Capacities.ToArray();
            q0 = taskInstance.Q0;
        }

        private Tuple<Solution, Solution> solve()
        {

            Solution initialSolution = createInitialSolution();
            Solution currentSolution = initialSolution;

            if (currentSolution == null) //impossible to create the initial solution
            {
                return null;
            }

            double bestTargetValue = evaluate(currentSolution);

            int iterationCount = 0;

            while (true)
            {
                List<Solution> neighbors = new List<Solution>();

                neighbors.AddRange(intraRoute(currentSolution));
                neighbors.AddRange(twoOpt(currentSolution));
                neighbors.AddRange(crossExchange(currentSolution));
                neighbors.AddRange(replaceVehicle(currentSolution));
                neighbors.AddRange(addVehicle(currentSolution));

                Console.WriteLine(">>>>iteration {2}: {0} neighbors total, currently best is {1}, started evaluating neighbors", neighbors.Count, bestTargetValue, ++iterationCount);

                double value = double.PositiveInfinity;
                Solution bestNeighbor = null;
                int evalCount = 0;
                object _lock = new Object();
                Parallel.ForEach(neighbors, x =>
                    {
                        double currentValue = evaluate(x);
                        lock (_lock)
                        {
                            if (currentValue < value)
                            {
                                value = currentValue;
                                bestNeighbor = x;
                            }
                        }
                    });

                double currentTargetValue = evaluate(bestNeighbor);
                                
                if (currentTargetValue + EPS >= bestTargetValue)
                {
                    break;
                }
                else
                {
                    currentSolution = bestNeighbor;
                    bestTargetValue = currentTargetValue;
                }
            }

            return Tuple.Create(initialSolution, currentSolution);
        }

        private Solution createInitialSolution()
        {
            List<Tuple<int, int>> sortedCapacities = q.Select((x, i) => Tuple.Create(i+1, x)).OrderByDescending(x => x.Item2).ToList();

            int lastServedCustomer = 0;

            List<Route> routes = new List<Route>();

            //trying first to put the most capacited vehicles to the routes
            for (int i = 0; i < sortedCapacities.Count; ++i)
            {
                int j = lastServedCustomer + 1;
                int currentLoad = 0;
                while (j < n)
                {
                    if (currentLoad + demand[j] <= sortedCapacities[i].Item2) //if vehicle can take the customer's demand onboard
                    {
                        currentLoad += demand[j];
                        ++j;
                    }
                    else
                    {
                        break;
                    }
                }

                if (currentLoad > 0)
                {
                    List<int> customers = new List<int>(j - lastServedCustomer);
                    for (int k = lastServedCustomer + 1; k < j; ++k)
                    {
                        customers.Add(k);
                    }

                    routes.Add(new Route
                    {
                        Vehicle = sortedCapacities[i].Item1,
                        Customers = customers
                    });
                }
                else
                {
                    break; //further vehicles are of a lesser capacity, so break if we failed to load
                }

                lastServedCustomer = j - 1;

                if (lastServedCustomer == (n - 1))
                {
                    break;
                }

            }

            if (lastServedCustomer < (n - 1))
            {
                //trying to add Q0 vehicles for the rest of customers
                int currentLoad = 0;
                List<int> currentList = new List<int>();
                for (int i = lastServedCustomer + 1; i < n; ++i)
                {
                    if (demand[i] > q0)
                    {
                        return null; //if we have a demand greater than Q0, then it is impossible to construct a solution
                    }
                    if (demand[i] + currentLoad <= q0)
                    {
                        currentLoad += demand[i];
                        currentList.Add(i);
                    }
                    else
                    {
                        routes.Add(new Route
                        {
                            Vehicle = 0,
                            Customers = currentList
                        });
                        currentLoad = demand[i];
                        currentList.Clear();
                        currentList.Add(i);
                    }
                }
                if (currentList.Any())
                {
                    routes.Add(new Route
                    {
                        Vehicle = 0,
                        Customers = currentList
                    });                    
                }
            }

            Solution result = new Solution
            {
                Routes = routes
            };

            return result;

        }

        #region Neighborhood generation methods

        private IEnumerable<Solution> intraRoute(Solution solution)
        {

            List<Solution> result = new List<Solution>();

            for (int r=0; r<solution.Routes.Count; ++r)
            {
                for (int lIntraPath = 2; lIntraPath < 4; ++lIntraPath) {
                    for (int i = 0; i < solution.Routes[r].Customers.Count - lIntraPath; ++i)
                    {
                        for (int j = Math.Max(0, i - lIntraPath); j <= Math.Min(solution.Routes[r].Customers.Count - 1 - lIntraPath, i + lIntraPath); ++j)
                        {
                            if (j == i) continue; //no reinsertion into the same place

                            Solution newSolution = solution.Clone();

                            var reinsertedElements = newSolution.Routes[r].Customers.GetRange(i, lIntraPath);

                            newSolution.Routes[r].Customers.RemoveRange(i, lIntraPath);

                            newSolution.Routes[r].Customers.InsertRange(j, reinsertedElements);

                            result.Add(newSolution);
                        }
                    }
                }
            }

            return result.Where(x => isValid(x));
        }

        private IEnumerable<Solution> twoOpt(Solution solution)
        {
            List<Solution> result = new List<Solution>();

            for (int i = 0; i < solution.Routes.Count - 1; ++i)
            {
                for (int j = i + 1; j < solution.Routes.Count; ++j)
                {
                    for (int split1 = 1; split1 < solution.Routes[i].Customers.Count; ++split1)
                    {
                        for (int split2 = 1; split2 < solution.Routes[j].Customers.Count; ++split2)
                        {
                            Solution newSolution = solution.Clone();
                            var route1Part2 = newSolution.Routes[i].Customers.GetRange(split1, newSolution.Routes[i].Customers.Count - split1);
                            var route2Part2 = newSolution.Routes[j].Customers.GetRange(split2, newSolution.Routes[j].Customers.Count - split2);
                            newSolution.Routes[i].Customers.RemoveRange(split1, newSolution.Routes[i].Customers.Count - split1);
                            newSolution.Routes[j].Customers.RemoveRange(split2, newSolution.Routes[j].Customers.Count - split2);
                            newSolution.Routes[i].Customers.AddRange(route2Part2);
                            newSolution.Routes[j].Customers.AddRange(route1Part2);

                            result.Add(newSolution);
                        }
                    }
                }
            }

            return result.Where(x => isValid(x));
        }

        private IEnumerable<Solution> crossExchange(Solution solution)
        {
            int lCross = 4; //maximum length of exchanged section of routes

            List<Solution> result = new List<Solution>();

            for (int length = 2; length < lCross; ++length)
            {
                for (int i = 0; i < solution.Routes.Count-1; ++i)
                {
                    for (int j = i + 1; j < solution.Routes.Count; ++j)
                    {
                        for (int pos1 = 0; pos1 < solution.Routes[i].Customers.Count - length; ++pos1)
                        {
                            for (int pos2 = 0; pos2 < solution.Routes[j].Customers.Count - length; ++pos2)
                            {
                                Solution newSolution = solution.Clone();

                                var route1Part = newSolution.Routes[i].Customers.GetRange(pos1, length);
                                var route2Part = newSolution.Routes[j].Customers.GetRange(pos2, length);
                                newSolution.Routes[i].Customers.RemoveRange(pos1, length);
                                newSolution.Routes[j].Customers.RemoveRange(pos2, length);
                                newSolution.Routes[i].Customers.InsertRange(pos1, route2Part);
                                newSolution.Routes[j].Customers.InsertRange(pos2, route1Part);

                                result.Add(newSolution);
                            }
                        }
                    }
                }
            }

            return result.Where(x => isValid(x));
        }

        private IEnumerable<Solution> replaceVehicle(Solution solution)
        {
            List<Solution> result = new List<Solution>();

            var usedVehicles = solution.Routes.Select(x => x.Vehicle).Where(x => x != 0);

            List<int> unusedVehicles = q.Select((x, i) => i + 1).Where(x => !usedVehicles.Contains(x)).ToList(); //without used

            for (int i = 0; i < solution.Routes.Count - 1; ++i)
            {
                foreach (var x in unusedVehicles)
                {
                    Solution newSolution = solution.Clone();
                    newSolution.Routes[i].Vehicle = x;
                    result.Add(newSolution);
                }
                //also try to replace with Q0
                if (solution.Routes[i].Vehicle != 0)
                {
                    Solution newSolution = solution.Clone();
                    newSolution.Routes[i].Vehicle = 0;
                    result.Add(newSolution);
                }
            }

            return result.Where(x => isValid(x));
        }

        private IEnumerable<Solution> addVehicle(Solution solution)
        {
            List<Solution> result = new List<Solution>();

            var usedVehicles = solution.Routes.Select(x => x.Vehicle).Where(x => x != 0);
            List<int> unusedVehicles = q.Select((x, i) => i + 1).Where(x => !usedVehicles.Contains(x)).ToList();
            unusedVehicles.Add(0);

            foreach (var v in unusedVehicles)
            {
                for (int i = 0; i < solution.Routes.Count; ++i)
                {
                    for (int split1 = 1; split1 < solution.Routes[i].Customers.Count; ++split1)
                    {
                        Solution newSolution = solution.Clone();

                        var routePart2 = newSolution.Routes[i].Customers.GetRange(split1, newSolution.Routes[i].Customers.Count - split1);
                        newSolution.Routes[i].Customers.RemoveRange(split1, newSolution.Routes[i].Customers.Count - split1);

                        newSolution.Routes.Add(new Route
                        {
                            Vehicle = v,
                            Customers = routePart2
                        });

                        result.Add(newSolution);
                        
                    }
                    
                }
            }

            return result.Where(x => isValid(x));
        }

        #endregion


        private bool isValid(Solution solution)
        {

            foreach (Route r in solution.Routes)
            {
                int load = 0;
                for (int i = 0; i < r.Customers.Count; ++i)
                {
                    load += demand[r.Customers[i]];
                }
                if (r.Vehicle == 0)
                {
                    if (load > q0)
                    {
                        return false;
                    }
                }
                else
                {
                    if (load > q[r.Vehicle - 1])
                    {
                        return false;
                    }
                }
            }


            return true;
        }

        private double evaluate(Solution solution)
        {
            double totalTime = 0.0;
            double totalVehicleCost = 0.0;
            double totalPenalty = 0.0;

            foreach (Route r in solution.Routes)
            {
                var l = r.ExtendWithDepots();

                for (int i = 0; i < l.Count - 1; ++i)
                {
                    totalTime += d[l[i], l[i + 1]];
                }

                if (r.Vehicle == 0)
                {
                    totalVehicleCost += q0*20;
                }
                else
                {
                    totalVehicleCost += q[r.Vehicle - 1];
                }

                totalPenalty += getMinPenalty(r);
            }


            double result = alpha * totalTime + beta * totalPenalty + alpha * totalVehicleCost;

            return result;
        }

        private string evaluateDetailed(Solution solution)
        {
            double totalTime = 0.0;
            double totalVehicleCost = 0.0;
            double totalPenalty = 0.0;

            StringBuilder details = new StringBuilder();

            foreach (Route r in solution.Routes)
            {
                var l = r.ExtendWithDepots();

                for (int i = 0; i < l.Count - 1; ++i)
                {
                    totalTime += d[l[i], l[i + 1]];
                }

                if (r.Vehicle == 0)
                {
                    totalVehicleCost += q0 * 10;
                }
                else
                {
                    totalVehicleCost += q[r.Vehicle - 1];
                }

                totalPenalty += getMinPenalty(r);
            }

            double result = alpha * totalTime + beta * totalPenalty + 0.03 * totalVehicleCost;

            details.AppendFormat("Target function value = {0}\nTotal travel time = {1}\nTotal penalty = {2}\nTotal vehicle cost = {3}\n", result, totalTime, totalPenalty, totalVehicleCost);
            
            return details.ToString();
        }

        private int getMinPenalty(Route r) {

            var l = r.ExtendWithDepots();
            
            int n = r.Customers.Count;

            PiecewiseLinearFunction[] f = new PiecewiseLinearFunction[r.Customers.Count + 2];
            f[0] = new PiecewiseLinearFunction(); //f = 0

            for (int h=1; h<=n+1; ++h) {
                //penalty function for current customer
                int customer = l[h];
                int prevCustomer = l[h-1];

               
                PiecewiseLinearFunction ph = PiecewiseLinearFunction.PenaltyFunction(startTime[customer], endTime[customer], penalty);
                int t0 = t[prevCustomer, customer];

                f[h] = f[h-1].Shift(-t0).Add(ph).Min();
            }

            return f[n+1].GetValue(endTime[0]);
        }

        public string Solve()
        {
            var result = solve();

            return string.Format("Initial Solution:\n{0}Best solution:\n{1}Details of initial solution:\n{2}Details of best solution:\n{3}",
                result.Item1.ToString(), result.Item2.ToString(), evaluateDetailed(result.Item1), evaluateDetailed(result.Item2));
        }


    }
}
