using Azure.Messaging.ServiceBus;
using InsuranceFunctionApp.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System;
using System.Data;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace InsuranceFunctionApp
{
    public static class ProcessInsuranceMessage
    {
        private const string QueueName = "queue1";        



        [FunctionName("ProcessInsuranceMessage")]
        public static async Task Run(
            [ServiceBusTrigger(QueueName, Connection = "ServiceBusConnectionString", IsSessionsEnabled = false)] ServiceBusReceivedMessage message,
            ServiceBusMessageActions messageActions,
            ILogger log)
        {
            try
            {
                if (message.Body == null)
                {
                    log.LogError("Message body is empty.");
                    return;
                }

                string messageBody = message.Body.ToString();
                log.LogInformation($"Received message body: {messageBody}");

                if (!IsValidJson(messageBody))
                {
                    log.LogError("Invalid JSON format.");
                    return;
                }

                var jsonMessage = JsonSerializer.Deserialize<OutputMessage>(messageBody);
                string authorName = Environment.GetEnvironmentVariable("authorName");
                if (jsonMessage?.authorName == authorName)
                {
                    var xmlMessage = ConvertJsonToXml(jsonMessage);
                    try
                    {
                        SaveToDatabase(xmlMessage);
                        await messageActions.CompleteMessageAsync(message);
                        log.LogInformation("Message processed.");
                    }
                    catch (Exception dbEx)
                    {
                        await messageActions.DeferMessageAsync(message);
                        log.LogError($"Error saving to database: {dbEx.Message}");
                        await File.WriteAllTextAsync("error.log", dbEx.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                log.LogError($"Error processing message: {ex.Message}");
                await File.WriteAllTextAsync("error.log", ex.ToString());
            }
        }



        #region private


        /// <summary>
        /// IsValidJson
        /// </summary>
        /// <param name="strInput"></param>
        /// <returns></returns>
        private static bool IsValidJson(string strInput)
        {
            strInput = strInput.Trim();
            try
            {
                var obj = JsonDocument.Parse(strInput);
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }


        /// <summary>
        /// ConvertJsonToXml
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private static string ConvertJsonToXml(OutputMessage message)
        {
            var xml = new XElement("InsuranceMessage",
                new XElement("Id", message.id),
                new XElement("Name", message.name),
                new XElement("Surname", message.surname),
                new XElement("ProcessDate", message.processDate),
                new XElement("AuthorName", message.authorName),
                new XElement("InsurancePayment",
                    new XElement("PaymentId", message.insurancePayment.paymentId),
                    new XElement("PaymentDatetime", message.insurancePayment.paymentDatetime),
                    new XElement("Franchise", message.insurancePayment.franchise),
                    new XElement("Currency", message.insurancePayment.currency),
                    new XElement("Amount", message.insurancePayment.amount)
                )
            );

            return xml.ToString();
        }


        /// <summary>
        /// SaveToDatabase
        /// </summary>
        /// <param name="xmlMessage"></param>
        private static void SaveToDatabase(string xmlMessage)
        {
            using (var connection = new SqlConnection(Environment.GetEnvironmentVariable("SqlConnectionString")))
            {
                connection.Open();
                using (var command = new SqlCommand("INSERT INTO InsuranceMessages (Message) VALUES (@xmlMessage)", connection))
                {
                    command.Parameters.Add(new SqlParameter("@xmlMessage", SqlDbType.NVarChar) { Value = xmlMessage });
                    command.ExecuteNonQuery();
                }
            }
        }

        #endregion


    }
}
