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
using System.Data;
using System.IO;
using System.Globalization;
using System.Text;

#pragma warning disable 1591
namespace CSharpTest.Net.Data
{
    public enum CsvOptions
    {
        None = 0,
        HasFieldHeaders = 1,
    }

    public class CsvReader : IDataReader
    {
        readonly Dictionary<string, int> _fieldNames;
        readonly TextReader _reader;
        readonly CsvOptions _options;
        readonly IFormatProvider _formatting;
        readonly char _delim;
        readonly int _depth;

        int _recordCount;
        bool _closed;
        string[] _currentFields;

        protected CsvReader(TextReader reader, CsvOptions options, char fieldDelim, IFormatProvider formatter, int depth)
        {
            _fieldNames = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            _delim = fieldDelim;
            _reader = reader;
            _options = options;
            _formatting = formatter;
            _depth = depth;

            _recordCount = 0;
            _closed = false;
            _currentFields = new string[0];

            ReadHeader();
        }

        #region ctor overloads
        public CsvReader(TextReader reader, CsvOptions options, char fieldDelim, IFormatProvider formatter)
            : this(reader, options, fieldDelim, formatter, 0)
        { }

        public CsvReader(string inputFile)
            : this(inputFile, CsvOptions.HasFieldHeaders)
        { }

        public CsvReader(string inputFile, CsvOptions options)
            : this(inputFile, options, ',', CultureInfo.CurrentCulture)
        { }

        public CsvReader(string inputFile, CsvOptions options, char fieldDelim, IFormatProvider formatter)
            : this(new StreamReader(File.Open(inputFile, FileMode.Open, FileAccess.Read, FileShare.Read)), options, fieldDelim, formatter)
        { }

        public CsvReader(TextReader reader)
            : this(reader, CsvOptions.HasFieldHeaders)
        { }

        public CsvReader(TextReader reader, CsvOptions options)
            : this(reader, options, ',', CultureInfo.CurrentCulture)
        { }
        #endregion

        public void Dispose()
        {
            Close();
        }

        public void Close()
        {
            _recordCount = -1;
            _currentFields = new string[0];
            _closed = true;
            _reader.Dispose();
        }

        public bool IsClosed
        {
            get { return _closed; }
        }

        public int Depth
        {
            get { return _depth; }
        }

        public bool NextResult()
        {
            Close();
            return false;
        }

        public static string[] ReadCsvLine(TextReader reader, Char delim)
        {
            bool pending = false;
            char[] newline = Environment.NewLine.ToCharArray();
            const char quote = '"';
            List<string> fields = new List<string>();

            StringBuilder sbField = new StringBuilder();
            int next;
            while (-1 != (next = reader.Read()))
            {
                Char ch = (Char)next;

                if (ch == delim || ch == newline[0])
                {
                    pending = ch == delim;
                    fields.Add(sbField.ToString());
                    sbField.Length = 0;
                }
                if (ch == newline[0])
                    break;//end of line
                if (ch == delim || Char.IsWhiteSpace(ch))
                    continue;

                if (ch == quote)
                {
                    while (true)
                    {
                        ReadUntil(reader, sbField, quote, (char)0xFFFF);
                        reader.Read();
                        if (reader.Peek() == quote)
                        {
                            sbField.Append((Char)reader.Read());
                            continue;
                        }
                        else
                            break;
                    }
                }
                else
                {
                    pending = true;
                    sbField.Append(ch);
                    ReadUntil(reader, sbField, delim, newline[0]);

                    int lastws = sbField.Length;
                    while (lastws > 0 && Char.IsWhiteSpace(sbField[lastws - 1]))
                        lastws--;

                    if (lastws != sbField.Length)
                        sbField.Length = lastws;
                }
            }
            if (pending)
            {
                fields.Add(sbField.ToString());
                sbField.Length = 0;
            }

            return fields.ToArray();
        }

        static void ReadUntil(TextReader reader, StringBuilder sb, char stop1, char stop2)
        {
            int ch = reader.Peek();
            while (ch != -1 && ch != stop1 && ch != stop2)
            {
                sb.Append((Char)reader.Read());
                ch = reader.Peek();
            }
        }

        void ReadHeader()
        {
            if ((_options & CsvOptions.HasFieldHeaders) == CsvOptions.HasFieldHeaders)
            {
                string[] lineText = ReadCsvLine(_reader, _delim);
                for (int i = 0; i < lineText.Length; i++)
                    _fieldNames[lineText[i]] = i;
            }
        }

