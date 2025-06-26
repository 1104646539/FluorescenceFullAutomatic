using Serilog;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluorescenceFullAutomatic.Sql
{
    internal class DB
    {
        public SqlSugarClient Db;
        public SqlSugarClient getDB()
        {
            return Db;
        }

        public DB(DbType dbType,string connectionString)
        {
            if (dbType == null || dbType < 0) {
                Log.Information($"connectionString={connectionString} ����ʧ�� dbType={dbType}");
                return;
            }
            try
            {
                //ͨ���������ֱ���������ݿ�
                Db = new SqlSugarClient(new ConnectionConfig()
                {
                    //�����������ַ������������ӳ�pooling=true;��ʾ�������ӳ�
                    //eg:min pool size=2;max poll size=4;��ʾ��С���ӳ�Ϊ2��������ӳ���4��Ĭ����100
                    DbType = dbType,
                    ConnectionString = connectionString,
                    IsAutoCloseConnection = true,//�Զ��ر�����
                    InitKeyType = InitKeyType.Attribute,

                }, db => {
                    db.Aop.OnLogExecuting = (sql, pars) =>
                    {
                        //Log.info(sql);//���sql,�鿴ִ��sql ������Ӱ��
                    };
                });
            }
            catch (Exception ex) {
                Log.Information($"connectionString={connectionString} ����ʧ��ex={ex.Message} dbType=${dbType}");
            }
            Log.Information($"connectionString={connectionString} ���ӳɹ�={IsValidConnection()} dbType=${dbType}");

            //var tables = Db.DbMaintenance.GetTableInfoList();//�߻��� , �����ؿ��Բ��߻���
            //foreach (var table in tables)
            //{
            //    Log.info("table=" + table.Name);
            //}

        }
        /// <summary>
        /// ���������Ƿ���Ч
        /// </summary>
        /// <returns></returns>
        public bool IsValidConnection() {
            if (Db == null) {
                return false;
            }
           return Db.Ado.IsValidConnection();
        }
        /// <summary>
        /// �ͷ�
        /// </summary>
        public void Dispose()
        {
            if (Db != null)
            {
                Db.Dispose();
            }
        }
    }
}
