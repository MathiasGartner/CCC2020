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


    class Program
    {
        static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("en-GB");

            //var filenames = Enumerable.Range(1, 5).Select(p => "..\\..\\..\\data\\level5_" + p + ".in").ToList();
            var filenames = new List<String>() { "..\\..\\..\\data\\test.in" };
            List<String> outputText = new List<String>();
            foreach (var filename in filenames)
            {
                Console.WriteLine(filename);
                string[] lines = System.IO.File.ReadAllLines(filename);
                int[] props = lines[0].Split(',').Select(p => p.AsInt()).ToArray();
                int N = lines[1].AsInt();

                //var s = flightDetails.Where(p => p.Contacts.Count > 0).SelectMany(p => p.Contacts.Select(m => p.Id + " " + m.ContactToFlightId + " " + m.Delay + " " + m.TimeStamp)).ToArray();
                //var s = result.Select(p => String.Format("{0:0.0000000000000} {1:0.0000000000000} {2:0.0000000000000}", p.Lat, p.Long, p.Altitude)).ToArray();
                var s = new String[] { "line1", "line2" };
                System.IO.File.WriteAllLines(filename + ".out", s);
            }
            Console.WriteLine("finished!");
            Console.ReadKey();
        }
    }
}
