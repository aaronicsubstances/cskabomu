﻿using Kabomu.Concurrency;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp
{
    internal class ProtocolUtilsInternal
    {
        public static int DetermineEffectiveOverallReqRespTimeoutMillis(IQuasiHttpSendOptions firstOptions,
            IQuasiHttpSendOptions fallbackOptions, int defaultValue)
        {
            if (firstOptions != null)
            {
                int effectiveValue = firstOptions.OverallReqRespTimeoutMillis;
                if (effectiveValue != 0)
                {
                    return effectiveValue;
                }
            }
            if (fallbackOptions != null)
            {
                int effectiveValue = fallbackOptions.OverallReqRespTimeoutMillis;
                if (effectiveValue != 0)
                {
                    return effectiveValue;
                }
            }
            return defaultValue;
        }

        public static int DetermineEffectiveMaxChunkSize(IQuasiHttpSendOptions firstOptions,
            IQuasiHttpSendOptions fallbackOptions, int secondFallback, int defaultValue)
        {
            if (firstOptions != null)
            {
                int effectiveValue = firstOptions.MaxChunkSize;
                if (effectiveValue > 0)
                {
                    return effectiveValue;
                }
            }
            if (fallbackOptions != null)
            {
                int effectiveValue = fallbackOptions.MaxChunkSize;
                if (effectiveValue > 0)
                {
                    return effectiveValue;
                }
            }
            return  secondFallback > 0 ? secondFallback : defaultValue;
        }

        public static IDictionary<string, object> DetermineEffectiveConnectivityParams(
            IQuasiHttpSendOptions firstOptions,
            IQuasiHttpSendOptions fallbackOptions)
        {
            var dest = new Dictionary<string, object>();
            // since we want first options to overwrite fall back options,
            // set fall back options first.
            if (fallbackOptions?.ConnectivityParams != null)
            {
                foreach (var item in fallbackOptions.ConnectivityParams)
                {
                    dest.Add(item.Key, item.Value);
                }
            }
            if (firstOptions?.ConnectivityParams != null)
            {
                foreach (var item in firstOptions.ConnectivityParams)
                {
                    if (dest.ContainsKey(item.Key))
                    {
                        dest[item.Key] = item.Value;
                    }
                    else
                    {
                        dest.Add(item.Key, item.Value);
                    }
                }
            }
            return dest;
        }

        public static async Task<IMutexApi> DetermineEffectiveMutexApi(IMutexApi preferred,
            IMutexApiFactory fallbackFactory)
        {
            if (preferred != null)
            {
                return preferred;
            }
            if (fallbackFactory != null)
            {
                return await fallbackFactory.Create();
            }
            return null;
        }
    }
}