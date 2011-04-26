#region Copyright 2010-2011 by Roger Knapp, Licensed under the Apache License, Version 2.0
/* Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *   http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
#endregion
using System;
using System.Collections.Generic;
using NUnit.Framework;
using CSharpTest.Net.Data;
using System.IO;
using CSharpTest.Net.IO;
using System.Data;

#pragma warning disable 1591
namespace CSharpTest.Net.Library.Test
{
    [TestFixture]
    [Category("TestCsvReader")]
    public partial class TestCsvReader
    {
        const string CSV_DOC1 = @"f1,f2, f3
1a, 1b, 1c
2a,2b , ""2"""",
c"" , extra
3a
4a,
5a,,
";
        static readonly string[,] CSV_DOC1_VAL = new string[,] 
        {
            { "1a", "1b", "1c" },
            { "2a", "2b", "2\",\r\nc" },
            { "3a", null, null },
            { "4a", "", null },
            { "5a", "", "" }
        };

        [Test]
        public void TestFieldInfo()
        {
            using (CsvReader r = new CsvReader(new StringReader(CSV_DOC1)))
            {
                Assert.AreEqual(3, r.FieldCount);
                for (int i = 0; i < r.FieldCount; i++)
                {
                    Assert.AreEqual(typeof(String), r.GetFieldType(i));
                    Assert.AreEqual(typeof(String).Name, r.GetDataTypeName(i));
                    Assert.AreEqual("f" + (i + 1).ToString(), r.GetName(i).ToString());
                    Assert.AreEqual(i, r.GetOrdinal("f" + (i + 1).ToString()));
                }

                while (r.Read())
                { }

                Assert.IsFalse(r.IsClosed);
                Assert.IsFalse(r.NextResult());
                Assert.IsTrue(r.IsClosed);
            }
        }

        [Test]
        public void TestJaggedField()
        {
            using (CsvReader r = new CsvReader(new StringReader(CSV_DOC1)))
            {
                Assert.AreEqual(0, r.Depth);
                Assert.IsFalse(r.IsClosed);
                Assert.AreEqual(3, r.FieldCount);
                Assert.IsTrue(r.Read());
                Assert.AreEqual(3, r.FieldCount);
                Assert.IsTrue(r.Read());
                
                //second row has an extra field
                Assert.AreEqual(4, r.FieldCount);
                Assert.AreEqual("3", r.GetName(3));
                Assert.AreEqual(3, r.GetOrdinal("3"));
                Assert.AreEqual("extra", r.GetValue(3));

                Assert.IsTrue(r.Read());
                Assert.IsFalse(r.IsDBNull(0));
                Assert.AreEqual("3a", r.GetString("f1"));
                Assert.IsTrue(r.IsDBNull(1));
                Assert.IsTrue(r.IsDBNull(2));

                Assert.IsTrue(r.Read());
                Assert.IsTrue(r.Read());
                Assert.IsFalse(r.Read());
            }
        }

        [Test]
        public void TestSchemaTable()
        {
            DataTable dt = new CsvReader(new StringReader(CSV_DOC1)).GetSchemaTable();

            Assert.AreEqual(3, dt.Columns.Count);
            Assert.AreEqual("f1", dt.Columns[0].ColumnName);
            Assert.AreEqual(typeof(String), dt.Columns[0].DataType);
            Assert.AreEqual("f2", dt.Columns[1].ColumnName);
            Assert.AreEqual(typeof(String), dt.Columns[1].DataType);
            Assert.AreEqual("f3", dt.Columns[2].ColumnName);
            Assert.AreEqual(typeof(String), dt.Columns[2].DataType);
        }

        [Test]
        public void TestFieldValues()
        {
            using (TempFile temp = new TempFile())
            {
                File.WriteAllText(temp.TempPath, CSV_DOC1);
                using (CsvReader r = new CsvReader(temp.TempPath))
                {
                    Assert.AreEqual(3, r.FieldCount);
                    int row = 0;
                    while (r.Read())
                    {
                        for (int col = 0; col < 3; col++)
                        {
                            Assert.AreEqual(CSV_DOC1_VAL[row, col], r.GetString(col));
                        }

                        row++;
                    }

                    Assert.AreEqual(5, row);
                    Assert.AreEqual(5, r.RecordsAffected);
                }
            }
        }

        [Test]
        public void TestIndexValue()
        {
            using (CsvReader r = new CsvReader(new StringReader(CSV_DOC1)))
            {
                r.Read();
                Assert.AreEqual("1a", r["f1"]);
                Assert.AreEqual("1b", r[1]);
                Assert.AreEqual("1c", r.GetString(2));
                Assert.AreEqual("1c", r.GetValues()[2]);
            }

        }

        [Test,ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void TestBadFieldName()
        {
            using (CsvReader r = new CsvReader(new StringReader(CSV_DOC1)))
            {
                r.Read();
                r.GetString("this does not exist");
            }
        }

        [Test]
        public void TestValueTypes()
        {
            string content = @"True, 129, f0a12b, a, string, ""a,b,c"", 1993-06-26 2:00pm, 0.25, {74A51B0C-21B1-42b4-BAE4-AE6480B8B8DB}, 20000, -2000000000, 2000000000000";
            using (CsvReader r = new CsvReader(new StringReader(content), CsvOptions.None))
            {
                int field = 0;
                byte[] buf = new byte[10];
                char[] ch = new char[10];

                Assert.IsTrue(r.Read());
                Assert.AreEqual(true, r.GetBoolean(field++));
                Assert.AreEqual((byte)129, r.GetByte(field++));

                Assert.AreEqual(3L, r.GetBytes(field++, 0, buf, 0, buf.Length));
                Assert.AreEqual((byte)0xf0, buf[0]);
                Assert.AreEqual((byte)0xa1, buf[1]);
                Assert.AreEqual((byte)0x2b, buf[2]);
                Assert.AreEqual((byte)0, buf[3]);

                Assert.AreEqual('a', r.GetChar(field++));

                Assert.AreEqual(6, r.GetChars(field++, 0, ch, 0, ch.Length));
                Assert.AreEqual("string", new String(ch, 0, 6));

                using (IDataReader c = r.GetData(field++))
                {
                    Assert.AreEqual(1, c.Depth);
                    Assert.AreEqual(0, c.FieldCount);
                    Assert.IsTrue(c.Read());
                    Assert.AreEqual(3, c.FieldCount);
                    Assert.AreEqual("a", c.GetString(0));
                    Assert.AreEqual("b", c.GetString(1));
                    Assert.AreEqual("c", c.GetString(2));
                }

                Assert.AreEqual(DateTime.Parse("1993-06-26 2:00pm"), r.GetDateTime(field++));

                Assert.AreEqual((decimal)0.25, r.GetDecimal(field));
                Assert.AreEqual(0.25f, r.GetFloat(field));
                Assert.AreEqual(0.25, r.GetDouble(field++));

                Assert.AreEqual(new Guid("74A51B0C-21B1-42b4-BAE4-AE6480B8B8DB"), r.GetGuid(field++));

                Assert.AreEqual((short)20000, r.GetInt16(field++));
                Assert.AreEqual(-2000000000, r.GetInt32(field++));
                Assert.AreEqual(2000000000000L, r.GetInt64(field++));
            }
        }
    }
}
