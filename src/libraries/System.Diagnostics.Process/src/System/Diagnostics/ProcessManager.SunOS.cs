// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;

// TODO: remove or scope to just the methods that need them
#pragma warning disable CA1822
#pragma warning disable IDE0060

namespace System.Diagnostics
{
    internal static partial class ProcessManager
    {
        /// <summary>Gets the IDs of all processes on the current machine.</summary>
        public static int[] GetProcessIds()
        {
            IEnumerable<int> pids = EnumerateProcessIds();
            return new List<int>(pids).ToArray();
        }

        /// <summary>Gets process infos for each process on the specified machine.</summary>
        /// <param name="processNameFilter">Optional process name to use as an inclusion filter.</param>
        /// <param name="machineName">The target machine.</param>
        /// <returns>An array of process infos, one per found process.</returns>
        public static ProcessInfo[] GetProcessInfos(string? processNameFilter, string machineName)
        {
            ThrowIfRemoteMachine(machineName);

            // Iterate through all process IDs to load information about each process
            IEnumerable<int> pids = EnumerateProcessIds();
            ArrayBuilder<ProcessInfo> processes = default;
            foreach (int pid in pids)
            {
                ProcessInfo? pi = CreateProcessInfo(pid, processNameFilter);
                if (pi != null)
                {
                    processes.Add(pi);
                }
            }

            return processes.ToArray();
        }

        /// <summary>Gets an array of module infos for the specified process.</summary>
        /// <param name="processId">The ID of the process whose modules should be enumerated.</param>
        /// <returns>The array of modules.</returns>
        internal static ProcessModuleCollection GetModules(int processId)
        {
            // GetModules(x)[0].FileName is often used to find the path to the executable, so at least
            // get that.
            // TODO: is there better way to get loaded modules?
            if (Interop.procfs.TryReadProcessStatusInfo(processId, out Interop.procfs.ProcessStatusInfo _, out string? shortProcessName))
            {
                string fullName = Process.GetUntruncatedProcessName(processId, shortProcessName);
                if (!string.IsNullOrEmpty(fullName))
                {
                    return new ProcessModuleCollection(1)
                    {
                        new ProcessModule(fullName, Path.GetFileName(fullName))
                    };
                }
            }
            return new ProcessModuleCollection(0);
        }

        /// <summary>
        /// Creates a ProcessInfo from the specified process ID.
        /// </summary>
        internal static ProcessInfo? CreateProcessInfo(int pid, string? processNameFilter = null)
        {
            // Negative PIDs aren't valid
            ArgumentOutOfRangeException.ThrowIfNegative(pid);

            // Could return null if the filter does not match.
            Debug.Assert(processNameFilter is null, "Not used on Linux");

            Interop.procfs.ProcessStatusInfo iinfo;
            string? procName;

            if (! Interop.procfs.TryReadProcessStatusInfo(pid, out iinfo, out procName))
            {
                return null;
            }

            // XXX Todo: filter by process like like OSX?
            // If filter specified and no match, return null
            return iCreateProcessInfo(ref iinfo, shortProcessName: procName);
        }

        /// <summary>
        /// Creates a ProcessInfo from the data parsed from a /proc/pid/psinfo file and the associated lwp directory.
        /// </summary>
        internal static ProcessInfo iCreateProcessInfo(ref Interop.procfs.ProcessStatusInfo iinfo,
                           string shortProcessName, string? fullProcessName = null)
        {
            int pid = iinfo.Pid;

            var pi = new ProcessInfo()
            {
                ProcessId = pid,

                // TODO: get BasePriority from lwp?
                ProcessName = fullProcessName ?? Process.GetUntruncatedProcessName(pid, shortProcessName) ?? string.Empty,

                // VirtualBytes = iinfo.VirtualBytes,
                WorkingSet = (long)iinfo.ResidentSetSize,
                SessionId = iinfo.SessionId,
            };

            // TODO: translate LWP to thread

            return pi;
        }

        // ----------------------------------
        // ---- Unix PAL layer ends here ----
        // ----------------------------------

        /// <summary>Enumerates the IDs of all processes on the current machine.</summary>
        internal static IEnumerable<int> EnumerateProcessIds()
        {
            // Parse /proc for any directory that's named with a number.  Each such
            // directory represents a process.
            foreach (string procDir in Directory.EnumerateDirectories(Interop.procfs.RootPath))
            {
                string dirName = Path.GetFileName(procDir);
                int pid;
                if (int.TryParse(dirName, NumberStyles.Integer, CultureInfo.InvariantCulture, out pid))
                {
                    Debug.Assert(pid >= 0);
                    yield return pid;
                }
            }
        }
    }
}