        public bool Read()
        {
            string[] lineText = ReadCsvLine(_reader, _delim);
            if (lineText.Length == 0 && _reader.Peek() == -1)
                return false;

            if (lineText.Length < _fieldNames.Count)
                Array.Resize(ref lineText, _fieldNames.Count);

            _recordCount++;
            _currentFields = lineText;
            return true;
        }

        public int RecordsAffected
        {
            get { return _recordCount; }
        }

        public int FieldCount
        {
            get { return Math.Max(_fieldNames.Count, _currentFields.Length); }
        }

        public string GetDataTypeName(int i)
        {
            return GetFieldType(i).Name;
        }

        public Type GetFieldType(int i)
        {
            return typeof(String);
        }

        public DataTable GetSchemaTable()
        {
            DataTable dt = new DataTable();
            for (int i = 0; i < FieldCount; i++)
                dt.Columns.Add(new DataColumn(GetName(i), GetFieldType(i)));
            return dt;
        }

        public string GetName(int i)
        {
            Check.InRange(i, 0, FieldCount - 1);

            foreach (KeyValuePair<string, int> kv in _fieldNames)
                if (kv.Value == i)
                    return kv.Key;

            return i.ToString();
        }

        public int GetOrdinal(string name)
        {
            int value;
            if (_fieldNames.TryGetValue(name, out value))
            {
                return value;
            }
            if (int.TryParse(name, out value))
            {
                if (value >= _fieldNames.Count && value < _currentFields.Length)
                    return value;
            }
            throw new ArgumentOutOfRangeException();
        }

        public object this[string name]
        {
            get { return GetValue(GetOrdinal(name)); }
        }

        public object this[int i]
        {
            get { return GetValue(i); }
        }

        public object GetValue(int i)
        {
            return _currentFields[i];
        }

        public object[] GetValues()
        {
            object[] values = new object[_currentFields.Length];
            GetValues(values);
            return values;
        }

        public int GetValues(object[] values)
        {
            _currentFields.CopyTo(values, 0);
            return _currentFields.Length;
        }

        public string GetString(string name)
        {
            return (string)_currentFields[GetOrdinal(name)];
        }

        public string GetString(int i)
        {
            return (string)_currentFields[i];
        }

        public bool IsDBNull(int i)
        {
            return _currentFields[i] == null;
        }

        public bool GetBoolean(int i)
        {
            return bool.Parse(GetString(i));
        }

        public byte GetByte(int i)
        {
            return byte.Parse(GetString(i), _formatting);
        }

        public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
        {
            string hex = GetString(i);
            int ordinal = 0;
            for (int chPos = (int)(fieldOffset * 2); chPos < hex.Length && ordinal < length; chPos += 2, ordinal++)
                buffer[bufferoffset + ordinal] = byte.Parse(hex.Substring(chPos, 2), NumberStyles.AllowHexSpecifier, _formatting);
            return ordinal;
        }

        public char GetChar(int i)
        {
            return GetString(i)[0];
        }

        public long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
        {
            string chars = GetString(i);
            length = Math.Min(chars.Length - (int)fieldoffset, length);
            Array.Copy(chars.ToCharArray(), fieldoffset, buffer, bufferoffset, length);
            return length;
        }

        public IDataReader GetData(int i)
        {
            return new CsvReader(new StringReader(GetString(i)), _options, _delim, _formatting, _depth + 1);
        }

        public DateTime GetDateTime(int i)
        {
            return DateTime.Parse(GetString(i), _formatting);
        }

        public decimal GetDecimal(int i)
        {
            return decimal.Parse(GetString(i), _formatting);
        }

        public double GetDouble(int i)
        {
            return double.Parse(GetString(i), _formatting);
        }

        public float GetFloat(int i)
        {
            return float.Parse(GetString(i), _formatting);
        }

        public Guid GetGuid(int i)
        {
            return new Guid(GetString(i));
        }

        public short GetInt16(int i)
        {
            return short.Parse(GetString(i), _formatting);
        }

        public int GetInt32(int i)
        {
            return int.Parse(GetString(i), _formatting);
        }

        public long GetInt64(int i)
        {
            return long.Parse(GetString(i), _formatting);
        }
    }
}
