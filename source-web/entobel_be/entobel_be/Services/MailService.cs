using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.IO;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
//using MailKit.Net.Smtp;
//using MailKit.Security;
//using MimeKit;
using System.Net.Mail;
using System.Net;

namespace entobel_be.Services
{
    public class MailService
    {
        private readonly DbService _dbService;
        private readonly ReportService _rpService;
        private string projectDirectory, mailTemplate;
        public MailService(DbService dbService, ReportService rpService)
        {
            _dbService = dbService;
            _rpService = rpService;
            projectDirectory = Directory.GetCurrentDirectory();
            mailTemplate = projectDirectory + "\\Assets\\Email Report Template.txt";
        }

        public bool SendMailReport()
        {
            try
            {
                // read mail data
                var mails = _dbService.ListMail();
                // get start & end of month
                var lastDayofMonth = DateTime.Now.AddHours(7).AddDays(-DateTime.Now.AddHours(7).Day);
                var report = _rpService.WriteMemoryStream(
                    new DateTime(lastDayofMonth.Year, lastDayofMonth.Month, 1, 0, 0, 0),
                    new DateTime(lastDayofMonth.Year, lastDayofMonth.Month, lastDayofMonth.Day, 0, 0, 0), 
                    "All");
                // init mail config
                MailMessage message = new MailMessage();
                SmtpClient smtp = new SmtpClient();
                message.From = new MailAddress("report.entobel@gmail.com", "S4M Report Server");
                for (var i = 0; i < mails.Count; i++)
                {
                    message.To.Add(new MailAddress(mails[i].Mail));
                }
                message.Subject = "Monthly Report";
                message.IsBodyHtml = true; //to make message body as html  
                message.Body = File.ReadAllText(mailTemplate);
                message.Attachments.Add(new Attachment(report, "report.csv", "text/csv"));
                smtp.Port = 587;
                smtp.Host = "smtp.gmail.com"; //for gmail host  
                smtp.EnableSsl = true;
                smtp.UseDefaultCredentials = false;
                smtp.Credentials = new NetworkCredential("report.entobel@gmail.com", "ymsuaspomiplsnft");
                smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                smtp.Timeout = 5000;
                smtp.Send(message);
                return true;
            }
            catch (Exception e) 
            { 
                System.Diagnostics.Debug.WriteLine(e.Message); 
                return false; 
            }

            //MimeMessage message = new MimeMessage();

            //MailboxAddress from = new MailboxAddress("S4M Report Server",
            //"sdiablo77@gmail.com");
            //message.From.Add(from);

            ////MailboxAddress to = new MailboxAddress("Customer",
            ////"anh.dao@s4mengineering.com");
            //InternetAddressList listTo = new InternetAddressList();
            ////for (var i = 0; i < listMail.Count; i++)
            ////{
            ////    listTo.Add(new MailboxAddress("Customer", listMail[i]));
            ////}
            //listTo.Add(new MailboxAddress("Customer", "anh.dao@s4mengineering.com"));
            //message.To.AddRange(listTo);

            //message.Subject = "Monthly Report";

            //BodyBuilder bodyBuilder = new BodyBuilder();
            ////bodyBuilder.HtmlBody = "<h1>Hello World!</h1>";
            ////var projectDirectory = Directory.GetCurrentDirectory();

            //string mailBody = File.ReadAllText(mailTemplate);
            ////string tableBody = "";
            ////// loop listAlarm to create row body
            ////for (var i = 0; i < listAlarm.Count; i++)
            ////{
            ////    var row = CreateRow(listAlarm[i], (i + 1).ToString());
            ////    tableBody = tableBody + row;
            ////}
            ////mailBody = mailBody.Replace("{{row}}", tableBody);

            //bodyBuilder.HtmlBody = mailBody;
            ////bodyBuilder.TextBody = "Hello World!";
            //var report = _rpService.WriteMemoryStream(DateTime.Now, DateTime.Now, "Day", "cups");


            //message.Body = bodyBuilder.ToMessageBody();

            //SmtpClient client = new SmtpClient();
            //client.CheckCertificateRevocation = false;
            //client.Connect("smtp.gmail.com", 465, true);
            //client.Authenticate("sdiablo77@gmail.com", "ohvvubsgvosdvxmr");

            //client.Send(message);
            //client.Disconnect(true);
            //client.Dispose();
        }

    }
}
