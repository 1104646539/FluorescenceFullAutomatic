using SqlSugar;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluorescenceFullAutomatic.Converters
{
    public class IntArrayToStringConvert : ISugarDataConverter
    {
        public SugarParameter ParameterConverter<T>(object columnValue, int columnIndex)
        {
            string name = "@pointArray" + columnIndex;
            var str = new SerializeService().SerializeObject(columnValue);

            return new SugarParameter(name, str);
        }

        public T QueryConverter<T>(IDataRecord dr, int i)
        {
            var str = dr.GetValue(i) + "";
            return new SerializeService().DeserializeObject<T>(str);
        }
    }
}
