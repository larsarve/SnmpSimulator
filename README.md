# SNMP Simulator

A simple SNMP simulator that can be used to test SNMP monitoring systems. The simulator can run with default values or be configured using JSON files to simulate different devices.

## Features

- SNMP v2c support
- Configurable OID values through JSON files
- Default system OIDs for common metrics
- Easy to extend with custom device configurations
- Support for custom OIDs through separate configuration files

## Installation

1. Clone the repository
2. Navigate to the SnmpSimulator directory
3. Run `dotnet build` to build the project
4. Run `dotnet run` to start the simulator

## Usage

### Default Mode

By default, the simulator will use built-in system OIDs. Simply run:

```bash
dotnet run
```

The simulator will start on port 16161 and respond to SNMP GET requests with default values.

### Custom Device Mode

To use a custom device configuration:

1. Create a `devices` folder in the same directory as the simulator
2. Create a JSON file with your device configuration (see example below)
3. Run the simulator

The simulator will automatically detect and use the JSON configuration file.

### Custom OID Mode

To add custom OIDs:

1. Create a `custom` folder in the same directory as the simulator
2. Create a JSON file with your custom OID configuration (see example below)
3. Run the simulator

The simulator will load all custom OIDs from JSON files in the custom directory.

## Device Configuration Example

Create a file named `mydevice.json` in the `devices` folder with the following structure:

```json
{
    "system": {
        "description": "Custom Device",
        "uptime": 123456,
        "contact": "admin@example.com",
        "name": "MyCustomDevice",
        "location": "Data Center A",
        "services": 72
    },
    "interfaces": {
        "1": {
            "description": "GigabitEthernet0/1",
            "type": 6,
            "mtu": 1500,
            "speed": 1000000000,
            "physaddress": "00:11:22:33:44:55",
            "adminstatus": 1,
            "operstatus": 1,
            "lastchange": 0,
            "inoctets": 1000000,
            "inucastpkts": 1000,
            "innucastpkts": 100,
            "indiscards": 0,
            "inerrors": 0,
            "inunknownprotos": 0,
            "outoctets": 2000000,
            "outucastpkts": 2000,
            "outnucastpkts": 200,
            "outdiscards": 0,
            "outerrors": 0,
            "outqlen": 0
        }
    }
}
```

## Custom OID Configuration Example

Create a file named `mycustomoids.json` in the `custom` folder with the following structure:

```json
{
    "oids": [
        {
            "oid": "1.3.6.1.4.1.9999.1.1.1",
            "type": "OctetString",
            "value": "Custom Device Information"
        },
        {
            "oid": "1.3.6.1.4.1.9999.1.1.2",
            "type": "Integer32",
            "value": "42"
        }
    ]
}
```

Supported ASN.1 types:
- OctetString: String values
- Integer32: 32-bit integer values
- Counter32: 32-bit counter values
- Gauge32: 32-bit gauge values
- TimeTicks: Time values in hundredths of seconds
- Oid: Object identifier values
- Null: Null values

## Testing the Simulator

You can test the simulator using the `snmpget` command:

```bash
snmpget -v2c -c public localhost:16161 1.3.6.1.2.1.1.1.0
```

This will return the system description of your configured device.

## Port Configuration

The simulator runs on port 16161 by default. You can modify this in the source code if needed.

## License

This project is open source and available under the MIT License. 