namespace InvoiceManagement.Domain.Platform;

public enum IdempotencyState : byte
{
    Processing = 1,
    Completed = 2,
    Failed = 3,
}
