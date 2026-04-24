using AdmissionPlex.Core.Interfaces.Repositories;
using AdmissionPlex.Api.Data;

namespace AdmissionPlex.Api.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    private IStudentRepository? _students;
    private IQuestionRepository? _questions;
    private ITestRepository? _tests;
    private ITestAttemptRepository? _testAttempts;
    private ICareerRepository? _careers;
    private ICutoffRepository? _cutoffs;
    private IChatRepository? _chat;
    private ICounsellorRepository? _counsellors;
    private IReferralRepository? _referrals;
    private IPaymentRepository? _payments;
    private IPageRepository? _pages;
    private INotificationRepository? _notifications;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    public IStudentRepository Students
        => _students ??= new StudentRepository(_context);

    public IQuestionRepository Questions
        => _questions ??= new QuestionRepository(_context);

    public ITestRepository Tests
        => _tests ??= new TestRepository(_context);

    public ITestAttemptRepository TestAttempts
        => _testAttempts ??= new TestAttemptRepository(_context);

    public ICareerRepository Careers
        => _careers ??= new CareerRepository(_context);

    public ICutoffRepository Cutoffs
        => _cutoffs ??= new CutoffRepository(_context);

    public IChatRepository Chat
        => _chat ??= new ChatRepository(_context);

    public ICounsellorRepository Counsellors
        => _counsellors ??= new CounsellorRepository(_context);

    public IReferralRepository Referrals
        => _referrals ??= new ReferralRepository(_context);

    public IPaymentRepository Payments
        => _payments ??= new PaymentRepository(_context);

    public IPageRepository Pages
        => _pages ??= new PageRepository(_context);

    public INotificationRepository Notifications
        => _notifications ??= new NotificationRepository(_context);

    public async Task<int> SaveChangesAsync()
        => await _context.SaveChangesAsync();

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
