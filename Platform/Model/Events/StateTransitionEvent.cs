using FluorescenceFullAutomatic.Platform.StateMachine;

namespace FluorescenceFullAutomatic.Platform.Model.Events
{
    /// <summary>
    /// ×´Ì¬Ç¨ÒÆÊÂ¼þ
    /// </summary>
    public class StateTransitionEvent : ITestEvent
    {
        /// <summary>
        /// Ô´×´Ì¬
        /// </summary>
        public DetectionState FromState { get; set; }

        /// <summary>
        /// Ä¿±ê×´Ì¬
        /// </summary>
        public DetectionState ToState { get; set; }

        /// <summary>
        /// ´¥·¢Æ÷
        /// </summary>
        public DetectionTrigger Trigger { get; set; }

        /// <summary>
        /// Ç¨ÒÆÔ­ÒòÃèÊö
        /// </summary>
        public string Reason { get; set; }

        public override string ToString()
        {
            return $"[×´Ì¬Ç¨ÒÆ] {FromState} --[{Trigger}]--> {ToState} ({Reason})";
        }
    }
}
