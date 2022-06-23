using Kabomu.Common;
using Kabomu.Common.Transports;
using Kabomu.QuasiHttp;
using Kabomu.Tests.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace Kabomu.Tests.Common.Transports
{
    /*public class MemoryBasedTransportTest
    {
        private readonly ITestOutputHelper _outputHelper;

        public MemoryBasedTransportTest(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        [Fact]
        public void TestOperationIncludingDirectSend()
        {
            // arrange.
            var eventLoop = new TestEventLoopApiPrev
            {
                RunMutexApiThroughPostCallback = true
            };
            var logger = new OutputEventLogger
            {
                Logs = new List<string>()
            };
            var hub = new MemoryBasedTransportHub();

            object londonEndpoint = "london";
            object kumasiEndpoint = "kumasi";
            var kumasiTranslations = new Dictionary<string, string>
            {
                { "one", "baako" }, { "two", "mmienu" }, { "three", "mmi\u025Bnsa" },
                { "four", "nnan" }, { "five", "nnum" }
            };
            var londonTranslations = new Dictionary<string, string>();
            foreach (var entry in kumasiTranslations)
            {
                londonTranslations.Add(entry.Value, entry.Key);
            }
            var londonClient = new TestQuasiHttpClient(londonEndpoint ,logger, londonTranslations);
            var kumasiClient = new TestQuasiHttpClient(kumasiEndpoint, logger, kumasiTranslations);
            hub.Clients.Add(londonEndpoint, londonClient);
            hub.Clients.Add(kumasiEndpoint, kumasiClient);

            var londonInstance = new MemoryBasedTransport
            {
                Hub = hub,
                Mutex = eventLoop,
                MaxChunkSize = 50
            };
            var kumasiInstance = new MemoryBasedTransport
            {
                Hub = hub,
                Mutex = eventLoop,
                MaxChunkSize = 30
            };
            londonClient.Transport = londonInstance;
            kumasiClient.Transport = kumasiInstance;

            var directRequestProcessingMapForLondon = new Dictionary<IQuasiHttpRequest, IQuasiHttpResponse>();
            IQuasiHttpRequest req1 = new DefaultQuasiHttpRequest(), req2 = new DefaultQuasiHttpRequest();
            IQuasiHttpResponse res1 = new DefaultQuasiHttpResponse(), res2 = new DefaultQuasiHttpResponse();
            directRequestProcessingMapForLondon.Add(req1, res1);
            directRequestProcessingMapForLondon.Add(req2, res2);
            londonClient.Application = new ConfigurableQuasiHttpApplication
            {
                ProcessRequestCallback = (req, resCb) =>
                {
                    if (!directRequestProcessingMapForLondon.ContainsKey(req))
                    {
                        resCb.Invoke(new Exception("not found"), null);
                        return;
                    }
                    var res = directRequestProcessingMapForLondon[req];
                    resCb.Invoke(null, res);
                }
            };
            
            var directRequestProcessingMapForKumasi = new Dictionary<IQuasiHttpRequest, IQuasiHttpResponse>();
            IQuasiHttpRequest req3 = new DefaultQuasiHttpRequest();
            IQuasiHttpResponse res3 = new DefaultQuasiHttpResponse();
            directRequestProcessingMapForKumasi.Add(req3, res3);
            kumasiClient.Application = new ConfigurableQuasiHttpApplication
            {
                ProcessRequestCallback = (req, resCb) =>
                {
                    if (!directRequestProcessingMapForKumasi.ContainsKey(req))
                    {
                        resCb.Invoke(new Exception("not found"), null);
                        return;
                    }
                    var res = directRequestProcessingMapForKumasi[req];
                    resCb.Invoke(null, res);
                }
            };

            // act.
            londonInstance.AllocateConnection(kumasiEndpoint, (e, conn) =>
            {
                logger.Logs.Add($"{londonEndpoint}.connect({e?.Message},{(conn != null).ToString().ToLower()})");
                if (e != null)
                {
                    return;
                }
                // test that empty writes succeed even without pending reads.
                londonInstance.WriteBytes(conn, new byte[0], 0, 0, e =>
                {
                    logger.Logs.Add($"{londonEndpoint}.write({e?.Message},)");
                    if (e != null)
                    {
                        return;
                    }
                    var queries = new List<string>
                    {
                        "one", "five"
                    };
                    londonClient.IssueQueries(conn, queries, 0);
                });
            });
            eventLoop.ScheduleTimeout(10, _ =>
            {
                kumasiInstance.AllocateConnection(londonEndpoint, (e, conn) =>
                {
                    logger.Logs.Add($"{kumasiEndpoint}.connect({e?.Message},{(conn != null).ToString().ToLower()})");
                    if (e != null)
                    {
                        return;
                    }
                    // test that empty reads succeed even without pending writes.
                    kumasiInstance.ReadBytes(conn, new byte[0], 0, 0, (e, len) =>
                    {
                        logger.Logs.Add($"{kumasiEndpoint}.read({e?.Message},)");
                        if (e != null)
                        {
                            return;
                        }
                        var queries = new List<string>
                        {
                            "baako", "nnan",
                        };
                        kumasiClient.IssueQueries(conn, queries, 0);
                    });
                });
            }, null);
            eventLoop.AdvanceTimeTo(1000);

            // assert.
            eventLoop.RunMutexApiThroughPostCallback = false;

            var cbCalled = false;
            kumasiInstance.ProcessSendRequest("accra", req1, (e, res) =>
            {
                Assert.False(cbCalled);
                Assert.NotNull(e);
                cbCalled = true;
            });
            Assert.True(cbCalled);

            cbCalled = false;
            londonInstance.ProcessSendRequest("manchester", req2, (e, res) =>
            {
                Assert.False(cbCalled);
                Assert.NotNull(e);
                cbCalled = true;
            });
            Assert.True(cbCalled);

            cbCalled = false;
            londonInstance.ProcessSendRequest(kumasiEndpoint, req3, (e, res) =>
            {
                Assert.False(cbCalled);
                Assert.Null(e);
                Assert.Equal(res3, res);
                cbCalled = true;
            });
            Assert.True(cbCalled);

            cbCalled = false;
            londonInstance.ProcessSendRequest(kumasiEndpoint, req1, (e, res) =>
            {
                Assert.False(cbCalled);
                Assert.NotNull(e);
                Assert.Equal("not found", e.Message);
                cbCalled = true;
            });
            Assert.True(cbCalled);

            cbCalled = false;
            kumasiInstance.ProcessSendRequest(londonEndpoint, req1, (e, res) =>
            {
                Assert.False(cbCalled);
                Assert.Null(e);
                Assert.Equal(res1, res);
                cbCalled = true;
            });
            Assert.True(cbCalled);

            cbCalled = false;
            kumasiInstance.ProcessSendRequest(londonEndpoint, req2, (e, res) =>
            {
                Assert.False(cbCalled);
                Assert.Null(e);
                Assert.Equal(res2, res);
                cbCalled = true;
            });
            Assert.True(cbCalled);

            cbCalled = false;
            kumasiInstance.ProcessSendRequest(londonEndpoint, req3, (e, res) =>
            {
                Assert.False(cbCalled);
                Assert.NotNull(e);
                Assert.Equal("not found", e.Message);
                cbCalled = true;
            });
            Assert.True(cbCalled);

            // test that releasing invalid connections causes no problems.
            kumasiInstance.OnReleaseConnection(null);
            londonInstance.OnReleaseConnection("meet");

            // assert expected connection usage.
            var expectedLogs = new List<string>();
            expectedLogs.Add($"{kumasiEndpoint}.accept(true)");
            expectedLogs.Add($"{londonEndpoint}.connect(,true)");
            expectedLogs.Add($"{londonEndpoint}.write(,)");
            expectedLogs.Add($"{kumasiEndpoint}.read(,o)");
            expectedLogs.Add($"{londonEndpoint}.write(,o)");
            expectedLogs.Add($"{kumasiEndpoint}.read(,ne)");
            expectedLogs.Add($"{londonEndpoint}.write(,ne)");
            expectedLogs.Add($"{londonEndpoint}.read(,b)");
            expectedLogs.Add($"{londonEndpoint}.read(,aako)");
            expectedLogs.Add($"{kumasiEndpoint}.write(,baako)");
            expectedLogs.Add($"{kumasiEndpoint}.read(,f)");
            expectedLogs.Add($"{londonEndpoint}.write(,f)");
            expectedLogs.Add($"{kumasiEndpoint}.read(,ive)");
            expectedLogs.Add($"{londonEndpoint}.write(,ive)");
            expectedLogs.Add($"{londonEndpoint}.read(,n)");
            expectedLogs.Add($"{londonEndpoint}.read(,num)");
            expectedLogs.Add($"{kumasiEndpoint}.write(,nnum)");
            expectedLogs.Add($"{kumasiEndpoint}.read(,x)");
            expectedLogs.Add($"{londonEndpoint}.write(,x)");
            expectedLogs.Add($"{kumasiEndpoint}.read(connection reset,)");
            expectedLogs.Add($"{londonEndpoint}.write(connection reset,)");

            expectedLogs.Add($"{londonEndpoint}.accept(true)");
            expectedLogs.Add($"{kumasiEndpoint}.connect(,true)");
            expectedLogs.Add($"{kumasiEndpoint}.read(,)");
            expectedLogs.Add($"{londonEndpoint}.read(,b)");
            expectedLogs.Add($"{kumasiEndpoint}.write(,b)");
            expectedLogs.Add($"{londonEndpoint}.read(,aako)");
            expectedLogs.Add($"{kumasiEndpoint}.write(,aako)");
            expectedLogs.Add($"{kumasiEndpoint}.read(,o)");
            expectedLogs.Add($"{kumasiEndpoint}.read(,ne)");
            expectedLogs.Add($"{londonEndpoint}.write(,one)");
            expectedLogs.Add($"{londonEndpoint}.read(,n)");
            expectedLogs.Add($"{kumasiEndpoint}.write(,n)");
            expectedLogs.Add($"{londonEndpoint}.read(,nan)");
            expectedLogs.Add($"{kumasiEndpoint}.write(,nan)");
            expectedLogs.Add($"{kumasiEndpoint}.read(,f)");
            expectedLogs.Add($"{kumasiEndpoint}.read(,our)");
            expectedLogs.Add($"{londonEndpoint}.write(,four)");
            expectedLogs.Add($"{londonEndpoint}.read(,x)");
            expectedLogs.Add($"{kumasiEndpoint}.write(,x)");
            expectedLogs.Add($"{londonEndpoint}.read(connection reset,)");
            expectedLogs.Add($"{kumasiEndpoint}.write(connection reset,)");

            logger.AssertEqual(expectedLogs, _outputHelper);
        }

        [Fact]
        public void TestOperationForReentrancy()
        {
            // arrange.
            var eventLoop = new TestEventLoopApiPrev
            {
                RunMutexApiThroughPostCallback = false
            };
            var logger = new OutputEventLogger
            {
                Logs = new List<string>()
            };
            var hub = new MemoryBasedTransportHub();

            object londonEndpoint = "london";
            object kumasiEndpoint = "kumasi";
            var kumasiTranslations = new Dictionary<string, string>
            {
                { "one", "baako" }, { "two", "mmienu" }, { "three", "mmi\u025Bnsa" },
                { "four", "nnan" }, { "five", "nnum" }
            };
            var londonTranslations = new Dictionary<string, string>();
            foreach (var entry in kumasiTranslations)
            {
                londonTranslations.Add(entry.Value, entry.Key);
            }
            var londonClient = new TestQuasiHttpClient(londonEndpoint, logger, londonTranslations);
            var kumasiClient = new TestQuasiHttpClient(kumasiEndpoint, logger, kumasiTranslations);
            hub.Clients.Add(londonEndpoint, londonClient);
            hub.Clients.Add(kumasiEndpoint, kumasiClient);

            var londonInstance = new MemoryBasedTransport
            {
                Hub = hub,
                Mutex = eventLoop,
                MaxChunkSize = 50
            };
            var kumasiInstance = new MemoryBasedTransport
            {
                Hub = hub,
                Mutex = eventLoop,
                MaxChunkSize = 30
            };
            londonClient.Transport = londonInstance;
            kumasiClient.Transport = kumasiInstance;

            // act.
            londonInstance.AllocateConnection(kumasiEndpoint, (e, conn) =>
            {
                logger.Logs.Add($"{londonEndpoint}.connect({e?.Message},{(conn != null).ToString().ToLower()})");
                if (e != null)
                {
                    return;
                }
                // test that empty writes succeed even without pending reads.
                londonInstance.WriteBytes(conn, new byte[0], 0, 0, e =>
                {
                    logger.Logs.Add($"{londonEndpoint}.write({e?.Message},)");
                    if (e != null)
                    {
                        return;
                    }
                    var queries = new List<string>
                    {
                        "one", "five"
                    };
                    londonClient.IssueQueries(conn, queries, 0);
                });
            });

            // assert
            var expectedLogs = new List<string>();
            expectedLogs.Add($"{kumasiEndpoint}.accept(true)");
            expectedLogs.Add($"{londonEndpoint}.connect(,true)");
            expectedLogs.Add($"{londonEndpoint}.write(,)");
            expectedLogs.Add($"{kumasiEndpoint}.read(,o)");
            expectedLogs.Add($"{londonEndpoint}.write(,o)");
            expectedLogs.Add($"{kumasiEndpoint}.read(,ne)");
            expectedLogs.Add($"{londonEndpoint}.write(,ne)");
            expectedLogs.Add($"{londonEndpoint}.read(,b)");
            expectedLogs.Add($"{londonEndpoint}.read(,aako)");
            expectedLogs.Add($"{kumasiEndpoint}.write(,baako)");
            expectedLogs.Add($"{kumasiEndpoint}.read(,f)");
            expectedLogs.Add($"{londonEndpoint}.write(,f)");
            expectedLogs.Add($"{kumasiEndpoint}.read(,ive)");
            expectedLogs.Add($"{londonEndpoint}.write(,ive)");
            expectedLogs.Add($"{londonEndpoint}.read(,n)");
            expectedLogs.Add($"{londonEndpoint}.read(,num)");
            expectedLogs.Add($"{kumasiEndpoint}.write(,nnum)");
            expectedLogs.Add($"{kumasiEndpoint}.read(,x)");
            expectedLogs.Add($"{kumasiEndpoint}.read(connection reset,)");
            expectedLogs.Add($"{londonEndpoint}.write(,x)");
            expectedLogs.Add($"{londonEndpoint}.write(connection reset,)");

            logger.AssertEqual(expectedLogs, _outputHelper);
        }

        [Fact]
        public void TestErrorUsage()
        {
            var instance = new MemoryBasedTransport();
            Assert.Throws<ArgumentException>(() =>
            {
                instance.ProcessSendRequest(null, new DefaultQuasiHttpRequest(), (e, res) => { });
            });
            Assert.Throws<ArgumentException>(() =>
            {
                instance.ProcessSendRequest(-1, null, (e, res) => { });
            });
            Assert.Throws<ArgumentException>(() =>
            {
                instance.ProcessSendRequest(-1, new DefaultQuasiHttpRequest(), null);
            });
            Assert.Throws<ArgumentException>(() =>
            {
                instance.AllocateConnection(null, (e, conn) => { });
            });
            Assert.Throws<ArgumentException>(() =>
            {
                instance.AllocateConnection(5, null);
            });
            Assert.Throws<ArgumentException>(() =>
            {
                instance.ReadBytes(null, new byte[0], 0, 0, (e, len) => { });
            });
            Assert.ThrowsAny<Exception>(() =>
            {
                instance.ReadBytes(4, new byte[2], 0, 1, (e, len) => { });
            });
            Assert.Throws<ArgumentException>(() =>
            {
                instance.ReadBytes(new MemoryBasedTransportConnectionInternal(4), new byte[1], 1, 1, (e, len) => { });
            });
            Assert.Throws<ArgumentException>(() =>
            {
                instance.ReadBytes(new MemoryBasedTransportConnectionInternal(4), new byte[1], 0, 1, null);
            });
            Assert.Throws<ArgumentException>(() =>
            {
                instance.WriteBytes(null, new byte[0], 0, 0, e => { });
            });
            Assert.ThrowsAny<Exception>(() =>
            {
                instance.WriteBytes(4, new byte[2], 0, 1, e => { });
            });
            Assert.Throws<ArgumentException>(() =>
            {
                instance.WriteBytes(new MemoryBasedTransportConnectionInternal(4), new byte[1], 1, 1, e => { });
            });
            Assert.Throws<ArgumentException>(() =>
            {
                instance.WriteBytes(new MemoryBasedTransportConnectionInternal(4), new byte[1], 0, 1, null);
            });
        }

        private class TestQuasiHttpClient : IQuasiHttpClient
        {
            private readonly OutputEventLogger _logger;
            private readonly Dictionary<string, string> _translations;

            public TestQuasiHttpClient(object localEndpoint, OutputEventLogger logger, Dictionary<string, string> translations)
            {
                LocalEndpoint = localEndpoint;
                _logger = logger;
                _translations = translations;
            }

            public object LocalEndpoint { get; }
            public int DefaultTimeoutMillis { get; set; }
            public IQuasiHttpApplication Application { get; set; }
            public IQuasiHttpTransport Transport { get; set; }

            public void IssueQueries(object connection, List<string> queries, int index)
            {
                if (index >= queries.Count)
                {
                    Transport.WriteBytes(connection, new byte[] { (byte)'x' }, 0, 1, e =>
                    {
                        _logger.Logs.Add($"{LocalEndpoint}.write({e?.Message},x)");
                        if (e != null)
                        {
                            return;
                        }
                        Transport.OnReleaseConnection(connection);
                        // test write error after release connection.
                        Transport.WriteBytes(connection, new byte[0], 0, 0, e =>
                        {
                            _logger.Logs.Add($"{LocalEndpoint}.write({e?.Message},)");
                        });
                    });
                    return;
                }
                var dataStrPrefix = queries[index].Substring(0, 1);
                var data = Encoding.UTF8.GetBytes(dataStrPrefix);
                Transport.WriteBytes(connection, data, 0, data.Length, e =>
                {
                    _logger.Logs.Add($"{LocalEndpoint}.write({e?.Message},{dataStrPrefix})");
                    if (e != null)
                    {
                        return;
                    }
                    var dataStrSuffix = queries[index].Substring(1);
                    var data = Encoding.UTF8.GetBytes(dataStrSuffix);
                    Transport.WriteBytes(connection, data, 0, data.Length, e =>
                    {
                        _logger.Logs.Add($"{LocalEndpoint}.write({e?.Message},{dataStrSuffix})");
                        if (e != null)
                        {
                            return;
                        }
                        var data = new byte[Transport.MaxChunkSize];
                        Transport.ReadBytes(connection, data, 0, 1, (e, len) =>
                        {
                            var dataStr = Encoding.UTF8.GetString(data, 0, len);
                            _logger.Logs.Add($"{LocalEndpoint}.read({e?.Message},{dataStr})");
                            if (e != null)
                            {
                                return;
                            }
                            Transport.ReadBytes(connection, data, 1, data.Length-1, (e, len) =>
                            {
                                var dataStr = Encoding.UTF8.GetString(data, 1, len);
                                _logger.Logs.Add($"{LocalEndpoint}.read({e?.Message},{dataStr})");
                                if (e != null)
                                {
                                    return;
                                }
                                IssueQueries(connection, queries, index + 1);
                            });
                        });
                    });
                });
            }

            public void OnReceive(object connection)
            {
                _logger.Logs.Add($"{LocalEndpoint}.accept({(connection != null).ToString().ToLower()})");
                MakeNextRead(connection);
            }

            private void MakeNextRead(object connection)
            {
                var data = new byte[Transport.MaxChunkSize];
                Transport.ReadBytes(connection, data, 0, data.Length, (e, bytesRead) =>
                {
                    string readMsg = null;
                    if (e == null)
                    {
                        readMsg = Encoding.UTF8.GetString(data, 0, bytesRead);
                    }
                    _logger.Logs.Add($"{LocalEndpoint}.read({e?.Message},{readMsg})");
                    if (e != null)
                    {
                        return;
                    }
                    if (readMsg == "x")
                    {
                        Transport.OnReleaseConnection(connection);
                        // test read error after release connection.
                        Transport.ReadBytes(connection, data, 0, 0, (e, len) =>
                        {
                            _logger.Logs.Add($"{LocalEndpoint}.read({e?.Message},)");
                        });
                        return;
                    }
                    Transport.ReadBytes(connection, data, 0, data.Length, (e, bytesRead) =>
                    {
                        string readMsg2 = null;
                        if (e == null)
                        {
                            readMsg2 = Encoding.UTF8.GetString(data, 0, bytesRead);
                        }
                        _logger.Logs.Add($"{LocalEndpoint}.read({e?.Message},{readMsg2})");
                        if (e != null)
                        {
                            return;
                        }
                        var translation = _translations[readMsg + readMsg2];
                        data = Encoding.UTF8.GetBytes(translation);
                        Transport.WriteBytes(connection, data, 0, data.Length, e =>
                        {
                            _logger.Logs.Add($"{LocalEndpoint}.write({e?.Message},{translation})");
                            if (e != null)
                            {
                                return;
                            }
                            MakeNextRead(connection);
                        });
                    });
                });
            }

            public void Send(object remoteEndpoint, IQuasiHttpRequest request, IQuasiHttpSendOptions options,
                Action<Exception, IQuasiHttpResponse> cb)
            {
                throw new NotImplementedException();
            }

            public void Reset(Exception cause, Action<Exception> cb)
            {
                throw new NotImplementedException();
            }
        }
    }*/
}
