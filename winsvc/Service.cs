﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using frogmore.winsvc.Enumerations;
using frogmore.winsvc.Flags;
using frogmore.winsvc.Structs;
using Microsoft.Win32.SafeHandles;

namespace frogmore.winsvc
{
    internal sealed class Service : SafeHandleZeroOrMinusOneIsInvalid, IService
    {
        // ReSharper disable InconsistentNaming
        private const int ERROR_INSUFFICIENT_BUFFER = 122;
        private const int SC_STATUS_PROCESS_INFO = 0;
        private const int SERVICE_CONTROL_STATUS_REASON_INFO = 1;
        // ReSharper restore InconsistentNaming

        public Service(IntPtr serviceHandle) : base(true)
        {
            handle = serviceHandle;
        }

        protected override bool ReleaseHandle()
        {
            return NativeMethods.CloseServiceHandle(handle);
        }

        public void Delete()
        {
            if (!NativeMethods.DeleteService(handle))
            {
                throw new Win32Exception();
            }
        }

        public void Start()
        {
            Start(new string[] {});
        }

        public void Start(string[] serviceArgVectors)
        {
            if (!NativeMethods.StartService(handle, serviceArgVectors.Length, serviceArgVectors))
            {
                throw new Win32Exception();
            }
        }

        public void Control(SERVICE_CONTROL control)
        {
            var status = new SERVICE_STATUS();

            if (!NativeMethods.ControlService(handle, control, ref status))
            {
                throw new Win32Exception();
            }
        }

        public void ControlEx(SERVICE_CONTROL control, ref SERVICE_CONTROL_STATUS_REASON_PARAMS reason)
        {
            if (!NativeMethods.ControlServiceEx(handle, control, SERVICE_CONTROL_STATUS_REASON_INFO, ref reason))
            {
                throw new Win32Exception();
            }
        }

        public QUERY_SERVICE_CONFIG QueryConfig()
        {
            int needed = 0;
            if (NativeMethods.QueryServiceConfig(handle, IntPtr.Zero, 0, ref needed))
            {
                throw new Win32Exception(0, $"Unexpected success in {nameof(Service)}.{nameof(QueryConfig)}");
            }

            if (Marshal.GetLastWin32Error() != ERROR_INSUFFICIENT_BUFFER)
            {
                throw new Win32Exception();
            }

            var bufferPtr = Marshal.AllocHGlobal(needed);
            try
            {
                if (!NativeMethods.QueryServiceConfig(handle, bufferPtr, needed, ref needed))
                {
                    throw new Win32Exception();
                }

                var privateConfig = (QUERY_SERVICE_CONFIG_PRIVATE) Marshal.PtrToStructure(bufferPtr, typeof(QUERY_SERVICE_CONFIG_PRIVATE));
                return new QUERY_SERVICE_CONFIG
                {
                    BinaryPathName = privateConfig.BinaryPathName,
                    Dependencies = privateConfig.Dependencies.Split('\0'),
                    DisplayName = privateConfig.DisplayName,
                    ErrorControl = privateConfig.ErrorControl,
                    LoadOrderGroup = privateConfig.LoadOrderGroup,
                    ServiceStartName = privateConfig.ServiceStartName,
                    ServiceType = privateConfig.ServiceType,
                    StartType = privateConfig.StartType,
                    TagId = privateConfig.TagId,
                };
            }
            finally
            {
                Marshal.FreeHGlobal(bufferPtr);
            }
        }

        public void ChangeConfig(
            SERVICE_TYPE serviceType, 
            SERVICE_START_TYPE startType, 
            SERVICE_ERROR_CONTROL errorControl, 
            string binaryPathName, 
            string loadOrderGroup, 
            IntPtr tagId, 
            IEnumerable<string> dependencies,
            string serviceStartName, 
            string password, 
            string displayName)
        {
            var dependenciesString = dependencies == null ? null : string.Join("\0", dependencies) + "\0"; // Collection of strings separated by null and terminated by double null

            if (!NativeMethods.ChangeServiceConfig(
                    handle, 
                    (uint) serviceType, 
                    (uint) startType, 
                    (uint) errorControl, 
                    binaryPathName,
                    loadOrderGroup, 
                    tagId, 
                    dependenciesString,
                    serviceStartName,
                    password,
                    displayName))
            {
                throw new Win32Exception();
            }
        }

        public void ChangeConfig2<T>(ref T info)
        {
            var level = (int) GetLevel<T>();
            IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(info));

