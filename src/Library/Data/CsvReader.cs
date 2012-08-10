#region Copyright 2010-2012 by Roger Knapp, Licensed under the Apache License, Version 2.0
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

namespace CSharpTest.Net.Data
{
    /// <summary>
    /// Options that define formatting of the CSV file
    /// </summary>
    public enum CsvOptions
    {
        /// <summary> No options defined </summary>
        None = 0,
        /// <summary> The first line contains the names of the fields </summary>
        HasFieldHeaders = 1,
    }

    /// <summary>
    /// Provides an <see cref="IDataReader"/> interface to CSV/Tab delimited text files.
    /// </summary>
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

        /// <summary> Constructs the CSV reader for the provided text reader </summary>
        /// <param name="reader">The text reader to read from</param>
        /// <param name="options">Options for parsing the text</param>
        /// <param name="fieldDelim">The character used to delineate fields</param>
        /// <param name="formatter">The format provided used for interpreting numbers and dates</param>
        /// <param name="depth">Provides for nested CSV parsers</param>
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
        /// <summary> Constructs the CSV reader for the provided text reader </summary>
        /// <param name="reader">The text reader to read from</param>
        /// <param name="options">Options for parsing the text</param>
        /// <param name="fieldDelim">The character used to delineate fields</param>
        /// <param name="formatter">The format provided used for interpreting numbers and dates</param>
        public CsvReader(TextReader reader, CsvOptions options, char fieldDelim, IFormatProvider formatter)
            : this(reader, options, fieldDelim, formatter, 0)
        { }

        /// <summary> Constructs the CSV reader for the provided text reader </summary>
        /// <param name="inputFile">The text file to read from</param>
        public CsvReader(string inputFile)
            : this(inputFile, CsvOptions.HasFieldHeaders)
        { }

        /// <summary> Constructs the CSV reader for the provided text reader </summary>
        /// <param name="inputFile">The text file to read from</param>
        /// <param name="options">Options for parsing the text</param>
        public CsvReader(string inputFile, CsvOptions options)
            : this(inputFile, options, ',', CultureInfo.CurrentCulture)
        { }

        /// <summary> Constructs the CSV reader for the provided text reader </summary>
        /// <param name="inputFile">The text file to read from</param>
        /// <param name="options">Options for parsing the text</param>
        /// <param name="fieldDelim">The character used to delineate fields</param>
        /// <param name="formatter">The format provided used for interpreting numbers and dates</param>
        public CsvReader(string inputFile, CsvOptions options, char fieldDelim, IFormatProvider formatter)
            : this(new StreamReader(File.Open(inputFile, FileMode.Open, FileAccess.Read, FileShare.Read)), options, fieldDelim, formatter)
        { }

        /// <summary> Constructs the CSV reader for the provided text reader </summary>
        /// <param name="reader">The text reader to read from</param>
        public CsvReader(TextReader reader)
            : this(reader, CsvOptions.HasFieldHeaders)
        { }

        /// <summary> Constructs the CSV reader for the provided text reader </summary>
        /// <param name="reader">The text reader to read from</param>
        /// <param name="options">Options for parsing the text</param>
        public CsvReader(TextReader reader, CsvOptions options)
            : this(reader, options, ',', CultureInfo.CurrentCulture)
        { }
        #endregion

        /// <summary>
        /// Disposes of the reader
        /// </summary>
        public void Dispose()
        {
            Close();
        }

        /// <summary>
        /// Closes the csv reader and disposes the underlying text reader
        /// </summary>
        public void Close()
        {
            _recordCount = -1;
            _currentFields = new string[0];
            _closed = true;
            _reader.Dispose();
        }

        /// <summary>
        /// Returns true if the reader has been closed
        /// </summary>
        public bool IsClosed
        {
            get { return _closed; }
        }

        /// <summary>
        /// Returns the depth (zero based) of the reader when using nested CSV parsers
        /// </summary>
        public int Depth
        {
            get { return _depth; }
        }

        bool IDataReader.NextResult()
        {
            Close();
            return false;
        }

        /// <summary>
        /// Provides a single-record parser of CSV content
        /// </summary>
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

        /// <summary>
        /// Advances the <see cref="T:System.Data.IDataReader"/> to the next record.
        /// </summary>
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
        /// <summary>
        /// Returns the current record number of the parser
        /// </summary>
        public int RecordsAffected
        {
            get { return _recordCount; }
        }
        /// <summary>
        /// Returns the number of fields defined in this record
        /// </summary>
        public int FieldCount
        {
            get { return Math.Max(_fieldNames.Count, _currentFields.Length); }
        }

