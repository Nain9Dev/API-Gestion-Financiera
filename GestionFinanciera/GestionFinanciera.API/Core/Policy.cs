using System;

namespace GestionFinanciera.API.Core
{
    public class Policy
    {
        public Guid Id { get; private set; }
        public string PolicyNumber { get; private set; }
        public decimal InsuredAmount { get; private set; }
        public bool IsActive { get; private set; }
        public DateTime IssueDate { get; private set; }

        public Policy(string policyNumber, decimal insuredAmount)
        {
            if (string.IsNullOrWhiteSpace(policyNumber))
                throw new ArgumentException("Policy number cannot be empty.");

            if (insuredAmount <= 0)
                throw new ArgumentException("Insured amount must be greater than zero.");

            Id = Guid.NewGuid();
            PolicyNumber = policyNumber;
            InsuredAmount = insuredAmount;
            IsActive = true;
            IssueDate = DateTime.Now;
        }

        public void CancelPolicy()
        {
            IsActive = false;
        }
    }
}