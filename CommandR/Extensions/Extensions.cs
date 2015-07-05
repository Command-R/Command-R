using System;
using System.Collections.Generic;
using System.Linq;

namespace CommandR.Extensions
{
    public static class Extensions
    {
        public static T CopyTo<T>(this object source, T dest, string[] patchFields = null, bool allowFlatten = true)
            where T : class
        {
            if (source == null || dest == null)
                return dest;

            var sourceProps = source.GetType().GetProperties();
            var destProps = dest.GetType().GetProperties().ToDictionary(x => x.Name);

            if (patchFields != null)
            {
                sourceProps = sourceProps.Where(x => patchFields.Contains(x.Name)).ToArray();
            }

            foreach (var sourceProp in sourceProps)
            {
                //Flatten complex properties
                if (allowFlatten && IsComplexProperty(sourceProp.PropertyType))
                {
                    CopyTo(sourceProp.Name, sourceProp.GetValue(source), dest);
                }

                //Set value
                var destProp = destProps.TryGetValue(sourceProp.Name);
                if (destProp == null || destProp.PropertyType != sourceProp.PropertyType)
                    continue;

                destProp.SetValue(dest, sourceProp.GetValue(source));
            }

            return dest;
        }

        /// <summary> Copy to with prefix for flattening </summary>
        private static void CopyTo(string prefix, object source, object dest)
        {
            if (source == null || dest == null)
                return;

            var sourceProps = source.GetType().GetProperties();
            var destProps =
                dest.GetType().GetProperties().Where(x => x.Name.StartsWith(prefix)).ToDictionary(x => x.Name);

            if (destProps.Count == 0)
                return;

            foreach (var sourceProp in sourceProps)
            {
                //Flatten complex properties
                if (IsComplexProperty(sourceProp.PropertyType))
                {
                    CopyTo(prefix + sourceProp.Name, sourceProp.GetValue(source), dest);
                }

                var destProp = destProps.TryGetValue(prefix + sourceProp.Name);
                if (destProp == null || destProp.PropertyType != sourceProp.PropertyType)
                    continue;

                destProp.SetValue(dest, sourceProp.GetValue(source));
            }
        }

        private static bool IsComplexProperty(Type type)
        {
            return type.IsClass && !type.FullName.StartsWith("System");
        }

        public static TV TryGetValue<TK, TV>(this IDictionary<TK, TV> dict, TK key, TV def = default(TV))
        {
            TV value;
            return dict.TryGetValue(key, out value) ? value : def;
        }

        public static bool ShouldUpdate(this IPatchable obj, string name)
        {
            return obj == null || obj.PatchFields == null || obj.PatchFields.Contains(name);
        }
    };
}
