# SNMP Simulator

A simple SNMP simulator that allows you to simulate SNMP devices and respond to SNMP requests. The simulator can run with default values or be configured using JSON files to simulate different devices.

## Features

- Simulates SNMP v2c device responses
- Configurable through JSON files
- Supports custom OIDs
- Supports device-specific configurations
- Configurable server settings (IP, port, community string)
- Configurable logging

## Installation

1. Clone the repository
2. Navigate to the SnmpSimulator directory
3. Run `dotnet build` to build the project
4. Run `dotnet run` to start the simulator

## Configuration

The simulator can be configured through the `appconfig.json` file:

```json
{
    "Server": {
        "IpAddress": "0.0.0.0",  // IP address to bind to (0.0.0.0 for all interfaces)
        "Port": 16162,           // Port to listen on
        "Community": "public"    // SNMP community string
    },
    "Paths": {
        "DevicesDirectory": "devices",  // Directory for device configurations
        "CustomDirectory": "custom"     // Directory for custom OID configurations
    },
    "Logging": {
        "LogLevel": "Information",      // Log level (Debug, Information, Warning, Error)
        "LogToConsole": true,           // Enable console logging
        "LogToFile": false,             // Enable file logging
        "LogFilePath": "logs/snmpsim.log" // Path to log file
    }
}
```

### Device Configuration

Device configurations are stored in JSON files in the `devices` directory. Example:

```json
{
    "System": {
        "Description": "Example Network Device",
        "Uptime": 1234567,
        "Contact": "admin@example.com",
        "Name": "Example-Device",
        "Location": "Data Center 1",
        "Services": 72
    },
    "Interfaces": {
        "1": {
            "Description": "GigabitEthernet0/1",
            "Type": 6,
            "Mtu": 1500,
            "Speed": 1000000000,
            "PhysAddress": "00:11:22:33:44:55",
            "AdminStatus": 1,
            "OperStatus": 1,
            "LastChange": 0,
            "InOctets": 1000000,
            "InUcastPkts": 1000,
            "InNUcastPkts": 100,
            "InDiscards": 0,
            "InErrors": 0,
            "InUnknownProtos": 0,
            "OutOctets": 2000000,
            "OutUcastPkts": 2000,
            "OutNUcastPkts": 200,
            "OutDiscards": 0,
            "OutErrors": 0,
            "OutQLen": 0
        }
    }
}
```

### Custom OIDs

Custom OIDs can be defined in JSON files in the `custom` directory. Example:

```json
{
    "Oids": [
        {
            "Oid": "1.3.6.1.4.1.9999.1.1.1",
            "Type": "string",
            "Value": "Custom String Value"
        },
        {
            "Oid": "1.3.6.1.4.1.9999.1.1.2",
            "Type": "integer",
            "Value": "42"
        }
    ]
}
```

Supported OID types:
- string
- integer
- timeticks
- gauge32
- counter32
- counter64
- ipaddress
- oid

## Usage

1. Build the project:
```bash
dotnet build
```

2. Run the simulator:
```bash
dotnet run
```

3. Test with SNMP commands:
```bash
# Get system description
snmpget -v2c -c public localhost:16162 1.3.6.1.2.1.1.1.0

# Get custom OID
snmpget -v2c -c public localhost:16162 1.3.6.1.4.1.9999.1.1.1
```

## Requirements

- .NET 8.0 or later
- SnmpSharpNet package
- SharpSnmpLib package

## License

MIT License 