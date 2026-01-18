namespace FluorescenceFullAutomatic.Platform.Model.Events
{
    /// <summary>
    /// 条码扫描事件
    /// </summary>
    public class BarcodeScanCompletedEvent : TestEventBase
    {
        /// <summary>
        /// 条码内容
        /// </summary>
        public string Barcode { get; set; }

        /// <summary>
        /// 是否扫描成功
        /// </summary>
        public bool Success { get; set; }
    }
}
