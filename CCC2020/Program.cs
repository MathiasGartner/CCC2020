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
    }

    public class Task
    {
        public int Id { get; set; }
        public int CompletionTime { get; set; }

        public int OptimumStartMinute { get; set; }

        public int Power { get; set; }
        public int StartInterval { get; set; }
        public int EndInterval { get; set; }
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
                sums.Add(Tuple.Create<int,int>(sum, 0));
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
    }

    class Program
    {
        static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("en-GB");
            
            var filenames = Enumerable.Range(1, 5).Select(p => "..\\..\\..\\data\\level3_" + p + ".in").ToList();
            //var filenames = new List<String>() { "..\\..\\..\\data\\level3_example.in" };
            List<String> outputText = new List<String>();

            foreach (var filename in filenames)
            {
                Console.WriteLine(filename);
                string[] lines = System.IO.File.ReadAllLines(filename);
                //int[] props = lines[0].Split(',').Select(p => p.AsInt()).ToArray();
                int maxPower = lines[0].AsInt();
                int maxBill = lines[1].AsInt();
                int N = lines[2].AsInt();

                List<Price> prices = new List<Price>();
                List<Task> tasks = new List<Task>();

                for (int i = 0; i < N; i++)
                {
                    prices.Add(new Price() { Cost = lines[i + 1 + 2].AsInt(), Minute = i });
                }

                int M = lines[N + 1].AsInt();
                for (int i = 0; i < M; i++)
                {
                    var data = lines[i + N + 2 + 2].Split(' ').Select(p => p.AsInt()).ToArray();
                    //tasks.Add(new Task() { Id = data[0], CompletionTime = data[1] });
                    tasks.Add(new Task() { Id = data[0], Power = data[1], StartInterval = data[2], EndInterval = data[3] });
                }

                //Level4

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
                //var s = new String[] { "" };

                System.IO.File.WriteAllLines(filename + ".out", s);
            }
            Console.WriteLine("finished!");
            Console.ReadKey();
        }
    }
}
