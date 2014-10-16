// 本文件为libgln的一部分，具体授权参见LICENSE文件。
// Copyright 2014, CHU.
// Create at 2014/10/16
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Bakachu.GLN
{
    /// <summary>
    /// 值
    /// </summary>
    public class Value
    {
        /// <summary>
        /// 输出格式化
        /// </summary>
        public class ValueOutputFormat
        {
            public char IndentCharacter = ' ';  // 缩进字符
            public int  IndentSize = 2;         // 缩进大小
            public bool AllmanStyle = false;    // 是否使用Allman样式输出花括号
        }

        // 写出的上下文
        private enum WriteContext
        {
            Symbol,
            Character,
            String
        }

        // 写出一个字符并处理转义
        private static void writeChar(TextWriter Writer, char c, WriteContext Context)
        {
            switch (c)
            {
                case '\b':
                    Writer.Write("\\b");
                    break;
                case '\f':
                    Writer.Write("\\f");
                    break;
                case '\n':
                    Writer.Write("\\n");
                    break;
                case '\r':
                    Writer.Write("\\r");
                    break;
                case '\t':
                    Writer.Write("\\t");
                    break;
                case '\v':
                    Writer.Write("\\v");
                    break;
                default:
                    switch (Context)
	                {
                        case WriteContext.Symbol:
                            switch (c)
                            {
                                case '[':
                                case ']':
                                case '(':
                                case ')':
                                case '{':
                                case '}':
                                case ';':
                                case ' ':
                                case '-':
                                    Writer.Write("\\");
                                    Writer.Write(c);
                                    return;
                            }
                            break;
                        case WriteContext.Character:
                            switch (c)
                            {
                                case '\'':
                                    Writer.Write("\\");
                                    Writer.Write(c);
                                    return;
                            }
                            break;
                        case WriteContext.String:
                            switch (c)
                            {
                                case '"':
                                    Writer.Write("\\");
                                    Writer.Write(c);
                                    return;
                            }
                            break;
	                }

                    if (Char.IsControl(c))
                        Writer.Write("\\u" + ((int)c).ToString("X4"));
                    else
                        Writer.Write(c);
                    break;
            }
        }

        private Parser.DataType _ValueType = Parser.DataType.Empty;
        private Parser.DataFormatType _FormatType = Parser.DataFormatType.None;
        private object _RealValue = null;

        /// <summary>
        /// 值类型
        /// </summary>
        public Parser.DataType ValueType
        {
            get
            {
                return _ValueType;
            }
            set
            {
                _ValueType = value;
            }
        }

        /// <summary>
        /// 格式化类型
        /// </summary>
        public Parser.DataFormatType FormatType
        {
            get
            {
                return _FormatType;
            }
            set
            {
                _FormatType = value;
            }
        }

        /// <summary>
        /// 内部的值对象
        /// </summary>
        /// <remarks>对于List类型将使用List&lt;Value&gt;存储</remarks>
        public object RealValue
        {
            get
            {
                return _RealValue;
            }
        }

        /// <summary>
        /// 索引器
        /// </summary>
        /// <param name="Index">下标</param>
        /// <returns>Value对象</returns>
        public Value this[int Index]
        {
            get
            {
                List<Value> pList = (List<Value>)_RealValue;
                return pList[Index];
            }
            set
            {
                List<Value> pList = (List<Value>)_RealValue;
                pList[Index] = value;
            }
        }
        
        /// <summary>
        /// 设置值为字符类型
        /// </summary>
        /// <param name="Character">字符字面值</param>
        public void SetValue(char Character)
        {
            _ValueType = Parser.DataType.Character;
            _FormatType = Parser.DataFormatType.None;
            _RealValue = Character;
        }

        /// <summary>
        /// 设置值为逻辑类型
        /// </summary>
        /// <param name="Boolean">逻辑字面值</param>
        public void SetValue(bool Boolean)
        {
            _ValueType = Parser.DataType.Boolean;
            _FormatType = Parser.DataFormatType.None;
            _RealValue = Boolean;
        }

        /// <summary>
        /// 设置值为整数类型
        /// </summary>
        /// <param name="Integer">整数字面值</param>
        /// <param name="bHexInteger">是否为十六进制整数</param>
        public void SetValue(long Integer, bool bHexInteger = false)
        {
            _ValueType = Parser.DataType.Integer;
            _FormatType = bHexInteger ? Parser.DataFormatType.HexInteger : Parser.DataFormatType.None;
            _RealValue = Integer;
        }

        /// <summary>
        /// 设置值为实数类型
        /// </summary>
        /// <param name="Real">实数字面值</param>
        public void SetValue(double Real)
        {
            _ValueType = Parser.DataType.Real;
            _FormatType = Parser.DataFormatType.None;
            _RealValue = Real;
        }

        /// <summary>
        /// 设置值为字符串
        /// </summary>
        /// <param name="Str">字符串字面值</param>
        /// <param name="IsSymbol">是否为符号</param>
        public void SetValue(string Str, bool IsSymbol = false)
        {
            _ValueType = IsSymbol ? Parser.DataType.Symbol : Parser.DataType.String;
            _FormatType = Parser.DataFormatType.None;
            _RealValue = Str;
        }

        /// <summary>
        /// 设置值为列表
        /// </summary>
        /// <param name="ValueList">列表</param>
        /// <param name="FormatType">格式化形式</param>
        public void SetValue(List<Value> ValueList, Parser.DataFormatType FormatType)
        {
            _ValueType = Parser.DataType.List;
            _FormatType = FormatType;
            _RealValue = ValueList;
        }

        /// <summary>
        /// 转到列表
        /// </summary>
        /// <returns>转换的列表</returns>
        public List<Value> ToList()
        {
            return (List<Value>)_RealValue;
        }

        /// <summary>
        /// 转换到其他类型
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <returns>转换的类型</returns>
        public T ToObject<T>()
        {
            return (T)Convert.ChangeType(_RealValue, typeof(T));
        }

        /// <summary>
        /// 转换到字符串
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            switch (ValueType)
            {
                case Parser.DataType.Empty:
                    return "empty";
                case Parser.DataType.Character:
                    return "char : " + _RealValue.ToString();
                case Parser.DataType.Boolean:
                    return "bool : " + _RealValue.ToString();
                case Parser.DataType.Integer:
                    return "int : " + _RealValue.ToString();
                case Parser.DataType.Real:
                    return "real : " + _RealValue.ToString();
                case Parser.DataType.String:
                    return "string : " + _RealValue.ToString();
                case Parser.DataType.Symbol:
                    return "symbol : " + _RealValue.ToString();
                case Parser.DataType.List:
                    return String.Format("list(count:{0})", ToList().Count);
                default:
                    return "unknown : " + _RealValue.ToString();
            }
        }

        // 内部实现
        private void Write(TextWriter Writer, ValueOutputFormat Format, int Indent)
        {
            string tIndent = Indent == 0 ? String.Empty : new string(Format.IndentCharacter, Format.IndentSize * Indent);

            switch (ValueType)
            {
                case Parser.DataType.Character:
                    Writer.Write('\'');
                    writeChar(Writer, (char)_RealValue, WriteContext.Character);
                    Writer.Write('\'');
                    break;
                case Parser.DataType.Boolean:
                    Writer.Write(((bool)_RealValue) == true ? "#true" : "#false");
                    break;
                case Parser.DataType.Integer:
                    if (FormatType == Parser.DataFormatType.HexInteger)
                        Writer.Write("0x" + ((long)_RealValue).ToString("X"));
                    else
                        Writer.Write((long)_RealValue);
                    break;
                case Parser.DataType.Real:
                    if (_RealValue.ToString() == ToObject<int>().ToString())
                    {
                        Writer.Write(_RealValue);
                        Writer.Write(".0");  // ugly
                    }
                    else
                        Writer.Write(_RealValue);
                    break;
                case Parser.DataType.String:
                case Parser.DataType.Symbol:
                    {
                        if (ValueType == Parser.DataType.String)
                            Writer.Write('"');

                        string t = (string)_RealValue;
                        for (int i = 0; i < t.Length; ++i)
                        {
                            writeChar(Writer, t[i], ValueType == Parser.DataType.Symbol ? WriteContext.Symbol : WriteContext.String);
                        }

                        if (ValueType == Parser.DataType.String)
                            Writer.Write('"');
                    }
                    break;
                case Parser.DataType.List:
                    {
                        List<Value> t = ToList();
                        Parser.DataFormatType tFormatType = FormatType;

                        // 退化
                        if ((tFormatType == Parser.DataFormatType.ExListFull || tFormatType == Parser.DataFormatType.ExListPair) && t.Count < 2)
                            tFormatType = Parser.DataFormatType.SList;
                        else if (tFormatType == Parser.DataFormatType.ExListBlock && t.Count < 1)
                            tFormatType = Parser.DataFormatType.SList;
                        else if (tFormatType == Parser.DataFormatType.TList && t.Count < 1)
                            tFormatType = Parser.DataFormatType.SList;

                        // 输出
                        switch (tFormatType)
                        {
                            case Parser.DataFormatType.TList:
                                t[0].Write(Writer, Format, 0);
                                Writer.Write("(");

                                for (int i = 1; i < t.Count; ++i)
                                {
                                    t[i].Write(Writer, Format, Indent);
                                    if (i != t.Count - 1)
                                        Writer.Write(' ');
                                }

                                Writer.Write(")");
                                break;
                            case Parser.DataFormatType.ExListPair:
                                t[0].Write(Writer, Format, 0);

                                if (t.Count > 2)
                                {
                                    Writer.Write("(");

                                    for (int i = 1; i < t.Count - 1; ++i)
                                    {
                                        t[i].Write(Writer, Format, Indent);
                                        if (i != t.Count - 2)
                                            Writer.Write(' ');
                                    }

                                    Writer.Write(")");
                                }

                                Writer.Write(" : ");

                                t[t.Count - 1].Write(Writer, Format, Indent);
                                break;
                            case Parser.DataFormatType.ExListBlock:
                                {
                                    t[0].Write(Writer, Format, Indent);
                                    if (Format.AllmanStyle)
                                        Writer.Write(" \n" + tIndent);
                                    else
                                        Writer.Write(" ");
                                    Writer.Write("{\n");

                                    string tSubIndent = new string(Format.IndentCharacter, Format.IndentSize * (Indent + 1));
                                    for (int i = 1; i < t.Count; ++i)
                                    {
                                        Writer.Write(tSubIndent);
                                        t[i].Write(Writer, Format, Indent + 1);
                                        Writer.Write('\n');
                                    }

                                    Writer.Write(tIndent);
                                    Writer.Write("}");
                                }
                                break;
                            case Parser.DataFormatType.ExListFull:
                                {
                                    t[0].Write(Writer, Format, Indent);
                                    Writer.Write(" : ");
                                    t[1].Write(Writer, Format, Indent);
                                    if (Format.AllmanStyle)
                                        Writer.Write(" \n" + tIndent);
                                    else
                                        Writer.Write(" ");
                                    Writer.Write("{\n");

                                    string tSubIndent = new string(Format.IndentCharacter, Format.IndentSize * (Indent + 1));
                                    for (int i = 2; i < t.Count; ++i)
                                    {
                                        Writer.Write(tSubIndent);
                                        t[i].Write(Writer, Format, Indent + 1);
                                        Writer.Write('\n');
                                    }

                                    Writer.Write(tIndent);
                                    Writer.Write("}");
                                }
                                break;
                            default:
                                if (t.Count == 0)
                                    Writer.Write("[]");
                                else
                                {
                                    Writer.Write("[\n");
                                    string tSubIndent = new string(Format.IndentCharacter, Format.IndentSize * (Indent + 1));
                                    for (int i = 0; i < t.Count; ++i)
                                    {
                                        Writer.Write(tSubIndent);
                                        t[i].Write(Writer, Format, Indent + 1);
                                        Writer.Write('\n');
                                    }

                                    Writer.Write(tIndent);
                                    Writer.Write(']');
                                }
                                break;
                        }
                    }
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// 写出对象
        /// </summary>
        /// <param name="Writer">写出器</param>
        /// <param name="Format">样式</param>
        public void Write(TextWriter Writer, ValueOutputFormat Format)
        {
            Write(Writer, Format, 0);
        }

        /// <summary>
        /// 写出对象
        /// </summary>
        /// <param name="Format">格式</param>
        /// <returns>写出的值</returns>
        public string Write(ValueOutputFormat Format)
        {
            StringBuilder tBuilder = new StringBuilder();
            using (StringWriter tWriter = new StringWriter(tBuilder))
            {
                Write(tWriter, Format, 0);
                return tBuilder.ToString();
            }
        }

        /// <summary>
        /// 初始化为空值
        /// </summary>
        /// <remarks>该值不会被写入输出流</remarks>
        public Value()
        { }

        /// <summary>
        /// 初始化为字符
        /// </summary>
        /// <param name="Character">字符字面量</param>
        public Value(char Character)
        {
            SetValue(Character);
        }

        /// <summary>
        /// 初始化为逻辑
        /// </summary>
        /// <param name="Boolean">逻辑字面量</param>
        public Value(bool Boolean)
        {
            SetValue(Boolean);
        }

        /// <summary>
        /// 初始化为整数
        /// </summary>
        /// <param name="Integer">整数字面量</param>
        /// <param name="bHexInteger">是否为十六进制字符</param>
        public Value(long Integer, bool bHexInteger = false)
        {
            SetValue(Integer, bHexInteger);
        }

        /// <summary>
        /// 初始化为实数
        /// </summary>
        /// <param name="Real">实数字面量</param>
        public Value(double Real)
        {
            SetValue(Real);
        }

        /// <summary>
        /// 初始化为字符串
        /// </summary>
        /// <param name="Str">字符串字面量</param>
        /// <param name="IsSymbol">是否为符号</param>
        public Value(string Str, bool IsSymbol = false)
        {
            SetValue(Str, IsSymbol);
        }

        /// <summary>
        /// 初始化为列表
        /// </summary>
        /// <param name="ValueList">列表</param>
        /// <param name="FormatType">格式化形式</param>
        public Value(List<Value> ValueList, Parser.DataFormatType FormatType = Parser.DataFormatType.SList)
        {
            SetValue(ValueList, FormatType);
        }
    }
}
