// 本文件为libgln的一部分，具体授权参见LICENSE文件。
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
            Empty,       // 内部使用
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
        /// 解析过程回调函数
        /// </summary>
        public interface IParseListener
        {
            /// <summary>
            /// 正在解析一个值
            /// </summary>
            /// <param name="Context">上下文</param>
            /// <param name="ValueType">数据类型</param>
            /// <param name="FormatType">格式化类型</param>
            /// <param name="Value">值</param>
            /// <param name="Parent">父列表</param>
            /// <remarks>
            /// 对于Value参数：
            ///     Character使用char传参
            ///     Boolean使用bool传参
            ///     Integer使用long传参
            ///     Real使用double传参
            ///     String和Symbol使用string传参
            ///     List使用OnBeginList的返回值传参
            /// </remarks>
            void OnParseValue(DataType ValueType, DataFormatType FormatType, object Value, object Parent);

            /// <summary>
            /// 解析到一个注释
            /// </summary>
            /// <param name="Context">上下文</param>
            /// <param name="Comment">注释</param>
            void OnParseComment(string Comment);

            /// <summary>
            /// 开始解析一个列表
            /// </summary>
            /// <returns>用户定义的列表容器</returns>
            object OnParseList();

            /// <summary>
            /// 结束解析
            /// </summary>
            /// <param name="Context">上下文</param>
            void OnReachEOF();
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
            internal ParseException(string Message, ParseContext Context)
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
        // 解析上下文
        internal class ParseContext
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

        // 匹配字符
        private static void Match(ParseContext Context, char c)
        {
            int t = Context.Read();
            if (t != c)
                throw new ParseException(String.Format("expected '{0}' but found '{1}'.", c, t == -1 ? "<EOF>" : ((char)t).ToString()), Context);
        }

        // 跳过空白
        private static bool SkipBlank(ParseContext Context)
        {
            bool bBlank = false;
            while (true)
            {
                int c = Context.Peek();
                if (c == -1 || !IsBlankCharacter((char)c))
                    return bBlank;
                else
                {
                    bBlank = true;
                    Context.Read();
                }   
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
        private static bool ParseComment(ParseContext Context)
        {
            if (Context.Peek() != ';')
                return false;

            Context.Read();
            StringBuilder ret = new StringBuilder();
            while (true)
            {
                int c = Context.Read();
                if ((c == '\r' && Context.Peek() != '\n') || c == '\n' || c == -1)  // CR, CRLF or LF
                {
                    Context.Listener.OnParseComment(ret.ToString());
                    return true;
                }
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
        private static object ParseUnsignedNumber(ParseContext Context, out DataType ValueType, out DataFormatType FormatType)
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

                    ValueType = DataType.Integer;
                    FormatType = DataFormatType.HexInteger;
                    return (long)tIntPart;
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
                    ValueType = DataType.Real;
                    return tFracPart * Math.Pow(10, bSymbol * tExpValue);
                }
                else
                {
                    ValueType = DataType.Real;
                    return tIntPart * Math.Pow(10, bSymbol * tExpValue);
                }
            }

            // 无指数部分
            if (bCastToDouble)
            {
                ValueType = DataType.Real;
                return tFracPart;
            }
            else
            {
                ValueType = DataType.Integer;
                return (long)tIntPart;
            }
        }

        // 解析数字或者符号或者逻辑型
        private static object ParseSymbolOrNumberOrBoolean(ParseContext Context, out DataType ValueType, out DataFormatType FormatType)
        {
            int c = Context.Peek();
            if (IsNumberCharacter((char)c))
                return ParseUnsignedNumber(Context, out ValueType, out FormatType);
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
                        object ret = ParseUnsignedNumber(Context, out ValueType, out FormatType);
                        switch (ValueType)
                        {
                            case DataType.Integer:
                                return -(long)ret;
                            case DataType.Real:
                                return -(double)ret;
                            default:
                                throw new ParseException("internal error.", Context);
                        }   
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
                        {
                            ValueType = DataType.Boolean;
                            return true;
                        }
                        else if (ret == "#false")
                        {
                            ValueType = DataType.Boolean;
                            return false;
                        }
                        else
                        {
                            ValueType = DataType.Symbol;
                            return ret;
                        }
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

        // 解析前缀元素
        //   不处理空白和注释
        //   对于非LIST对象不触发回调
        private static object ParseNonPostfixValue(ParseContext Context, out DataType ValueType, out DataFormatType FormatType)
        {
            int c = Context.Peek();
            switch (c)
            {
                case '\'':
                    ValueType = DataType.Character;
                    FormatType = DataFormatType.None;
                    return ParseCharacter(Context);
                case '"':
                    ValueType = DataType.String;
                    FormatType = DataFormatType.None;
                    return ParseString(Context);
                case '[':
                    {
                        ValueType = DataType.List;
                        FormatType = DataFormatType.SList;

                        Context.Read();
                        object tContainer = Context.Listener.OnParseList();

                        object tSub;
                        DataType tSubValueType;
                        DataFormatType tSubFormatType;
                        while (true)
                        {
                            tSub = ParseElement(Context, out tSubValueType, out tSubFormatType, tContainer);
                            if (tSubValueType != DataType.Empty)
                                Context.Listener.OnParseValue(tSubValueType, tSubFormatType, tSub, tContainer);
                            else
                                break;
                        }
                        Match(Context, ']');

                        return tContainer;
                    }
                default:
                    if (IsTerminalCharacter((char)c) || c == -1)  // 一般不会执行到，保险起见
                        throw new ParseException(
                            String.Format("unexpected character '{0}'.", c == -1 ? "<EOF>" : ((char)c).ToString()),
                            Context
                            );
                    else
                        return ParseSymbolOrNumberOrBoolean(Context, out ValueType, out FormatType);
            }
        }

        // 解析元素
        //   处理空白和注释
        //   忽略终结符并返回Empty
        //   完成ExList或者TList的语法解析
        private static object ParseElement(ParseContext Context, out DataType ValueType, out DataFormatType FormatType, object Parent, bool bExList = true)
        {
            // 跳过空白和注释
            while (SkipBlank(Context) || ParseComment(Context)) { }

            // 检查下一个字符是否为除'['以外的终结符
            int c = Context.Peek();
            if ((c != '[' && IsTerminalCharacter((char)c)) || c == -1)
            {
                ValueType = DataType.Empty;
                FormatType = DataFormatType.None;
                return null;
            }

            // 解析前缀部分
            DataType tSubValueType;
            DataFormatType tSubFormatType;
            object tSub = ParseNonPostfixValue(Context, out tSubValueType, out tSubFormatType);

            while (true)
            {
                // 跳过空白和注释
                while (SkipBlank(Context) || ParseComment(Context)) { }

                // 是否为TList或者ExList
                c = Context.Peek();
                if (c == '(' || (bExList && (c == ':' || c == '{')))
                {
                    Context.Read();

                    // 跳过空白和注释
                    while (SkipBlank(Context) || ParseComment(Context)) { }

                    if (c == '(')  // TList语法
                    {
                        // 扩展tSub
                        object tContainer = Context.Listener.OnParseList();

                        // 将之前的结果加入列表
                        Context.Listener.OnParseValue(tSubValueType, tSubFormatType, tSub, tContainer);

                        // 读取一组Element
                        while (true)
                        {
                            tSub = ParseElement(Context, out tSubValueType, out tSubFormatType, tContainer);
                            if (tSubValueType != DataType.Empty)
                                Context.Listener.OnParseValue(tSubValueType, tSubFormatType, tSub, tContainer);
                            else
                                break;
                        }

                        // 匹配结尾
                        Match(Context, ')');

                        tSubValueType = DataType.List;
                        tSubFormatType = DataFormatType.TList;
                        tSub = tContainer;
                    }

                    bool bPair = false;
                    if (c == ':')  // ExList语法
                    {
                        object tContainer = tSub;

                        // 若之前的类型不为List，则对其进行扩展
                        // 否则丢弃之前的Format
                        if (tSubValueType != DataType.List)
                        {
                            // 扩展tSub
                            tContainer = Context.Listener.OnParseList();

                            // 将之前的结果加入列表
                            Context.Listener.OnParseValue(tSubValueType, tSubFormatType, tSub, tContainer);
                        }

                        // 读取一个不包含ExList的元素
                        tSub = ParseElement(Context, out tSubValueType, out tSubFormatType, tContainer, false);
                        if (tSubValueType != DataType.Empty)
                            Context.Listener.OnParseValue(tSubValueType, tSubFormatType, tSub, tContainer);
                        else
                            throw new ParseException("expect element after ':'.", Context);

                        // 跳过空白和注释
                        while (SkipBlank(Context) || ParseComment(Context)) { }

                        // 解析后续的'{'
                        c = Context.Peek();
                        if (c == '{')
                            Context.Read();

                        bPair = true;
                        tSubValueType = DataType.List;
                        tSubFormatType = DataFormatType.ExListPair;
                        tSub = tContainer;
                    }

                    if (c == '{')  // ExList语法
                    {
                        object tContainer = tSub;

                        // 若之前的类型不为List，则对其进行扩展
                        // 否则丢弃之前的Format
                        if (tSubValueType != DataType.List)
                        {
                            // 扩展tSub
                            tContainer = Context.Listener.OnParseList();

                            // 将之前的结果加入列表
                            Context.Listener.OnParseValue(tSubValueType, tSubFormatType, tSub, tContainer);
                        }

                        // 读取一组Element
                        while (true)
                        {
                            tSub = ParseElement(Context, out tSubValueType, out tSubFormatType, tContainer);
                            if (tSubValueType != DataType.Empty)
                                Context.Listener.OnParseValue(tSubValueType, tSubFormatType, tSub, tContainer);
                            else
                                break;
                        }

                        // 匹配结尾
                        Match(Context, '}');

                        tSubValueType = DataType.List;
                        tSubFormatType = bPair ? DataFormatType.ExListFull : DataFormatType.ExListBlock;
                        tSub = tContainer;
                    }
                }
                else
                {
                    // 结束
                    ValueType = tSubValueType;
                    FormatType = tSubFormatType;
                    return tSub;
                }
            }
        }
        #endregion

        #region 接口函数
        private class ValueConstructor : Bakachu.GLN.Parser.IParseListener
        {
            private List<Value> _RootList = new List<Value>();

            public List<Value> Root
            {
                get
                {
                    return _RootList;
                }
            }

            public void OnParseValue(Bakachu.GLN.Parser.DataType ValueType, Bakachu.GLN.Parser.DataFormatType FormatType, object Value, object Parent)
            {
                List<Value> tTarget = Parent == null ? _RootList : (List<Value>)Parent;

                switch (ValueType)
                {
                    case DataType.Character:
                        tTarget.Add(new Value((char)Value));
                        break;
                    case DataType.Boolean:
                        tTarget.Add(new Value((bool)Value));
                        break;
                    case DataType.Integer:
                        tTarget.Add(new Value((long)Value, FormatType == DataFormatType.HexInteger));
                        break;
                    case DataType.Real:
                        tTarget.Add(new Value((double)Value));
                        break;
                    case DataType.String:
                        tTarget.Add(new Value((string)Value));
                        break;
                    case DataType.Symbol:
                        tTarget.Add(new Value((string)Value, true));
                        break;
                    case DataType.List:
                        tTarget.Add(new Value((List<Value>)Value, FormatType));
                        break;
                    default:
                        break;
                }
            }

            public void OnParseComment(string Comment)
            {
                // 忽略注释
            }

            public object OnParseList()
            {
                return new List<Value>();
            }

            public void OnReachEOF()
            {
                // 忽略EOF信息
            }
        }

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
        /// <param name="Enc">编码，默认UTF8</param>
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
            ParseContext tContext = new ParseContext(Source, Listener, SourceDesc);

            while (true)
            {
                DataType tValueType;
                DataFormatType tFormatType;
                object tObj = ParseElement(tContext, out tValueType, out tFormatType, null);

                if (tValueType != DataType.Empty)
                    tContext.Listener.OnParseValue(tValueType, tFormatType, tObj, null);
                else
                    break;
            }

            tContext.Listener.OnReachEOF();
        }

        /// <summary>
        /// 从字符串解析到List
        /// </summary>
        /// <param name="Source">字符串源</param>
        /// <returns>解析结果</returns>
        public static Value ParseToListFromString(string Source)
        {
            ValueConstructor tConstructor = new ValueConstructor();
            FromString(Source, tConstructor);
            return new Value(tConstructor.Root);
        }

        /// <summary>
        /// 从文件解析到List
        /// </summary>
        /// <param name="SourceFile">文件源</param>
        /// <param name="Enc">编码，默认UTF8</param>
        /// <returns>解析结果</returns>
        public static Value ParseToListFromFile(string SourceFile, Encoding Enc = null)
        {
            ValueConstructor tConstructor = new ValueConstructor();
            FromFile(SourceFile, tConstructor, Enc);
            return new Value(tConstructor.Root);
        }

        /// <summary>
        /// 从TextReader解析到List
        /// </summary>
        /// <param name="Source">源</param>
        /// <param name="SourceDesc">源描述</param>
        /// <returns>解析结果</returns>
        public static Value ParseToListFromReader(TextReader Source, string SourceDesc = "user")
        {
            ValueConstructor tConstructor = new ValueConstructor();
            FromReader(Source, tConstructor, SourceDesc);
            return new Value(tConstructor.Root);
        }
        #endregion
    }
}
