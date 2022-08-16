using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Kabomu.Tests.Common
{
    public class CsvUtilsTest
    {
        [Theory]
        [MemberData(nameof(CreateTestEscapeValueData))]
        public void TestEscapeValue(string raw, string expected)
        {
            var actual = CsvUtils.EscapeValue(raw);
            Assert.Equal(expected, actual);
        }

        public static List<object[]> CreateTestEscapeValueData()
        {
            return new List<object[]>
            {
                new object[]{ "", "\"\"" },
                new object[]{ "d", "d" },
                new object[]{ "\n", "\"\n\"" },
                new object[]{ "\r", "\"\r\"" },
                new object[]{ "m,n", "\"m,n\"" },
                new object[]{ "m\"n", "\"m\"\"n\"" }
            };
        }

        [Theory]
        [MemberData(nameof(CreateTestUnescapeValueData))]
        public void TestUnescapeValue(string escaped, string expected)
        {
            var actual = CsvUtils.UnescapeValue(escaped);
            Assert.Equal(expected, actual);
        }

        public static List<object[]> CreateTestUnescapeValueData()
        {
            return new List<object[]>
            {
                new object[]{ "\"\"", "" },
                new object[]{ "d", "d" },
                new object[]{ "\"\n\"", "\n" },
                new object[]{ "\"\r\"", "\r" },
                new object[]{ "\"m,n\"", "m,n" },
                new object[]{ "\"m\"\"n\"", "m\"n" }
            };
        }

        [Theory]
        [MemberData(nameof(CreateTestUnescapeValueForErrorsData))]
        public void TestUnescapeValueForErrors(string escaped)
        {
            Assert.Throws<ArgumentException>(() =>
            {
                CsvUtils.UnescapeValue(escaped);
            });
        }

        public static List<object[]> CreateTestUnescapeValueForErrorsData()
        {
            return new List<object[]>
            {
                new object[]{ "\"" },
                new object[]{ "d\"" },
                new object[]{ "\"\"\"" },
                new object[]{ "," },
                new object[]{ "m,n\n" },
                new object[]{ "\"m\"n" }
            };
        }

        [Theory]
        [MemberData(nameof(CreateTestSerializeData))]
        public void TestSerialize(IList<IList<string>> rows, string expected)
        {
            var actual = CsvUtils.Serialize(rows);
            Assert.Equal(expected, actual);
        }

        public static List<object[]> CreateTestSerializeData()
        {
            var testData = new List<object[]>();

            var rows = new List<IList<string>>();
            var expected = "";
            testData.Add(new object[] { rows, expected });

            rows = new List<IList<string>>
            {
                new List<string>{ "" }
            };
            expected = "\"\"\n";
            testData.Add(new object[] { rows, expected });

            rows = new List<IList<string>>
            {
                new List<string>()
            };
            expected = "\n";
            testData.Add(new object[] { rows, expected });

            rows = new List<IList<string>>
            {
                new List<string>{ "a" },
                new List<string>{ "b", "c" },
            };
            expected = "a\nb,c\n";
            testData.Add(new object[] { rows, expected });

            rows = new List<IList<string>>
            {
                new List<string>{ },
                new List<string>{ ",", "c" },
            };
            expected = "\n\",\",c\n";
            testData.Add(new object[] { rows, expected });

            rows = new List<IList<string>>
            {
                new List<string>{ "head", "tail", "." },
                new List<string>{ "\n", " c\"d " },
                new List<string>{ }
            };
            expected = "head,tail,.\n\"\n\",\" c\"\"d \"\n\n";
            testData.Add(new object[] { rows, expected });

            rows = new List<IList<string>>
            {
                new List<string>{ "a\nb,c\n" },
                new List<string>{ "\n\",\",c\n", "head,tail,.\n\"\n\",\" c\"\"d \"\n\n" },
            };
            expected = "\"a\nb,c\n\"\n" +
                "\"\n\"\",\"\",c\n\",\"head,tail,.\n\"\"\n\"\",\"\" c\"\"\"\"d \"\"\n\n\"\n";
            testData.Add(new object[] { rows, expected });

            return testData;
        }

        [Theory]
        [MemberData(nameof(CreateTestDeserializeData))]
        public void TestDeserialize(string csv, List<List<string>> expected)
        {
            var actual = CsvUtils.Deserialize(csv);
            Assert.Equal(expected, actual);
        }

        public static List<object[]> CreateTestDeserializeData()
        {
            var testData = new List<object[]>();

            var csv = "";
            var expected = new List<List<string>>();
            testData.Add(new object[] { csv, expected });

            csv = "\"\"";
            expected = new List<List<string>>
            {
                new List<string>{ "" }
            };
            testData.Add(new object[] { csv, expected });

            csv = "\n";
            expected = new List<List<string>>
            {
                new List<string>()
            };
            testData.Add(new object[] { csv, expected });

            csv = "\"\",\"\"\n";
            expected = new List<List<string>>
            {
                new List<string>{ "", "" }
            };
            testData.Add(new object[] { csv, expected });

            csv = "\"\",\"\"";
            expected = new List<List<string>>
            {
                new List<string>{ "", "" }
            };
            testData.Add(new object[] { csv, expected });

            csv = "a\nb,c\n";
            expected = new List<List<string>>
            {
                new List<string>{ "a" },
                new List<string>{ "b", "c" },
            };
            testData.Add(new object[] { csv, expected });

            csv = "a\nb,c";
            expected = new List<List<string>>
            {
                new List<string>{ "a" },
                new List<string>{ "b", "c" },
            };
            testData.Add(new object[] { csv, expected });

            csv = "a,\"\"\nb,c";
            expected = new List<List<string>>
            {
                new List<string>{ "a", "" },
                new List<string>{ "b", "c" },
            };
            testData.Add(new object[] { csv, expected });

            csv = "a\nb,";
            expected = new List<List<string>>
            {
                new List<string>{ "a" },
                new List<string>{ "b", "" },
            };
            testData.Add(new object[] { csv, expected });

            csv = "\"a\"\n\"b\",\"\""; // test for unnecessary quotes
            expected = new List<List<string>>
            {
                new List<string>{ "a" },
                new List<string>{ "b", "" },
            };
            testData.Add(new object[] { csv, expected });

            csv = "\r\n\",\",c\r\n";
            expected = new List<List<string>>
            {
                new List<string>{ },
                new List<string>{ ",", "c" },
            };
            testData.Add(new object[] { csv, expected });

            csv = "\n\",\",c";
            expected = new List<List<string>>
            {
                new List<string>{ },
                new List<string>{ ",", "c" },
            };
            testData.Add(new object[] { csv, expected });

            csv = "head,tail,.\n\"\n\",\" c\"\"d \"\n\n";
            expected = new List<List<string>>
            {
                new List<string>{ "head", "tail", "." },
                new List<string>{ "\n", " c\"d " },
                new List<string>{ }
            };
            testData.Add(new object[] { csv, expected });

            csv = "head,tail,.\n\"\n\",\" c\"\"d \"\n";
            expected = new List<List<string>>
            {
                new List<string>{ "head", "tail", "." },
                new List<string>{ "\n", " c\"d " }
            };
            testData.Add(new object[] { csv, expected });

            csv = "head,tail,.\n\"\r\n\",\" c\"\"d \"\r";
            expected = new List<List<string>>
            {
                new List<string>{ "head", "tail", "." },
                new List<string>{ "\r\n", " c\"d " }
            };
            testData.Add(new object[] { csv, expected });

            csv = "\"a\nb,c\n\"\n" +
                "\"\n\"\",\"\",c\n\",\"head,tail,.\n\"\"\n\"\",\"\" c\"\"\"\"d \"\"\n\n\"\n";
            expected = new List<List<string>>
            {
                new List<string>{ "a\nb,c\n" },
                new List<string>{ "\n\",\",c\n", "head,tail,.\n\"\n\",\" c\"\"d \"\n\n" },
            };
            testData.Add(new object[] { csv, expected });

            return testData;
        }

        [Theory]
        [MemberData(nameof(CreateTestDeserializeForErrorsData))]
        public void TestDeserializeForErrors(string csv)
        {
            Assert.Throws<ArgumentException>(() =>
            {
                CsvUtils.Deserialize(csv);
            });
        }

        public static List<object[]> CreateTestDeserializeForErrorsData()
        {
            return new List<object[]>
            {
                new object[]{ "\"" },
                new object[]{ "\"1\"2" },
                new object[]{ "1\"\"2\"" },
                new object[]{ "1,2\",3" },
            };
        }
    }
}
