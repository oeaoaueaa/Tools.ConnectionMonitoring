using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;

namespace Tools.ConnectionMonitoring
{
    // Enum for protocol types. 
    public enum Protocol
    {
        TCP,
        UDP
    }

    // Enum to define the set of values used to indicate the type of table returned by  
    // calls made to the function 'GetExtendedTcpTable'. 
    public enum TcpTableClass
    {
        TCP_TABLE_BASIC_LISTENER,
        TCP_TABLE_BASIC_CONNECTIONS,
        TCP_TABLE_BASIC_ALL,
        TCP_TABLE_OWNER_PID_LISTENER,
        TCP_TABLE_OWNER_PID_CONNECTIONS,
        TCP_TABLE_OWNER_PID_ALL,
        TCP_TABLE_OWNER_MODULE_LISTENER,
        TCP_TABLE_OWNER_MODULE_CONNECTIONS,
        TCP_TABLE_OWNER_MODULE_ALL
    }

    // Enum to define the set of values used to indicate the type of table returned by calls 
    // made to the function GetExtendedUdpTable. 
    public enum UdpTableClass
    {
        UDP_TABLE_BASIC,
        UDP_TABLE_OWNER_PID,
        UDP_TABLE_OWNER_MODULE
    }

    // Enum for different possible states of TCP connection 
    public enum MibTcpState
    {
        CLOSED = 1,
        LISTENING = 2,
        SYN_SENT = 3,
        SYN_RCVD = 4,
        ESTABLISHED = 5,
        FIN_WAIT1 = 6,
        FIN_WAIT2 = 7,
        CLOSE_WAIT = 8,
        CLOSING = 9,
        LAST_ACK = 10,
        TIME_WAIT = 11,
        DELETE_TCB = 12,
        NONE = 0
    }

    /// <summary> 
    /// The structure contains information that describes an IPv4 TCP connection with  
    /// IPv4 addresses, ports used by the TCP connection, and the specific process ID 
    /// (PID) associated with connection. 
    /// </summary> 
    [StructLayout(LayoutKind.Sequential)]
    public struct MIB_TCPROW_OWNER_PID
    {
        public MibTcpState state;
        public uint localAddr;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] localPort;
        public uint remoteAddr;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] remotePort;
        public int owningPid;
    }

    /// <summary> 
    /// The structure contains a table of process IDs (PIDs) and the IPv4 TCP links that  
    /// are context bound to these PIDs. 
    /// </summary> 
    [StructLayout(LayoutKind.Sequential)]
    public struct MIB_TCPTABLE_OWNER_PID
    {
        public uint dwNumEntries;
        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.Struct,
            SizeConst = 1)]
        public MIB_TCPROW_OWNER_PID[] table;
    }

    /// <summary> 
    /// This class provides access an IPv4 TCP connection addresses and ports and its 
    /// associated Process IDs and names. 
    /// </summary> 
    [StructLayout(LayoutKind.Sequential)]
    public class TcpProcessRecord
    {
        [DisplayName("Local Address")]
        public IPAddress LocalAddress { get; set; }
        [DisplayName("Local Port")]
        public ushort LocalPort { get; set; }
        [DisplayName("Remote Address")]
        public IPAddress RemoteAddress { get; set; }
        [DisplayName("Remote Port")]
        public ushort RemotePort { get; set; }
        [DisplayName("State")]
        public MibTcpState State { get; set; }
        [DisplayName("Process ID")]
        public int ProcessId { get; set; }
        [DisplayName("Process Name")]
        public string ProcessName { get; set; }

        public TcpProcessRecord(IPAddress localIp, IPAddress remoteIp, ushort localPort,
            ushort remotePort, int pId, MibTcpState state)
        {
            LocalAddress = localIp;
            RemoteAddress = remoteIp;
            LocalPort = localPort;
            RemotePort = remotePort;
            State = state;
            ProcessId = pId;
            // Getting the process name associated with a process id. 
            if (Process.GetProcesses().Any(process => process.Id == pId))
            {
                ProcessName = Process.GetProcessById(ProcessId).ProcessName;
            }
        }
    }

    /// <summary> 
    /// The structure contains an entry from the User Datagram Protocol (UDP) listener 
    /// table for IPv4 on the local computer. The entry also includes the process ID 
    /// (PID) that issued the call to the bind function for the UDP endpoint. 
    /// </summary> 
    [StructLayout(LayoutKind.Sequential)]
    public struct MIB_UDPROW_OWNER_PID
    {
        public uint localAddr;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] localPort;
        public int owningPid;
    }

    /// <summary> 
    /// The structure contains the User Datagram Protocol (UDP) listener table for IPv4 
    /// on the local computer. The table also includes the process ID (PID) that issued 
    /// the call to the bind function for each UDP endpoint. 
    /// </summary> 
    [StructLayout(LayoutKind.Sequential)]
    public struct MIB_UDPTABLE_OWNER_PID
    {
        public uint dwNumEntries;
        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.Struct,
            SizeConst = 1)]
        public UdpProcessRecord[] table;
    }

    /// <summary> 
    /// This class provides access an IPv4 UDP connection addresses and ports and its 
    /// associated Process IDs and names. 
    /// </summary> 
    [StructLayout(LayoutKind.Sequential)]
    public class UdpProcessRecord
    {
        [DisplayName("Local Address")]
        public IPAddress LocalAddress { get; set; }
        [DisplayName("Local Port")]
        public uint LocalPort { get; set; }
        [DisplayName("Process ID")]
        public int ProcessId { get; set; }
        [DisplayName("Process Name")]
        public string ProcessName { get; set; }

        public UdpProcessRecord(IPAddress localAddress, uint localPort, int pId)
        {
            LocalAddress = localAddress;
            LocalPort = localPort;
            ProcessId = pId;
            if (Process.GetProcesses().Any(process => process.Id == pId))
                ProcessName = Process.GetProcessById(ProcessId).ProcessName;
        }
    }
}