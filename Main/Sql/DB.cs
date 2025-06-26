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
                Log.Information($"connectionString={connectionString} 连接失败 dbType={dbType}");
                return;
            }
            try
            {
                //通过这个可以直接连接数据库
                Db = new SqlSugarClient(new ConnectionConfig()
                {
                    //可以在连接字符串中设置连接池pooling=true;表示开启连接池
                    //eg:min pool size=2;max poll size=4;表示最小连接池为2，最大连接池是4；默认是100
                    DbType = dbType,
                    ConnectionString = connectionString,
                    IsAutoCloseConnection = true,//自动关闭连接
                    InitKeyType = InitKeyType.Attribute,

                }, db => {
                    db.Aop.OnLogExecuting = (sql, pars) =>
                    {
                        //Log.info(sql);//输出sql,查看执行sql 性能无影响
                    };
                });
            }
            catch (Exception ex) {
                Log.Information($"connectionString={connectionString} 连接失败ex={ex.Message} dbType=${dbType}");
            }
            Log.Information($"connectionString={connectionString} 连接成功={IsValidConnection()} dbType=${dbType}");

            //var tables = Db.DbMaintenance.GetTableInfoList();//走缓存 , 有重载可以不走缓存
            //foreach (var table in tables)
            //{
            //    Log.info("table=" + table.Name);
            //}

        }
        /// <summary>
        /// 测试连接是否有效
        /// </summary>
        /// <returns></returns>
        public bool IsValidConnection() {
            if (Db == null) {
                return false;
            }
           return Db.Ado.IsValidConnection();
        }
        /// <summary>
        /// 释放
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
