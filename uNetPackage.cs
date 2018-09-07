using System.Collections;
using System.Collections.Generic;

using System.Net;

using System;


/// <summary>
/// Класс универсального сетевого пакета
/// </summary>
public class uNetPackage /*: MonoBehaviour*/ {
	/// <summary>
	/// Идентификатор класса сообщения
	/// </summary>
	private ushort m_cls_id;
	/// <summary>
	/// Идентификатор данных
	/// </summary>
	private ushort m_cmd_id;
	/// <summary>
	/// Данные
	/// </summary>
	private byte[] m_data;


	/// <summary>
	/// Конвструктор по умолчанию
	/// </summary>
	public uNetPackage(){
		m_cls_id = 0;
		m_cmd_id = 0;
		m_data = null;
	}
	/// <summary>
	/// Конструктор с параметрами и с копимрованием данных
	/// </summary>
	/// <param name="_classID">Идентификатор класса сообщения</param>
	/// <param name="_cmdID">Идентификатор данных</param>
	/// <param name="_data">Данные</param>
	public uNetPackage(ushort _classID, ushort _cmdID=0, byte[] _data=null){
		m_cls_id = _classID;
		m_cmd_id = _cmdID;
		m_data = (_data == null) ? null : (byte[])_data.Clone ();
	}
	/// <summary>
	/// Конструктор с параметрами и с указанием длины массива данных
	/// </summary>
	/// <param name="_classID">Идентификатор класса сообщения.</param>
	/// <param name="_cmdID">Идентификатор данных</param>
	/// <param name="_len">Длина массива данных в байтах</param>
	public uNetPackage(ushort _classID, ushort _cmdID, ushort _len){
		m_cls_id = _classID;
		m_cmd_id = _cmdID;
		m_data = (_len == 0) ? null : new byte[_len];
	}
	/// <summary>
	/// Свойство идентификатора класса сообщения
	/// </summary>
	/// <value>Идентификатор класса сообщения</value>
	public ushort ClassID {
		get {
			return m_cls_id;
		}
		set {
			m_cls_id = value;
		}
	}
	/// <summary>
	/// Свойство идентификатора данных
	/// </summary>
	/// <value>Идентификатор данных</value>
	public ushort CmdID {
		get {
			return m_cmd_id;
		}
		set {
			m_cmd_id = value;
		}
	}
	/// <summary>
	/// Свойство длины данных
	/// </summary>
	/// <value>Длина данных</value>
	public ushort Len {
		get {
			return (m_data == null) ? (ushort)0 : (ushort)m_data.Length;
		}
		set {
            if (m_data == null)
            {
                if (value != 0)
                {
                    m_data = new byte[value];
                    return;
                }
            }
			if (m_data.Length == value)
				return;
			if (value == 0) {
				m_data = null;
				return;
			}
			byte[] newData = new byte[value];
			if (m_data.Length < value)
				m_data.CopyTo (newData, 0);
			else
				for (int i = 0; i < newData.Length; i++)
					newData [i] = m_data [i];
		}
	}

	/// <summary>
	/// Запись байтов в массив данных
	/// </summary>
	/// <param name="_M">Записываемый массив</param>
	/// <param name="_startIndex">Индекс места начала записи в массиве данных</param>
	public bool SetDataPart(byte[]_M, ushort _startIndex=0){
		if ((_M == null) || (_startIndex >= m_data.Length) || ((_startIndex + _M.Length) > m_data.Length))
			return false;        
        _M.CopyTo (m_data, _startIndex);
		return true;
	}

    public void setData(byte[] _M)
    {
        m_data = (byte[])_M.Clone();
    }

	/// <summary>
	/// Получить копию данных
	/// </summary>
	/// <returns>Данные в виде байтового массива</returns>
	public byte[] GetData(){
		return (byte[])m_data.Clone ();
	}
	/// <summary>
	/// Доступ к массиву байтов данных по индексу
	/// </summary>
	/// <param name="_index">Номре байта данных</param>
	public byte this [ushort _index] {
		get {
			return m_data [_index];
		}
		set {
			m_data [_index] = value;
		}
	}
	/// <summary>
	/// Формирование содержания из байтового массива
	/// </summary>
	/// <returns>Успешность операции</returns>
	/// <param name="_M">Массив байтов</param>
	public bool BuildFromArray(byte[] _M){
		if ((_M == null) || (_M.Length < 6))
			return false;
		m_cls_id = System.BitConverter.ToUInt16 (_M, 0);
		m_cmd_id = System.BitConverter.ToUInt16 (_M, 2);
		m_data = new byte[System.BitConverter.ToUInt16 (_M, 4)];
		for (int i = 0; i < m_data.Length; i++)
			m_data [i] = _M [i + 6];
		return true;
	}
	/// <summary>
	/// Преобразует пакет в байтовый массив (для передачи по сети)
	/// </summary>
	/// <returns>Массив байтов</returns>
	public byte[] ToArray(){
		byte[] R = new byte[6 + ((m_data == null) ? 0 : m_data.Length)];
		System.BitConverter.GetBytes (m_cls_id).CopyTo (R, 0);
		System.BitConverter.GetBytes (m_cmd_id).CopyTo (R, 2);
		if (m_data == null)
			System.BitConverter.GetBytes ((ushort)0).CopyTo (R, 4);
		else {
			System.BitConverter.GetBytes ((ushort)m_data.Length).CopyTo (R, 4);
			m_data.CopyTo (R, 6);
		}
		return R;
	}
}