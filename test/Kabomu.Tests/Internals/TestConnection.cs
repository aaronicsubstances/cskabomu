using System.IO;
using System.Threading.Tasks;

namespace Kabomu.Tests.Internals
{
    class TestConnection
    {
        public MemoryStream InputStream { get; set; }
        public MemoryStream OutputStream { get; set; }
        public int ReadDelayMillis { get; set; }
        public int WriteDelayMillis { get; set; }
        public int ReleaseDelayMillis { get; set; }

    }
}
