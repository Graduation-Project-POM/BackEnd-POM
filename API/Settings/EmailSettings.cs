namespace MIO.API.Settings
{
    public class EmailSettings
    {
            public string SenderEmail { get; set; }
            public string SenderPassword { get; set; }
            public string SenderName { get; set; }
            public string SmtpHost { get; set; }
            public int Port { get; set; }
    }
}
