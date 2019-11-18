package org.smi.common.messageSerialization;

import java.lang.annotation.ElementType;
import java.lang.annotation.Retention;
import java.lang.annotation.RetentionPolicy;
import java.lang.annotation.Target;
import java.lang.reflect.Field;
import java.lang.reflect.Type;
import java.util.List;

import com.google.gson.Gson;
import com.google.gson.JsonDeserializationContext;
import com.google.gson.JsonDeserializer;
import com.google.gson.JsonElement;
import com.google.gson.JsonParseException;

/**
 * Adds the feature to use required fields in models.
 * 
 * Based on stackexchange discussion @see <a href=
 * "https://stackoverflow.com/questions/21626690/gson-optional-and-required-fields">gson
 * optional and required fields</a>
 *
 * @param <T>
 *            Model to parse to.
 */
public class JsonDeserializerWithOptions<T> implements JsonDeserializer<T> {

	/**
	 * To mark required fields of the model: json parsing will be failed if these
	 * fields won't be provided.
	 */
	@Retention(RetentionPolicy.RUNTIME) // to make reading of this field possible at the runtime
	@Target(ElementType.FIELD) // to make annotation accessible throw the reflection
	public @interface FieldRequired {
	}

	/**
	 * Called when the model is being parsed.
	 *
	 * @param je
	 *            Source json string.
	 * @param type
	 *            Object's model.
	 * @param jdc
	 *            Unused in this case.
	 *
	 * @return Parsed object.
	 *
	 * @throws JsonParseException
	 *             When parsing is impossible.
	 */
	@Override
	public T deserialize(JsonElement je, Type type, JsonDeserializationContext jdc) throws JsonParseException {

		// Parsing object as usual.
		T pojo = new Gson().fromJson(je, type);

		// Getting all fields of the class and checking if all required ones were
		// provided.
		checkRequiredFields(pojo.getClass().getDeclaredFields(), pojo);

		// Checking if all required fields of parent classes were provided.
		checkSuperClasses(pojo);

		// All checks are ok.
		return pojo;
	}

	/**
	 * Checks whether all required fields were provided in the class.
	 *
	 * @param fields
	 *            Fields to be checked.
	 * @param pojo
	 *            Instance to check fields in.
	 *
	 * @throws JsonParseException
	 *             When some required field was not met.
	 */
	private void checkRequiredFields(Field[] fields, Object pojo) throws JsonParseException {

		// Checking nested list items too.
		if (pojo instanceof List) {

			final List<?> pojoList = (List<?>) pojo;

			for (final Object pojoListPojo : pojoList) {

				checkRequiredFields(pojoListPojo.getClass().getDeclaredFields(), pojoListPojo);
				checkSuperClasses(pojoListPojo);
			}
		}

		for (Field f : fields) {

			// If some field has required annotation.
			if (f.getAnnotation(FieldRequired.class) != null) {

				try {

					// Trying to read this field's value and check that it truly has value.
					f.setAccessible(true);

					Object fieldObject = f.get(pojo);

					if (fieldObject == null) {

						// Required value is null - throwing error.
						throw new JsonParseException(
								String.format("%1$s -> %2$s", pojo.getClass().getSimpleName(), f.getName()));

					} else {

						checkRequiredFields(fieldObject.getClass().getDeclaredFields(), fieldObject);
						checkSuperClasses(fieldObject);
					}

				} catch (SecurityException | IllegalArgumentException | IllegalAccessException e) {

					throw new JsonParseException(e);
				}

			}
		}
	}

	/**
	 * Checks whether all super classes have all required fields.
	 *
	 * @param pojo
	 *            Object to check required fields in its superclasses.
	 *
	 * @throws JsonParseException
	 *             When some required field was not met.
	 */
	private void checkSuperClasses(Object pojo) throws JsonParseException {

		Class<?> superclass = pojo.getClass();

		while ((superclass = superclass.getSuperclass()) != null) {

			checkRequiredFields(superclass.getDeclaredFields(), pojo);
		}
	}

}
