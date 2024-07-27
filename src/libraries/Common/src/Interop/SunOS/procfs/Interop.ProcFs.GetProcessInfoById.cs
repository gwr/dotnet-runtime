// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.IO;

internal static partial class Interop
{
    internal static partial class @procfs
    {

        /// <summary>
        /// Attempts to get status info for the specified process ID.
        /// </summary>
        /// <param name="pid">PID of the process to read status info for.</param>
        /// <param name="result">The pointer to ProcessInfo instance.</param>
        /// <returns>
        /// true if the process status was read; otherwise, false.
        /// </returns>

        // ProcessManager.SunOS.cs calls this
        // "unsafe" due to use of fixed-size buffers

        internal static unsafe bool GetProcessInfoById(int pid, out ProcessInfo result)
        {
            result = default;
            bool ret = false;
            string fileName = "?";
            IntPtr ptr = 0;

            try
            {
                fileName = GetInfoFilePathForProcess(pid);
                int size = Marshal.SizeOf<psinfo>();
                ptr = Marshal.AllocHGlobal(size);

                BinaryReader br = new BinaryReader(File.OpenRead(fileName));
                byte[] buf = br.ReadBytes(size);
                Marshal.Copy(buf, 0, ptr, size);

                procfs.psinfo pr = Marshal.PtrToStructure<psinfo>(ptr);

                result.Pid = pr.pr_pid;
                result.ParentPid = pr.pr_ppid;
                result.SessionId = pr.pr_sid;
                result.VirtualSize = (nuint)pr.pr_size * 1024; // pr_rssize is in Kbytes
                result.ResidentSetSize = (nuint)pr.pr_rssize * 1024; // pr_rssize is in Kbytes
                result.StartTime.TvSec = pr.pr_start.tv_sec;
                result.StartTime.TvNsec = pr.pr_start.tv_nsec;
                result.CpuTotalTime.TvSec = pr.pr_time.tv_sec;
                result.CpuTotalTime.TvNsec = pr.pr_time.tv_nsec;
                result.Args = Marshal.PtrToStringUTF8((IntPtr)pr.pr_psargs);

                // We get LWP[1] for "free"
                result.Lwp1.Tid = pr.pr_lwp.pr_lwpid;
                result.Lwp1.Priority = pr.pr_lwp.pr_pri;
                result.Lwp1.NiceVal = (int)pr.pr_lwp.pr_nice;
                result.Lwp1.Status = (char)pr.pr_lwp.pr_sname;

                ret = true;
            }
            catch (Exception e)
            {
                Debug.Fail($"Failed to read \"{fileName}\": {e}");
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }

            return ret;
        }

    }
}
