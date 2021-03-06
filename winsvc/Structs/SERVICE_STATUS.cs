﻿using System.Runtime.InteropServices;
using frogmore.winsvc.Enumerations;
using frogmore.winsvc.Flags;

// ReSharper disable InconsistentNaming
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable FieldCanBeMadeReadOnly.Global

namespace frogmore.winsvc.Structs
{
    [StructLayout(LayoutKind.Sequential, Pack = 0, CharSet = CharSet.Unicode)]
    public struct SERVICE_STATUS
    {
        public SERVICE_TYPE dwServiceType;
        public SERVICE_STATE dwCurrentState;
        public uint dwControlsAccepted;
        public uint dwWin32ExitCode;
        public uint dwServiceSpecificExitCode;
        public uint dwCheckPoint;
        public uint dwWaitHint;
    }
}
