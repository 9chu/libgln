// 本文件为libgln的一部分，具体授权参见LICENSE文件。
// Copyright 2014, CHU.
// Create at 2014/10/17
#pragma once
#include <cstdint>
#include <stdexcept>

namespace gln
{
	/// @brief 异常基类
	class Exception :
		public std::exception
	{
	private:
		std::string m_sExceptionDesc;
	public:  // 接口实现
		const char* what()const;
	public:
		Exception(const char* fmtText, ...);
	};

	/// @brief 无效参数异常
	class InvaildArgument :
		public Exception
	{
	public:
		InvaildArgument(const char* fmtText, ...);
	};

	/// @brief 文件打开失败异常
	class FileReaderOpenFailed :
		public Exception
	{
	public:
		FileReaderOpenFailed(const char* fmtText, ...);
	};

	/// @brief 解析失败异常
	class ParseFailed :
		public Exception
	{
	private:
		const char* m_sSourceDesc;
		size_t m_Pos;
		size_t m_Line;
		size_t m_Row;
	public:
		ParseFailed(const char* sSourceDesc, size_t Pos, size_t Line, size_t Row, const char* fmtText, ...);
	};
}
