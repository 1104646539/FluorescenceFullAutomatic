using System.Threading.Tasks;
using FluorescenceFullAutomatic.Platform.Model.Events;
using FluorescenceFullAutomatic.Platform.StateMachine;

namespace FluorescenceFullAutomatic.Platform.Services
{
    /// <summary>
    /// QC 质控流程状态机接口
    /// </summary>
    public interface IQCStateMachine
    {
        /// <summary>
        /// 触发状态迁移
        /// </summary>
        Task FireAsync(QCTrigger trigger);

        /// <summary>
        /// 校验是否可以开始质控
        /// </summary>
        QCValidationErrorType ValidateStartQC();

        /// <summary>
        /// 用户确认已添加检测卡
        /// </summary>
        Task HandleCardAddedConfirmAsync();

        /// <summary>
        /// 用户重试推卡
        /// </summary>
        Task HandleRetryPushCardAsync();

        /// <summary>
        /// 取消质控
        /// </summary>
        Task HandleCancelQCAsync();
    }
}
