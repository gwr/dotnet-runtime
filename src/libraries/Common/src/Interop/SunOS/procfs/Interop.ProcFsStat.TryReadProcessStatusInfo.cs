// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.IO;

// The const int PRARGSZ show up as unused.  Not sure why.
#pragma warning disable CA1823

internal static partial class Interop
{
    internal static partial class @procfs
    {
        internal const string RootPath = "/proc/";
        private const string psinfoFileName = "/psinfo";

        // Constants from sys/procfs.h
        private const int PRARGSZ = 80;
        private const int PRCLSZ = 8;
        private const int PRFNSZ = 16;

        [StructLayout(LayoutKind.Sequential)]
        internal struct @timestruc_t
        {
            public long tv_sec;
            public long tv_nsec;
        }

        // lwp ps(1) information file.  /proc/<pid>/lwp/<lwpid>/lwpsinfo
        // "unsafe" because it has fixed sized arrays.
        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct @lwpsinfo
        {
            private     int     pr_flag;        /* lwp flags (DEPRECATED; do not use) */
            public      uint    pr_lwpid;       /* lwp id */
            private     long    pr_addr;        /* internal address of lwp */
            private     long    pr_wchan;       /* wait addr for sleeping lwp */
            public      byte    pr_stype;       /* synchronization event type */
            public      byte    pr_state;       /* numeric lwp state */
            public      byte    pr_sname;       /* printable character for pr_state */
            public      byte    pr_nice;        /* nice for cpu usage */
            private     short   pr_syscall;     /* system call number (if in syscall) */
            private     byte    pr_oldpri;      /* pre-SVR4, low value is high priority */
            private     byte    pr_cpu;         /* pre-SVR4, cpu usage for scheduling */
            public      int     pr_pri;         /* priority, high value is high priority */
            private     ushort  pr_pctcpu;      /* fixed pt. % of recent cpu time */
            private     ushort  pr_pad;
            public      timestruc_t pr_start;   /* lwp start time, from the epoch */
            public      timestruc_t pr_time;    /* usr+sys cpu time for this lwp */
            private     fixed byte pr_clname[PRCLSZ];   /* scheduling class name */
            private     fixed byte pr_name[PRFNSZ];     /* name of system lwp */
            private     int     pr_onpro;               /* processor which last ran this lwp */
            private     int     pr_bindpro;     /* processor to which lwp is bound */
            private     int     pr_bindpset;    /* processor set to which lwp is bound */
            private     int     pr_lgrp;        /* lwp home lgroup */
            private     fixed int       pr_filler[4];   /* reserved for future use */
        }

        // process ps(1) information file.  /proc/<pid>/psinfo
        // "unsafe" because it has fixed sized arrays.
        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct @psinfo
        {
            private     int     pr_flag;        /* process flags (DEPRECATED; do not use) */
            public      int     pr_nlwp;        /* number of active lwps in the process */
            public      int     pr_pid;         /* unique process id */
            public      int     pr_ppid;        /* process id of parent */
            public      int     pr_pgid;        /* pid of process group leader */
            public      int     pr_sid;         /* session id */
            public      uint    pr_uid;         /* real user id */
            public      uint    pr_euid;        /* effective user id */
            public      uint    pr_gid;         /* real group id */
            public      uint    pr_egid;        /* effective group id */
            private     long    pr_addr;        /* address of process */
            public      ulong   pr_size;        /* size of process image in Kbytes */
            public      ulong   pr_rssize;      /* resident set size in Kbytes */
            private     ulong   pr_pad1;
            private     ulong   pr_ttydev;      /* controlling tty device (or PRNODEV) */
            private     ushort  pr_pctcpu;      /* % of recent cpu time used by all lwps */
            private     ushort  pr_pctmem;      /* % of system memory used by process */
            public      timestruc_t pr_start;   /* process start time, from the epoch */
            public      timestruc_t pr_time;    /* usr+sys cpu time for this process */
            public      timestruc_t pr_ctime;   /* usr+sys cpu time for reaped children */
            public      fixed byte pr_fname[PRFNSZ];    /* name of execed file */
            public      fixed byte pr_psargs[PRARGSZ];  /* initial characters of arg list */
            public      int     pr_wstat;       /* if zombie, the wait() status */
            public      int     pr_argc;        /* initial argument count */
            private     long pr_argv;   /* address of initial argument vector */
            private     long pr_envp;   /* address of initial environment vector */
            private     byte    pr_dmodel;      /* data model of the process */
            private     fixed byte pr_pad2[3];
            public      int     pr_taskid;      /* task id */
            public      int     pr_projid;      /* project id */
            public      int     pr_nzomb;       /* number of zombie lwps in the process */
            public      int     pr_poolid;      /* pool id */
            public      int     pr_zoneid;      /* zone id */
            public      int     pr_contract;    /* process contract */
            private     fixed int pr_filler[1]; /* reserved for future use */
            public      lwpsinfo pr_lwp;        /* information for representative lwp */
        }

        // Ouput type for TryReadProcessStatusInfo()
        internal struct ProcessStatusInfo
        {
            internal int Pid;
            internal int ParentPid;
            internal int SessionId;
            internal int Priority;
            internal nuint VirtualSize;
            internal nuint ResidentSetSize;
            internal Interop.Sys.TimeSpec StartTime;
            internal string? Args;
            // add more fields when needed.
        }

        internal static string GetInfoFilePathForProcess(int pid) =>
            string.Create(null, stackalloc char[256], $"{RootPath}{(uint)pid}{psinfoFileName}");

        /// <summary>
        /// Attempts to get status info for the specified process ID.
        /// </summary>
        /// <param name="pid">PID of the process to read status info for.</param>
        /// <param name="result">The pointer to processStatusInfo instance.</param>
        /// <returns>
        /// true if the process status was read; otherwise, false.
        /// </returns>

        // ProcessManager.SunOS.cs calls this
        // "unsafe" due to use of fixed-size buffers

        internal static unsafe bool TryReadProcessStatusInfo(int pid, out ProcessStatusInfo result)
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
                result.Priority = pr.pr_lwp.pr_pri;
                result.VirtualSize = (nuint)pr.pr_size * 1024; // pr_rssize is in Kbytes
                result.ResidentSetSize = (nuint)pr.pr_rssize * 1024; // pr_rssize is in Kbytes
                result.StartTime.TvSec = pr.pr_start.tv_sec;
                result.StartTime.TvNsec = pr.pr_start.tv_nsec;
                result.Args = Marshal.PtrToStringUTF8((IntPtr)pr.pr_psargs);

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
