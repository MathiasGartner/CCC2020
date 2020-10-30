using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CCC2020
{
    #region Helpers

    public static class Logger
    {
        public static void Log(string message)
        {
            System.Console.WriteLine(message);
            System.Diagnostics.Debug.WriteLine(message);
        }
    }

    public static class StringExtension
    {
        public static int AsInt(this String str)
        {
            return Convert.ToInt32(str);
        }

        public static long AsLong(this String str)
        {
            return Convert.ToInt64(str);
        }

        public static double AsDouble(this String str)
        {
            //return double.Parse(str.Replace(".", ","));
            return double.Parse(str);
        }
    }

    public static class EnumerableExtension
    {
        public static T PickRandom<T>(this IEnumerable<T> source)
        {
            return source.PickRandom(1).Single();
        }

        public static IEnumerable<T> PickRandom<T>(this IEnumerable<T> source, int count)
        {
            return source.Shuffle().Take(count);
        }

        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source)
        {
            return source.OrderBy(x => Guid.NewGuid());
        }

        public static IEnumerable<List<T>> Partition<T>(this IList<T> source, Int32 size)
        {
            for (int i = 0; i < Math.Ceiling(source.Count / (Double)size); i++)
                yield return new List<T>(source.Skip(size * i).Take(size));
        }
    }

    public class Helpers
    {
        public static Random rand = new Random();

        public static T DeepClone<T>(T obj)
        {
            using (var ms = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(ms, obj);
                ms.Position = 0;

                return (T)formatter.Deserialize(ms);
            }
        }
    }

    #endregion

    public class Price
    {
        public int Cost { get; set; }
        public int Minute { get; set; }
        public List<int> AvailablePowerPerHousehold { get; set; }
        public int MaxAvailablePower { get; set; }
        //public int Consumers { get; set; }
        public List<int> ConsumersPerHousehold { get; set; }

        public int EffectiveCost
        {
            get
            {
                return (int)Math.Round(this.Cost * (1.0 + (this.MaxAvailablePower * this.AvailablePowerPerHousehold.Count - this.AvailablePowerPerHousehold.Sum()) / ((double)this.MaxAvailablePower)));
            }
        }

        public Price()
        {
            this.AvailablePowerPerHousehold = new List<int>();
            this.ConsumersPerHousehold = new List<int>();
        }
    }

    public class Consumption
    {
        public Price Price { get; set; }
        public int Power { get; set; }
        public int EffectivePrice
        {
            get
            {
                return this.Power * this.Price.EffectiveCost;
            }
        }
    }

    public class Household
    {
        public int Id { get; set; }
        public List<Task> Tasks { get; set; }

        public Household()
        {
            this.Tasks = new List<Task>();
        }
    }

    public class Task
    {
        public int Id { get; set; }
        public int CompletionTime { get; set; }

        public int OptimumStartMinute { get; set; }

        public int Power { get; set; }
        public int StartInterval { get; set; }
        public int EndInterval { get; set; }

        public List<Consumption> Consumptions { get; set; }

        public bool HasFinished { get; set; }

        public double Importance
        {
            get
            {
                return this.Power / ((double)this.EndInterval - this.StartInterval);
            }
        }

        public Task()
        {
            this.Consumptions = new List<Consumption>();
        }

        public long TotalCost
        {
            get
            {
                //return this.Consumptions.Select(p => p.Power * p.Price.Cost).Sum();
                return this.Consumptions.Select(p => p.EffectivePrice).Sum();
            }
        }

        public int TotalPower
        {
            get
            {
                return this.Consumptions.Select(p => p.Power).Sum();
            }
        }
    }

    public class PowerOptimum
    {
        public int Duration { get; set; }
        public int StartMinute { get; set; }
    }

    public class CostForInterval
    {
        public int StartMinute { get; set; }
        public int TotalCost { get; set; }
    }

    public class PowerManager
    {
        public List<PowerOptimum> Optimums { get; set; }

        public PowerManager()
        {
            this.Optimums = new List<PowerOptimum>();
        }

        public void FindOptimum(Task t, List<Price> prices)
        {
            var opt = this.Optimums.Where(p => p.Duration == t.CompletionTime).SingleOrDefault();
            if (opt != null)
            {
                t.OptimumStartMinute = opt.StartMinute;
            }
            else
            {
                List<Tuple<int, int>> sums = new List<Tuple<int, int>>();
                int sum = 0;
                for (int i = 0; i < t.CompletionTime; i++)
                {
                    sum += prices[i].Cost;
                }
                sums.Add(Tuple.Create<int, int>(sum, 0));
                for (int i = t.CompletionTime; i < prices.Count; i++)
                {
                    sum -= prices[i - t.CompletionTime].Cost;
                    sum += prices[i].Cost;
                    sums.Add(Tuple.Create<int, int>(sum, i - t.CompletionTime + 1));
                }
                t.OptimumStartMinute = sums.OrderBy(p => p.Item1).ThenBy(p => p.Item2).First().Item2;
                this.Optimums.Add(new PowerOptimum() { Duration = t.CompletionTime, StartMinute = t.OptimumStartMinute });
            }
        }

        public void FindCheapestMinute(Task t, List<Price> prices)
        {
            var pricesInInterval = prices.Skip(t.StartInterval).Take(t.EndInterval - t.StartInterval + 1).ToList();
            var cheapestMinute = pricesInInterval.OrderBy(p => p.Cost).ThenBy(p => p.Minute).First().Minute;
            t.OptimumStartMinute = cheapestMinute;
        }

        public long GetTotalBill(List<Task> tasks)
        {
            return tasks.Sum(p => p.TotalCost);
        }

        public long GetTotalBill(List<Household> hh)
        {
            return hh.Select(p => p.Tasks.Sum(t => t.TotalCost)).Sum();
        }

        public void DistributeEnergy(List<Task> tasks, List<Price> prices, int maxPower, long maxBill, int maxConcurrent, int hhId)
        {
            double factor = 1.0;

            var sortedTasks = tasks.OrderByDescending(p => p.Importance).ToList();

            int i = 0;

            //foreach (var t in sortedTasks)
            while (sortedTasks.Count > 0)
            {
                var t = sortedTasks.First();
                if (i % 10 == 0)
                {
                    Console.WriteLine(i);
                }
                i++;
                var timeslots = prices.Skip(t.StartInterval).Take(t.EndInterval - t.StartInterval + 1).Where(p => p.AvailablePowerPerHousehold[hhId - 1] > 0 && p.ConsumersPerHousehold[hhId -1] < maxConcurrent).OrderBy(p => p.EffectiveCost).ToList();
                while (!t.HasFinished)
                {
                    if (timeslots.Count == 0)
                    {
                        var finished = tasks.Where(p => p.HasFinished).Where(p => p.Consumptions.Any(c => c.Price.Minute >= t.StartInterval && c.Price.Minute <= t.EndInterval)).Shuffle().First();
                        foreach (var c in finished.Consumptions)
                        {
                            c.Price.AvailablePowerPerHousehold[hhId - 1] += c.Power;
                            c.Price.ConsumersPerHousehold[hhId - 1]--;
                        }
                        finished.Consumptions.Clear();
                        finished.HasFinished = false;
                        sortedTasks.Add(finished);
                        timeslots = prices.Skip(t.StartInterval).Take(t.EndInterval - t.StartInterval + 1).Where(p => p.AvailablePowerPerHousehold[hhId - 1] > 0 && p.ConsumersPerHousehold[hhId - 1] < maxConcurrent).OrderBy(p => p.EffectiveCost).ToList();
                    }
                    var ts = timeslots.First();
                    //int powerToDraw = System.Convert.ToInt32(Math.Floor(ts.AvailablePower * factor));
                    int powerToDraw = ts.AvailablePowerPerHousehold[hhId - 1];
                    if (powerToDraw > (t.Power - t.TotalPower))
                    {
                        powerToDraw = (t.Power - t.TotalPower);
                    }
                    ts.AvailablePowerPerHousehold[hhId - 1] -= powerToDraw;
                    ts.ConsumersPerHousehold[hhId - 1]++;
                    t.Consumptions.Add(new Consumption() { Power = powerToDraw, Price = ts });
                    if (t.TotalPower == t.Power)
                    {
                        t.HasFinished = true;
                    }
                    timeslots.RemoveAt(0);
                }
                sortedTasks.RemoveAt(0);
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("en-GB");

            var filenames = Enumerable.Range(1, 5).Select(p => "..\\..\\..\\data\\level7_" + p + ".in").ToList();
            //var filenames = new List<String>() { "..\\..\\..\\data\\level7_example.in" };
            List<String> outputText = new List<String>();

            foreach (var filename in filenames)
            {
                Console.WriteLine(filename);
                string[] lines = System.IO.File.ReadAllLines(filename);
                //int[] props = lines[0].Split(',').Select(p => p.AsInt()).ToArray();
                int maxPower = lines[0].AsInt();
                long maxBill = lines[1].AsLong();
                int maxConcurrent = lines[2].AsInt();
                int offset = 3;
                int N = lines[offset].AsInt();

                List<Price> prices = new List<Price>();
                //List<Task> tasks = new List<Task>();
                List<Household> households = new List<Household>();

                for (int i = 0; i < N; i++)
                {
                    prices.Add(new Price() { Cost = lines[i + 1 + offset].AsInt(), Minute = i, MaxAvailablePower = maxPower});
                }

                int H = lines[N + 1 + offset].AsInt();
                int currentLine = N + 1 + offset + 1;
                for (int j = 0; j < H; j++)
                {
                    var hh = new Household() { Id = j + 1 };
                    households.Add(hh);
                    int M = lines[currentLine].AsInt();
                    currentLine++;
                    for (int i = 0; i < M; i++)
                    {
                        var data = lines[currentLine].Split(' ').Select(p => p.AsInt()).ToArray();
                        //tasks.Add(new Task() { Id = data[0], CompletionTime = data[1] });
                        hh.Tasks.Add(new Task() { Id = data[0], Power = data[1], StartInterval = data[2], EndInterval = data[3] });
                        currentLine++;
                    }
                }

                //Level 7
                foreach (var p in prices)
                {
                    for (int i = 0; i < H; i++)
                    {
                        p.ConsumersPerHousehold.Add(0);
                        p.AvailablePowerPerHousehold.Add(maxPower);
                    }
                }
                PowerManager pm = new PowerManager();
                foreach(var hh in households)
                {
                    pm.DistributeEnergy(hh.Tasks, prices, maxPower, maxBill, maxConcurrent, hh.Id);
                }

                var total = pm.GetTotalBill(households);
                System.Console.WriteLine("price:" + total + "(" + (maxBill - total) + ")");

                List<String> s = new List<String>();
                s.Add(H.ToString());
                foreach (var hh in households)
                {
                    s.Add(hh.Id.ToString());
                    s.Add(hh.Tasks.Count.ToString());
                    foreach (var t in hh.Tasks)
                    {
                        s.Add(t.Id + " " + String.Join(" ", t.Consumptions.Select(c => c.Price.Minute + " " + c.Power).ToList()));
                    }
                }
                

                //var total = pm.GetTotalBill(tasks);
                //System.Console.WriteLine("price:" + total + "(" + (maxBill - total) + ")");

                //Level4, 5, 6 
                //PowerManager pm = new PowerManager();
                //pm.DistributeEnergy(tasks, prices, maxPower, maxBill, maxConcurrent);
                //var s = tasks.Select(p => p.Id + " " + String.Join(" ", p.Consumptions.Select(c => c.Price.Minute + " " + c.Power).ToList())).ToList();
                //s.Insert(0, M.ToString());

                //var total = pm.GetTotalBill(tasks);
                //System.Console.WriteLine("price:" + total + "(" + (maxBill - total) + ")");

                //Level4
                //PowerManager pm = new PowerManager();
                //pm.DistributeEnergy(tasks, prices, maxPower, maxBill);
                //var s = tasks.Select(p => p.Id + " " + String.Join(" ", p.Consumptions.Select(c => c.Price.Minute + " " + c.Power).ToList())).ToList();
                //s.Insert(0, M.ToString());

                //var total = pm.GetTotalBill(tasks);
                //System.Console.WriteLine("price:" + total + "(" + (maxBill - total) + ")");

                //Level3
                //PowerManager pm = new PowerManager();
                //foreach(var t in tasks)
                //{
                //    pm.FindCheapestMinute(t, prices);
                //}
                //var s = tasks.Select(p => p.Id + " " + p.OptimumStartMinute + " " + p.Power).ToList();
                //s.Insert(0, M.ToString());

                //Level 2
                //PowerManager pm = new PowerManager();
                //foreach(var t in tasks)
                //{
                //    pm.FindOptimum(t, prices);
                //}

                //var s = tasks.Select(p => p.Id + " " + p.OptimumStartMinute).ToList();
                //s.Insert(0, M.ToString());

                //Level1
                //var minPrice = prices.OrderBy(p => p.Cost).ThenBy(p => p.Minute).First().Minute;

                //var s = flightDetails.Where(p => p.Contacts.Count > 0).SelectMany(p => p.Contacts.Select(m => p.Id + " " + m.ContactToFlightId + " " + m.Delay + " " + m.TimeStamp)).ToArray();
                //var s = result.Select(p => String.Format("{0:0.0000000000000} {1:0.0000000000000} {2:0.0000000000000}", p.Lat, p.Long, p.Altitude)).ToArray();

                //var s = new String[] { minPrice.ToString() };

                System.IO.File.WriteAllLines(filename + ".out", s);
            }
            Console.WriteLine("finished!");
            Console.ReadKey();
        }
    }
}
