namespace GorevTakipUygulamasi.Configuration
{
    public class LogicAppSettings
    {
        public string ScheduleReminderUrl { get; set; } = "";
        public string CancelReminderUrl { get; set; } = "";
        public string SendEmailUrl { get; set; } = "";
        public string TestConnectionUrl { get; set; } = "";
        public int TimeoutSeconds { get; set; } = 30;
        public int RetryCount { get; set; } = 3;
    }
}
