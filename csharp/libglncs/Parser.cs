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
        public interface ParseListener
        {
            /// <summary>
            /// 正在解析一个原子值
            /// </summary>
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
            void OnParseAtomValue(DataType AtomType, DataFormatType FormatType, object Value);

            /// <summary>
            /// 开始解析一个列表
            /// </summary>
            void OnStartParseList();

            /// <summary>
            /// 结束对一个列表的解析
            /// </summary>
            /// <param name="FormatType">列表的格式化类型</param>
            void OnEndParseList(DataFormatType FormatType);
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
                    return false;
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
                value = c - 'a';
                return true;
            }
            else if (c >= 'A' && c <= 'F')
            {
                value = c - 'A';
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
        /// <summary>
        /// 从字符串进行解析
        /// </summary>
        /// <param name="Source">源</param>
        /// <param name="Listener">解析回调</param>
        public static void FromString(string Source, ParseListener Listener)
        {
            using (StringReader tReader = new StringReader(Source))
            {
                FromReader(tReader, Listener);
            }
        }

        /// <summary>
        /// 从文件进行解析
        /// </summary>
        /// <param name="SourceFile">源</param>
        /// <param name="Listener">解析回调</param>
        /// <param name="Enc">编码</param>
        public static void FromFile(string SourceFile, ParseListener Listener, Encoding Enc = null)
        {
            if (Enc == null)
                Enc = Encoding.UTF8;
            using (StreamReader tReader = new StreamReader(SourceFile, Enc, true))
            {
                FromReader(tReader, Listener);
            }
        }

        /// <summary>
        /// 从TextReader进行解析
        /// </summary>
        /// <param name="Source">源</param>
        /// <param name="Listener">解析回调</param>
        public static void FromReader(TextReader Source, ParseListener Listener)
        {
            // TODO..
        }
        #endregion
    }
}
