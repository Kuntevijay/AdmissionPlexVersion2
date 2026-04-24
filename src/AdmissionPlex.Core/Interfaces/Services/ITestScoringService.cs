using AdmissionPlex.Core.Entities.Tests;

namespace AdmissionPlex.Core.Interfaces.Services;

public interface ITestScoringService
{
    Task<TestAttempt> ScoreStreamSelectorAsync(long attemptId);
    Task<TestAttempt> ScorePsychometricTestAsync(long attemptId);
    Task<IEnumerable<InterestScore>> ComputeInterestScoresAsync(long attemptId);
    Task<IEnumerable<AptitudeScore>> ComputeAptitudeScoresAsync(long attemptId);
    Task<(int IqScore, string IqCategory)> ComputeIqBenchmarkAsync(IEnumerable<AptitudeScore> aptitudeScores);
    Task<IEnumerable<CareerSuitabilityScore>> ComputeCareerSuitabilityAsync(long attemptId, IEnumerable<InterestScore> interestScores, IEnumerable<AptitudeScore> aptitudeScores);
}
