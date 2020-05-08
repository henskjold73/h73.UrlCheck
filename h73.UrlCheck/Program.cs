using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using MoreLinq;

namespace h73.UrlCheck
{
    class Program
    {
        private static UrlCheckConfiguration _config;
        private static Dictionary<string, string> _output;

        static void Main(string[] args)
        {
            _config = UrlCheckConfiguration.GetConfiguration();
            _output = new Dictionary<string, string>();
            var tasks = new List<Task>();

            tasks.AddRange(_config.Urls.Select(x => Task.Run(async () =>
            {
                while (true)
                {
                    if (await UrlCheck(x))
                    {
                        await Task.Delay(_config.CheckInterval * 1000);
                    }
                    else
                    {
                        await Task.Delay(_config.FailedPause * 1000);
                    }
                }
            })));

            tasks.Add(Task.Run(async () =>
            {
                while (true)
                {
                    Console.Clear();
                    _output.ForEach(o => Console.WriteLine(o.Value));
                    await Task.Delay(5000);
                }
            }));

            Task.WaitAll(tasks.ToArray());
        }

        private static async Task<bool> UrlCheck(string url)
        {
            var request = WebRequest.Create(url);
            var requestFailed = false;
            try
            {
                var response = request.GetResponse();
                _output[url] = $"{url} - OK";
            }
            catch (Exception e)
            {
                requestFailed = true;
            }

            if (requestFailed)
            {
                var count = 0;
                while (requestFailed)
                {
                    try
                    {
                        requestFailed = false;
                        var response = request.GetResponse();
                    }
                    catch (Exception e)
                    {
                        requestFailed = true;
                        _output[url] = $"{url} - Failed {count + 1} times";
                    }
                    if (count >= _config.Retries)
                    {
                        Send(url);
                        return false;
                    }

                    count++;
                    await Task.Delay(_config.RetryInterval * 1000);
                }
            }

            return true;
        }

        private static void Send(string url)
        {
            _output[url] = $"{url} - Failed {_config.Retries} times. Reported";

            var client = new SmtpClient(_config.SmtpConfig.Host);
            if(_config.SmtpConfig?.Port > 0) client.Port = _config.SmtpConfig.Port;
            client.EnableSsl = _config.SmtpConfig.EnableSsl;
            if (!string.IsNullOrEmpty(_config.SmtpConfig?.User) && !string.IsNullOrEmpty(_config.SmtpConfig?.Password)) 
                client.Credentials = new NetworkCredential(_config.SmtpConfig.User, _config.SmtpConfig.Password);

            var mail = new MailMessage {From = new MailAddress(_config.FromEmail)};
            mail.To.Add(_config.ReportEmail);
            mail.IsBodyHtml = true;
            mail.Subject = $"Request failed {url}";
            mail.Body = $"{string.Join("<br />", _output.Select(x=>x.Value))}";
            try
            {
                client.Send(mail);
            }
            catch (Exception e)
            {
                _output["LastEmailError"] = $"{e.Message} - {e.InnerException.Message}";
            }
            

        }
    }
}