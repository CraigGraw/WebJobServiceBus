namespace APIService
{
    public class QueueTestMessage
    {
        public QueueTestMessage(string body, string sessionId)
        {
            Body = body;
            SessionId = sessionId;
        }
        
        public string Body { get; }

        public string SessionId { get; }
    }
}