        string IDataRecord.GetDataTypeName(int i)
        {
            return GetFieldType(i).Name;
        }
        /// <summary>
        /// Returns typeof(String)
        /// </summary>
        public Type GetFieldType(int i)
        {
            return typeof(String);
        }
        /// <summary>
        /// Returns a DataTable which defines the columns in this CSV file
        /// </summary>
        public DataTable GetSchemaTable()
        {
            DataTable dt = new DataTable();
            for (int i = 0; i < FieldCount; i++)
                dt.Columns.Add(new DataColumn(GetName(i), GetFieldType(i)));
            return dt;
        }
        /// <summary>
        /// Returns the name of the column by ordinal
        /// </summary>
        public string GetName(int i)
        {
            Check.InRange(i, 0, FieldCount - 1);

            foreach (KeyValuePair<string, int> kv in _fieldNames)
                if (kv.Value == i)
                    return kv.Key;

            return i.ToString();
        }
        /// <summary>
        /// Returns the ordinal of the column by name
        /// </summary>
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
        /// <summary>
        /// Returns the string content of the field by name
        /// </summary>
        public object this[string name]
        {
            get { return GetValue(GetOrdinal(name)); }
        }
        /// <summary>
        /// Returns the string content of the field by ordinal
        /// </summary>
        public object this[int i]
        {
            get { return GetValue(i); }
        }
        /// <summary>
        /// Returns the string content of the field by ordinal
        /// </summary>
        public object GetValue(int i)
        {
            return _currentFields[i];
        }
        /// <summary>
        /// Returns an object[] containing all the strings for the current record.
        /// </summary>
        public object[] GetValues()
        {
            object[] values = new object[_currentFields.Length];
            GetValues(values);
            return values;
        }
        /// <summary>
        /// Fills an object[] with all the strings for the current record.
        /// </summary>
        public int GetValues(object[] values)
        {
            _currentFields.CopyTo(values, 0);
            return _currentFields.Length;
        }

        /// <summary>
        /// Gets the string value of the specified field.
        /// </summary>
        public string GetString(string name)
        {
            return (string)_currentFields[GetOrdinal(name)];
        }

        /// <summary>
        /// Gets the string value of the specified field.
        /// </summary>
        public string GetString(int i)
        {
            return (string)_currentFields[i];
        }

        /// <summary>
        /// Return whether the specified field is set to null.
        /// </summary>
        public bool IsDBNull(int i)
        {
            return _currentFields[i] == null;
        }

        /// <summary>
        /// Gets the value of the specified column as a Boolean.
        /// </summary>
        public bool GetBoolean(int i)
        {
            return bool.Parse(GetString(i));
        }

        /// <summary>
        /// Gets the 8-bit unsigned integer value of the specified column.
        /// </summary>
        public byte GetByte(int i)
        {
            return byte.Parse(GetString(i), _formatting);
        }

        /// <summary>
        /// Reads a stream of bytes from the specified column offset into the buffer as an array, starting at the given buffer offset.
        /// </summary>
        public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
        {
            string hex = GetString(i);
            int ordinal = 0;
            for (int chPos = (int)(fieldOffset * 2); chPos < hex.Length && ordinal < length; chPos += 2, ordinal++)
                buffer[bufferoffset + ordinal] = byte.Parse(hex.Substring(chPos, 2), NumberStyles.AllowHexSpecifier, _formatting);
            return ordinal;
        }

        /// <summary>
        /// Gets the character value of the specified column.
        /// </summary>
        public char GetChar(int i)
        {
            return GetString(i)[0];
        }

        /// <summary>
        /// Reads a stream of characters from the specified column offset into the buffer as an array, starting at the given buffer offset.
        /// </summary>
        public long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
        {
            string chars = GetString(i);
            length = Math.Min(chars.Length - (int)fieldoffset, length);
            Array.Copy(chars.ToCharArray(), fieldoffset, buffer, bufferoffset, length);
            return length;
        }

        /// <summary>
        /// Returns a <see cref="CsvReader"/> for the specified column ordinal.
        /// </summary>
        public IDataReader GetData(int i)
        {
            return new CsvReader(new StringReader(GetString(i)), _options, _delim, _formatting, _depth + 1);
        }

        /// <summary>
        /// Gets the date and time data value of the specified field.
        /// </summary>
        public DateTime GetDateTime(int i)
        {
            return DateTime.Parse(GetString(i), _formatting);
        }

        /// <summary>
        /// Gets the fixed-position numeric value of the specified field.
        /// </summary>
        public decimal GetDecimal(int i)
        {
            return decimal.Parse(GetString(i), _formatting);
        }

        /// <summary>
        /// Gets the double-precision floating point number of the specified field.
        /// </summary>
        public double GetDouble(int i)
        {
            return double.Parse(GetString(i), _formatting);
        }

        /// <summary>
        /// Gets the single-precision floating point number of the specified field.
        /// </summary>
        public float GetFloat(int i)
        {
            return float.Parse(GetString(i), _formatting);
        }

        /// <summary>
        /// Returns the GUID value of the specified field.
        /// </summary>
        public Guid GetGuid(int i)
        {
            return new Guid(GetString(i));
        }

        /// <summary>
        /// Gets the 16-bit signed integer value of the specified field.
        /// </summary>
        public short GetInt16(int i)
        {
            return short.Parse(GetString(i), _formatting);
        }

        /// <summary>
        /// Gets the 32-bit signed integer value of the specified field.
        /// </summary>
        public int GetInt32(int i)
        {
            return int.Parse(GetString(i), _formatting);
        }

        /// <summary>
        /// Gets the 64-bit signed integer value of the specified field.
        /// </summary>
        public long GetInt64(int i)
        {
            return long.Parse(GetString(i), _formatting);
        }
    }
}
