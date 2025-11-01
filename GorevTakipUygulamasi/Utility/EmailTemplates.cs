using GorevTakipUygulamasi.Models;

namespace GorevTakipUygulamasi.Utility
{
        public static class EmailTemplates
        {
            public static string GetReminderEmailHtml(ReminderNotificationDto notification)
            {
                return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Hatırlatıcı</title>
    <style>
        body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; margin: 0; padding: 20px; background-color: #f5f5f5; }}
        .container {{ max-width: 600px; margin: 0 auto; background: white; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 12px rgba(0,0,0,0.1); }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 30px; text-align: center; color: white; }}
        .content {{ padding: 30px; }}
        .reminder-card {{ background: #f8f9fa; border-radius: 8px; padding: 20px; margin: 20px 0; }}
        .time-badge {{ background: #667eea; color: white; padding: 6px 12px; border-radius: 6px; font-size: 14px; font-weight: 600; }}
        .footer {{ background: #f8f9fa; padding: 20px; text-align: center; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>🔔 Hatırlatıcı</h1>
            <p>Planladığınız aktivitenizin zamanı geldi!</p>
        </div>
        <div class=""content"">
            <div class=""reminder-card"">
                <h2>{notification.Title}</h2>
                {(!string.IsNullOrEmpty(notification.Description) ? $"<p>{notification.Description}</p>" : "")}
                <p><span class=""time-badge"">{notification.ScheduledDateTime:dd MMMM yyyy HH:mm}</span></p>
            </div>
            <p>Bu hatırlatıcıyı <strong>{notification.ScheduledDateTime:dd MMMM yyyy HH:mm}</strong> için ayarlamıştınız.</p>
        </div>
        <div class=""footer"">
            <p>Bu email otomatik olarak gönderilmiştir. • Hatırlatıcı Uygulaması</p>
        </div>
    </div>
</body>
</html>";
            }

            public static string GetReminderEmailSubject(ReminderNotificationDto notification)
            {
                return $"🔔 Hatırlatıcı: {notification.Title}";
            }
        }
    }
