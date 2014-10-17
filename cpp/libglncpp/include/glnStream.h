// ���ļ�Ϊlibgln��һ���֣�������Ȩ�μ�LICENSE�ļ���
// Copyright 2014, CHU.
// Create at 2014/10/17
#pragma once
#include <cstdint>
#include <fstream>

#include "glnException.h"

namespace gln
{
	/// @breif �ı���ȡ�ӿ�
	struct ITextReader
	{
		/// @brief  ��ȡһ���ַ�
		/// @return �����ַ���UCS2���룬����������β�򷵻�-1
		virtual int32_t Read() = 0;
	};

	/// @brief �ı�д��ӿ�
	struct ITextWriter
	{
		/// @brief     д��һ���ַ���
		/// @param[in] Buf    ��'\0'��β���ַ�������
		/// @param[in] Length �ַ�����������
		virtual void Write(const char* Buf, size_t Length) = 0;
	};

	/// @brief �ļ���ʵ��
	class FileReader :
		public ITextReader
	{
	private:
		std::ifstream m_fsFile;
	public:  // �ӿ�ʵ��
		int32_t Read();
	public:
		/// @brief     ���ļ������ж�ȡ
		/// @exception ����ʧ���׳�FileReaderOpenFailed�쳣
		FileReader(const char* Path);
	};

	/// @brief �ַ�����ʵ��
	class StringReader :
		public ITextReader
	{
	private:
		const char* m_csData;
	public:  // �ӿ�ʵ��
		int32_t Read();
	public:
		/// @brief     ���ַ�������������ȡ��
		/// @note      StringReader������ԭʼ����
		/// @exception �����ݿ�ָ�뽫�׳�InvaildArgument�쳣
		StringReader(const char* Source);
	};
}
