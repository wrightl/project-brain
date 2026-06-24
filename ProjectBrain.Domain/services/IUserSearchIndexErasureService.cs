namespace ProjectBrain.Domain;

public interface IUserSearchIndexErasureService
{
    Task<int> DeleteAllDocumentsForUserAsync(string userId, CancellationToken cancellationToken = default);
}
