namespace ProjectBrain.Domain;

public record AdminDashboardAggregateResponse(
    int TotalUsers,
    int TotalCoaches,
    int NormalUsers,
    int LoggedInUsers,
    int TotalAiQueriesDaily,
    int TotalAiQueriesMonthly,
    long TotalFileStorageBytes,
    double TotalFileStorageMegabytes
);
