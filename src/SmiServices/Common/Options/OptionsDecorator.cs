using System;
using System.Reflection;
using YamlDotNet.Serialization;

namespace SmiServices.Common.Options;

public abstract class OptionsDecorator : IOptionsDecorator
{
    public abstract GlobalOptions Decorate(GlobalOptions options);

    protected static void ForAll<T>(IOptions globals, Func<T, T> setter) where T : IOptions
    {
        //for each property on branch
        foreach (var p in globals.GetType().GetProperties())
        {
            if (p.GetCustomAttribute(typeof(YamlIgnoreAttribute)) is not null) continue;

            var currentValue = p.GetValue(globals)!;

            //if it's a T then call the action (note that we check the property Type because we are interested in the property even if it is null
            if (p.PropertyType.IsAssignableFrom(typeof(T)))
            {
                //the delegate changes the value of the property of Type T (or creates a new instance from scratch)
                var result = setter((T)currentValue);

                //store the result of the delegate for this property
                p.SetValue(globals, result);
            }

            //process its children
            if (currentValue is IOptions subOptions)
            {
                ForAll(subOptions, setter);
            }
        }
    }
}
