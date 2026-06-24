namespace ProjectBrain.AI;

using ProjectBrain.Domain;

public class UserSearchIndexErasureService : IUserSearchIndexErasureService
{
    private readonly ISearchIndexService _searchIndexService;

    public UserSearchIndexErasureService(ISearchIndexService searchIndexService)
    {
        _searchIndexService = searchIndexService;
    }

    public Task<int> DeleteAllDocumentsForUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        return _searchIndexService.DeleteAllDocumentsForUserAsync(userId);
    }
}
