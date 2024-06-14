using System;

namespace InsuranceFunctionApp.Models
{
    internal class OutputMessage
    {
        public int id { get; set; }
        public string name { get; set; }
        public string surname { get; set; }
        public DateTime processDate { get; set; }
        public string authorName { get; set; }
        public InsurancePayment insurancePayment { get; set; }
    }
}
