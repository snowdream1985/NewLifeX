﻿using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using NewLife.Reflection;
using NewLife.Serialization;

namespace NewLife.Data
{
    /// <summary>数据集</summary>
    public class DbSet
    {
        #region 属性
        /// <summary>数据列</summary>
        public String[] Columns { get; set; }

        /// <summary>数据列类型</summary>
        public Type[] Types { get; set; }

        /// <summary>数据行</summary>
        public IList<Object[]> Rows { get; set; }
        #endregion

        #region 构造
        #endregion

        #region 方法
        /// <summary>读取数据</summary>
        /// <param name="dr"></param>
        public void Read(IDataReader dr)
        {
            var count = dr.FieldCount;

            // 字段
            var cs = new String[count];
            var ts = new Type[count];
            for (var i = 0; i < count; i++)
            {
                cs[i] = dr.GetName(i);
                ts[i] = dr.GetFieldType(i);
            }
            Columns = cs;
            Types = ts;

            // 数据
            var rs = new List<Object[]>();
            while (dr.Read())
            {
                var row = new Object[count];
                for (var i = 0; i < count; i++)
                {
                    var val = dr[i];
                    if (val == DBNull.Value) val = GetDefault(Types[i].GetTypeCode());
                    row[i] = val;
                }
                rs.Add(row);
            }
            Rows = rs;
        }

        private const Byte _Ver = 1;

        /// <summary>从数据流读取</summary>
        /// <param name="ms"></param>
        public void Read(Stream ms)
        {
            var bn = new Binary
            {
                EncodeInt = true,
                Stream = ms,
            };

            // 头部，版本和压缩标记
            var ver = bn.Read<Byte>();
            var flag = bn.Read<Byte>();

            // 写入头部
            var count = bn.Read<Int32>();
            Columns = new String[count];
            Types = new Type[count];
            for (var i = 0; i < count; i++)
            {
                Columns[i] = bn.Read<String>();
                var tc = (TypeCode)bn.Read<Byte>();
                Types[i] = tc.ToString().GetTypeEx(false);
            }

            // 写入数据
            var count2 = bn.Read<Int32>();
            Rows = new List<Object[]>(count2);
            for (var k = 0; k < count2; k++)
            {
                var row = new Object[count];
                for (var i = 0; i < count; i++)
                {
                    row[i] = bn.Read(Types[i]);
                }
                Rows.Add(row);
            }
        }

        /// <summary>写入数据流</summary>
        /// <param name="ms"></param>
        public void Write(Stream ms)
        {
            var count = Columns.Length;

            var bn = new Binary
            {
                EncodeInt = true,
                Stream = ms,
            };

            // 头部，版本和压缩标记
            bn.Write(_Ver);
            bn.Write((Byte)0);

            // 写入头部
            bn.Write(count);
            for (var i = 0; i < count; i++)
            {
                bn.Write(Columns[i]);
                bn.Write((Byte)Types[i].GetTypeCode());
            }

            // 写入数据
            count = Rows.Count;
            bn.Write(count);
            foreach (var row in Rows)
            {
                for (var i = 0; i < row.Length; i++)
                {
                    bn.Write(row[i], Types[i]);
                }
            }
        }

        /// <summary>读取</summary>
        /// <param name="pk"></param>
        /// <returns></returns>
        public Boolean Read(Packet pk)
        {
            if (pk == null || pk.Total == 0) return false;

            Read(pk.GetStream());

            return true;
        }

        /// <summary>转数据包</summary>
        /// <returns></returns>
        public Packet ToPacket()
        {
            var ms = new MemoryStream
            {
                Position = 8
            };

            Write(ms);

            ms.Position = 8;
            return new Packet(ms);
        }
        #endregion

        #region 辅助
        /// <summary>数据集</summary>
        /// <returns></returns>
        public override String ToString() => "DbSet[{0}][{1}]".F(Columns == null ? 0 : Columns.Length, Rows == null ? 0 : Rows.Count);

        private static IDictionary<TypeCode, Object> _Defs;
        private static Object GetDefault(TypeCode tc)
        {
            if (_Defs == null)
            {
                var dic = new Dictionary<TypeCode, Object>();
                foreach (TypeCode item in Enum.GetValues(typeof(TypeCode)))
                {
                    Object val = null;
                    switch (item)
                    {
                        case TypeCode.Boolean:
                            val = false;
                            break;
                        case TypeCode.Char:
                            val = (Char)0;
                            break;
                        case TypeCode.SByte:
                            val = (SByte)0;
                            break;
                        case TypeCode.Byte:
                            val = (Byte)0;
                            break;
                        case TypeCode.Int16:
                            val = (Int16)0;
                            break;
                        case TypeCode.UInt16:
                            val = (UInt16)0;
                            break;
                        case TypeCode.Int32:
                            val = (Int32)0;
                            break;
                        case TypeCode.UInt32:
                            val = (UInt32)0;
                            break;
                        case TypeCode.Int64:
                            val = (Int64)0;
                            break;
                        case TypeCode.UInt64:
                            val = (UInt64)0;
                            break;
                        case TypeCode.Single:
                            val = (Single)0;
                            break;
                        case TypeCode.Double:
                            val = (Double)0;
                            break;
                        case TypeCode.Decimal:
                            val = (Decimal)0;
                            break;
                        case TypeCode.DateTime:
                            val = DateTime.MinValue;
                            break;
                        case TypeCode.Empty:
                        case TypeCode.Object:
                        case TypeCode.DBNull:
                        case TypeCode.String:
                        default:
                            val = null;
                            break;
                    }
                    dic[item] = val;
                }
                _Defs = dic;
            }

            return _Defs[tc];
        }
        #endregion
    }
}