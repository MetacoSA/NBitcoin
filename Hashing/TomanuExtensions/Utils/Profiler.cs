using System;
using System.Diagnostics;
using System.Threading;

namespace TomanuExtensions.Utils
{
    public class Profiler
    {
        public static long Profile(Action a_action, int a_tries = 5, int a_action_repeats = 1, bool a_boost = true)
        {
            IntPtr old_aff = Process.GetCurrentProcess().ProcessorAffinity;
            ProcessPriorityClass old_proc_prior = Process.GetCurrentProcess().PriorityClass;
            ThreadPriority old_thread_prio = Thread.CurrentThread.Priority;

            if (a_boost)
            {
                Process.GetCurrentProcess().ProcessorAffinity = new IntPtr(1);
                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.RealTime;
                Thread.CurrentThread.Priority = ThreadPriority.Highest;
            }

            long result = long.MaxValue;
            Stopwatch sw = new Stopwatch();

            try
            {
                for (int i = 0; i < a_tries; i++)
                {
                    sw.Restart();

                    for (int j = 0; j < a_action_repeats; j++)
                        a_action();

                    sw.Stop();

                    if (sw.ElapsedMilliseconds < result)
                        result = sw.ElapsedMilliseconds;
                }
            }
            finally
            {
                if (a_boost)
                {
                    Process.GetCurrentProcess().ProcessorAffinity = old_aff;
                    Process.GetCurrentProcess().PriorityClass = old_proc_prior;
                    Thread.CurrentThread.Priority = old_thread_prio;
                }
            }

            return result / a_action_repeats;
        }
    }
}