namespace CNPJ.Processing.Infra.Kafka.Exceptions
{
    public class KafkaHelperException : Exception
    {

        public KafkaHelperException()
        {

        }
        public KafkaHelperException(string message, Exception inner)
            : base(message, inner)
        {

        }
    }
}