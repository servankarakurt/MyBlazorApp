namespace GorevTakipUygulamasi.Configuration
{
    public class AzureStorageSettings
    {
        public string ConnectionString { get; set; } = "";
        public string RemindersTableName { get; set; } = "Reminders";
        public string UsersTableName { get; set; } = "Users";
    }
}
