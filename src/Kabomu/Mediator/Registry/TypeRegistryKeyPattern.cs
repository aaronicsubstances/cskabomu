﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Mediator.Registry
{
    public class TypeRegistryKeyPattern : IRegistryKeyPattern
    {
        private readonly Type _type;

        public TypeRegistryKeyPattern(Type type)
        {
            _type = type ?? throw new ArgumentNullException(nameof(type));
        }

        public bool IsMatch(object input)
        {
            if (input is Type inputType)
            {
                return _type.IsAssignableFrom(inputType);
            }
            else if (input is TypeRegistryKeyPattern inputTypePattern)
            {
                return _type.IsAssignableFrom(inputTypePattern._type);
            }
            return false;
        }
    }
}