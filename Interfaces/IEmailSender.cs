namespace JustAskIndia.Interfaces
{

        public interface IEmailSender
        {
            Task<bool> SendToSingle(string toAddress, string subject, string content);
            Task<bool> SendToMultiple(List<string> toAddresses, string? ccAddress, string subject, string content);
        }
}

