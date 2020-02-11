
# TODO for feature/extraction-refactoring

- Ensure that any changes to the RabbitMQ messages are reflected in both the C# and Java codebases

# TODO for feature/extraction-refactoring-cohort-packager

- Refactor the event loop
  - Consumers listening from high-volume queues should only be blocked during the "is extraction done" check
  - The "is extraction done" check should be quick to determine
  - If no messages are received in `n` seconds on any queue, then check if any jobs have completed

- Make operations transactional
- Documentation: Schema examples
- Usage: requiements for csv input files

- Fix all TODOs introduced in this branch :^)


---

Other:

MongoDbPopulator 
- Flushtime not specified as seconds/ms
- first write time is not until the FlushTime has been reached