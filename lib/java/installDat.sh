#!/bin/bash

# Install DATs dependencies to your local repo
mvn install:install-file -Dfile="./lib/CTP.jar" -DgroupId="dat" -DartifactId="CTP" -Dversion="1.0" -Dpackaging="jar" -DgeneratePom="true"
mvn install:install-file -Dfile="./lib/dcm4che.jar" -DgroupId="dat" -DartifactId="dcm4che" -Dversion="1.0" -Dpackaging="jar" -DgeneratePom="true"
mvn install:install-file -Dfile="./lib/util.jar" -DgroupId="dat" -DartifactId="util" -Dversion="1.0" -Dpackaging="jar" -DgeneratePom="true"
mvn install:install-file -Dfile="./lib/log4j.jar" -DgroupId="dat" -DartifactId="log4j" -Dversion="1.0" -Dpackaging="jar" -DgeneratePom="true"
mvn install:install-file -Dfile="./lib/pixelmed_codec.jar" -DgroupId="dat" -DartifactId="pixelmed_codec" -Dversion="1.0" -Dpackaging="jar" -DgeneratePom="true"
mvn install:install-file -Dfile="./lib/dcm4che-imageio-rle-2.0.25.jar" -DgroupId="dat" -DartifactId="dcm4che-imageio-rle-2.0.25" -Dversion="1.0" -Dpackaging="jar" -DgeneratePom="true"

# Install some required CTP dependencies
mvn install:install-file -Dfile="./lib/clibwrapper_jiio.jar" -DgroupId="dat" -DartifactId="clibwrapper_jiio" -Dversion="1.1" -Dpackaging="jar" -DgeneratePom="true"
mvn install:install-file -Dfile="./lib/jai_imageio.jar" -DgroupId="dat" -DartifactId="jai_imageio" -Dversion="1.1" -Dpackaging="jar" -DgeneratePom="true"

# Install DAT, giving it the pom we have generated
mvn install:install-file -Dfile="./lib/DAT.jar" -DpomFile="./lib/pom.xml"

