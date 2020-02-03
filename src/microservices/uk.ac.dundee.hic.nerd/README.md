# Name Entity Recognition Daemon

Primary Author: [James A Sutherland](https://github.com/jas88)


## Contents

1. [Background](#background)
1. [Setup](#setup)
1. [Running](#running)


## Background

This standalone process is designed to classify text strings sent by the [IsIdentifiable](../Microservices.IsIdentifiable/README.md#socket-rules) application. It accepts TCP connections on localhost port 1881, returning classification results as expected by the IsIdentifiable microservice.


## Setup

No setup is required, just run the jar file as documented below. The "<&- &" will cause it to disconnect from the terminal once initialised and run as a daemon. (For development use, you can also skip that and terminate it with ctrl-C on the console when finished.)


## Running

`java -jar nerd.jar <&- &`

To shutdown again:

`fuser -k -TERM -n tcp 1881`
