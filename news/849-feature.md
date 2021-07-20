CTPAnonymiser refactoring
- Reduce memory footprint (issue #837)
- Simplify RabbitMQ message handling
- Stop creating temporary copy of input file - no longer needed in EPCC environment without Lustre FS (per issue #836)
- Add checks input file is readable not just extant, hopefully fixing issue #533