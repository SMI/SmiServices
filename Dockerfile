ARG SDK_VERSION=
ARG RUNTIME_VERSION=

FROM mcr.microsoft.com/dotnet/sdk:$SDK_VERSION AS build

WORKDIR /src
COPY . ./
RUN : \
    && dotnet publish \
        -p:Platform=x64 \
        --output ./dist \
        --verbosity quiet \
        --nologo \
        ./src/applications/Applications.SmiRunner \
    && cd ./dist \
    && rm -f Smi.NLog.config default.yaml DynamicRules.txt Targets.yaml

FROM mcr.microsoft.com/dotnet/runtime:$RUNTIME_VERSION
WORKDIR /opt/smi/SmiServices
COPY --from=build /src/dist .

ENV DOTNET_EnableDiagnostics=0

ENTRYPOINT ["./smi"]
