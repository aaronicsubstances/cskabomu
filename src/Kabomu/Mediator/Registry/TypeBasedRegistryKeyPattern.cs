using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Mediator.Registry
{
    public class TypeBasedRegistryKeyPattern : IRegistryKeyPattern
    {
        private readonly Type _type;

        public TypeBasedRegistryKeyPattern(Type type)
        {
            _type = type ?? throw new ArgumentNullException(nameof(type));
        }

        public bool IsMatch(object input)
        {
            if (input is Type inputType)
            {
                return _type.IsAssignableFrom(inputType);
            }
            else if (input is TypeBasedRegistryKey inputTypeBasedKey)
            {
                return _type.IsAssignableFrom(inputTypeBasedKey.TypeValue);
            }
            return false;
        }
    }
}
