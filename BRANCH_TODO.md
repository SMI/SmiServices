
# TODO for feature/extraction-refactoring

- Ensure that any changes to the RabbitMQ messages are reflected in both the C# and Java codebases

# TODO for feature/extraction-refactoring-cohort-packager

- Refactor the event loop
  - Consumers listening from high-volume queues should only be blocked during the "is extraction done" check
  - The "is extraction done" check should be quick to determine
  - If no messages are received in `n` seconds on any queue, then check if any jobs have completed

