using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;

namespace Tools.ConnectionMonitoring
{
    public class SocketConnections
    {
        // The version of IP used by the TCP/UDP endpoint. AF_INET is used for IPv4. 
        private const int AF_INET = 2;
        // List of Active TCP Connections. 
        private static List<TcpProcessRecord> TcpActiveConnections = null;
        // List of Active UDP Connections. 
        private static List<UdpProcessRecord> UdpActiveConnections = null;

        // The GetExtendedTcpTable function retrieves a table that contains a list of 
        // TCP endpoints available to the application. Decorating the function with 
        // DllImport attribute indicates that the attributed method is exposed by an 
        // unmanaged dynamic-link library 'iphlpapi.dll' as a static entry point. 
        [DllImport("iphlpapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern uint GetExtendedTcpTable(IntPtr pTcpTable, ref int pdwSize,
            bool bOrder, int ulAf, TcpTableClass tableClass, uint reserved = 0);

        // The GetExtendedUdpTable function retrieves a table that contains a list of 
        // UDP endpoints available to the application. Decorating the function with 
        // DllImport attribute indicates that the attributed method is exposed by an 
        // unmanaged dynamic-link library 'iphlpapi.dll' as a static entry point. 
        [DllImport("iphlpapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern uint GetExtendedUdpTable(IntPtr pUdpTable, ref int pdwSize,
            bool bOrder, int ulAf, UdpTableClass tableClass, uint reserved = 0);

        /// <summary> 
        /// This function reads and parses the active TCP socket connections available 
        /// and stores them in a list. 
        /// </summary> 
        /// <returns> 
        /// It returns the current set of TCP socket connections which are active. 
        /// </returns> 
        /// <exception cref="OutOfMemoryException"> 
        /// This exception may be thrown by the function Marshal.AllocHGlobal when there 
        /// is insufficient memory to satisfy the request. 
        /// </exception> 
        public static List<TcpProcessRecord> GetAllTcpConnections()
        {
            int bufferSize = 0;
            List<TcpProcessRecord> tcpTableRecords = new List<TcpProcessRecord>();

            // Getting the size of TCP table, that is returned in 'bufferSize' variable. 
            uint result = GetExtendedTcpTable(IntPtr.Zero, ref bufferSize, true, AF_INET,
                TcpTableClass.TCP_TABLE_OWNER_PID_ALL);

            // Allocating memory from the unmanaged memory of the process by using the 
            // specified number of bytes in 'bufferSize' variable. 
            IntPtr tcpTableRecordsPtr = Marshal.AllocHGlobal(bufferSize);

            try
            {
                // The size of the table returned in 'bufferSize' variable in previous 
                // call must be used in this subsequent call to 'GetExtendedTcpTable' 
                // function in order to successfully retrieve the table. 
                result = GetExtendedTcpTable(tcpTableRecordsPtr, ref bufferSize, true,
                    AF_INET, TcpTableClass.TCP_TABLE_OWNER_PID_ALL);

                // Non-zero value represent the function 'GetExtendedTcpTable' failed, 
                // hence empty list is returned to the caller function. 
                if (result != 0)
                    return new List<TcpProcessRecord>();

                // Marshals data from an unmanaged block of memory to a newly allocated 
                // managed object 'tcpRecordsTable' of type 'MIB_TCPTABLE_OWNER_PID' 
                // to get number of entries of the specified TCP table structure. 
                MIB_TCPTABLE_OWNER_PID tcpRecordsTable = (MIB_TCPTABLE_OWNER_PID)
                    Marshal.PtrToStructure(tcpTableRecordsPtr,
                        typeof(MIB_TCPTABLE_OWNER_PID));
                IntPtr tableRowPtr = (IntPtr)((long)tcpTableRecordsPtr +
                                              Marshal.SizeOf(tcpRecordsTable.dwNumEntries));

                // Reading and parsing the TCP records one by one from the table and 
                // storing them in a list of 'TcpProcessRecord' structure type objects. 
                for (int row = 0; row < tcpRecordsTable.dwNumEntries; row++)
                {
                    MIB_TCPROW_OWNER_PID tcpRow = (MIB_TCPROW_OWNER_PID)Marshal.
                        PtrToStructure(tableRowPtr, typeof(MIB_TCPROW_OWNER_PID));
                    tcpTableRecords.Add(new TcpProcessRecord(
                        new IPAddress(tcpRow.localAddr),
                        new IPAddress(tcpRow.remoteAddr),
                        BitConverter.ToUInt16(new byte[2] {
                            tcpRow.localPort[1],
                            tcpRow.localPort[0] }, 0),
                        BitConverter.ToUInt16(new byte[2] {
                            tcpRow.remotePort[1],
                            tcpRow.remotePort[0] }, 0),
                        tcpRow.owningPid, tcpRow.state));
                    tableRowPtr = (IntPtr)((long)tableRowPtr + Marshal.SizeOf(tcpRow));
                }
            }
            catch (OutOfMemoryException outOfMemoryException)
            {
                Console.Error.WriteLine(outOfMemoryException.Message);
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception.Message);
            }
            finally
            {
                Marshal.FreeHGlobal(tcpTableRecordsPtr);
            }
            return tcpTableRecords != null ? tcpTableRecords.Distinct()
                .ToList<TcpProcessRecord>() : new List<TcpProcessRecord>();
        }

        /// <summary> 
        /// This function reads and parses the active UDP socket connections available 
        /// and stores them in a list. 
        /// </summary> 
        /// <returns> 
        /// It returns the current set of UDP socket connections which are active. 
        /// </returns> 
        /// <exception cref="OutOfMemoryException"> 
        /// This exception may be thrown by the function Marshal.AllocHGlobal when there 
        /// is insufficient memory to satisfy the request. 
        /// </exception> 
        private static List<UdpProcessRecord> GetAllUdpConnections()
        {
            int bufferSize = 0;
            List<UdpProcessRecord> udpTableRecords = new List<UdpProcessRecord>();

            // Getting the size of UDP table, that is returned in 'bufferSize' variable. 
            uint result = GetExtendedUdpTable(IntPtr.Zero, ref bufferSize, true,
                AF_INET, UdpTableClass.UDP_TABLE_OWNER_PID);

            // Allocating memory from the unmanaged memory of the process by using the 
            // specified number of bytes in 'bufferSize' variable. 
            IntPtr udpTableRecordPtr = Marshal.AllocHGlobal(bufferSize);

            try
            {
                // The size of the table returned in 'bufferSize' variable in previous 
                // call must be used in this subsequent call to 'GetExtendedUdpTable' 
                // function in order to successfully retrieve the table. 
                result = GetExtendedUdpTable(udpTableRecordPtr, ref bufferSize, true,
                    AF_INET, UdpTableClass.UDP_TABLE_OWNER_PID);

                // Non-zero value represent the function 'GetExtendedUdpTable' failed, 
                // hence empty list is returned to the caller function. 
                if (result != 0)
                    return new List<UdpProcessRecord>();

                // Marshals data from an unmanaged block of memory to a newly allocated 
                // managed object 'udpRecordsTable' of type 'MIB_UDPTABLE_OWNER_PID' 
                // to get number of entries of the specified TCP table structure. 
                MIB_UDPTABLE_OWNER_PID udpRecordsTable = (MIB_UDPTABLE_OWNER_PID)
                    Marshal.PtrToStructure(udpTableRecordPtr, typeof(MIB_UDPTABLE_OWNER_PID));
                IntPtr tableRowPtr = (IntPtr)((long)udpTableRecordPtr +
                                              Marshal.SizeOf(udpRecordsTable.dwNumEntries));

                // Reading and parsing the UDP records one by one from the table and 
                // storing them in a list of 'UdpProcessRecord' structure type objects. 
                for (int i = 0; i < udpRecordsTable.dwNumEntries; i++)
                {
                    MIB_UDPROW_OWNER_PID udpRow = (MIB_UDPROW_OWNER_PID)
                        Marshal.PtrToStructure(tableRowPtr, typeof(MIB_UDPROW_OWNER_PID));
                    udpTableRecords.Add(new UdpProcessRecord(new IPAddress(udpRow.localAddr),
                        BitConverter.ToUInt16(new byte[2] { udpRow.localPort[1],
                            udpRow.localPort[0] }, 0), udpRow.owningPid));
                    tableRowPtr = (IntPtr)((long)tableRowPtr + Marshal.SizeOf(udpRow));
                }
            }
            catch (OutOfMemoryException outOfMemoryException)
            {
                Console.Error.WriteLine(outOfMemoryException.Message);
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception.Message);
            }
            finally
            {
                Marshal.FreeHGlobal(udpTableRecordPtr);
            }
            return udpTableRecords != null ? udpTableRecords.Distinct()
                .ToList<UdpProcessRecord>() : new List<UdpProcessRecord>();
        }
    }
}