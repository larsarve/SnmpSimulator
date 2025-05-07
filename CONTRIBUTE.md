# Contributing to SNMP Simulator

Thank you for your interest in contributing to the SNMP Simulator! This document provides guidelines and instructions for developers who want to contribute to the project.

## Development Environment Setup

1. Install .NET 8.0 SDK
2. Clone the repository
3. Open the solution in your preferred IDE (Visual Studio, VS Code, etc.)
4. Restore NuGet packages

## Project Structure

- `Program.cs`: Main entry point and SNMP server implementation
- `README.md`: User documentation
- `CONTRIBUTE.md`: Developer documentation
- `devices/`: Directory for device configuration files

## Code Style Guidelines

- Use C# 8.0 features
- Follow standard C# naming conventions
- Use meaningful variable and method names
- Add XML documentation for public methods and classes
- Keep methods focused and small
- Use async/await for I/O operations

## Adding New Features

1. Create a new branch for your feature
2. Implement the feature following the code style guidelines
3. Add appropriate tests
4. Update documentation if needed
5. Submit a pull request

## Testing

- Test your changes thoroughly
- Ensure backward compatibility
- Test with different SNMP clients
- Verify custom device configurations

## SNMP Implementation Details

The simulator uses the SnmpSharpNet library for SNMP protocol implementation. Key components:

- `SnmpV2Packet`: Handles SNMP v2c packet creation and parsing
- `UdpTransport`: Manages UDP communication
- `Oid`: Represents SNMP object identifiers
- `Vb`: Represents variable bindings

## Adding New OIDs

To add new OIDs:

1. Update the system OIDs dictionary in `Program.cs`
2. Add corresponding values in the device configuration JSON schema
3. Update documentation with new OID descriptions

## Debugging

- Use the built-in debug logging
- Monitor UDP traffic using tools like Wireshark
- Check SNMP client logs for error messages

## Pull Request Process

1. Fork the repository
2. Create your feature branch
3. Commit your changes
4. Push to the branch
5. Open a pull request

## License

By contributing to this project, you agree that your contributions will be licensed under the project's MIT License. 