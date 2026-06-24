namespace ProjectBrain.Domain;

public interface IUserBlobErasureService
{
    Task<int> DeleteAllUserFilesAsync(string userId, CancellationToken cancellationToken = default);
}
