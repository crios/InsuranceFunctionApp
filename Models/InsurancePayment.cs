using System;

namespace InsuranceFunctionApp.Models
{
    public class InsurancePayment
    {
        public int paymentId { get; set; }
        public DateTime paymentDatetime { get; set; }
        public string franchise { get; set; }
        public string currency { get; set; }
        public int amount { get; set; }
    }
}
