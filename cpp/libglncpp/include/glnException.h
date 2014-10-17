// ���ļ�Ϊlibgln��һ���֣�������Ȩ�μ�LICENSE�ļ���
// Copyright 2014, CHU.
// Create at 2014/10/17
#pragma once
#include <cstdint>
#include <stdexcept>

namespace gln
{
	/// @brief �쳣����
	class Exception :
		public std::exception
	{
	private:
		std::string m_sExceptionDesc;
	public:  // �ӿ�ʵ��
		const char* what()const;
	public:
		Exception(const char* fmtText, ...);
	};

	/// @brief ��Ч�����쳣
	class InvaildArgument :
		public Exception
	{
	public:
		InvaildArgument(const char* fmtText, ...);
	};

	/// @brief �ļ���ʧ���쳣
	class FileReaderOpenFailed :
		public Exception
	{
	public:
		FileReaderOpenFailed(const char* fmtText, ...);
	};

	/// @brief ����ʧ���쳣
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
