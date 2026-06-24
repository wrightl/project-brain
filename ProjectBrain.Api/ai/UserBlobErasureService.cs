namespace ProjectBrain.AI;

using ProjectBrain.Domain;

public class UserBlobErasureService : IUserBlobErasureService
{
    private readonly Storage _storage;

    public UserBlobErasureService(Storage storage)
    {
        _storage = storage;
    }

    public Task<int> DeleteAllUserFilesAsync(string userId, CancellationToken cancellationToken = default)
    {
        return _storage.DeleteAllUserFiles(userId);
    }
}
