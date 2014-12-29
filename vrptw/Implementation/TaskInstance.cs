using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CaseGenerator;
using System.IO;

namespace Implementation
{
    public class TaskInstance
    {
        static readonly char[] DELIMITER_CHARS = { ' ', '\n', '\t' };

        public List<Customer> Customers { get; set; }
        public List<int> Capacities { get; set; }
        public int Q0 { get; set; } //capacity of additional vehicle type

        public static TaskInstance FromFile(string filename)
        {
            TaskInstance task = new TaskInstance();
                                 
            string[] lines = File.ReadAllLines(filename);

            #region Parse vehicle capacities

            int[] vehicleValues = lines[4].Split(DELIMITER_CHARS, StringSplitOptions.RemoveEmptyEntries).Select(x => int.Parse(x)).ToArray();
            int vehicleCount = vehicleValues[0];

            task.Capacities = new List<int>(vehicleCount);

            for (int i = 1; i <= vehicleCount; ++i)
            {
                task.Capacities.Add(vehicleValues[i]);
            }

            task.Q0 = vehicleValues.Last();

            #endregion


            #region Parse customers

            task.Customers = new List<Customer>(lines.Length);

            for (int i = 8; i < lines.Length; ++i)
            {
                if (string.IsNullOrWhiteSpace(lines[i]))
                {
                    continue;
                }

                int[] values = lines[i].Split(DELIMITER_CHARS, StringSplitOptions.RemoveEmptyEntries).Select(x => int.Parse(x)).ToArray();

                task.Customers.Add(new Customer
                {
                    Id = values[0],
                    X = values[1],
                    Y = values[2],
                    Demand = values[3],
                    StartTime = values[4],
                    EndTime = values[5],
                    ServiceTime = values[6]
                });
           }

            #endregion


            return task;
        }
    }
}
