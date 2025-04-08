using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace EmailServiceFunction
{
    public class SendGrid
    {
        private readonly string _sendGridApiKey;
        private readonly string _sendGridFromEmail;
        private readonly ILogger<SendGrid> _logger;

        public SendGrid(IConfiguration configuration, ILogger<SendGrid> logger)
        {
            _sendGridApiKey = configuration["Values:SendGridApiKey"] ?? throw new ArgumentNullException("SendGrid API Key is missing.");
            _sendGridFromEmail = configuration["Values:SendGridFromEmail"] ?? throw new ArgumentNullException("From Email is missing.");
            _logger = logger;
        }

        [Function("SendGridEmail")]
        public async Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
        {
            // Read the request body as a string
            string requestBody;
            using (StreamReader reader = new StreamReader(req.Body))
            {
                requestBody = await reader.ReadToEndAsync();
            }

            // Deserialize the request body into an EmailRequest object
            EmailRequest? emailRequest =
                JsonConvert.DeserializeObject<EmailRequest>(requestBody);

            if (emailRequest == null)
            {
                return new BadRequestObjectResult("Invalid email request payload.");
            }

            try
            {
                var result = await SendEmailAsync(emailRequest);
                if (result)
                {
                    return new OkObjectResult("Email sent successfully.");
                }

                return new BadRequestObjectResult("Failed to send email.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return new BadRequestObjectResult("Failed to send email.");
            }
        }

        public async Task<bool> SendEmailAsync(EmailRequest emailRequest)
        {
            try
            {
                var client = new SendGridClient(_sendGridApiKey);
                var from = new EmailAddress(_sendGridFromEmail, "App Support");
                var to = new EmailAddress(emailRequest.To);
                var msg = MailHelper.CreateSingleEmail(from, to, emailRequest.Subject, emailRequest.Body, emailRequest.IsHtml ? emailRequest.Body : null);
                var response = await client.SendEmailAsync(msg);

                if (!response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Body.ReadAsStringAsync();
                    _logger.LogError($"SendGrid API Error: {response.StatusCode} - {responseBody}");
                    throw new ApplicationException($"Failed to send email. SendGrid returned {response.StatusCode}.");
                }

                return response.StatusCode is System.Net.HttpStatusCode.OK or System.Net.HttpStatusCode.Accepted;
            }
            catch (ArgumentException ex)
            {
                //_logger.LogError(ex, "Invalid email parameters.");
                throw new ApplicationException("Invalid email parameters provided.", ex);
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "Unexpected error while sending email.");
                throw new ApplicationException("An unexpected error occurred while sending the email.", ex);
            }
        }

    }
}
