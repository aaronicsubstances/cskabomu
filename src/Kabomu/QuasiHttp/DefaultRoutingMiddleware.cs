using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp
{
    public class DefaultRoutingMiddleware : IQuasiHttpMiddleware
    {
        public string RequestBodyTypeOverride { get; set; }
        public object RequestBodySerializationInfo { get; set; }
        public QuasiHttpMiddlewareCallback RequestProcessingDelegate { get; set; }

        public void ProcessPostRequest(QuasiHttpRequestMessage request, IQuasiHttpApplication application,
            Action<Exception, object> cb)
        {
            var effectiveContentType = RequestBodyTypeOverride ?? request.ContentType;
            if (effectiveContentType != null && effectiveContentType != "application/octet-stream" &&
                request.Body is IQuasiHttpBody serializedRequestBody)
            {
                object deserializedRequestBody = application.Deserialize(serializedRequestBody,
                    effectiveContentType, RequestBodySerializationInfo);
                request = new QuasiHttpRequestMessage
                {
                    Host = request.Host,
                    Path = request.Path,
                    ContentLength = request.ContentLength,
                    ContentType = request.ContentType,
                    CustomHeaders = request.CustomHeaders,
                    Body = deserializedRequestBody
                };
            }
            RequestProcessingDelegate.Invoke(request, cb);
        }
    }
}
