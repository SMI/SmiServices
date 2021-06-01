# Notes for developing SmiServices

## Generating local HTML coverage reports

Following the process described [here](https://docs.microsoft.com/en-us/dotnet/core/testing/unit-testing-code-coverage?tabs=linux#generate-reports)

```console
$ dotnet tool update -g dotnet-reportgenerator-globaltool
...
$ .azure-pipelines/scripts/run-dotnet-tests.py
...

$ reportgenerator \
    -reporttypes:html \
    -targetdir:htmlcov \
    -reports:coverage/coverage.cobertura.xml
...
2021-06-01T17:27:04: Writing report file 'htmlcov/index.html'
```

This can then be hosted on a local webpage using Python:

```console
$ python3 -m http.server --directory htmlcov/
Serving HTTP on 0.0.0.0 port 8000 (http://0.0.0.0:8000/) ...
```
