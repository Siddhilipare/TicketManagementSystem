using System;
using TicketDAL.Dal;

namespace Ticket_Management_System.Helpers
{
    /// <summary>
    /// Branded, HTML formatted emails for the account flows (OTP verification,
    /// password reset). Uses TicketDAL.Dal.EmailHelper as the SMTP transport.
    /// </summary>
    public static class AccountEmailService
    {
        public static void SendOtp(string toEmail, string otp, string purpose)
        {
            string heading;
            string intro;

            if (string.Equals(purpose, "PasswordReset", StringComparison.OrdinalIgnoreCase))
            {
                heading = "Reset your password";
                intro = "We received a request to reset your password. Enter the code below to continue.";
            }
            else
            {
                heading = "Verify your email address";
                intro = "Thank you for registering with Simplify. Enter the code below to verify your account.";
            }

            string body = @"
<!DOCTYPE html>
<html>
<body style='font-family: Arial, sans-serif; background: #f4f4f4; margin: 0; padding: 20px;'>
  <div style='max-width: 480px; margin: 0 auto; background: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 8px rgba(0,0,0,0.1);'>
    <div style='background: #1a73e8; padding: 28px 32px;'>
      <div style='font-size: 22px; font-weight: 700; color: #ffffff;'>Simplify</div>
      <div style='font-size: 13px; color: rgba(255,255,255,0.8); margin-top: 4px;'>IT Ticket Management</div>
    </div>
    <div style='padding: 32px; text-align: center;'>
      <h2 style='font-size: 20px; color: #1a1a1a; margin: 0 0 12px 0;'>" + heading + @"</h2>
      <p style='font-size: 14px; color: #555; line-height: 1.6; margin: 0 0 24px 0;'>" + intro + @"</p>
      <div style='display: inline-block; background: #f1f5fe; border: 1px solid #d8e2fb; color: #1a73e8;
                  font-size: 30px; font-weight: 700; letter-spacing: 8px; padding: 14px 28px; border-radius: 8px;'>
        " + otp + @"
      </div>
      <p style='font-size: 12px; color: #999; margin-top: 24px; line-height: 1.5;'>
        This code expires in 5 minutes. Do not share it with anyone.<br/>
        If you did not request this, you can safely ignore this email.
      </p>
      <hr style='border: none; border-top: 1px solid #eee; margin: 24px 0 16px;'/>
      <div style='font-size: 11px; color: #bbb;'>Simplify &mdash; IT Support System</div>
    </div>
  </div>
</body>
</html>";

            EmailHelper.Send(toEmail, "Your verification code - Simplify IT Support", body);
        }
    }
}
