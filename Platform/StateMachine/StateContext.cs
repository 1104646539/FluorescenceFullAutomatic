using System.Linq;
using FluorescenceFullAutomatic.Core.Model;
using FluorescenceFullAutomatic.Platform.Model;
using FluorescenceFullAutomatic.Platform.Services;

namespace FluorescenceFullAutomatic.Platform.StateMachine
{
    /// <summary>
    /// 状态机业务数据上下文
    /// </summary>
    public class StateContext
    {
        private readonly IConfigService _configService;

        public StateContext(IConfigService configService)
        {
            _configService = configService;
        }

        // ========== 当前状态 ==========
        
        /// <summary>
        /// 当前状态
        /// </summary>
        public DetectionState CurrentState { get; set; } = DetectionState.Idle;

        // ========== 样本架相关 ==========
        
        /// <summary>
        /// 第一排样本架位置
        /// </summary>
        public int SampleShelfFirstPos { get; set; } = -1;

        /// <summary>
        /// 最后一排样本架位置
        /// </summary>
        public int SampleShelfLastPos { get; set; } = -1;

        /// <summary>
        /// 当前样本架位置
        /// </summary>
        public int CurrentShelfPos { get; set; } = -1;

        /// <summary>
        /// 当前样本位（0-4）
        /// </summary>
        public int CurrentSamplePos { get; set; } = 0;

        /// <summary>
        /// 样本架状态数组（6排）
        /// </summary>
        public bool[] SampleShelfStates { get; set; } = new bool[6];

        // ========== 设备状态 ==========
        
        /// <summary>
        /// 卡仓是否存在
        /// </summary>
        public bool CardExist { get; set; }

        /// <summary>
        /// 卡数量
        /// </summary>
        public int CardNum { get; set; }

        /// <summary>
        /// 清洗液是否存在
        /// </summary>
        public bool CleanoutFluidExist { get; set; }

        // ========== 当前样本信息 ==========
        
        /// <summary>
        /// 当前加样的检测结果（当前样本位正在处理的结果）
        /// </summary>
        public TestResult CurrentAddingSampleTestResult { get; set; }

        /// <summary>
        /// 正在移动到反应区的检测结果
        /// 由于移动反应区与下一个样本处理并行，需要单独保存
        /// </summary>
        public TestResult MovingToReactionAreaTestResult { get; set; }

        /// <summary>
        /// 当前样本类型
        /// </summary>
        public SampleType CurrentSampleType { get; set; }

        /// <summary>
        /// 当前样本条码
        /// </summary>
        public string CurrentBarcode { get; set; }

        // ========== 条件标志 ==========
        
        /// <summary>
        /// 取样是否完成
        /// </summary>
        public bool SamplingCompleted { get; set; }

        /// <summary>
        /// 推卡是否完成
        /// </summary>
        public bool PushCardCompleted { get; set; }

        /// <summary>
        /// 清洗是否完成
        /// </summary>
        public bool CleaningCompleted { get; set; } = true;

        /// <summary>
        /// 是否正在等待清洗完成以便取样
        /// </summary>
        public bool IsWaitingForCleaningToSample { get; set; } = false;

        /// <summary>
        /// 推卡是否成功
        /// </summary>
        public bool PushCardSuccess { get; set; }

        // ========== 重试计数 ==========
        
        /// <summary>
        /// 推卡重试次数
        /// </summary>
        public int PushCardRetryCount { get; set; }

        /// <summary>
        /// 推卡最大重试次数
        /// </summary>
        public const int MaxPushCardRetry = 3;

        // ========== 反应区 ==========
        
        /// <summary>
        /// 当前反应区X坐标
        /// </summary>
        public int ReactionAreaX { get; set; } = -1;

        /// <summary>
        /// 当前反应区Y坐标
        /// </summary>
        public int ReactionAreaY { get; set; } = 0;
        /// <summary>
        /// 当前正在检测的反应区X坐标
        /// </summary>
        public int ReactionAreaTestX { get; set; } = -1;

        /// <summary>
        /// 当前正在检测的反应区Y坐标
        /// </summary>
        public int ReactionAreaTestY { get; set; } = 0;
        /// <summary>
        /// 正在检测的检测结果ID
        /// </summary>
        public int TestResultId = -1;

        // ========== 计算属性 ==========

        /// <summary>
        /// 仪器状态是否正常（核心硬件条件）
        /// </summary>
        public bool IsMachineStatusValid =>
            CardExist && CardNum > 0 && CleanoutFluidExist && SampleShelfStates.Any(x => x);

        /// <summary>
        /// 获取仪器状态异常的描述信息
        /// </summary>
        public string GetMachineStatusErrorMessage()
        {
            if (IsMachineStatusValid) return string.Empty;

            string msg = $"仪器状态异常,{(CardExist == false ? "卡仓不存在," : "")}{(CardNum <= 0 ? "检测卡不足," : "")}{(CleanoutFluidExist == false ? "清洗液不存在," : "")}{(SampleShelfStates.Any(x => x == true) ? "" : "样本架不存在")}";
            return msg.TrimEnd(',');
        }

        /// <summary>
        /// 从原始模型更新状态
        /// </summary>
        public void UpdateFromModel(MachineStatusModel model)
        {
            if (model == null) return;

            CardExist = model.CardExist == "1";
            CardNum = int.TryParse(model.CardNum, out var cn) ? cn : 0;
            CleanoutFluidExist = model.CleanoutFluid == "1";
            
            if (model.SampleShelf != null && model.SampleShelf.Count >= 6)
            {
                for (int i = 0; i < 6; i++)
                {
                    SampleShelfStates[i] = model.SampleShelf[i] == 1;
                }
            }
        }

