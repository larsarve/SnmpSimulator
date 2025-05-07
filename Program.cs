using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.IO;
using SnmpSharpNet;

namespace SnmpSimulator
{
    class Program
    {
        private static Dictionary<string, AsnType> systemOids = new Dictionary<string, AsnType>();
        private static Dictionary<string, AsnType> interfaceOids = new Dictionary<string, AsnType>();
        private static Dictionary<string, AsnType> customOids = new Dictionary<string, AsnType>();
        private static int port = 16162;

        static void Main(string[] args)
        {
            Console.WriteLine("SNMP Simulator starting...");
            
            // Load device configuration if available
            LoadDeviceConfiguration();

            // Load custom OID configuration if available
            LoadCustomConfiguration();

            // Initialize default OIDs if no configuration was loaded
            if (systemOids.Count == 0)
            {
                InitializeDefaultOids();
            }

            try
            {
                Console.WriteLine($"Attempting to bind to port {port}...");
                using (UdpClient udpClient = new UdpClient(port))
                {
                    Console.WriteLine($"Successfully bound to port {port}");
                    Console.WriteLine($"SNMP Simulator listening on port {port}...");

                    while (true)
                    {
                        try
                        {
                            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                            Console.WriteLine("Waiting for SNMP request...");
                            byte[] requestBytes = udpClient.Receive(ref remoteEndPoint);
                            Console.WriteLine($"Received request from {remoteEndPoint}");
                            Console.WriteLine($"Request bytes: {BitConverter.ToString(requestBytes)}");

                            try
                            {
                                // Create a new packet to decode into
                                SnmpV2Packet responsePacket = new SnmpV2Packet();
                                responsePacket.decode(requestBytes, requestBytes.Length);

                                // Save the request OIDs before clearing the VbList
                                var requestOids = new List<Vb>();
                                foreach (Vb vb in responsePacket.Pdu.VbList)
                                {
                                    requestOids.Add(vb);
                                }

                                // Clear the VbList and add our response variables
                                responsePacket.Pdu.VbList.Clear();

                                // Process each variable binding from the saved request
                                foreach (Vb vb in requestOids)
                                {
                                    string oid = vb.Oid.ToString();
                                    Console.WriteLine($"Processing OID: {oid}");

                                    // Try to find the OID in our dictionaries
                                    if (systemOids.TryGetValue(oid, out AsnType? systemValue))
                                    {
                                        Console.WriteLine($"Found system OID: {oid} = {systemValue}");
                                        responsePacket.Pdu.VbList.Add(new Vb(new Oid(oid), systemValue));
                                    }
                                    else if (interfaceOids.TryGetValue(oid, out AsnType? interfaceValue))
                                    {
                                        Console.WriteLine($"Found interface OID: {oid} = {interfaceValue}");
                                        responsePacket.Pdu.VbList.Add(new Vb(new Oid(oid), interfaceValue));
                                    }
                                    else if (customOids.TryGetValue(oid, out AsnType? customValue))
                                    {
                                        Console.WriteLine($"Found custom OID: {oid} = {customValue}");
                                        responsePacket.Pdu.VbList.Add(new Vb(new Oid(oid), customValue));
                                    }
                                    else
                                    {
                                        Console.WriteLine($"OID not found: {oid}, returning noSuchInstance");
                                        // If OID not found, return noSuchInstance
                                        responsePacket.Pdu.VbList.Add(new Vb(new Oid(oid), new Null()));
                                    }
                                }

                                // Set the PDU type to Response
                                responsePacket.Pdu.Type = PduType.Response;

                                // Send the response
                                byte[] responseBytes = responsePacket.encode();
                                udpClient.Send(responseBytes, responseBytes.Length, remoteEndPoint);
                                Console.WriteLine("Response sent successfully");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error processing request: {ex.Message}");
                                Console.WriteLine(ex.StackTrace);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error receiving request: {ex.Message}");
                            Console.WriteLine(ex.StackTrace);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }

        private static void LoadCustomConfiguration()
        {
            string customPath = Path.Combine(Directory.GetCurrentDirectory(), "custom");
            if (Directory.Exists(customPath))
            {
                string[] jsonFiles = Directory.GetFiles(customPath, "*.json");
                if (jsonFiles.Length > 0)
                {
                    foreach (string configFile in jsonFiles)
                    {
                        Console.WriteLine($"Loading custom configuration from: {configFile}");

                        try
                        {
                            string jsonContent = File.ReadAllText(configFile);
                            var customConfig = JsonSerializer.Deserialize<CustomOidConfig>(jsonContent);

                            if (customConfig?.Oids != null)
                            {
                                Console.WriteLine($"Found {customConfig.Oids.Count} custom OIDs in configuration");
                                foreach (var oidConfig in customConfig.Oids)
                                {
                                    if (oidConfig != null)
                                    {
                                        Console.WriteLine($"Processing custom OID: {oidConfig.Oid}, Type: {oidConfig.Type}, Value: {oidConfig.Value}");
                                        AsnType? value = CreateAsnTypeFromConfig(oidConfig);
                                        if (value != null)
                                        {
                                            customOids[oidConfig.Oid] = value;
                                            Console.WriteLine($"Successfully added custom OID: {oidConfig.Oid} = {value}");
                                        }
                                        else
                                        {
                                            Console.WriteLine($"Failed to create ASN.1 type for OID: {oidConfig.Oid}");
                                        }
                                    }
                                }
                                Console.WriteLine($"Total custom OIDs loaded: {customOids.Count}");
                                foreach (var oid in customOids.Keys)
                                {
                                    Console.WriteLine($"Loaded custom OID: {oid} = {customOids[oid]}");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error loading custom configuration {configFile}: {ex.Message}");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("No custom configuration files found in custom directory");
                }
            }
            else
            {
                Console.WriteLine($"Custom directory not found at: {customPath}");
            }
        }

        private static AsnType? CreateAsnTypeFromConfig(CustomOidConfig.OidConfig config)
        {
            try
            {
                Console.WriteLine($"Creating ASN.1 type for OID {config.Oid}: Type={config.Type}, Value={config.Value}");
                switch (config.Type.ToLowerInvariant())
                {
                    case "integer":
                        if (int.TryParse(config.Value, out int intValue))
                        {
                            Console.WriteLine($"Successfully parsed integer value: {intValue}");
                            return new Integer32(intValue);
                        }
                        Console.WriteLine($"Failed to parse integer value: {config.Value}");
                        return null;

                    case "string":
                        Console.WriteLine($"Creating string value: {config.Value}");
                        return new OctetString(config.Value);

                    case "oid":
                        Console.WriteLine($"Creating OID value: {config.Value}");
                        return new Oid(config.Value);

                    case "timeticks":
                        if (uint.TryParse(config.Value, out uint ticks))
                        {
                            Console.WriteLine($"Successfully parsed timeticks value: {ticks}");
                            return new TimeTicks(ticks);
                        }
                        Console.WriteLine($"Failed to parse timeticks value: {config.Value}");
                        return null;

                    case "gauge32":
                        if (uint.TryParse(config.Value, out uint gauge))
                        {
                            Console.WriteLine($"Successfully parsed gauge32 value: {gauge}");
                            return new Gauge32(gauge);
                        }
                        Console.WriteLine($"Failed to parse gauge32 value: {config.Value}");
                        return null;

                    case "counter32":
                        if (uint.TryParse(config.Value, out uint counter))
                        {
                            Console.WriteLine($"Successfully parsed counter32 value: {counter}");
                            return new Counter32(counter);
                        }
                        Console.WriteLine($"Failed to parse counter32 value: {config.Value}");
                        return null;

                    case "counter64":
                        if (ulong.TryParse(config.Value, out ulong counter64))
                        {
                            Console.WriteLine($"Successfully parsed counter64 value: {counter64}");
                            return new Counter64(counter64);
                        }
                        Console.WriteLine($"Failed to parse counter64 value: {config.Value}");
                        return null;

                    case "ipaddress":
                        if (IPAddress.TryParse(config.Value, out IPAddress? ip))
                        {
                            Console.WriteLine($"Successfully parsed IP address: {ip}");
                            return new IpAddress(ip);
                        }
                        Console.WriteLine($"Failed to parse IP address: {config.Value}");
                        return null;

                    default:
                        Console.WriteLine($"Unknown ASN.1 type: {config.Type}");
                        return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating ASN.1 type for OID {config.Oid}: {ex.Message}");
                return null;
            }
        }

        private static void LoadDeviceConfiguration()
        {
            string devicesPath = Path.Combine(Directory.GetCurrentDirectory(), "devices");
            if (Directory.Exists(devicesPath))
            {
                string[] jsonFiles = Directory.GetFiles(devicesPath, "*.json");
                if (jsonFiles.Length > 0)
                {
                    // Use the first JSON file found
                    string configFile = jsonFiles[0];
                    Console.WriteLine($"Loading device configuration from: {configFile}");

                    try
                    {
                        string jsonContent = File.ReadAllText(configFile);
                        var deviceConfig = JsonSerializer.Deserialize<DeviceConfig>(jsonContent);

                        // Load system OIDs
                        if (deviceConfig?.System != null)
                        {
                            systemOids["1.3.6.1.2.1.1.1.0"] = new OctetString(deviceConfig.System.Description);
                            systemOids["1.3.6.1.2.1.1.3.0"] = new TimeTicks(deviceConfig.System.Uptime);
                            systemOids["1.3.6.1.2.1.1.4.0"] = new OctetString(deviceConfig.System.Contact);
                            systemOids["1.3.6.1.2.1.1.5.0"] = new OctetString(deviceConfig.System.Name);
                            systemOids["1.3.6.1.2.1.1.6.0"] = new OctetString(deviceConfig.System.Location);
                            systemOids["1.3.6.1.2.1.1.7.0"] = new Integer32(deviceConfig.System.Services);
                        }

                        // Load interface OIDs
                        if (deviceConfig?.Interfaces != null)
                        {
                            foreach (var iface in deviceConfig.Interfaces)
                            {
                                string ifIndex = iface.Key;
                                var ifConfig = iface.Value;

                                // Interface description
                                interfaceOids[$"1.3.6.1.2.1.2.2.1.2.{ifIndex}"] = new OctetString(ifConfig.Description);
                                // Interface type
                                interfaceOids[$"1.3.6.1.2.1.2.2.1.3.{ifIndex}"] = new Integer32(ifConfig.Type);
                                // Interface MTU
                                interfaceOids[$"1.3.6.1.2.1.2.2.1.4.{ifIndex}"] = new Integer32(ifConfig.Mtu);
                                // Interface speed
                                interfaceOids[$"1.3.6.1.2.1.2.2.1.5.{ifIndex}"] = new Integer32(ifConfig.Speed);
                                // Interface physical address
                                interfaceOids[$"1.3.6.1.2.1.2.2.1.6.{ifIndex}"] = new OctetString(ifConfig.PhysAddress);
                                // Interface admin status
                                interfaceOids[$"1.3.6.1.2.1.2.2.1.7.{ifIndex}"] = new Integer32(ifConfig.AdminStatus);
                                // Interface oper status
                                interfaceOids[$"1.3.6.1.2.1.2.2.1.8.{ifIndex}"] = new Integer32(ifConfig.OperStatus);
                                // Interface last change
                                interfaceOids[$"1.3.6.1.2.1.2.2.1.9.{ifIndex}"] = new TimeTicks(ifConfig.LastChange);
                                // Interface in octets
                                interfaceOids[$"1.3.6.1.2.1.2.2.1.10.{ifIndex}"] = new Counter32(ifConfig.InOctets);
                                // Interface in unicast packets
                                interfaceOids[$"1.3.6.1.2.1.2.2.1.11.{ifIndex}"] = new Counter32(ifConfig.InUcastPkts);
                                // Interface in non-unicast packets
                                interfaceOids[$"1.3.6.1.2.1.2.2.1.12.{ifIndex}"] = new Counter32(ifConfig.InNUcastPkts);
                                // Interface in discards
                                interfaceOids[$"1.3.6.1.2.1.2.2.1.13.{ifIndex}"] = new Counter32(ifConfig.InDiscards);
                                // Interface in errors
                                interfaceOids[$"1.3.6.1.2.1.2.2.1.14.{ifIndex}"] = new Counter32(ifConfig.InErrors);
                                // Interface in unknown protos
                                interfaceOids[$"1.3.6.1.2.1.2.2.1.15.{ifIndex}"] = new Counter32(ifConfig.InUnknownProtos);
                                // Interface out octets
                                interfaceOids[$"1.3.6.1.2.1.2.2.1.16.{ifIndex}"] = new Counter32(ifConfig.OutOctets);
                                // Interface out unicast packets
                                interfaceOids[$"1.3.6.1.2.1.2.2.1.17.{ifIndex}"] = new Counter32(ifConfig.OutUcastPkts);
                                // Interface out non-unicast packets
                                interfaceOids[$"1.3.6.1.2.1.2.2.1.18.{ifIndex}"] = new Counter32(ifConfig.OutNUcastPkts);
                                // Interface out discards
                                interfaceOids[$"1.3.6.1.2.1.2.2.1.19.{ifIndex}"] = new Counter32(ifConfig.OutDiscards);
                                // Interface out errors
                                interfaceOids[$"1.3.6.1.2.1.2.2.1.20.{ifIndex}"] = new Counter32(ifConfig.OutErrors);
                                // Interface out queue length
                                interfaceOids[$"1.3.6.1.2.1.2.2.1.21.{ifIndex}"] = new Gauge32(ifConfig.OutQLen);
                            }
                        }

                        Console.WriteLine("Device configuration loaded successfully");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error loading device configuration: {ex.Message}");
                        Console.WriteLine("Falling back to default OIDs");
                        InitializeDefaultOids();
                    }
                }
                else
                {
                    Console.WriteLine("No device configuration files found in devices directory");
                    InitializeDefaultOids();
                }
            }
            else
            {
                Console.WriteLine("Devices directory not found, using default OIDs");
                InitializeDefaultOids();
            }
        }

        private static void InitializeDefaultOids()
        {
            // System OIDs
            systemOids["1.3.6.1.2.1.1.1.0"] = new OctetString("SNMP Simulator v1.0");
            systemOids["1.3.6.1.2.1.1.3.0"] = new TimeTicks(123456);
            systemOids["1.3.6.1.2.1.1.4.0"] = new OctetString("admin@example.com");
            systemOids["1.3.6.1.2.1.1.5.0"] = new OctetString("SNMP-Simulator");
            systemOids["1.3.6.1.2.1.1.6.0"] = new OctetString("Data Center");
            systemOids["1.3.6.1.2.1.1.7.0"] = new Integer32(72);

            // Interface OIDs (example for interface 1)
            string ifIndex = "1";
            interfaceOids[$"1.3.6.1.2.1.2.2.1.2.{ifIndex}"] = new OctetString("GigabitEthernet0/1");
            interfaceOids[$"1.3.6.1.2.1.2.2.1.3.{ifIndex}"] = new Integer32(6); // ethernetCsmacd
            interfaceOids[$"1.3.6.1.2.1.2.2.1.4.{ifIndex}"] = new Integer32(1500);
            interfaceOids[$"1.3.6.1.2.1.2.2.1.5.{ifIndex}"] = new Integer32(1000000000);
            interfaceOids[$"1.3.6.1.2.1.2.2.1.6.{ifIndex}"] = new OctetString("00:11:22:33:44:55");
            interfaceOids[$"1.3.6.1.2.1.2.2.1.7.{ifIndex}"] = new Integer32(1); // up
            interfaceOids[$"1.3.6.1.2.1.2.2.1.8.{ifIndex}"] = new Integer32(1); // up
            interfaceOids[$"1.3.6.1.2.1.2.2.1.9.{ifIndex}"] = new TimeTicks(0);
            interfaceOids[$"1.3.6.1.2.1.2.2.1.10.{ifIndex}"] = new Counter32(1000000);
            interfaceOids[$"1.3.6.1.2.1.2.2.1.11.{ifIndex}"] = new Counter32(1000);
            interfaceOids[$"1.3.6.1.2.1.2.2.1.12.{ifIndex}"] = new Counter32(100);
            interfaceOids[$"1.3.6.1.2.1.2.2.1.13.{ifIndex}"] = new Counter32(0);
            interfaceOids[$"1.3.6.1.2.1.2.2.1.14.{ifIndex}"] = new Counter32(0);
            interfaceOids[$"1.3.6.1.2.1.2.2.1.15.{ifIndex}"] = new Counter32(0);
            interfaceOids[$"1.3.6.1.2.1.2.2.1.16.{ifIndex}"] = new Counter32(2000000);
            interfaceOids[$"1.3.6.1.2.1.2.2.1.17.{ifIndex}"] = new Counter32(2000);
            interfaceOids[$"1.3.6.1.2.1.2.2.1.18.{ifIndex}"] = new Counter32(200);
            interfaceOids[$"1.3.6.1.2.1.2.2.1.19.{ifIndex}"] = new Counter32(0);
            interfaceOids[$"1.3.6.1.2.1.2.2.1.20.{ifIndex}"] = new Counter32(0);
            interfaceOids[$"1.3.6.1.2.1.2.2.1.21.{ifIndex}"] = new Gauge32(0);
        }
    }

    // Configuration classes for JSON deserialization
    public class DeviceConfig
    {
        public SystemConfig? System { get; set; }
        public Dictionary<string, InterfaceConfig>? Interfaces { get; set; }
    }

    public class SystemConfig
    {
        public string Description { get; set; } = string.Empty;
        public uint Uptime { get; set; }
        public string Contact { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public int Services { get; set; }
    }

    public class InterfaceConfig
    {
        public string Description { get; set; } = string.Empty;
        public int Type { get; set; }
        public int Mtu { get; set; }
        public int Speed { get; set; }
        public string PhysAddress { get; set; } = string.Empty;
        public int AdminStatus { get; set; }
        public int OperStatus { get; set; }
        public uint LastChange { get; set; }
        public uint InOctets { get; set; }
        public uint InUcastPkts { get; set; }
        public uint InNUcastPkts { get; set; }
        public uint InDiscards { get; set; }
        public uint InErrors { get; set; }
        public uint InUnknownProtos { get; set; }
        public uint OutOctets { get; set; }
        public uint OutUcastPkts { get; set; }
        public uint OutNUcastPkts { get; set; }
        public uint OutDiscards { get; set; }
        public uint OutErrors { get; set; }
        public uint OutQLen { get; set; }
    }

    public class CustomOidConfig
    {
        public List<OidConfig>? Oids { get; set; }

        public class OidConfig
        {
            public string Oid { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty;
            public string Value { get; set; } = string.Empty;
        }
    }
}

