namespace UNIR.TFE.Polyrepo.Multiplication.Module.Application
{
    public class MultiplicationAppService : IMultiplicationAppService
    {
        public string Key => "mul";

        public decimal Execute(decimal a, decimal b) => a * b;
    }
}
