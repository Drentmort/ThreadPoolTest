using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TimerConsoleTest
{
    internal class Program
    {
        private static ManualResetEvent @event = new ManualResetEvent(false);

        private static double summ = 0;
        private static double count = 0;

        static void Main(string[] args)
        {
            WriteEmumChoiseToConsole<PollType>("Enter poll type");
            var pollType = ReadEmumChoiseFromConsole<PollType>();

            WriteEmumChoiseToConsole<SyncObjType>("Enter sych object type");
            var syncObjType = ReadEmumChoiseFromConsole<SyncObjType>();

            WriteEmumChoiseToConsole<SyncObjLocalization>("Enter sych object location");
            var syncObjLocalization = ReadEmumChoiseFromConsole<SyncObjLocalization>();

            var threadCount = WriteAndReadUInt32GetToConsole("Enter thread count", 1000);
            
            var waitMillisec = WriteAndReadUInt32GetToConsole("Enter poll step in milliseconds", 500);

            var workImmitationTime = WriteAndReadUInt32GetToConsole("Enter imitation work time in milliseconds", 10);

            Console.WriteLine("Every ready, enter any symbol to start..");
            Console.ReadLine();

            for (int i = 0; i < threadCount; i++)
            {
                var Poller = new PollingClass();
                Poller.PollLockHandler += time =>
                {
                    Console.WriteLine($"{DateTime.Now} call takes {time.TotalMilliseconds} milliseconds");
                    summ += time.TotalMilliseconds;
                    count++;
                    Console.WriteLine($"{DateTime.Now} average {summ / count} milliseconds");

                };
                Poller.Start(pollType, syncObjType, syncObjLocalization, (int)waitMillisec, (int)workImmitationTime);
            }          
            @event.WaitOne();
        }

        private static uint WriteAndReadUInt32GetToConsole(string calling, uint defaultVal)
        {
            Console.WriteLine($"{calling} (default {defaultVal})");
            if (!uint.TryParse(Console.ReadLine(), out var output))
            {
                output = defaultVal;
            }
            return output;
        }
        
        private static void WriteEmumChoiseToConsole<TEnum>(string calling)
            where TEnum : struct
        {
            Console.WriteLine(calling);
            foreach (var v in Enum.GetValues(typeof(TEnum)))
            {
                Console.WriteLine($"{v} - {(int)v}");
            }
        }

        private static TEnum ReadEmumChoiseFromConsole<TEnum>()
            where TEnum : struct
        {
            var input = Console.ReadLine();
            if (int.TryParse(input, out var outputNum) && Enum.IsDefined(typeof(TEnum), outputNum))
            { 
                return (TEnum)Enum.ToObject(typeof(TEnum), outputNum);
            }
            else
            {
                return default;
            }
        }
    }

    
}
