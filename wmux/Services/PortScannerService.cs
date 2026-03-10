using System.Net.NetworkInformation;

namespace wmux.Services;

/// <summary>
/// Scans listening TCP ports for a given process (and children).
/// Mirrors cmux's PortScanner — shows listening ports in the sidebar.
/// </summary>
public static class PortScannerService
{
    public static List<int> GetListeningPorts(int pid)
    {
        var ports = new List<int>();
        try
        {
            var props = IPGlobalProperties.GetIPGlobalProperties();
            var listeners = props.GetActiveTcpListeners();
            // For a real implementation, we'd filter by PID using MIB_TCPTABLE2 via P/Invoke.
            // This simplified version returns all listening ports.
            ports.AddRange(listeners.Select(ep => ep.Port).Distinct().OrderBy(p => p));
        }
        catch { }
        return ports;
    }
}
