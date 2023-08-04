using System;
using System.Reflection;

namespace Smi.Common.Options
{
    public abstract class OptionsDecorator : IOptionsDecorator
    {
        public abstract GlobalOptions Decorate(GlobalOptions options);

        protected void ForAll<T>(IOptions globals, Func<T, T> setter) where T : IOptions
        {
            //for each property on branch
            foreach (PropertyInfo p in globals.GetType().GetProperties())
            {
                var currentValue = p.GetValue(globals) ?? throw new Exception("Could not get property value");

                //if it's a T then call the action (note that we check the property Type because we are interested in the property even if it is null
                if (p.PropertyType.IsAssignableFrom(typeof(T)))
                {
                    //the delegate changes the value of the property of Type T (or creates a new instance from scratch)
                    var result = setter((T)currentValue);

                    //store the result of the delegate for this property
                    p.SetValue(globals, result);
                }

                //process it's children
                if (currentValue is IOptions subOptions)
                {
                    ForAll(subOptions, setter);
                }
            }
        }
    }
}
