using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluorescenceFullAutomatic.Platform.Model;
using FluorescenceFullAutomatic.Platform.Sql;

namespace FluorescenceFullAutomatic.Platform.Services
{
    public interface IPointService
    {
        Point GetPoint(int pointId);

        int InsertPoint(Point point);

        bool UpdatePoint(Point point);

    }
    public class PointService : IPointService
    {
        public PointService()
        {
        }

        public Point GetPoint(int pointId)
        {
            return SqlHelper.getInstance().GetPoint(pointId);
        }

        public int InsertPoint(Point point)
        {
            return SqlHelper.getInstance().InsertPoint(point);
        }

        public bool UpdatePoint(Point point)
        {
            return SqlHelper.getInstance().UpdatePoint(point);
        }
    }
}
