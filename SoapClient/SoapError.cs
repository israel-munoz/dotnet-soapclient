namespace SoapClientService
{
    /// <summary>
    /// Request error model
    /// </summary>
    internal class SoapError
    {
        /// <summary>
        /// Error code
        /// </summary>
        public string Code { get; set; }
        /// <summary>
        /// Error message
        /// </summary>
        public string Message { get; set; }
        /// <summary>
        /// Error detail
        /// </summary>
        public string Detail { get; set; }
    }
}
