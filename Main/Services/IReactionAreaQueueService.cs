using FluorescenceFullAutomatic.Model;
using FluorescenceFullAutomatic.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace FluorescenceFullAutomatic.Services
{
    public interface IReactionAreaQueueService
    {
        public event Func<ReactionAreaItem, bool> _dequeueCallback;

        void Enqueue(ReactionAreaItem item);

        void SetDequeueDuration(int durationSeconds);

        void Clear();

        int Count();

        bool IsEmpty();

        bool IsFull();
    }

    public class ReactionAreaQueueRepository : IReactionAreaQueueService
    {
        private Queue<ReactionAreaItem> _queue = new Queue<ReactionAreaItem>();
        private Timer _timer;
        private int dequeueSeconds = 60;
        public event Func<ReactionAreaItem, bool> _dequeueCallback;
        public int MaxCount = 30;
        public ReactionAreaQueueRepository()
        {
            // 初始化定时器，每秒检查一次
            _timer = new Timer(1000);
            _timer.Elapsed += Timer_Elapsed;
            _timer.AutoReset = true;
            _timer.Enabled = true;
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Dequeue();
        }

        /// <summary>
        /// 出队操作
        /// </summary>
        /// <returns>是否成功出队</returns>
        public bool Dequeue()
        {
            if (_queue.Count == 0)
                return false;

            var item = _queue.Peek();
            long currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            //Log.Information($"出队时间:{item.ReactionAreaY} {item.ReactionAreaX} {item.EnqueueTime} + {dequeueSeconds} （{item.EnqueueTime + dequeueSeconds}）<= {currentTime}");
            // 检查是否达到出队时间
            if (item.EnqueueTime + dequeueSeconds <= currentTime)
            {
                // 调用回调函数，判断是否成功出队
                if (_dequeueCallback != null && _dequeueCallback(item))
                {
                    ReactionAreaItem item1 = _queue.Dequeue();
                    //Log.Information($"出队1:{item1.ReactionAreaY} {item1.ReactionAreaX} {item1.TestResult.Id}");
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 入队操作
        /// </summary>
        /// <param name="item">要入队的项目</param>
        public void Enqueue(ReactionAreaItem item)
        {
            if (item == null)
                return;

            // 设置入队时间
            item.EnqueueTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            _queue.Enqueue(item);
        }

        /// <summary>
        /// 清空队列
        /// </summary>
        public void Clear()
        {
            _queue.Clear();
        }

        public void SetDequeueDuration(int durationSeconds)
        {
            this.dequeueSeconds = durationSeconds;
        }

        public int Count()
        {
            if (_queue == null) return 0;
            return _queue.Count;
        }

        public bool IsEmpty()
        {
            return _queue == null || _queue.Count == 0;
        }

        public bool IsFull()
        {
            return Count() >= MaxCount;
        }
    }

}
