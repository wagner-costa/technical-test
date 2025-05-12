namespace CNPJ.Processing.Infra.Kafka.Models
{
    public class DataTopic<T>
    {
        public Guid ID { get; set; }
        public string CreateDate { get; set; }
        public string Topic { get; set; } = string.Empty;
        public T Message { get; set; } = default!;

        public DataTopic()
        {
            ID = Guid.NewGuid();
            CreateDate = DateTime.Now.ToString();
        }
    }
}