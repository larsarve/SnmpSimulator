using Lextm.SharpSnmpLib;
using System.Net;
using System.Net.Sockets;

namespace SnmpSimulator
{
    class Program
    {
        private static readonly Dictionary<string, ISnmpData> _systemOids = new()
        {
            { "1.3.6.1.2.1.1.1.0", new OctetString("Test System Description") }, // sysDescr
            { "1.3.6.1.2.1.1.2.0", new ObjectIdentifier("1.3.6.1.4.1.8072.3.2.10") }, // sysObjectID
            { "1.3.6.1.2.1.1.3.0", new TimeTicks(123456) }, // sysUpTime
            { "1.3.6.1.2.1.1.4.0", new OctetString("Test Contact") }, // sysContact
            { "1.3.6.1.2.1.1.5.0", new OctetString("Test System Name") }, // sysName
            { "1.3.6.1.2.1.1.6.0", new OctetString("Test Location") }, // sysLocation
            { "1.3.6.1.2.1.1.7.0", new Integer32(72) }, // sysServices
        };

        static void Main(string[] args)
        {
            try
            {
                // Use a non-privileged port for testing
                var port = 16161;
                var ip = IPAddress.Any;
                var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                var endpoint = new IPEndPoint(ip, port);
                socket.Bind(endpoint);

                Console.WriteLine($"SNMP Simulator listening on port {port}");
                Console.WriteLine("Press Ctrl+C to exit");

                var buffer = new byte[65536];
                EndPoint remoteEndpoint = new IPEndPoint(IPAddress.Any, 0);

                while (true)
                {
                    try
                    {
                        Console.WriteLine("Waiting for SNMP request...");
                        var bytesRead = socket.ReceiveFrom(buffer, ref remoteEndpoint);
                        Console.WriteLine($"Received {bytesRead} bytes from {remoteEndpoint}");

                        // Print the raw bytes for debugging
                        Console.WriteLine("Raw request bytes:");
                        for (int i = 0; i < bytesRead; i++)
                        {
                            Console.Write($"{buffer[i]:X2} ");
                        }
                        Console.WriteLine();

                        var requestOid = ExtractRequestOid(buffer, bytesRead);
                        Console.WriteLine($"Extracted OID: {requestOid ?? "null"}");

                        if (requestOid != null && _systemOids.TryGetValue(requestOid, out var value))
                        {
                            Console.WriteLine($"Found value for OID {requestOid}");
                            var response = CreateResponse(value);
                            Console.WriteLine($"Sending response of {response.Length} bytes");
                            socket.SendTo(response, remoteEndpoint);
                            Console.WriteLine("Response sent");
                        }
                        else
                        {
                            Console.WriteLine($"No value found for OID {requestOid}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing request: {ex.Message}");
                        Console.WriteLine($"Stack trace: {ex.StackTrace}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to start SNMP simulator: {ex.Message}");
            }
        }

        private static string? ExtractRequestOid(byte[] buffer, int length)
        {
            try
            {
                // Simple OID extraction - this is a basic implementation
                // Real SNMP would need proper BER/ASN.1 decoding
                for (int i = 0; i < length - 10; i++)
                {
                    if (buffer[i] == 0x06) // OID type
                    {
                        var oidLength = buffer[i + 1];
                        if (oidLength > 0 && i + 2 + oidLength <= length)
                        {
                            var oidBytes = new byte[oidLength];
                            Array.Copy(buffer, i + 2, oidBytes, 0, oidLength);
                            return DecodeOid(oidBytes);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting OID: {ex.Message}");
            }
            return null;
        }

        private static string DecodeOid(byte[] oidBytes)
        {
            // Basic OID decoding - this is a simplified implementation
            if (oidBytes.Length < 1) return string.Empty;

            var result = new List<int> { oidBytes[0] / 40, oidBytes[0] % 40 };
            var value = 0;

            for (int i = 1; i < oidBytes.Length; i++)
            {
                if ((oidBytes[i] & 0x80) == 0)
                {
                    value = (value << 7) | oidBytes[i];
                    result.Add(value);
                    value = 0;
                }
                else
                {
                    value = (value << 7) | (oidBytes[i] & 0x7F);
                }
            }

            return string.Join(".", result);
        }

        private static byte[] CreateResponse(ISnmpData value)
        {
            // Simple response creation - this is a basic implementation
            // Real SNMP would need proper BER/ASN.1 encoding
            var response = new List<byte>();
            response.AddRange(new byte[] { 0x30, 0x00 }); // Sequence
            response.AddRange(new byte[] { 0x02, 0x01, 0x01 }); // Version 2c
            response.AddRange(new byte[] { 0x04, 0x06, 0x70, 0x75, 0x62, 0x6C, 0x69, 0x63 }); // Community "public"
            response.AddRange(new byte[] { 0xA2, 0x00 }); // Response PDU
            response.AddRange(new byte[] { 0x02, 0x01, 0x00 }); // Request ID
            response.AddRange(new byte[] { 0x02, 0x01, 0x00 }); // Error Status
            response.AddRange(new byte[] { 0x02, 0x01, 0x00 }); // Error Index
            response.AddRange(value.ToBytes());

            // Fix lengths
            var totalLength = response.Count - 2;
            response[1] = (byte)totalLength;
            response[11] = (byte)(totalLength - 12);

            return response.ToArray();
        }
    }
}
