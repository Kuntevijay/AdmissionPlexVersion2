namespace AdmissionPlex.Core.Interfaces.Services;

public interface IReportGenerationService
{
    Task<byte[]> GeneratePsychometricReportPdfAsync(long attemptId);
    Task<string> SaveReportAsync(long attemptId, byte[] pdfBytes);
}
