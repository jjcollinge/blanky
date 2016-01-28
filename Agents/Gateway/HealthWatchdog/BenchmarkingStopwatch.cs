using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace HealthWatchdog
{
    // Reuseable Stopwatch wrapper
    public class BenchmarkStopwatch : IDisposable
    {
        public ILogger logger;
        private Stopwatch stopwatch;
        private string name;

        
        public static BenchmarkStopwatch Start(string name, ILogger logger, [CallerMemberName] string memberName = "")
        {
            
            return new BenchmarkStopwatch(name + "::" + memberName, logger);
        }


        private BenchmarkStopwatch(string instanceName, ILogger logger)
        {
            this.logger = logger;
            name = instanceName;
            stopwatch = new Stopwatch();
            stopwatch.Start();
        }

        #region IDisposable implementation

        // dispose stops stopwatch and prints time, could do anytying here
        public void Dispose()
        {
            stopwatch.Stop();
            logger.LogTimedEvent(stopwatch.Elapsed.TotalSeconds, name);

            var message = string.Format("{0} Total seconds: {1}", name, stopwatch.Elapsed.TotalSeconds);
            

#if DEBUG
            Console.WriteLine(message);
#endif
        }



        #endregion
    }
}
