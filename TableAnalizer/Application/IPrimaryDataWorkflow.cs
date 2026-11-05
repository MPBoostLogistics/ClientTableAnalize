namespace TableAnalizer.Application
{
    public interface IPrimaryDataWorkflow
    {
        Task<int> RunAsync(CancellationToken cancellationToken = default);
    }
}