// 本文件为libgln的一部分，具体授权参见LICENSE文件。
// Copyright 2014, CHU.
// Create at 2014/10/17
#pragma once
#include <cstdint>
#include <fstream>

#include "glnException.h"

namespace gln
{
	/// @breif 文本读取接口
	struct ITextReader
	{
		/// @brief  读取一个字符
		/// @return 返回字符的UCS2编码，若遇到流结尾则返回-1
		virtual int32_t Read() = 0;
	};

	/// @brief 文本写入接口
	struct ITextWriter
	{
		/// @brief     写入一个字符串
		/// @param[in] Buf    非'\0'结尾的字符缓冲区
		/// @param[in] Length 字符缓冲区长度
		virtual void Write(const char* Buf, size_t Length) = 0;
	};

	/// @brief 文件流实现
	class FileReader :
		public ITextReader
	{
	private:
		std::ifstream m_fsFile;
	public:  // 接口实现
		int32_t Read();
	public:
		/// @brief     打开文件并进行读取
		/// @exception 若打开失败抛出FileReaderOpenFailed异常
		FileReader(const char* Path);
	};

	/// @brief 字符串流实现
	class StringReader :
		public ITextReader
	{
	private:
		const char* m_csData;
	public:  // 接口实现
		int32_t Read();
	public:
		/// @brief     从字符串常量创建读取器
		/// @note      StringReader不拷贝原始数据
		/// @exception 若传递空指针将抛出InvaildArgument异常
		StringReader(const char* Source);
	};
}
