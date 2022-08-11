using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using EasyPeasy.Common;
using EasyPeasy.Services.Interface.v1;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.Extensions.Configuration;

namespace EasyPeasy.Services.Implementation.v1
{
    public class EmailTemplateService : IEmailTemplateService
    {
        private readonly IConfiguration configuration;

        public EmailTemplateService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }
        public bool Welcome(string toName, string toEmail)
        {
            try
            {
                var sb = new StringBuilder();
                sb.Append("<!doctype html>");
                sb.Append("<html style='padding: 0;margin: 0;'>");
                sb.Append("<head>");
                sb.Append("    <meta charset='utf-8'>");
                sb.Append("    <meta name='description' content=''>");
                sb.Append("    <meta name='viewport' content='width=device-width, initial-scale=1'>");
                sb.Append("    <link rel='shortcut icon' href='https://easypeasycards.com/assets/img/favicon.png'>");
                sb.Append("    <!-- Title");
                sb.Append("      ================== -->");
                sb.Append("    <title>Welcome</title>");
                sb.Append("</head>");
                sb.Append("<body style='padding: 0;margin: 0;font-family:sans-serif;width: 100%;height: 100%;text-align: center;background: #EFEFEF;text-align: center;background-image: url(bg-grey.jpg);background-size:100% 100%;'>");
                sb.Append("    <main style='position: relative;text-align:center;display: inline-block;width: 85%;height: auto;margin: 0 auto;max-width: 620px;margin: 100px 0 0px;background: #fff;box-shadow: 0px 0px 76px rgba(0, 0, 0, 0.16);border-radius: 20px;'>");
                sb.Append("        <header style='width: 100%;min-height: 90px;background-color: #fff;border-radius: 20px 20px 0 0;display: inline-block;padding:70px 0 15px;float: left;'>");
                sb.Append("            <div style='max-width: 95%;text-align: center;margin: 0 auto;'>");
                sb.Append("                <a style='display: inline-block;float:none;max-width: 300px;margin: 0 auto;'>");
                sb.Append("                    <img src='https://d12oonsp3a9sve.cloudfront.net/logo.png' alt='logo' style='text-align: center;margin:0 auto;max-width:100%;'>");
                sb.Append("                </a>");
                sb.Append("            </div>");
                sb.Append("        </header>");
                sb.Append("        <div style='display: inline-block;width: 100%;text-align: center;background-color: #ffffff;float: left;border-radius: 0 0 20px 20px;padding-bottom: 50px;'>");
                sb.Append("            <div style='font-weight:lighter;display: inline-block;width: 100%;max-width: 90%;margin: 0 auto;padding: 15px 0 30px;'>");
                sb.Append("                <p style='margin: 0;color:#333333;font-size: 20px;margin: 15px 0 5px;line-height: 25px;font-weight: normal;text-align: center;'>Welcome</p>");
                sb.Append($"                <p style='margin: 0;text-align:center;color:#00A87E !important;font-size: 40px;margin-top:10px;;'>{toName}</p>");
                sb.Append("                <p style='color:#333333;font-size: 18px;margin: 15px 0 50px;line-height: 25px;font-weight: normal;text-align: center;'>");
                sb.Append("                    Congratulations, Your account has been successfully created. Now, you can start sending or receiving eGift cards.");
                sb.Append("                </p>");
                sb.Append("                <a href='https://easypeasycards.com/#explore' style='text-decoration:none; padding:15px 0;background-color:#00D9A3 !important;color:white !important;border-radius:30px !important;text-align:center;width: 200px;display: inline-block; font-weight:bold;position: relative;");
                sb.Append("                            box-shadow: 0px 10px 20px rgba(0, 0, 0, 0.16);border-bottom:7px solid #00A87E !important;text-shadow:0 2px 0 rgba(0,0,0,0.26);'>");
                sb.Append("                    Explore Cards");
                sb.Append("                </a>");
                sb.Append("            </div>");
                sb.Append("        </div>");
                sb.Append("    </main>");
                sb.Append("    <ul style='display: inline-block;width: 100%;text-align: center;margin: 30px 0 0;padding: 0;'>");
                sb.Append("        <li style='display: inline-block;margin: 0;'>");
                sb.Append("            <a href='https://easypeasycards.com/terms-conditions' style='display: inline-block;position:relative;border:hidden;text-decoration:none !important ;outline: 0 !important;font-size:16px;color:#333333;margin: 0 10px;'>Terms & Condition</a>");
                sb.Append("        </li>");
                sb.Append("        <li style='display: inline-block;margin: 0;'>");
                sb.Append("            <a href='https://easypeasycards.com/privacy-policy' style='display: inline-block;position:relative;border:hidden;text-decoration:none !important ;outline: 0 !important;font-size:16px;color:#333333;margin: 0 10px;'>Privacy Policy</a>");
                sb.Append("        </li>");
                sb.Append("    </ul>");
                sb.Append("    <ul style='display: inline-block;width: 100%;text-align: center;margin: 30px 0 50px;padding: 0;'>");
                sb.Append("        <li style='display: inline-block;margin: 0 10px;'>");
                sb.Append("            <a target='_blank' href='https://www.facebook.com/EasyPeasyCards' style='display: inline-block;position:relative;border:hidden;text-decoration:none !important ;outline: 0 !important;'>");
                sb.Append("                <img src='https://api.hooley.app/OtherMedia/icon-1.png' alt='facebook' style='-ms-interpolation-mode: bicubic;'>");
                sb.Append("            </a>");
                sb.Append("        </li>");
                sb.Append("        <li style='display: inline-block;margin: 0 10px;'>");
                sb.Append("            <a target='_blank' href='https://www.instagram.com/easypeasycards' style='display: inline-block;position:relative;border:hidden;text-decoration:none !important ;outline: 0 !important;'>");
                sb.Append("                <img src='https://api.hooley.app/OtherMedia/icon-3.png' alt='instagram' style='-ms-interpolation-mode: bicubic;'>");
                sb.Append("            </a>");
                sb.Append("        </li>");
                sb.Append("    </ul> ");
                sb.Append("</body>");
                sb.Append("</html>");

                _ = Task.Run(() => { Email.SendEmail("Welcome to EasyPeasy", sb.ToString(), configuration.GetValue<string>("Email"), configuration.GetValue<string>("Password"), toEmail, null, ""); });

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public bool Subscription(string toEmail, string packageName, decimal price, string cardMaskedNumber, string cardExpiry, DateTime invoiceTime, string invoiceId, string paymentMethodLogo, int paymentMethodType)
        {
            try
            {
                var paymentMthod = paymentMethodType == 1 ? "Card" : "Paypal";
                var sb = new StringBuilder();
                sb.Append("<!doctype html>");
                sb.Append("<html style='padding: 0;margin: 0;'>");
                sb.Append("<head>");
                sb.Append("    <meta charset='utf-8'>");
                sb.Append("    <meta name='description' content=''>");
                sb.Append("    <meta name='viewport' content='width=device-width, initial-scale=1'>");
                sb.Append("    <link rel='shortcut icon' href='https://easypeasycards.com/assets/img/favicon.png'>");
                sb.Append("    <!-- Title");
                sb.Append("      ================== -->");
                sb.Append("    <title>Thanks For Subscription</title>");
                sb.Append("</head>");
                sb.Append("<body style='padding: 0;margin: 0;font-family:sans-serif;width: 100%;height: 100%;text-align: center;background: #EFEFEF;text-align: center;background-image: url(bg-grey.jpg);background-size:100% 100%;'>");
                sb.Append("    <main style='position: relative;text-align:center;display: inline-block;width: 85%;height: auto;margin: 0 auto;max-width: 620px;margin: 100px 0 0px;background: #fff;box-shadow: 0px 0px 76px rgba(0, 0, 0, 0.16);border-radius: 20px;'>");
                sb.Append("        <header style='width: 100%;min-height: 90px;background-color: #fff;border-radius: 20px 20px 0 0;display: inline-block;padding:70px 0 15px;float: left;'>");
                sb.Append("            <div style='max-width: 95%;text-align: center;margin: 0 auto;'>");
                sb.Append("                <a style='display: inline-block;float:none;max-width: 300px;margin: 0 auto;'>");
                sb.Append("                    <img src='https://d12oonsp3a9sve.cloudfront.net/logo.png' alt='logo' style='text-align: center;margin:0 auto;max-width:100%;'>");
                sb.Append("                </a>");
                sb.Append("            </div>");
                sb.Append("        </header>");
                sb.Append("        <div style='display: inline-block;width: 100%;text-align: center;background-color: #ffffff;float: left;border-radius: 0 0 20px 20px;padding-bottom: 0px;'>");
                sb.Append("            <div style='font-weight:lighter;display: inline-block;width: 100%;max-width: 90%;margin: 0 auto;padding: 15px 0 50px;'>");
                sb.Append("                <p style='margin: 0;text-align:center;color:#000000;font-weight:bold;font-size: 20px;margin: 15px 0 5px;'>Thank you for subscribing</p>");
                sb.Append($"                <p style='margin: 0;text-align:center;color:#00A87E !important;font-size: 40px;margin-top:10px;'>{packageName}</p>");
                sb.Append("                <p style='color:#333333;font-size: 18px;margin: 15px 0 0px;line-height: 25px;font-weight: normal;text-align: center;'>");
                sb.Append("                    We appreciate your most recent subscription.");
                sb.Append("                </p>");
                sb.Append("                <ul style='display: inline-block;width: 100%;position: relative;margin: 30px 0 0;padding: 0;'>");
                sb.Append("                    <li style='display: inline-block;width: 100%;position: relative;border-top: 1px solid #E0E0E0;border-bottom: 1px solid #E0E0E0;padding-top: 15px;padding-bottom: 20px;margin-left:0 !important;list-style: none;'>");
                sb.Append($"                        <div style='display : Inline-block;float:left;padding-top:5px;position:relative;margin-right:5px;'><img src='{paymentMethodLogo}' alt='img'></div><p style='display: inline-block;float: left;font-size:26px;color:#333333;margin: 3px 0 0;'>");
                sb.Append($"                            ");
                sb.Append($"                            {paymentMthod}");
                sb.Append("                        </p> ");
                sb.Append($"                        <span style='display: inline-block;float: right;font-size:26px;color:#333333;margin: 7px 0 0;'>${price.ToString("F")}</span>");
                sb.Append("                    </li>");
                sb.Append("                    <li style='margin-left:0 !important;display: inline-block;width: 100%;position: relative;padding-top: 20px;padding-bottom: 15px;list-style: none;'>");
                sb.Append("                        <p style='color:#000000;font-size: 26px;font-weight:bold;margin: 0;line-height: 35px;font-weight: normal;text-align: left;'>");
                sb.Append($"                            {"******" + cardMaskedNumber.Substring(6)}");
                sb.Append("                        </p>");
                sb.Append($"                        <p style='color:#000000;font-size: 16px;margin: 0;line-height: 25px;font-weight: normal;text-align: left;'>Expired {cardExpiry}</p>");
                sb.Append("                    </li>");
                sb.Append("                    <!-- <li style='display: inline-block;width: 100%;position: relative;border-top: 1px solid #E0E0E0;border-bottom: 1px solid #E0E0E0;padding-top: 15px;padding-bottom: 20px;list-style: none;'>");
                sb.Append("                        <p style='display: inline-block;float: left;font-size:26px;color:#333333;margin: 0;'>");
                sb.Append("                            <img style='top: 5px;position: relative;' src='card-2.svg' alt='img'>");
                sb.Append("                            PayPal");
                sb.Append("                        </p>");
                sb.Append("                        <span style='display: inline-block;float: right;font-size:26px;color:#333333;margin: 7px 0 0;'>$34.99</span>");
                sb.Append("                    </li>");
                sb.Append("                    <li style='display: inline-block;width: 100%;position: relative;padding-top: 20px;padding-bottom: 15px;list-style: none;'>");
                sb.Append("                        <p style='color:#000000;font-size: 26px;font-weight:bold;margin: 0;line-height: 35px;font-weight: normal;text-align: left;'>");
                sb.Append("                            Johnphillip@paypal.com");
                sb.Append("                        </p>");
                sb.Append("                        <p style='color:#000000;font-size: 16px;margin: 0;line-height: 25px;font-weight: normal;text-align: left;'>Expired 10/27</p>");
                sb.Append("                    </li> -->");
                sb.Append("                </ul>");
                sb.Append($"                <p style='font-size: 16px;color:#000000;text-align: left;margin:0 0 5px;'>{invoiceTime.ToString("MMMM dd, yyyy - h:mm tt")}</p>");
                sb.Append($"                <p style='font-size: 16px;color:#000000;text-align: left;margin: 0;'><b>Invoice Id:</b> {invoiceId}</p>");
                sb.Append("            </div>");
                sb.Append("        </div>");
                sb.Append("    </main>");
                sb.Append("    <ul style='display: inline-block;width: 100%;text-align: center;margin: 30px 0 0;padding: 0;'>");
                sb.Append("        <li style='display: inline-block;margin: 0;'>");
                sb.Append("            <a href='https://easypeasycards.com/terms-conditions' style='display: inline-block;position:relative;border:hidden;text-decoration:none !important ;outline: 0 !important;font-size:16px;color:#333333;margin: 0 10px;'>Terms & Condition</a>");
                sb.Append("        </li>");
                sb.Append("        <li style='display: inline-block;margin: 0;'>");
                sb.Append("            <a href='https://easypeasycards.com/privacy-policy' style='display: inline-block;position:relative;border:hidden;text-decoration:none !important ;outline: 0 !important;font-size:16px;color:#333333;margin: 0 10px;'>Privacy Policy</a>");
                sb.Append("        </li>");
                sb.Append("    </ul>");
                sb.Append("    <ul style='display: inline-block;width: 100%;text-align: center;margin: 30px 0 50px;padding: 0;'>");
                sb.Append("        <li style='display: inline-block;margin: 0 10px;'>");
                sb.Append("            <a target='_blank' href='https://www.facebook.com/EasyPeasyCards' style='display: inline-block;position:relative;border:hidden;text-decoration:none !important ;outline: 0 !important;'>");
                sb.Append("                <img src='https://api.hooley.app/OtherMedia/icon-1.png' alt='facebook' style='-ms-interpolation-mode: bicubic;'>");
                sb.Append("            </a>");
                sb.Append("        </li>");
                sb.Append("        <li style='display: inline-block;margin: 0 10px;'>");
                sb.Append("            <a target='_blank' href='https://www.instagram.com/easypeasycards' style='display: inline-block;position:relative;border:hidden;text-decoration:none !important ;outline: 0 !important;'>");
                sb.Append("                <img src='https://api.hooley.app/OtherMedia/icon-3.png' alt='instagram' style='-ms-interpolation-mode: bicubic;'>");
                sb.Append("            </a>");
                sb.Append("        </li>");
                sb.Append("    </ul> ");
                sb.Append("</body>");
                sb.Append("</html>");

                _ = Task.Run(() => { Email.SendEmail("Thank you for subscription", sb.ToString(), configuration.GetValue<string>("Email"), configuration.GetValue<string>("Password"), toEmail, null, ""); });

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public bool UnSubscribed(string toEmail, string packageName, DateTime? validTill)
        {
            try
            {
                var sb = new StringBuilder();
                sb.Append($"<!doctype html>");
                sb.Append($"<html style='padding: 0;margin: 0;'>");
                sb.Append($"<head>");
                sb.Append($"    <meta charset='utf-8'>");
                sb.Append($"    <meta name='description' content=''>");
                sb.Append($"    <meta name='viewport' content='width=device-width, initial-scale=1'>");
                sb.Append($"    <link rel='shortcut icon' href='https://easypeasycards.com/assets/img/favicon.png'>");
                sb.Append($"    <!-- Title");
                sb.Append($"      ================== -->");
                sb.Append($"    <title>My Package</title>");
                sb.Append($"</head>");
                sb.Append($"<body style='padding: 0;margin: 0;font-family:sans-serif;width: 100%;height: 100%;text-align: center;background: #EFEFEF;text-align: center;background-image: url(bg-grey.jpg);background-size:100% 100%;'>");
                sb.Append($"    <main style='position: relative;text-align:center;display: inline-block;width: 85%;height: auto;margin: 0 auto;max-width: 620px;margin: 100px 0 0px;background: #fff;box-shadow: 0px 0px 76px rgba(0, 0, 0, 0.16);border-radius: 20px;'>");
                sb.Append($"        <header style='width: 100%;min-height: 90px;background-color: #fff;border-radius: 20px 20px 0 0;display: inline-block;padding:70px 0 15px;float: left;'>");
                sb.Append($"            <div style='max-width: 95%;text-align: center;margin: 0 auto;'>");
                sb.Append($"                <a style='display: inline-block;float:none;max-width: 300px;margin: 0 auto;'>");
                sb.Append($"                    <img src='https://d12oonsp3a9sve.cloudfront.net/logo.png' alt='logo' style='text-align: center;margin:0 auto;max-width:100%;'>");
                sb.Append($"                </a>");
                sb.Append($"            </div>");
                sb.Append($"        </header>");
                sb.Append($"        <div style='display: inline-block;width: 100%;text-align: center;background-color: #ffffff;float: left;border-radius: 0 0 20px 20px;padding-bottom: 0px;'>");
                sb.Append($"            <div style='font-weight:lighter;display: inline-block;width: 100%;max-width: 90%;margin: 0 auto;padding: 15px 0 30px;'>");
                sb.Append($"                <p style='margin: 0;text-align:center;color:#000000;font-weight:bold;font-size: 20px;margin: 15px 0 5px;'>Your package</p>");
                sb.Append($"                <p style='margin: 0;text-align:center;color:#00A87E !important;font-size: 40px;margin-top:10px;;'>{packageName}</p>");
                sb.Append($"                <p style='color:#333333;font-size: 18px;margin: 15px 0 0px;line-height: 25px;font-weight: normal;text-align: center;'>");
                sb.Append($"                    has been unsubscribed successfully ");
                sb.Append($"                </p>");
                sb.Append($"                <div style='font-weight:lighter;display: inline-block;width: 100%;max-width: 90%;margin: 25px 0 5px;border-top: 1px solid #E0E0E0;padding-top: 18px;'>");
                sb.Append($"                    <p style='margin: 0;text-align:center;color:#000000;font-size: 16px;margin: 15px 0 5px;'><b>Valid Till:</b> {validTill?.ToString("MMMM dd, yyyy")}</p>");
                sb.Append($"                </div>");
                sb.Append($"            </div>");
                sb.Append($"        </div>");
                sb.Append($"    </main>");
                sb.Append($"    <ul style='display: inline-block;width: 100%;text-align: center;margin: 30px 0 0;padding: 0;'>");
                sb.Append($"        <li style='display: inline-block;margin: 0;'>");
                sb.Append($"            <a href='https://easypeasycards.com/terms-conditions' style='display: inline-block;position:relative;border:hidden;text-decoration:none !important ;outline: 0 !important;font-size:16px;color:#333333;margin: 0 10px;'>Terms & Condition</a>");
                sb.Append($"        </li>");
                sb.Append($"        <li style='display: inline-block;margin: 0;'>");
                sb.Append($"            <a href='https://easypeasycards.com/privacy-policy' style='display: inline-block;position:relative;border:hidden;text-decoration:none !important ;outline: 0 !important;font-size:16px;color:#333333;margin: 0 10px;'>Privacy Policy</a>");
                sb.Append($"        </li>");
                sb.Append($"    </ul>");
                sb.Append($"    <ul style='display: inline-block;width: 100%;text-align: center;margin: 30px 0 50px;padding: 0;'>");
                sb.Append($"        <li style='display: inline-block;margin: 0 10px;'>");
                sb.Append($"            <a target='_blank' href='https://www.facebook.com/EasyPeasyCards' style='display: inline-block;position:relative;border:hidden;text-decoration:none !important ;outline: 0 !important;'>");
                sb.Append($"                <img src='https://api.hooley.app/OtherMedia/icon-1.png' alt='facebook' style='-ms-interpolation-mode: bicubic;'>");
                sb.Append($"            </a>"); 
                sb.Append($"        </li>");
                sb.Append($"        <li style='display: inline-block;margin: 0 10px;'>");
                sb.Append($"            <a target='_blank' href='https://www.instagram.com/easypeasycards' style='display: inline-block;position:relative;border:hidden;text-decoration:none !important ;outline: 0 !important;'>");
                sb.Append($"                <img src='https://api.hooley.app/OtherMedia/icon-3.png' alt='instagram' style='-ms-interpolation-mode: bicubic;'>");
                sb.Append($"            </a>");
                sb.Append($"        </li>");
                sb.Append($"    </ul> ");
                sb.Append($"</body>");
                sb.Append($"</html>");

                _ = Task.Run(() => { Email.SendEmail("Subscription was Cancelled", sb.ToString(), configuration.GetValue<string>("Email"), configuration.GetValue<string>("Password"), toEmail, null, ""); });

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public bool CongratulationsCardReceived(string toEmail, decimal price, string fromName, string brandName, string brandLogo, string greetingId, string secondaryColor)
        {
            try
            {

                var sb = new StringBuilder();
              
                sb.Append($"</head>");
                sb.Append($"<body style='padding: 0;margin: 0;font-family:sans-serif;width: 100%;height: 100%;text-align: center;background: #EFEFEF;text-align: center;background-image: url(bg-grey.jpg);background-size:100% 100%;'>");
                sb.Append($"    <main style='position: relative;text-align:center;display: inline-block;width: 85%;height: auto;margin: 0 auto;max-width: 620px;margin: 100px 0 0px;background: #fff;box-shadow: 0px 0px 76px rgba(0, 0, 0, 0.16);border-radius: 20px;'>");
                sb.Append($"        <header style='width: 100%;min-height: 90px;background-color: #fff;border-radius: 20px 20px 0 0;display: inline-block;padding:70px 0 15px;float: left;'>");
                sb.Append($"            <div style='max-width: 95%;text-align: center;margin: 0 auto;'>");
                sb.Append($"                <a style='display: inline-block;float:none;max-width: 300px;margin: 0 auto;'>");
                sb.Append($"                    <img src='https://d12oonsp3a9sve.cloudfront.net/logo.png' alt='logo' style='text-align: center;margin:0 auto;max-width:100%;'>");
                sb.Append($"                </a>");
                sb.Append($"            </div>");
                sb.Append($"        </header>");
                sb.Append($"        <div style='display: inline-block;width: 100%;text-align: center;background-color: #ffffff;float: left;border-radius: 0 0 20px 20px;padding-bottom: 30px;'>");
                sb.Append($"            <div style='font-weight:lighter;display: inline-block;width: 100%;max-width: 90%;margin: 0 auto;padding: 15px 0;'>");
                sb.Append($"                <p style='margin: 0;color:#333333;font-size: 26px;margin: 15px 0 0px;line-height: 25px;font-weight: bold;text-align: center;'>Congratulations</p>");
                sb.Append($"                <p style='color:#333333;font-size: 16px;margin: 10px 0 15px;line-height: 25px;font-weight: normal;text-align: center;'>");
                sb.Append($"                    You have received an eGift card of {brandName} worth ${price} from");
                sb.Append($"                </p>");
                sb.Append($"                <p style='margin: 0;text-align:center;color:#00A87E !important;font-size: 40px;margin-top:10px;;'>{fromName}</p>");
                sb.Append($"                <div style='box-shadow:inset 58px 68px 140px rgba(0,0,0,0.2) !important;text-shadow:3px 3px 3px #ff0 !important;font-size:30px !important;" +
                          $"color:#C3C3C3 !important;width: auto;padding:20px 20px 10px;margin-top:25px;margin-bottom:15px;display: inline-block;position: relative;" +
                          $"border-radius: 20px;background-color:{secondaryColor} !important;border:1px solid #E0E0E0;'>");

                //sb.Append($"                <p style='margin: 0;color:#333333;font-size: 26px;margin: 15px 0 0px;line-height: 25px;font-weight: bold;text-align: center;'>{brandName}</p>");


                sb.Append($"                    <img style='text-shadow:3px 3px 3px rgba(0,0,0,0.2) !important;font-size:30px !important;color:#c3c3c3 !important;' src='{brandLogo}' alt='{brandName}'>");
                sb.Append($"                </div>");
                sb.Append($"                <p style='color:#333333;font-size: 16px;margin: 15px 0 20px;line-height: 25px;font-weight: normal;text-align: center;'>");
                sb.Append($"                    Enjoy your most recent eGift card.");
                sb.Append($"                </p>");
                sb.Append($"                <a href='https://easypeasycards.com/go?id={greetingId}' style='text-align:center;width: 200px;display: inline-block;text-decoration:none; padding:15px 0;color:white !important;" +
                          $"                            border-radius:30px !important; font-weight:bold;position: relative;background-color:#00D9A3 !important;background-color:#00D9A3 !important;");
                sb.Append($"                            box-shadow: 0px 10px 20px rgba(0, 0, 0, 0.16);border-bottom:7px solid #00A87E !important;text-shadow:0 2px 0 rgba(0,0,0,0.26);'>");
                sb.Append($"                    View Card");
                sb.Append($"                </a>");
                sb.Append($"            </div>");
                sb.Append($"        </div>");
                sb.Append($"    </main>");
                sb.Append($"    <ul style='display: inline-block;width: 100%;text-align: center;margin: 30px 0 0;padding: 0;'>");
                sb.Append($"        <li style='display: inline-block;margin: 0;'>");
                sb.Append($"            <a href='https://easypeasycards.com/terms-conditions' style='display: inline-block;position:relative;border:hidden;text-decoration:none !important ;outline: 0 !important;font-size:16px;color:#333333;margin: 0 10px;'>Terms & Condition</a>");
                sb.Append($"        </li>");
                sb.Append($"        <li style='display: inline-block;margin: 0;'>");
                sb.Append($"            <a href='https://easypeasycards.com/privacy-policy' style='display: inline-block;position:relative;border:hidden;text-decoration:none !important ;outline: 0 !important;font-size:16px;color:#333333;margin: 0 10px;'>Privacy Policy</a>");
                sb.Append($"        </li>");
                sb.Append($"    </ul>");
                sb.Append($"     <ul style='display: inline-block;width: 100%;text-align: center;margin: 30px 0 50px;padding: 0;'>");
                sb.Append($"        <li style='display: inline-block;margin: 0 10px;'>");
                sb.Append($"            <a target='_blank' href='https://www.facebook.com/EasyPeasyCards' style='display: inline-block;position:relative;border:hidden;text-decoration:none !important ;outline: 0 !important;'>");
                sb.Append($"                <img src='https://api.hooley.app/OtherMedia/icon-1.png' alt='facebook' style='-ms-interpolation-mode: bicubic;'>");
                sb.Append($"            </a>");
                sb.Append($"        </li>");
                sb.Append($"        <li style='display: inline-block;margin: 0 10px;'>");
                sb.Append($"            <a target='_blank' href='https://www.instagram.com/easypeasycards' style='display: inline-block;position:relative;border:hidden;text-decoration:none !important ;outline: 0 !important;'>");
                sb.Append($"                <img src='https://api.hooley.app/OtherMedia/icon-3.png' alt='instagram' style='-ms-interpolation-mode: bicubic;'>");
                sb.Append($"            </a>");
                sb.Append($"        </li>");
                sb.Append($"    </ul> ");
                sb.Append($"</body>");
                sb.Append($"</html>");

                _ = Task.Run(() => { Email.SendEmail("Congratulations You have received an eGift card", sb.ToString(), configuration.GetValue<string>("Email"), configuration.GetValue<string>("Password"), toEmail, null, ""); });

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

    }
}