            try
            {
                Marshal.StructureToPtr(info, ptr, false);
                if (!NativeMethods.ChangeServiceConfig2(handle, level, ptr))
                {
                    throw new Win32Exception();
                }
            }
            finally
            {
                Marshal.DestroyStructure(ptr, typeof(T)); // Clean up any strings
                Marshal.FreeHGlobal(ptr);
            }
        }

        public SERVICE_STATUS QueryStatus()
        {
            var status = new SERVICE_STATUS();
            if (!NativeMethods.QueryServiceStatus(handle, ref status))
            {
                throw new Win32Exception();
            }

            return status;
        }

        public SERVICE_STATUS_PROCESS QueryStatusEx()
        {
            int needed = 0;

            if (NativeMethods.QueryServiceStatusEx(handle, SC_STATUS_PROCESS_INFO, IntPtr.Zero, 0, ref needed))
            {
                throw new Win32Exception(0, $"Unexpected success in {nameof(Service)}.{nameof(QueryStatusEx)}");
            }

            // We expect an ERROR_MORE_DATA error as the buffer size passed in was zero, otherwise something strage is going on
            if (Marshal.GetLastWin32Error() != ERROR_INSUFFICIENT_BUFFER)
            {
                throw new Win32Exception();
            }

            IntPtr bufferPtr = Marshal.AllocHGlobal(needed);
            var ptr = bufferPtr;
            try
            {
                if (!NativeMethods.QueryServiceStatusEx(handle, SC_STATUS_PROCESS_INFO, bufferPtr, needed, ref needed))
                {
                    throw new Win32Exception();
                }

                return (SERVICE_STATUS_PROCESS) Marshal.PtrToStructure(ptr, typeof(SERVICE_STATUS_PROCESS));
            }
            finally
            {
                Marshal.FreeHGlobal(bufferPtr);
            }

        }

        public IEnumerable<ENUM_SERVICE_STATUS> EnumDependentServices(SERVICE_STATE_FLAGS states)
        {
            // ReSharper disable once InconsistentNaming
            const int ERROR_MORE_DATA = 234;

            int needed = 0;
            int servicesReturned = 0;

            if (NativeMethods.EnumDependentServices(handle, states, IntPtr.Zero, 0, ref needed, ref servicesReturned))
            {
                yield break; // No dependent services
            }

            // We expect an ERROR_MORE_DATA error as the buffer size passed in was zero, otherwise something strage is going on
            if (Marshal.GetLastWin32Error() != ERROR_MORE_DATA)
            {
                throw new Win32Exception();
            }

            IntPtr bufferPtr = Marshal.AllocHGlobal(needed);
            var ptr = bufferPtr;
            try
            {
                if (!NativeMethods.EnumDependentServices(handle, states, bufferPtr, needed, ref needed, ref servicesReturned))
                {
                    throw new Win32Exception();
                }

                for (int i = 0; i < servicesReturned; i++)
                {
                    yield return (ENUM_SERVICE_STATUS)Marshal.PtrToStructure(ptr, typeof(ENUM_SERVICE_STATUS));
                    ptr += Marshal.SizeOf(typeof(ENUM_SERVICE_STATUS));
                }
            }
            finally
            {
                Marshal.FreeHGlobal(bufferPtr);
            }
        }

        public T QueryConfig2<T>()
        {
            var level = GetLevel<T>();
            int needed = 0;

            if (NativeMethods.QueryServiceConfig2(handle, (uint) level, IntPtr.Zero, 0, ref needed))
            {
                throw new Win32Exception(0, $"Unexpected success in {nameof(Service)}.{nameof(QueryConfig2)}");
            }

            if (Marshal.GetLastWin32Error() != ERROR_INSUFFICIENT_BUFFER)
            {
                throw new Win32Exception();
            }

            var bufferPtr = Marshal.AllocHGlobal(needed);
            try
            {
                if (!NativeMethods.QueryServiceConfig2(handle, (uint) level, bufferPtr, needed, ref needed))
                {
                    throw new Win32Exception();
                }

                return (T)Marshal.PtrToStructure(bufferPtr, typeof(T));
            }
            finally
            {
                Marshal.FreeHGlobal(bufferPtr);
            }
        }

        private static SERVICE_CONFIG GetLevel<T>()
        {
            if (typeof(T).IsAssignableFrom(typeof(SERVICE_DESCRIPTION)) )
            {
                return SERVICE_CONFIG.SERVICE_CONFIG_DESCRIPTION;
            }

            throw new ApplicationException($"Unrecognised type {typeof(T)}");
        }
    }
}