        /// <summary>
        /// 自检是否失败
        /// </summary>
        public bool IsSelfInspectionFailed { get; set; }

        /// <summary>
        /// 仪器是否处于空闲/就绪状态
        /// </summary>
        public bool IsIdleOrReady => CurrentState == DetectionState.Idle || CurrentState == DetectionState.Ready || CurrentState == DetectionState.Completed;

        /// <summary>
        /// 是否还有更多样本
        /// </summary>
        public bool HasMoreSamples
        {
            get
            {
                // 当前位置还没到最后
                if (CurrentSamplePos < 4)
                    return true;

                // 当前样本架还没到最后一排
                if (CurrentShelfPos < SampleShelfLastPos)
                    return true;

                return false;
            }
        }

        /// <summary>
        /// 是否是当前排的最后一个位置
        /// </summary>
        public bool IsLastPositionInCurrentRow => CurrentSamplePos >= 4;

        /// <summary>
        /// 是否还有下一排样本架
        /// </summary>
        public bool HasNextRow
        {
            get
            {
                // 检查当前排之后是否还有有样本的排
                for (int i = CurrentShelfPos + 1; i < 6; i++)
                {
                    if (SampleShelfStates[i])
                        return true;
                }
                return false;
            }
        }

        /// <summary>
        /// 是否需要扫码（样本管类型 且 开启了扫码配置 才需要）
        /// </summary>
        public bool RequireBarcode => CurrentSampleType == SampleType.SampleTube && IsNeedScanBarcode();

        /// <summary>
        /// 加样前置条件是否满足
        /// 注意：清洗不是加样的前置条件，而是取样的前置条件
        /// </summary>
        public bool AllConditionsMetForAddingSample =>
            SamplingCompleted && PushCardCompleted && PushCardSuccess;

        // ========== 方法 ==========
        
        /// <summary>
        /// 重置所有状态（准备开始新的检测）
        /// </summary>
        public void Reset()
        {
            SampleShelfFirstPos = -1;
            SampleShelfLastPos = -1;
            CurrentShelfPos = -1;
            CurrentSamplePos = 0;
            SampleShelfStates = new bool[6];

            CardExist = false;
            CardNum = 0;
            CleanoutFluidExist = false;

            CurrentAddingSampleTestResult = null;
            MovingToReactionAreaTestResult = null;
            CurrentSampleType = SampleType.None;
            CurrentBarcode = null;

            ResetConditionFlags();

            PushCardRetryCount = 0;
            // ReactionAreaX = -1;
            // ReactionAreaY = 0;
        }

        /// <summary>
        /// 重置条件标志
        /// </summary>
        public void ResetConditionFlags()
        {
            SamplingCompleted = false;
            PushCardCompleted = false;
            CleaningCompleted = true;
            PushCardSuccess = false;
        }

        /// <summary>
        /// 初始化样本架状态（找到第一排和最后一排）
        /// </summary>
        public void InitSampleShelfState()
        {
            for (int i = 0; i < 6; i++)
            {
                if (SampleShelfStates[i])
                {
                    if (SampleShelfFirstPos == -1)
                    {
                        SampleShelfFirstPos = i;
                    }
                    SampleShelfLastPos = i;
                }
            }
            CurrentShelfPos = SampleShelfFirstPos;
            CurrentSamplePos = 0;
        }

        /// <summary>
        /// 移动到同一排的下一个样本位
        /// </summary>
        public void MoveToNextSampleInRow()
        {
            if (CurrentSamplePos < 4)
            {
                CurrentSamplePos++;
            }
        }

        /// <summary>
        /// 移动到下一排样本架
        /// </summary>
        public void MoveToNextRow()
        {
            CurrentSamplePos = 0;
            for (int i = CurrentShelfPos + 1; i < 6; i++)
            {
                if (SampleShelfStates[i])
                {
                    CurrentShelfPos = i;
                    break;
                }
            }
        }

        /// <summary>
        /// 移动到下一个样本（兼容旧方法，内部自动判断是否跨排）
        /// </summary>
        public void MoveToNextSample()
        {
            if (CurrentSamplePos < 4)
            {
                // 同一排的下一个样本位
                CurrentSamplePos++;
            }
            else
            {
                // 移动到下一排样本架
                CurrentSamplePos = 0;
                for (int i = CurrentShelfPos + 1; i <= SampleShelfLastPos; i++)
                {
                    if (SampleShelfStates[i])
                    {
                        CurrentShelfPos = i;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 获取取样类型字符串
        /// </summary>
        public string GetSampleTypeString()
        {
            return CurrentSampleType switch
            {
                SampleType.SampleTube => "1",  // 样本管
                SampleType.SampleCup => "2",   // 样本杯
                _ => "0"
            };
        }

        /// <summary>
        /// 获取取样体积（从配置读取）
        /// </summary>
        public int GetSampleVolume()
        {
            return _configService?.SamplingVolume() ?? 100; // 默认100μL
        }

        /// <summary>
        /// 获取清洗时长
        /// </summary>
        public int GetCleanoutDuration()
        {
            return _configService?.CleanoutDuration() ?? 5; // 默认5秒
        }

        /// <summary>
        /// 是否保留反应区
        /// </summary>
        public bool GetRetainReactionArea()
        {
            return _configService?.RetainReactionArea() ?? false;
        }

        /// <summary>
        /// 是否需要扫码
        /// </summary>
        public bool IsNeedScanBarcode()
        {
            return _configService?.IsScanBarcode() ?? false;
        }
    }
}
