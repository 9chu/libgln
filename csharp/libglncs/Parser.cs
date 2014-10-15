﻿// 本文件为libgln的一部分，具体授权参见LICENSE文件。
// Copyright 2014, CHU.
// Create at 2014/10/14
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Bakachu.GLN
{
    /// <summary>
    /// 实现解析器
    /// </summary>
    public abstract class Parser
    {
        #region 公开定义
        /// <summary>
        /// 解析后的数据类型
        /// </summary>
        public enum DataType
        {
            Character,   // 字符
            Boolean,     // 逻辑
            Integer,     // 整数
            Real,        // 小数
            String,      // 字符串
            Symbol,      // 符号
            List         // 列表
        }

        /// <summary>
        /// 数据所用的格式化方式
        /// </summary>
        public enum DataFormatType
        {
            None,         // 正常格式
            HexInteger,   // 十六进制整数
            SList,        // S-expr
            TList,        // T-expr
            ExListPair,   // 扩展列表值键对形式
            ExListBlock,  // 扩展列表块形式
            ExListFull    // 扩展列表完整形式
        }

        /// <summary>
        /// 解析上下文
        /// </summary>
        public class ParseContext
        {
            private TextReader _Reader;
            private IParseListener _Listener;
            private string _SourceDesc;
            private long _Position = 0;
            private long _Line = 1;
            private long _Row = 1;

            /// <summary>
            /// 监听器
            /// </summary>
            public IParseListener Listener
            {
                get
                {
                    return _Listener;
                }
            }

            /// <summary>
            /// 源的描述名称
            /// </summary>
            public string SourceDesc
            {
                get
                {
                    return _SourceDesc;
                }
            }

            /// <summary>
            /// 位置
            /// </summary>
            public long Position
            {
                get
                {
                    return _Position;
                }
            }

            /// <summary>
            /// 行号
            /// </summary>
            public long Line
            {
                get
                {
                    return _Line;
                }
            }

            /// <summary>
            /// 列号
            /// </summary>
            public long Row
            {
                get
                {
                    return _Row;
                }
            }

            /// <summary>
            /// 获取下一个字符，若到达结尾则返回-1
            /// </summary>
            /// <returns>读取的值</returns>
            internal int Peek()
            {
                return _Reader.Peek();
            }
            
            /// <summary>
            /// 获取下一个字符，若到达结尾则返回-1
            /// </summary>
            /// <returns>读取的值</returns>
            internal int Read()
            {
                int c = _Reader.Read();
                if ((c == '\r' && _Reader.Peek() != '\n') || c == '\n')
                {
                    _Line++;
                    _Row = 0;
                }
                if (c != -1)
                {
                    _Position++;
                    _Row++;
                }
                return c;
            }

            /// <summary>
            /// 解析上下文
            /// </summary>
            /// <param name="Reader">读取器</param>
            /// <param name="Listener">监听器</param>
            /// <param name="SourceDesc">源描述</param>
            internal ParseContext(TextReader Reader, IParseListener Listener, string SourceDesc = "user")
            {
                _Reader = Reader;
                _Listener = Listener;
                _SourceDesc = SourceDesc;
            }
        }

        /// <summary>
        /// 解析过程回调函数
        /// </summary>
        public interface IParseListener
        {
            /// <summary>
            /// 正在解析一个原子值
            /// </summary>
            /// <param name="Context">上下文</param>
            /// <param name="AtomType">数据类型</param>
            /// <param name="FormatType">格式化类型</param>
            /// <param name="Value">值</param>
            /// <remarks>
            /// 对于Value参数：
            ///     Character使用char传参
            ///     Boolean使用bool传参
            ///     Integer使用long传参
            ///     Real使用double传参
            ///     String和Symbol使用string传参
            /// </remarks>
            void OnParseAtomValue(ParseContext Context, DataType AtomType, DataFormatType FormatType, object Value);

            /// <summary>
            /// 解析到一个注释
            /// </summary>
            /// <param name="Context">上下文</param>
            /// <param name="Comment">注释</param>
            void OnParseComment(ParseContext Context, string Comment);

            /// <summary>
            /// 开始解析一个列表
            /// </summary>
            void OnStartParseList(ParseContext Context);

            /// <summary>
            /// 结束对一个列表的解析
            /// </summary>
            /// <param name="Context">上下文</param>
            /// <param name="FormatType">列表的格式化类型</param>
            void OnEndParseList(ParseContext Context, DataFormatType FormatType);

            /// <summary>
            /// 结束解析
            /// </summary>
            /// <param name="Context">上下文</param>
            void OnReachEOF(ParseContext Context);
        }

        /// <summary>
        /// 解析时异常
        /// </summary>
        public class ParseException : Exception
        {
            // 合并异常信息
            private static string CombineExceptionMessage(string Message, ParseContext Context)
            {
                return String.Format("{0}({1},{2},{3}) : {4}", 
                    Context.SourceDesc, 
                    Context.Line, 
                    Context.Row, 
                    Context.Position, 
                    Message);
            }

            private string _RawMessage; // 原始信息
            private long   _Position;   // 位置
            private long   _Line;       // 行
            private long   _Row;        // 列

            /// <summary>
            /// 原始信息
            /// </summary>
            public string RawMessage
            {
                get
                {
                    return _RawMessage;
                }
            }

            /// <summary>
            /// 位置
            /// </summary>
            public long Position
            {
                get
                {
                    return _Position;
                }
            }

            /// <summary>
            /// 行号
            /// </summary>
            public long Line
            {
                get
                {
                    return _Line;
                }
            }

            /// <summary>
            /// 列号
            /// </summary>
            public long Row
            {
                get
                {
                    return _Row;
                }
            }

            /// <summary>
            /// 解析时异常
            /// </summary>
            /// <param name="Message">异常信息</param>
            /// <param name="Context">上下文</param>
            public ParseException(string Message, ParseContext Context)
                : base(CombineExceptionMessage(Message, Context))
            {
                _RawMessage = Message;
                _Position = Context.Position;
                _Line = Context.Line;
                _Row = Context.Row;
            }
        }
        #endregion

        #region 字符检查
        /// <summary>
        /// 是否为空白符
        /// </summary>
        /// <param name="c">被检查字符</param>
        /// <returns>检查结果</returns>
        private static bool IsBlankCharacter(char c)
        {
            switch (c)
            {
                case '\t':
                case '\v':
                case '\r':
                case '\n':
                case ' ':
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// 是否为终结字符
        /// </summary>
        /// <param name="c">被检查字符</param>
        /// <returns>检查结果</returns>
        private static bool IsTerminalCharacter(char c)
        {
            switch (c)
            {
                case '[':
                case '{':
                case '(':
                case ']':
                case '}':
                case ')':
                case ';':
                case ':':
                    return true;
                default:
                    return IsBlankCharacter(c);
            }
        }

        /// <summary>
        /// 是否为数字字符
        /// </summary>
        /// <param name="c">被检查字符</param>
        /// <returns>检查结果</returns>
        private static bool IsNumberCharacter(char c)
        {
            return c >= '0' && c <= '9';
        }

        /// <summary>
        /// 是否为非0数字字符
        /// </summary>
        /// <param name="c">被检查字符</param>
        /// <returns>检查结果</returns>
        private static bool IsNonZeroNumberCharacter(char c)
        {
            return c >= '1' && c <= '9';
        }

        /// <summary>
        /// 是否为十六进制字符并返回解析结果
        /// </summary>
        /// <param name="c">被检查字符</param>
        /// <param name="value">解析结果</param>
        /// <returns>检查结果</returns>
        private static bool IsHexNumberCharacter(char c, out int value)
        {
            if (c >= '0' && c <= '9')
            {
                value = c - '0';
                return true;
            }
            else if (c >= 'a' && c <= 'f')
            {
                value = c - 'a' + 10;
                return true;
            }
            else if (c >= 'A' && c <= 'F')
            {
                value = c - 'A' + 10;
                return true;
            }
            else
            {
                value = 0;
                return false;
            }   
        }
        #endregion

        #region 解析函数
        // 匹配字符
        private static void Match(ParseContext Context, char c)
        {
            int t = Context.Read();
            if (t != c)
                throw new ParseException(String.Format("expected '{0}' but found '{1}'.", c, t == -1 ? "<EOF>" : ((char)t).ToString()), Context);
        }

        // 跳过空白
        private static void SkipBlank(ParseContext Context)
        {
            while (true)
            {
                int c = Context.Peek();
                if (c == -1 || !IsBlankCharacter((char)c))
                    break;
                else
                    Context.Read();
            }
        }

        // 解析转义字符序列
        //   已读取'\'
        private static char ParseEscapeCharacter(ParseContext Context)
        {
            int c = Context.Read();
            switch (c)
            {
                case 'b':
                    return '\b';
                case 'f':
                    return '\f';
                case 'n':
                    return '\n';
                case 'r':
                    return '\r';
                case 't':
                    return '\t';
                case 'v':
                    return '\v';
                case 'u':
                    {
                        int num = 0;
                        for (int i = 0; i < 4; ++i)
                        {
                            int t;
                            c = Context.Read();
                            if (!IsHexNumberCharacter((char)c, out t))
                                throw new ParseException(String.Format("expected hex number but found '{0}'.", c == -1 ? "<EOF>" : ((char)c).ToString()), Context);
                            num = (num << 4) + t;
                        }
                        return (char)num;
                    }
                case -1:
                    throw new ParseException("unexpected character '<EOF>'.", Context);
                default:
                    return (char)c;
            }
        }

        // 解析注释
        private static string ParseComment(ParseContext Context)
        {
            Match(Context, ';');

            StringBuilder ret = new StringBuilder();
            while (true)
            {
                int c = Context.Read();
                if ((c == '\r' && Context.Peek() != '\n') || c == '\n' || c == -1)  // CR, CRLF or LF
                    return ret.ToString();
                else
                    ret.Append((char)c);
            }
        }

        // 解析字符
        private static char ParseCharacter(ParseContext Context)
        {
            Match(Context, '\'');

            int c;
            switch (c = Context.Read())
            {
                case '\\':
                    {
                        char t = ParseEscapeCharacter(Context);
                        Match(Context, '\'');
                        return t;
                    }
                case -1:
                    throw new ParseException("unexpected character '<EOF>'.", Context);
                default:
                    {
                        char t = (char)c;
                        Match(Context, '\'');
                        return t;
                    }
            }
        }

        // 解析字符串
        private static string ParseString(ParseContext Context)
        {
            Match(Context, '"');

            StringBuilder ret = new StringBuilder();
            while (true)
            {
                int c;
                switch (c = Context.Read())
                {
                    case '\\':
                        {
                            char t = ParseEscapeCharacter(Context);
                            ret.Append(t);
                            break;
                        }
                    case '"':
                        return ret.ToString();
                    case -1:
                        throw new ParseException("unexpected character '<EOF>'.", Context);
                    default:
                        {
                            ret.Append((char)c);
                            break;
                        }
                }
            }
        }

        // 解析数字
        private static object ParseUnsignedNumber(ParseContext Context, out DataFormatType FormatType)
        {
            bool bCastToDouble = false;
            FormatType = DataFormatType.None;

            int c = Context.Peek();

            // ===== 整数部分 =====
            ulong tIntPart = 0;
            if (c == '0')  // 读取0或者0x
            {
                Context.Read();
                c = Context.Peek();
                if (c == 'x')
                {
                    Context.Read();

                    int len = 0;
                    while (true)
                    {
                        int t;
                        c = Context.Read();
                        if (IsHexNumberCharacter((char)c, out t))
                        {
                            len++;
                            if (len > 16)
                                throw new ParseException("integer literal is too long.", Context);
                            tIntPart = (tIntPart << 4) + (ulong)t;
                        }
                        else
                            throw new ParseException(
                                String.Format("expected hex number but found '{0}'.", c == -1 ? "<EOF>" : ((char)c).ToString()),
                                Context
                                );

                        c = Context.Peek();
                        if (IsTerminalCharacter((char)c) || c == -1)
                            break;
                    }

                    FormatType = DataFormatType.HexInteger;
                    return tIntPart;
                }
            }
            else if (IsNonZeroNumberCharacter((char)c))  // 读取数字
            {
                while (true)
                {
                    c = Context.Read();
                    if (IsNumberCharacter((char)c))
                    {
                        tIntPart = tIntPart * 10 + (ulong)(c - '0');
                    }
                    else
                        throw new ParseException(
                            String.Format("expected decimal number but found '{0}'.", c == -1 ? "<EOF>" : ((char)c).ToString()),
                            Context
                            );

                    c = Context.Peek();
                    if (IsTerminalCharacter((char)c) || c == -1 || c == '.' || c == 'e' || c == 'E')
                        break;
                }
            }
            else
                throw new ParseException(
                    String.Format("expected number but found '{0}'.", c == -1 ? "<EOF>" : ((char)c).ToString()),
                    Context
                    );

            // ===== 小数部分 =====
            double tFracPart = 0;
            c = Context.Peek();
            if (c == '.')
            {
                c = Context.Read();

                double tFracExp = 0.1;
                while (true)
                {
                    c = Context.Read();
                    if (IsNumberCharacter((char)c))
                    {
                        tFracPart += (double)(c - '0') * tFracExp;
                        tFracExp /= 10;
                    }
                    else
                        throw new ParseException(
                            String.Format("expected decimal number but found '{0}'.", c == -1 ? "<EOF>" : ((char)c).ToString()),
                            Context
                            );

                    c = Context.Peek();
                    if (IsTerminalCharacter((char)c) || c == -1 || c == 'e' || c == 'E')
                        break;
                }
                
                bCastToDouble = true;
                tFracPart += tIntPart;
            }

            // ===== 指数部分 =====
            c = Context.Peek();
            if (c == 'e' || c == 'E')
            {
                c = Context.Read();

                // 检查符号
                double bSymbol = 1;
                c = Context.Peek();
                if (c == '-')
                {
                    Context.Read();
                    bSymbol = -1;
                }

                uint tExpValue = 0;
                while (true)
                {
                    c = Context.Read();
                    if (IsNumberCharacter((char)c))
                    {
                        tExpValue = tExpValue * 10 + (uint)(c - '0');
                    }
                    else
                        throw new ParseException(
                            String.Format("expected decimal number but found '{0}'.", c == -1 ? "<EOF>" : ((char)c).ToString()),
                            Context
                            );

                    c = Context.Peek();
                    if (IsTerminalCharacter((char)c) || c == -1)
                        break;
                }

                // 以指数形式结束
                if (bCastToDouble)
                {
                    return tFracPart * Math.Pow(10, bSymbol * tExpValue);
                }
                else
                {
                    return tIntPart * Math.Pow(10, bSymbol * tExpValue);
                }
            }

            // 无指数部分
            if (bCastToDouble)
                return tFracPart;
            else
                return tIntPart;
        }

        // 解析数字或者符号或者逻辑型
        private static object ParseSymbolOrNumberOrBoolean(ParseContext Context, out DataFormatType FormatType)
        {
            int c = Context.Peek();
            if (IsNumberCharacter((char)c))
            {
                object ret = ParseUnsignedNumber(Context, out FormatType);
                if (FormatType == DataFormatType.HexInteger)
                    return (long)ret;
                else
                    return ret;
            }
            else if (c == -1)
                throw new ParseException("unexpected character '<EOF>'.", Context);
            else
            {
                StringBuilder tStrBuilder = null;
                if (c == '-')
                {
                    Context.Read();
                    c = Context.Peek();
                    if (IsNumberCharacter((char)c))
                    {
                        object ret = ParseUnsignedNumber(Context, out FormatType);
                        if (ret.GetType() == typeof(ulong))
                            return -(long)((ulong)ret);
                        else if (ret.GetType() == typeof(double))
                            return -(double)ret;
                        else
                            throw new ParseException("internal error.", Context);
                    }
                    else
                    {
                        tStrBuilder = new StringBuilder();
                        tStrBuilder.Append('-');
                    }
                }
                else
                    tStrBuilder = new StringBuilder();

                while (true)
                {
                    if (IsTerminalCharacter((char)c) || c == -1)
                    {
                        FormatType = DataFormatType.None;

                        string ret = tStrBuilder.ToString();
                        if (ret == "#true")
                            return true;
                        else if (ret == "#false")
                            return false;
                        else
                            return ret;
                    }
                    else if (c == '\\')
                    {
                        Context.Read();
                        tStrBuilder.Append(ParseEscapeCharacter(Context));
                    }
                    else
                    {
                        tStrBuilder.Append((char)c);
                        Context.Read();
                    }
                    c = Context.Peek();
                }
            }
        }

        // 解析无后缀语法值
        private static void ParseNonPostfixValue(ParseContext Context)
        {
            // TODO
        }
        #endregion

        #region 接口函数
        /// <summary>
        /// 从字符串进行解析
        /// </summary>
        /// <param name="Source">源</param>
        /// <param name="Listener">解析回调</param>
        public static void FromString(string Source, IParseListener Listener)
        {
            using (StringReader tReader = new StringReader(Source))
            {
                FromReader(tReader, Listener, "string");
            }
        }

        /// <summary>
        /// 从文件进行解析
        /// </summary>
        /// <param name="SourceFile">源</param>
        /// <param name="Listener">解析回调</param>
        /// <param name="Enc">编码</param>
        public static void FromFile(string SourceFile, IParseListener Listener, Encoding Enc = null)
        {
            if (Enc == null)
                Enc = Encoding.UTF8;
            using (StreamReader tReader = new StreamReader(SourceFile, Enc, true))
            {
                FromReader(tReader, Listener, Path.GetFileName(SourceFile));
            }
        }

        /// <summary>
        /// 从TextReader进行解析
        /// </summary>
        /// <param name="Source">源</param>
        /// <param name="Listener">解析回调</param>
        /// <param name="SourceDesc">源描述</param>
        public static void FromReader(TextReader Source, IParseListener Listener, string SourceDesc = "user")
        {
            throw new NotImplementedException();
            // DoParse(new ParseContext(Source, Listener, SourceDesc));
        }

        public static void Test()
        {
            ParseContext t = new ParseContext(new StringReader("-0xFF sa"), null);
            DataFormatType tt;
            object a = ParseSymbolOrNumberOrBoolean(t, out tt);

        }
        #endregion
    }
}
