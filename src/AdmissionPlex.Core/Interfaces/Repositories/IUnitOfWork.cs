namespace AdmissionPlex.Core.Interfaces.Repositories;

public interface IUnitOfWork : IDisposable
{
    IStudentRepository Students { get; }
    IQuestionRepository Questions { get; }
    ITestRepository Tests { get; }
    ITestAttemptRepository TestAttempts { get; }
    ICareerRepository Careers { get; }
    ICutoffRepository Cutoffs { get; }
    IChatRepository Chat { get; }
    ICounsellorRepository Counsellors { get; }
    IReferralRepository Referrals { get; }
    IPaymentRepository Payments { get; }
    IPageRepository Pages { get; }
    INotificationRepository Notifications { get; }
    Task<int> SaveChangesAsync();
}
