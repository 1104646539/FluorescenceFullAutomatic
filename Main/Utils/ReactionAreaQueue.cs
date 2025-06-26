using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluorescenceFullAutomatic.Model;
using System.Timers;
using Serilog;

namespace FluorescenceFullAutomatic.Utils
{
    public class ReactionAreaQueue
    {
        private Queue<ReactionAreaItem> _queue = new Queue<ReactionAreaItem>();
        private Timer _timer;
        private int enqueueDurationSeconds = 60;
        public  event Func<ReactionAreaItem, bool> _dequeueCallback;
        private static ReactionAreaQueue Instance;
        private ReactionAreaQueue(){
            // 初始化定时器，每秒检查一次
            _timer = new Timer(1000);
            _timer.Elapsed += Timer_Elapsed;
            _timer.AutoReset = true;
            _timer.Enabled = true;
        }

        public void SetEnqueueDuration(int enqueueDurationSeconds){
            this.enqueueDurationSeconds = enqueueDurationSeconds;

        }

        public static ReactionAreaQueue getInstance(){
            if(Instance == null){
                Instance = new ReactionAreaQueue();
            }
            return Instance;
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
            Log.Information($"出队时间:{item.ReactionAreaY} {item.ReactionAreaX} {item.EnqueueTime} + {enqueueDurationSeconds} （{item.EnqueueTime + enqueueDurationSeconds}）<= {currentTime}");
            // 检查是否达到出队时间
            if (item.EnqueueTime + enqueueDurationSeconds <= currentTime)
            {
                // 调用回调函数，判断是否成功出队
                if (_dequeueCallback != null && _dequeueCallback(item))
                {   
                    ReactionAreaItem item1 = _queue.Dequeue();
                    Log.Information($"出队1:{item1.ReactionAreaY} {item1.ReactionAreaX} {item1.TestResult.Id}");
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

        /// <summary>
        /// 获取队列中的项目数量
        /// </summary>
        public int Count => _queue.Count;

        /// <summary>
        /// 检查队列是否为空
        /// </summary>
        public bool IsEmpty => _queue.Count == 0;
    }
}
