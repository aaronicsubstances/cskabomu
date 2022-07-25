﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.EntityBody
{
    /// <summary>
    /// Represents byte stream derived from a string's UTF-8 representation.
    /// </summary>
    public class StringBody : SerializableObjectBody
    {
        public StringBody(string content) :
            base(content, SerializeContent)
        {
        }

        private static byte[] SerializeContent(object obj)
        {
            var content = (string)obj;
            var dataBytes = Encoding.UTF8.GetBytes(content);
            return dataBytes;
        }

        public string StringContent => (string)Content;
    }
}
