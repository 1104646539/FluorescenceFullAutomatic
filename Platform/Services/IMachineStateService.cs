using FluorescenceFullAutomatic.Core.Config;

namespace FluorescenceFullAutomatic.Platform.Services
{
    /// <summary>
    /// 仪器状态服务接口，用于管理仪器运行状态
    /// </summary>
    public interface IMachineStateService
    {
        /// <summary>
        /// 获取当前仪器状态
        /// </summary>
        MachineStatus CurrentMachineStatus { get; }

        /// <summary>
        /// 设置当前仪器状态
        /// </summary>
        /// <param name="status">新的仪器状态</param>
        void SetMachineStatus(MachineStatus status);

        /// <summary>
        /// 检查仪器是否处于运行错误状态
        /// </summary>
        /// <returns>如果仪器处于运行错误状态返回true，否则返回false</returns>
        bool IsRunningError();

        /// <summary>
        /// 检查仪器是否正在运行中
        /// </summary>
        /// <returns>如果仪器正在运行返回true，否则返回false</returns>
        bool IsRunning();

        /// <summary>
        /// 检查仪器是否已准备好
        /// </summary>
        /// <returns>如果仪器已准备好返回true，否则返回false</returns>
        bool IsPrepare();
    }
    /// <summary>
    /// 仪器状态服务实现，用于管理仪器运行状态
    /// </summary>
    public class MachineStateService : IMachineStateService
    {
        /// <summary>
        /// 获取当前仪器状态
        /// </summary>
        public MachineStatus CurrentMachineStatus
        {
            get => SystemGlobal.MachineStatus;
            private set => SystemGlobal.MachineStatus = value;
        }

        /// <summary>
        /// 设置当前仪器状态
        /// </summary>
        /// <param name="status">新的仪器状态</param>
        public void SetMachineStatus(MachineStatus status)
        {
            SystemGlobal.MachineStatus = status;
        }

        /// <summary>
        /// 检查仪器是否处于运行错误状态
        /// </summary>
        /// <returns>如果仪器处于运行错误状态返回true，否则返回false</returns>
        public bool IsRunningError()
        {
            return SystemGlobal.MachineStatus.IsRunningError();
        }

        /// <summary>
        /// 检查仪器是否正在运行中
        /// </summary>
        /// <returns>如果仪器正在运行返回true，否则返回false</returns>
        public bool IsRunning()
        {
            return SystemGlobal.MachineStatus.IsRunning();
        }

        /// <summary>
        /// 检查仪器是否已准备好
        /// </summary>
        /// <returns>如果仪器已准备好返回true，否则返回false</returns>
        public bool IsPrepare()
        {
            return SystemGlobal.MachineStatus.IsPrepare();
        }
    }
}