namespace CNPJ.Processing.Infra.Models
{
    public class ErrorResponse
    {
        public int Status { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> Errors { get; set; } = new();
    }
}
