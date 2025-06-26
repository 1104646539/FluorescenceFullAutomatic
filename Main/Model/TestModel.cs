using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluorescenceFullAutomatic.Model
{
    /// <summary>
    /// 检测结果
    /// </summary>
    public class TestModel
    {
        /// <summary>
        /// t
        /// </summary>
        private string t;

        public string T
        {
            get { return t; }
            set { t = value; }
        }
        /// <summary>
        /// c
        /// </summary>
        private string c;

        public string C
        {
            get { return c; }
            set { c = value; }
        }
        /// <summary>
        /// t2
        /// </summary>
        private string t2;

        public string T2
        {
            get { return t2; }
            set { t2 = value; }
        }
        /// <summary>
        /// c2
        /// </summary>
        private string c2;

        public string C2
        {
            get { return c2; }
            set { c2 = value; }
        }
        /// <summary>
        /// 图像
        /// </summary>
        private List<int> point;

        public List<int> Point
        {
            get { return point; }
            set { point = value; }
        }
        /// <summary>
        /// 卡片类型 0单联卡，1双联卡
        /// </summary>
        private string cardType;

        public string CardType
        {
            get { return cardType; }
            set { cardType = value; }
        }
        /// <summary>
        /// 检测类型 0普通卡，1质控
        /// </summary>
        private string testType;

        public string TestType
        {
            get { return testType; }
            set { testType = value; }
        }

        /// <summary>
        /// 顶点位置依次是 T,C, T2, C2
        /// </summary>
        private int[] location;
        public int[] Location
        {
            get { return location; }
            set { location = value; }
        }

    }
}
