# First Install

For the moment you have to manually install DAT and its dependencies to your local Maven repo for the build to find it. To do this, run installDat.bat (installDat.sh on linux) once before you try and build for the first time.

# Maven commands

You can do all this from inside Eclipse but will have to set up the Run Configurations yourself. Otherwise, from the SMIPlugin/java/Microservices directory, the commands are:

**To build all projects**:
`mvn clean package`

Can append `-DskipTests` to that if you want to build without running tests, or change the `skipTests` option in `SMIPlugin/Java/Microservices/pom.xml` file to `true`.

**To build a single project**

`mvn clean package -pl Microservices.ExtractorCL -am`

To build only the ExtractorCL program as well as the required `Microservices.Common` project, for example. Maven flags used are:

`-pl,--projects <arg> // Comma-delimited list of specified reactor projects to build instead of all projects. A project can be specified by [groupId]:artifactId or by its relative path`

`-am, --also-make // If project list is specified, also build projects required by the list`

**To run**:

First `cd` into one of the `Microservice.*` directories, then:

`java -jar target\***-portable-***.jar ...`

where `...` are any command line options you wish to supply the microservice.

**To package deployable archives**:
Back in `SMIPlugin/Java/Microservices`, build the projects as above, then run:

`mvn assembly:single@create-deployable`

this will create zip archives in each of the target directories containing all the resources and jars needed to deploy the microservices on another machine.

# Configs

The Java microservices share the same yaml config files as the C# microservices. Documentation can be found [here](../Microservices/Microservices.Common/Options/RabbitMqConfigOptions.md).

# Logging notes

We use Logback in Java to log to both file and console. Logback seems to be the best successor to log4j, see this excellent resource to learn more: https://stackify.com/logging-logback/

To properly set up the logging across the microservices, we use the following method:

1. The file `SmiLogbackConfig.xml` is copied from `SMIPlugin\Java\Microservices\res` to each of the output target directories.
2. At the beginning of the main method of each of the microservices, we call `SmiLogging.Setup();`, which is a helper class to configure everything.
3. In every class of the program, you can then create a logger object with `private static final Logger _logger = LoggerFactory.getLogger(MyClass.class);`.
4. Then just implement your log messages appropriately.

Everything is logged to file, `INFO`, `WARN`, and `ERROR` are also logged to console.
