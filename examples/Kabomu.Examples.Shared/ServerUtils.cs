﻿using System;
using System.Threading.Tasks;

namespace Kabomu.Examples.Shared
{
    public static class ServerUtils
    {
        public static async Task AcceptConnections(
            Func<Task<bool>> receiveCb, Func<Exception, Task<bool>> doneCheck)
        {
            Exception prevError = null;
            var more = true;
            while (more)
            {
                if (await doneCheck.Invoke(prevError))
                {
                    break;
                }
                try
                {
                    more = await receiveCb();
                    prevError = null;
                }
                catch (Exception e)
                {
                    prevError = e;
                }
            }
        }
    }
}