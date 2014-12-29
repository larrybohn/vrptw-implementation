using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace CaseGenerator
{
    class Generator
    {
        public static string Generate(int dimension, int vehicleCount, int caseType, int randSeed, int maxCoord = 100, int maxTimespan = 1000, int maxCapacity = 750)
        {
            ++vehicleCount; //include vehicles of type Q0

            List<Customer> randomCustomers = new List<Customer>(dimension);
            List<Customer> clusteredCustomers = new List<Customer>(dimension);
            List<Customer> customers = new List<Customer>(dimension);

            Random rand = new Random(randSeed);

            string locationGenerationMethod;
            string timeWindowsLength;
            string vehicleCapacitiesDistribution;

            #region Generate locations of customers randomly
            for (int i = 1; i < dimension; ++i)
            {
                randomCustomers.Add(new Customer { X = rand.Next(0, maxCoord), Y = rand.Next(0, maxCoord), Id = i, Demand = 15 * rand.Next(1, 3) });
            }
            #endregion

            #region Generate locations of customers by grouping them into clusters
            int clustersCount = rand.Next(2, (int)Math.Sqrt(dimension));
            int currentId = 1;

            for (int k = 0; k < clustersCount; ++k)
            {
                double alpha = (0.5 * k / clustersCount) * Math.PI; //cluster baseline angle
                double radius = 0.1 + rand.NextDouble() * 0.9; //cluster base distance from zero

                int clusterSize;
                if (k == clustersCount - 1)
                {
                    //put all the remaining customers into the last cluster
                    clusterSize = dimension - clusteredCustomers.Count - 1;
                }
                else
                {
                    //otherwise choose the cluster size randomly
                    clusterSize = rand.Next(dimension / clustersCount - 2, dimension / clustersCount + 3);
                }

                for (int i = 0; i < clusterSize; ++i)
                {
                    //add some aberration to the angle and radius
                    double _alpha = alpha + (rand.NextDouble() - 1.0) / clustersCount;
                    if (_alpha < 0)
                    {
                        _alpha = Math.Abs(_alpha);
                    }
                    if (_alpha > Math.PI / 2.0)
                    {
                        _alpha = Math.PI - _alpha;
                    }
                    double _radius = radius + (rand.NextDouble() - 1.0) / clustersCount;

                    //convert polar coords to Cartesian
                    double _x = maxCoord * _radius * Math.Cos(_alpha);
                    double _y = maxCoord * _radius * Math.Sin(_alpha);

                    int demand = 15 * rand.Next(1, 3);

                    clusteredCustomers.Add(new Customer { X = (int)_x, Y = (int)_y, Id = currentId++, Demand = demand });
                }
            }
            #endregion

            #region Combine random and clustered customers depending on the caseType
            if (caseType % 3 == 0)
            {
                customers = randomCustomers;
                locationGenerationMethod = "random";
            }
            else if (caseType % 3 == 1)
            {
                customers = clusteredCustomers;
                locationGenerationMethod = "clustered";
            }
            else
            {
                customers = clusteredCustomers;
                int randomizationDegree = rand.Next(dimension / 8, dimension / 2);
                for (int i = 0; i < randomizationDegree; ++i)
                {
                    int m = dimension / randomizationDegree;
                    int substituteIndex = rand.Next(m * i, m * (i + 1));
                    customers[substituteIndex] = randomCustomers[substituteIndex];
                }
                locationGenerationMethod = "combined";
            }

            #endregion

            #region Generate service time windows for customers

            timeWindowsLength = "";
            foreach (Customer c in customers)
            {

                int timespanWidth;
                if ((caseType / 3) % 3 == 0)
                {
                    timespanWidth = rand.Next(maxTimespan / 4 - maxTimespan / 16, maxTimespan / 4 + maxTimespan / 16);
                    timeWindowsLength = "narrow";
                }
                else if ((caseType / 3) % 3 == 1)
                {
                    timespanWidth = rand.Next(maxTimespan / 2 - maxTimespan / 16, maxTimespan / 2 + maxTimespan / 16);
                    timeWindowsLength = "moderate";
                }
                else
                {
                    timespanWidth = rand.Next(3 * maxTimespan / 4 - maxTimespan / 16, 3 * maxTimespan / 4 + maxTimespan / 16);
                    timeWindowsLength = "wide";
                }

                c.StartTime = rand.Next(0, maxTimespan - timespanWidth);
                c.EndTime = c.StartTime + timespanWidth;
                c.ServiceTime = maxTimespan / 11;
            }
            #endregion

            #region Add the depot

            int depotX = maxCoord / 2 + rand.Next(-maxCoord / 8, +maxCoord / 8);
            int depotY = maxCoord / 2 + rand.Next(-maxCoord / 8, +maxCoord / 8);
            customers.Add(new Customer { X = depotX, Y = depotY, Id = 0, Demand = 10, EndTime = maxTimespan, ServiceTime = 0 });

            #endregion

            #region Generate vehicles

            List<int> vehicles = new List<int>(vehicleCount);

            if ((caseType / 9) % 3 == 0)
            {
                int capacity = rand.Next(2 * maxCapacity / 3, maxCapacity + 1);
                for (int i = 0; i < vehicleCount; ++i)
                {
                    vehicles.Add(capacity);
                }
                vehicleCapacitiesDistribution = "equal";
            }
            else if ((caseType / 9) % 3 == 1)
            {
                for (int i = 0; i < vehicleCount; ++i)
                {
                    int capacity = rand.Next(2 * maxCapacity / 3, maxCapacity + 1);
                    vehicles.Add(capacity);
                }
                vehicleCapacitiesDistribution = "moderately random";
            }
            else
            {
                for (int i = 0; i < vehicleCount; ++i)
                {
                    int capacity = rand.Next(1, maxCapacity + 1);
                    vehicles.Add(capacity);
                }
                vehicleCapacitiesDistribution = "totally random";
            }

            #endregion

            StringBuilder result = new StringBuilder();
            result.AppendFormat("N={0}, M={1}, random seed={2}, case type={3} (locations: {4}, time windows: {5}, vehicle capacities: {6})\n\nVehicle\nNumber\t\t\tCapacities\n",
                dimension, vehicleCount, randSeed, caseType, locationGenerationMethod, timeWindowsLength, vehicleCapacitiesDistribution);

            result.AppendFormat("{0}\t\t\t", vehicleCount);
            for (int i = 0; i < vehicleCount; ++i)
            {
                result.AppendFormat("{0} ", vehicles[i]);
            }
            result.Append("\n\n\n\n");

            foreach (var x in customers)
            {
                result.AppendFormat("{0} {1} {2} {3} {4} {5} {6}\n", x.Id, x.X, x.Y, x.Demand, x.StartTime, x.EndTime, x.ServiceTime);
            }

            return result.ToString();
        }

        static void Main(string[] args)
        {

            for (int type = 0; type < 27; ++type)
            {
                for (int seed = 17; seed < 20; ++seed)
                {
                    File.WriteAllText(string.Format("in_type{0:00}_seed{1:00}.txt", type, seed), Generate(100, 10, type, seed));
                }
            }
        }
    }
}
