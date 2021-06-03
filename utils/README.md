# Utils

## CI build scripts

-   buildArtefacts.py. Creates packages for all apps in this repo
-   createReleaseChangelog.py. Creates the changelog text for upload to GitHub's Releases page
-   dotnet-build.bash. Builds the dotnet services in Release mode
-   install-ctp.bash. Installs the required libs for CTP
-   run-java-tests.bash. Runs the Java tests
-   runDotnetTests.py. Runs the dotnet tests in Release mode and generates a coverage report
-   updateChangelog.py. Updates the CHANGELOG from the news files

## Other utils

-   RabbitMqTidyQueues. Automatically deletes any Control queues which have no consumers (those that have not been deleted by their host due to a crash).
-   RabbitMQ Dump Queue. Allows dumping of all messages in a RabbitMQ queue to JSON files. It can be downloaded from [here](https://github.com/dubek/rabbitmq-dump-queue).

