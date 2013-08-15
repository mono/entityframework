﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity.Migrations.Utilities
{
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Text;

    /// <summary>
    ///     The same as <see cref="System.CodeDom.Compiler.IndentedTextWriter" /> but works in partial trust.
    /// </summary>
    public class IndentedTextWriter : TextWriter
    {
        /// <summary>
        ///     Specifies the default tab string. This field is constant.
        /// </summary>
        public const string DefaultTabString = "    ";

        private readonly TextWriter _writer;
        private int _indentLevel;
        private bool _tabsPending;
        private readonly string _tabString;

        /// <summary>
        ///     Gets the encoding for the text writer to use.
        /// </summary>
        /// <returns>
        ///     An <see cref="T:System.Text.Encoding" /> that indicates the encoding for the text writer to use.
        /// </returns>
        public override Encoding Encoding
        {
            get { return _writer.Encoding; }
        }

        /// <summary>
        ///     Gets or sets the new line character to use.
        /// </summary>
        /// <returns> The new line character to use. </returns>
        public override string NewLine
        {
            get { return _writer.NewLine; }
            set { _writer.NewLine = value; }
        }

        /// <summary>
        ///     Gets or sets the number of spaces to indent.
        /// </summary>
        /// <returns> The number of spaces to indent. </returns>
        public int Indent
        {
            get { return _indentLevel; }
            set
            {
                if (value < 0)
                {
                    value = 0;
                }
                _indentLevel = value;
            }
        }

        /// <summary>
        ///     Gets the <see cref="T:System.IO.TextWriter" /> to use.
        /// </summary>
        /// <returns>
        ///     The <see cref="T:System.IO.TextWriter" /> to use.
        /// </returns>
        public TextWriter InnerWriter
        {
            get { return _writer; }
        }

        /// <summary>
        ///     Initializes a new instance of the IndentedTextWriter class using the specified text writer and default tab string.
        /// </summary>
        /// <param name="writer">
        ///     The <see cref="T:System.IO.TextWriter" /> to use for output.
        /// </param>
        public IndentedTextWriter(TextWriter writer)
            : this(writer, DefaultTabString)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the IndentedTextWriter class using the specified text writer and tab string.
        /// </summary>
        /// <param name="writer">
        ///     The <see cref="T:System.IO.TextWriter" /> to use for output.
        /// </param>
        /// <param name="tabString"> The tab string to use for indentation. </param>
        [SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "string")]
        public IndentedTextWriter(TextWriter writer, string tabString)
            : base(CultureInfo.InvariantCulture)
        {
            _writer = writer;
            _tabString = tabString;
            _indentLevel = 0;
            _tabsPending = false;
        }

        /// <summary>
        ///     Closes the document being written to.
        /// </summary>
        public override void Close()
        {
            _writer.Close();
        }

        /// <summary>
        ///     Flushes the stream.
        /// </summary>
        public override void Flush()
        {
            _writer.Flush();
        }

        /// <summary>
        ///     Outputs the tab string once for each level of indentation according to the
        ///     <see
        ///         cref="P:System.CodeDom.Compiler.IndentedTextWriter.Indent" />
        ///     property.
        /// </summary>
        protected virtual void OutputTabs()
        {
            if (!_tabsPending)
            {
                return;
            }
            for (var index = 0; index < _indentLevel; ++index)
            {
                _writer.Write(_tabString);
            }
            _tabsPending = false;
        }

        /// <summary>
        ///     Writes the specified string to the text stream.
        /// </summary>
        /// <param name="value"> The string to write. </param>
        public override void Write(string value)
        {
            OutputTabs();
            _writer.Write(value);
        }

        /// <summary>
        ///     Writes the text representation of a Boolean value to the text stream.
        /// </summary>
        /// <param name="value"> The Boolean value to write. </param>
        public override void Write(bool value)
        {
            OutputTabs();
            _writer.Write(value);
        }

        /// <summary>
        ///     Writes a character to the text stream.
        /// </summary>
        /// <param name="value"> The character to write. </param>
        public override void Write(char value)
        {
            OutputTabs();
            _writer.Write(value);
        }

        /// <summary>
        ///     Writes a character array to the text stream.
        /// </summary>
        /// <param name="buffer"> The character array to write. </param>
        public override void Write(char[] buffer)
        {
            OutputTabs();
            _writer.Write(buffer);
        }

        /// <summary>
        ///     Writes a subarray of characters to the text stream.
        /// </summary>
        /// <param name="buffer"> The character array to write data from. </param>
        /// <param name="index"> Starting index in the buffer. </param>
        /// <param name="count"> The number of characters to write. </param>
        public override void Write(char[] buffer, int index, int count)
        {
            OutputTabs();
            _writer.Write(buffer, index, count);
        }

        /// <summary>
        ///     Writes the text representation of a Double to the text stream.
        /// </summary>
        /// <param name="value"> The double to write. </param>
        public override void Write(double value)
        {
            OutputTabs();
            _writer.Write(value);
        }

        /// <summary>
        ///     Writes the text representation of a Single to the text stream.
        /// </summary>
        /// <param name="value"> The single to write. </param>
        public override void Write(float value)
        {
            OutputTabs();
            _writer.Write(value);
        }

        /// <summary>
        ///     Writes the text representation of an integer to the text stream.
        /// </summary>
        /// <param name="value"> The integer to write. </param>
        public override void Write(int value)
        {
            OutputTabs();
            _writer.Write(value);
        }

        /// <summary>
        ///     Writes the text representation of an 8-byte integer to the text stream.
        /// </summary>
        /// <param name="value"> The 8-byte integer to write. </param>
        public override void Write(long value)
        {
            OutputTabs();
            _writer.Write(value);
        }

        /// <summary>
        ///     Writes the text representation of an object to the text stream.
        /// </summary>
        /// <param name="value"> The object to write. </param>
        public override void Write(object value)
        {
            OutputTabs();
            _writer.Write(value);
        }

        /// <summary>
        ///     Writes out a formatted string, using the same semantics as specified.
        /// </summary>
        /// <param name="format"> The formatting string. </param>
        /// <param name="arg0"> The object to write into the formatted string. </param>
        public override void Write(string format, object arg0)
        {
            OutputTabs();
            _writer.Write(format, arg0);
        }

        /// <summary>
        ///     Writes out a formatted string, using the same semantics as specified.
        /// </summary>
        /// <param name="format"> The formatting string to use. </param>
        /// <param name="arg0"> The first object to write into the formatted string. </param>
        /// <param name="arg1"> The second object to write into the formatted string. </param>
        public override void Write(string format, object arg0, object arg1)
        {
            OutputTabs();
            _writer.Write(format, arg0, arg1);
        }

        /// <summary>
        ///     Writes out a formatted string, using the same semantics as specified.
        /// </summary>
        /// <param name="format"> The formatting string to use. </param>
        /// <param name="arg"> The argument array to output. </param>
        public override void Write(string format, params object[] arg)
        {
            OutputTabs();
            _writer.Write(format, arg);
        }

        /// <summary>
        ///     Writes the specified string to a line without tabs.
        /// </summary>
        /// <param name="value"> The string to write. </param>
        public void WriteLineNoTabs(string value)
        {
            _writer.WriteLine(value);
        }

        /// <summary>
        ///     Writes the specified string, followed by a line terminator, to the text stream.
        /// </summary>
        /// <param name="value"> The string to write. </param>
        public override void WriteLine(string value)
        {
            OutputTabs();
            _writer.WriteLine(value);
            _tabsPending = true;
        }

        /// <summary>
        ///     Writes a line terminator.
        /// </summary>
        public override void WriteLine()
        {
            OutputTabs();
            _writer.WriteLine();
            _tabsPending = true;
        }

        /// <summary>
        ///     Writes the text representation of a Boolean, followed by a line terminator, to the text stream.
        /// </summary>
        /// <param name="value"> The Boolean to write. </param>
        public override void WriteLine(bool value)
        {
            OutputTabs();
            _writer.WriteLine(value);
            _tabsPending = true;
        }

        /// <summary>
        ///     Writes a character, followed by a line terminator, to the text stream.
        /// </summary>
        /// <param name="value"> The character to write. </param>
        public override void WriteLine(char value)
        {
            OutputTabs();
            _writer.WriteLine(value);
            _tabsPending = true;
        }

        /// <summary>
        ///     Writes a character array, followed by a line terminator, to the text stream.
        /// </summary>
        /// <param name="buffer"> The character array to write. </param>
        public override void WriteLine(char[] buffer)
        {
            OutputTabs();
            _writer.WriteLine(buffer);
            _tabsPending = true;
        }

        /// <summary>
        ///     Writes a subarray of characters, followed by a line terminator, to the text stream.
        /// </summary>
        /// <param name="buffer"> The character array to write data from. </param>
        /// <param name="index"> Starting index in the buffer. </param>
        /// <param name="count"> The number of characters to write. </param>
        public override void WriteLine(char[] buffer, int index, int count)
        {
            OutputTabs();
            _writer.WriteLine(buffer, index, count);
            _tabsPending = true;
        }

        /// <summary>
        ///     Writes the text representation of a Double, followed by a line terminator, to the text stream.
        /// </summary>
        /// <param name="value"> The double to write. </param>
        public override void WriteLine(double value)
        {
            OutputTabs();
            _writer.WriteLine(value);
            _tabsPending = true;
        }

        /// <summary>
        ///     Writes the text representation of a Single, followed by a line terminator, to the text stream.
        /// </summary>
        /// <param name="value"> The single to write. </param>
        public override void WriteLine(float value)
        {
            OutputTabs();
            _writer.WriteLine(value);
            _tabsPending = true;
        }

        /// <summary>
        ///     Writes the text representation of an integer, followed by a line terminator, to the text stream.
        /// </summary>
        /// <param name="value"> The integer to write. </param>
        public override void WriteLine(int value)
        {
            OutputTabs();
            _writer.WriteLine(value);
            _tabsPending = true;
        }

        /// <summary>
        ///     Writes the text representation of an 8-byte integer, followed by a line terminator, to the text stream.
        /// </summary>
        /// <param name="value"> The 8-byte integer to write. </param>
        public override void WriteLine(long value)
        {
            OutputTabs();
            _writer.WriteLine(value);
            _tabsPending = true;
        }

        /// <summary>
        ///     Writes the text representation of an object, followed by a line terminator, to the text stream.
        /// </summary>
        /// <param name="value"> The object to write. </param>
        public override void WriteLine(object value)
        {
            OutputTabs();
            _writer.WriteLine(value);
            _tabsPending = true;
        }

        /// <summary>
        ///     Writes out a formatted string, followed by a line terminator, using the same semantics as specified.
        /// </summary>
        /// <param name="format"> The formatting string. </param>
        /// <param name="arg0"> The object to write into the formatted string. </param>
        public override void WriteLine(string format, object arg0)
        {
            OutputTabs();
            _writer.WriteLine(format, arg0);
            _tabsPending = true;
        }

        /// <summary>
        ///     Writes out a formatted string, followed by a line terminator, using the same semantics as specified.
        /// </summary>
        /// <param name="format"> The formatting string to use. </param>
        /// <param name="arg0"> The first object to write into the formatted string. </param>
        /// <param name="arg1"> The second object to write into the formatted string. </param>
        public override void WriteLine(string format, object arg0, object arg1)
        {
            OutputTabs();
            _writer.WriteLine(format, arg0, arg1);
            _tabsPending = true;
        }

        /// <summary>
        ///     Writes out a formatted string, followed by a line terminator, using the same semantics as specified.
        /// </summary>
        /// <param name="format"> The formatting string to use. </param>
        /// <param name="arg"> The argument array to output. </param>
        public override void WriteLine(string format, params object[] arg)
        {
            OutputTabs();
            _writer.WriteLine(format, arg);
            _tabsPending = true;
        }

        /// <summary>
        ///     Writes the text representation of a UInt32, followed by a line terminator, to the text stream.
        /// </summary>
        /// <param name="value"> A UInt32 to output. </param>
        [CLSCompliant(false)]
        public override void WriteLine(uint value)
        {
            OutputTabs();
            _writer.WriteLine(value);
            _tabsPending = true;
        }
    }
}
