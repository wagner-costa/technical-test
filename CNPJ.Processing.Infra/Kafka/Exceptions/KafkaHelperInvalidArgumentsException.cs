namespace CNPJ.Processing.Infra.Kafka.Exceptions
{
    public class KafkaHelperInvalidArgumentsException : Exception
    {
        public KafkaHelperInvalidArgumentsException()
        {

        }
        public KafkaHelperInvalidArgumentsException(string message, Exception inner)
            : base(message, inner)
        {

        }
    }
}
