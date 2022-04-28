﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp
{
    public interface IQuasiHttpBody
    {
        void OnDataRead(QuasiHttpBodyCallback cb);
        void OnEndRead(Exception error);
    }
}