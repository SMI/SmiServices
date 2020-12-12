# INTRODUCTION

https://mircwiki.rsna.org/index.php?title=Developing_DICOM_Anonymizer_Extensions

The best way to deploy CTP extensions is to put them in jars that are placed in the CTP/libraries directory or any of its subdirectories

To be recognized as an anonymizer extension, a class must implement two interfaces:

* org.rsna.ctp.plugin.Plugin
* org.rsna.ctp.stdstages.anonymizer.dicom.AnonymizerExtension

The best way to implement an AnonymizerExtension is to extend the org.rsna.ctp.plugin.AbstractPlugin class and then implement the single method required by the AnonymizerExtension interface.

The AnonymizerExtension interface provides the call method. The method takes one argument, org.rsna.ctp.stdstages.anonymizer.dicom.FnCall. The FnCall object provides access to the anonymizer script, the context dataset, the element being processed, and all the arguments of the @call instruction in the script being executed.

# BUILD

```
mkdir -p source/java
mkdir -p source/resources
mkdir -p build/org/smi
mkdir -p libraries
mkdir -p products
ln -s ../../../../../../../../../../lib/java/util.jar libraries/util.jar
ln -s ../../../../../../../../../../lib/java/CTP.jar libraries/CTP.jar
ant
```

# INSTALL

Add to the CTP script `data/ctp/ctp-whitelist.script` something like this to call the extension:

```
@call(ExtID,"-SUFFIX")
```

Add to the CTP configuration file a reference to the extension so it gets loaded:

```
<Plugin
    class="org.smi.SRAnonymizerExtension"
    id="ExtID"
    name="SRAnonymizerExtension"
    prefix="PREFIX-"
    root="roots/SRAnonymizerExtension"/>
```

# Reading config.xml

To read config.xml call `Configuration.load();`

```
import org.rsna.ctp.Configuration;

    /* Need to explicitly load the configuration (config.xml) to learn about Plugins */
    Configuration c = Configuration.load();
```

That's fine but listClasspath won't work with recent versions of the JDK.
The way CTP itself gets around this is shown below.
We would need to do something similar in our main().

```
                /* also need to do something like this, to allow listClasspath to work:
                static final File cwdir = new File(".");
                static final String mainClassName = "org.rsna.ctp.ClinicalTrialProcessor";
                JarClassLoader clsloader = JarClassLoader.getInstance(new File[] { cwdir });
                Thread.currentThread().setContextClassLoader(clsloader);
                Class ctpClass = clsloader.loadClass(mainClassName);
                ctpClass.getConstructor( new Class[0] ).newInstance( new Object[0] );
                which seems to be re-creating the Main program using a JarClassLoader.
                */
```

Alternatively we can modify CTP itself (more accurately, it's Util library):

```
diff --git a/source/java/org/rsna/util/ClasspathUtil.java b/source/java/org/rsna/util/ClasspathUtil.java
index 5463688..a12f8d8 100644
--- a/source/java/org/rsna/util/ClasspathUtil.java
+++ b/source/java/org/rsna/util/ClasspathUtil.java
@@ -18,8 +18,9 @@ public class ClasspathUtil {
 	 * @return the current classpath or the empty array if an error occurs.
 	 */
 	public static URL[] getClasspath() {
-		URLClassLoader cl = (URLClassLoader) cpu.getClass().getClassLoader();
-		try { return cl.getURLs(); }
+		// XXX abrooks moved try to capture getClassLoader() call too
+		try { URLClassLoader cl = (URLClassLoader) cpu.getClass().getClassLoader();
+		return cl.getURLs(); }
 		catch (Throwable t) { return new URL[0]; }
 	}
```

# Old info

However our program doesn't read `config.xml` so we need to initialise the plugin manually, see `ctp/Configuration.java`

```
					else if (tagName.equals("Plugin")) {
						String className = childElement.getAttribute("class").trim();
						if (!className.equals("")) {
							try {
								Class theClass = Class.forName(className);
								Class[] signature = { Element.class };
								Constructor constructor = theClass.getConstructor(signature);
								Object[] args = { childElement };
								Plugin plugin = (Plugin)constructor.newInstance(args);
								registerPlugin(plugin);
								pluginsList.add(plugin);
								if (className.equals("mirc.MIRC")) isMIRC = true;
							}
							catch (Exception ex) { logger.error(childElement.getAttribute("name") + ": Unable to load "+className,ex); }
						}
						else logger.error(childElement.getAttribute("name") + ": Plugin with no class attribute");
					}

String className = "org.smi.SRAnonymizerExtension";
Class theClass = Class.forName(className);
Class[] signature = { Element.class };
Constructor constructor = theClass.getConstructor(signature);
Object[] args = { childElement };
Plugin plugin = (Plugin)constructor.newInstance(args);
registerPlugin(plugin);
```
