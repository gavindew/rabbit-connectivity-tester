# rabbit-connectivity-tester

This tool is used to test connectivity to a rabbit instance. It does this by attempting to connect to the given rabbit instance / virtual host with the provided credentials.

Usage:
```
Description:
  Rabbit Connectivity Checker

Usage:
  rabbit-connectivity-tester [options]

Options:
  -hn, --hostname <hostname> (REQUIRED)        Rabbit hostname
  -vh, --virtualhost <virtualhost> (REQUIRED)  Rabbit virtualhost name
  -u, --username <username> (REQUIRED)         Username to connect to virtual host
  -p, --password <password> (REQUIRED)         Password to connect to virtual host
  --version                                    Show version information
  -?, -h, --help                               Show help and usage information
```
