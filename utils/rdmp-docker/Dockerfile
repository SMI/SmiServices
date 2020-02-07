
FROM mcr.microsoft.com/mssql/server

USER root

RUN apt-get update -y && apt-get install -y unzip

WORKDIR /usr/bin/rdmp

RUN wget -q https://packages.microsoft.com/config/ubuntu/16.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb && \
    dpkg -i packages-microsoft-prod.deb && \
    apt-get update && \
    apt-get -y install apt-transport-https && \
    apt-get -y install dotnet-sdk-2.2

ENV ACCEPT_EULA=Y
ENV SA_PASSWORD=SA_PASSWORD1.

COPY install-rdmp.sh .
RUN ./install-rdmp.sh
