using Kabomu.Mediator.Path;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Kabomu.Tests.Mediator.Path
{
    public class PathUtilsInternalTest
    {
        [Fact]
        public void TestReverseUnnecessaryUriEscapes()
        {
            var actual = PathUtilsInternal.ReverseUnnecessaryUriEscapes("");
            Assert.Equal("", actual);

            actual = PathUtilsInternal.ReverseUnnecessaryUriEscapes("ad");
            Assert.Equal("ad", actual);

            actual = PathUtilsInternal.ReverseUnnecessaryUriEscapes("%");
            Assert.Equal("%", actual);

            actual = PathUtilsInternal.ReverseUnnecessaryUriEscapes("%%gh");
            Assert.Equal("%%gh", actual);

            actual = PathUtilsInternal.ReverseUnnecessaryUriEscapes("%25%20ad\n%41");
            Assert.Equal("%25%20ad\nA", actual);

            actual = PathUtilsInternal.ReverseUnnecessaryUriEscapes("%20%25ad\n%41%42-%61%63%");
            Assert.Equal("%20%25ad\nAB-ac%", actual);
        }

        [Fact]
        public void TestFastConvertPercentEncodedToPositiveNum()
        {
            var s = new StringBuilder();
            s.Append("%=:-21-%24-%26-%27-%28-%29-%2a-%2b-%2c-%3B-%3D-%3A-%40-%25-");
            s.Append("%41-%42-%59-%5a-%61-%62-%79-%7a-%30-%31-%38-%39");

            var actual = PathUtilsInternal.FastConvertPercentEncodedToPositiveNum(s, 0);
            Assert.Equal(0, actual);

            actual = PathUtilsInternal.FastConvertPercentEncodedToPositiveNum(s, 1);
            Assert.Equal(0, actual);

            actual = PathUtilsInternal.FastConvertPercentEncodedToPositiveNum(s, 4);
            Assert.Equal('!', actual);

            actual = PathUtilsInternal.FastConvertPercentEncodedToPositiveNum(s, 8);
            Assert.Equal('$', actual);

            actual = PathUtilsInternal.FastConvertPercentEncodedToPositiveNum(s, 12);
            Assert.Equal('&', actual);

            actual = PathUtilsInternal.FastConvertPercentEncodedToPositiveNum(s, 16);
            Assert.Equal('\'', actual);

            actual = PathUtilsInternal.FastConvertPercentEncodedToPositiveNum(s, 20);
            Assert.Equal('(', actual);

            actual = PathUtilsInternal.FastConvertPercentEncodedToPositiveNum(s, 24);
            Assert.Equal(')', actual);

            actual = PathUtilsInternal.FastConvertPercentEncodedToPositiveNum(s, 28);
            Assert.Equal('*', actual);

            actual = PathUtilsInternal.FastConvertPercentEncodedToPositiveNum(s, 32);
            Assert.Equal('+', actual);

            actual = PathUtilsInternal.FastConvertPercentEncodedToPositiveNum(s, 36);
            Assert.Equal(',', actual);

            actual = PathUtilsInternal.FastConvertPercentEncodedToPositiveNum(s, 40);
            Assert.Equal(';', actual);

            actual = PathUtilsInternal.FastConvertPercentEncodedToPositiveNum(s, 44);
            Assert.Equal('=', actual);

            actual = PathUtilsInternal.FastConvertPercentEncodedToPositiveNum(s, 48);
            Assert.Equal(':', actual);

            actual = PathUtilsInternal.FastConvertPercentEncodedToPositiveNum(s, 52);
            Assert.Equal('@', actual);

            actual = PathUtilsInternal.FastConvertPercentEncodedToPositiveNum(s, 56);
            Assert.Equal(0, actual);

            actual = PathUtilsInternal.FastConvertPercentEncodedToPositiveNum(s, 60);
            Assert.Equal('A', actual);

            actual = PathUtilsInternal.FastConvertPercentEncodedToPositiveNum(s, 64);
            Assert.Equal('B', actual);

            actual = PathUtilsInternal.FastConvertPercentEncodedToPositiveNum(s, 68);
            Assert.Equal('Y', actual);

            actual = PathUtilsInternal.FastConvertPercentEncodedToPositiveNum(s, 72);
            Assert.Equal('Z', actual);

            actual = PathUtilsInternal.FastConvertPercentEncodedToPositiveNum(s, 76);
            Assert.Equal('a', actual);

            actual = PathUtilsInternal.FastConvertPercentEncodedToPositiveNum(s, 80);
            Assert.Equal('b', actual);

            actual = PathUtilsInternal.FastConvertPercentEncodedToPositiveNum(s, 84);
            Assert.Equal('y', actual);

            actual = PathUtilsInternal.FastConvertPercentEncodedToPositiveNum(s, 88);
            Assert.Equal('z', actual);

            actual = PathUtilsInternal.FastConvertPercentEncodedToPositiveNum(s, 92);
            Assert.Equal('0', actual);

            actual = PathUtilsInternal.FastConvertPercentEncodedToPositiveNum(s, 96);
            Assert.Equal('1', actual);

            actual = PathUtilsInternal.FastConvertPercentEncodedToPositiveNum(s, 100);
            Assert.Equal('8', actual);

            actual = PathUtilsInternal.FastConvertPercentEncodedToPositiveNum(s, 104);
            Assert.Equal('9', actual);
        }
    }
}
