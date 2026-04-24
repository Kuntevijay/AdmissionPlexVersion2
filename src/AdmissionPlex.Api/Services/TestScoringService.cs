using Microsoft.EntityFrameworkCore;
using AdmissionPlex.Core.Entities.Tests;
using AdmissionPlex.Core.Entities.Careers;
using AdmissionPlex.Core.Enums;
using AdmissionPlex.Core.Interfaces.Repositories;
using AdmissionPlex.Core.Interfaces.Services;
using AdmissionPlex.Api.Data;
using AdmissionPlex.Shared.Constants;

namespace AdmissionPlex.Api.Services;

public class TestScoringService : ITestScoringService
{
    private readonly IUnitOfWork _uow;
    private readonly AppDbContext _context;
    private readonly ILogger<TestScoringService> _logger;

    public TestScoringService(IUnitOfWork uow, AppDbContext context, ILogger<TestScoringService> logger)
    {
        _uow = uow;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Score a stream selector test — tallies Science/Commerce/Arts affinity
    /// </summary>
    public async Task<TestAttempt> ScoreStreamSelectorAsync(long attemptId)
    {
        var attempt = await _uow.TestAttempts.GetWithResponsesAsync(attemptId);
        if (attempt == null) throw new KeyNotFoundException("Attempt not found.");

        var scienceScore = 0m;
        var commerceScore = 0m;
        var artsScore = 0m;

        foreach (var response in attempt.Responses)
        {
            if (response.SelectedOption == null) continue;
            switch (response.SelectedOption.StreamTag)
            {
                case StreamType.Science: scienceScore += response.SelectedOption.ScoreValue; break;
                case StreamType.Commerce: commerceScore += response.SelectedOption.ScoreValue; break;
                case StreamType.Arts: artsScore += response.SelectedOption.ScoreValue; break;
            }
        }

        var max = Math.Max(scienceScore, Math.Max(commerceScore, artsScore));
        attempt.RecommendedStream = max == scienceScore ? StreamType.Science
            : max == commerceScore ? StreamType.Commerce
            : StreamType.Arts;

        attempt.Status = AttemptStatus.Completed;
        attempt.CompletedAt = DateTime.UtcNow;
        _uow.TestAttempts.Update(attempt);
        await _uow.SaveChangesAsync();

        _logger.LogInformation("Stream selector scored for attempt {AttemptId}: {Stream}", attemptId, attempt.RecommendedStream);
        return attempt;
    }

    /// <summary>
    /// Score a full psychometric test — interest, aptitude, IQ, career suitability
    /// </summary>
    public async Task<TestAttempt> ScorePsychometricTestAsync(long attemptId)
    {
        var attempt = await _uow.TestAttempts.GetWithResponsesAsync(attemptId);
        if (attempt == null) throw new KeyNotFoundException("Attempt not found.");

        // Step 1: Compute interest scores
        var interestScores = await ComputeInterestScoresAsync(attemptId);

        // Step 2: Compute aptitude scores
        var aptitudeScores = await ComputeAptitudeScoresAsync(attemptId);

        // Step 3: Compute IQ benchmark
        var (iqScore, iqCategory) = await ComputeIqBenchmarkAsync(aptitudeScores);

        // Step 4: Compute career suitability
        var suitabilityScores = await ComputeCareerSuitabilityAsync(
            attemptId, interestScores, aptitudeScores);

        // Step 5: Update attempt
        attempt.OverallIqScore = iqScore;
        attempt.IqCategory = Enum.Parse<IqCategory>(iqCategory);
        attempt.Status = AttemptStatus.Completed;
        attempt.CompletedAt = DateTime.UtcNow;
        _uow.TestAttempts.Update(attempt);
        await _uow.SaveChangesAsync();

        _logger.LogInformation(
            "Psychometric test scored for attempt {AttemptId}: IQ={IQ}, Category={Cat}",
            attemptId, iqScore, iqCategory);

        // Reload with full results
        return (await _uow.TestAttempts.GetWithFullResultsAsync(attemptId))!;
    }

    public async Task<IEnumerable<InterestScore>> ComputeInterestScoresAsync(long attemptId)
    {
        var attempt = await _uow.TestAttempts.GetWithResponsesAsync(attemptId);
        if (attempt == null) throw new KeyNotFoundException("Attempt not found.");

        var interestCategories = await _uow.Questions.GetAllInterestCategoriesAsync();
        var scores = new List<InterestScore>();

        foreach (var category in interestCategories)
        {
            // Get all responses for questions in this interest category
            var categoryResponses = attempt.Responses
                .Where(r => r.Question.SectionType == SectionType.Interest
                         && r.Question.InterestCategoryId == category.Id)
                .ToList();

            var rawScore = categoryResponses.Sum(r => r.ScoreObtained);
            var maxPossible = categoryResponses.Sum(r => r.Question.MaxScore);
            var percentile = maxPossible > 0
                ? Math.Round((rawScore / maxPossible) * 100, 2)
                : 0;

            scores.Add(new InterestScore
            {
                AttemptId = attemptId,
                InterestCategoryId = category.Id,
                RawScore = rawScore,
                MaxPossibleScore = maxPossible,
                PercentileScore = percentile
            });
        }

        // Assign ranks (1 = highest)
        var ranked = scores.OrderByDescending(s => s.PercentileScore).ToList();
        for (int i = 0; i < ranked.Count; i++)
            ranked[i].RankOrder = i + 1;

        // Remove existing scores for this attempt and save new
        var existing = await _context.InterestScores
            .Where(s => s.AttemptId == attemptId).ToListAsync();
        _context.InterestScores.RemoveRange(existing);
        await _context.InterestScores.AddRangeAsync(scores);
        await _context.SaveChangesAsync();

        return scores;
    }

    public async Task<IEnumerable<AptitudeScore>> ComputeAptitudeScoresAsync(long attemptId)
    {
        var attempt = await _uow.TestAttempts.GetWithResponsesAsync(attemptId);
        if (attempt == null) throw new KeyNotFoundException("Attempt not found.");

        var aptitudeCategories = await _uow.Questions.GetAllAptitudeCategoriesAsync();
        var scores = new List<AptitudeScore>();

        foreach (var category in aptitudeCategories)
        {
            var categoryResponses = attempt.Responses
                .Where(r => r.Question.SectionType == SectionType.Aptitude
                         && r.Question.AptitudeCategoryId == category.Id)
                .ToList();

            var rawScore = categoryResponses.Sum(r => r.ScoreObtained);
            var maxPossible = categoryResponses.Sum(r => r.Question.MaxScore);
            var percentile = maxPossible > 0
                ? Math.Round((rawScore / maxPossible) * 100, 2)
                : 0;

            scores.Add(new AptitudeScore
            {
                AttemptId = attemptId,
                AptitudeCategoryId = category.Id,
                RawScore = rawScore,
                MaxPossibleScore = maxPossible,
                PercentileScore = percentile
            });
        }

        var ranked = scores.OrderByDescending(s => s.PercentileScore).ToList();
        for (int i = 0; i < ranked.Count; i++)
            ranked[i].RankOrder = i + 1;

        var existing = await _context.AptitudeScores
            .Where(s => s.AttemptId == attemptId).ToListAsync();
        _context.AptitudeScores.RemoveRange(existing);
        await _context.AptitudeScores.AddRangeAsync(scores);
        await _context.SaveChangesAsync();

        return scores;
    }

    public Task<(int IqScore, string IqCategory)> ComputeIqBenchmarkAsync(IEnumerable<AptitudeScore> aptitudeScores)
    {
        var scoresList = aptitudeScores.ToList();
        if (!scoresList.Any())
            return Task.FromResult((0, nameof(Core.Enums.IqCategory.BelowAverage)));

        // Weighted average of all aptitude percentiles → map to IQ scale
        // Simple mapping: average percentile * 1.4 + 30 (gives range ~30-170)
        var avgPercentile = scoresList.Average(s => (double)s.PercentileScore);
        var iqScore = (int)Math.Round(avgPercentile * 1.4 + 30);

        // Clamp to reasonable range
        iqScore = Math.Clamp(iqScore, 50, 170);

        var category = iqScore switch
        {
            < AppConstants.DefaultIqBelowAverage => nameof(Core.Enums.IqCategory.BelowAverage),
            < AppConstants.DefaultIqAverage => nameof(Core.Enums.IqCategory.Average),
            < AppConstants.DefaultIqAboveAverage => nameof(Core.Enums.IqCategory.AboveAverage),
            _ => nameof(Core.Enums.IqCategory.Superior)
        };

        return Task.FromResult((iqScore, category));
    }

    public async Task<IEnumerable<CareerSuitabilityScore>> ComputeCareerSuitabilityAsync(
        long attemptId,
        IEnumerable<InterestScore> interestScores,
        IEnumerable<AptitudeScore> aptitudeScores)
    {
        var careers = await _uow.Careers.GetAllWithWeightsAsync();
        var interestList = interestScores.ToList();
        var aptitudeList = aptitudeScores.ToList();
        var suitabilityScores = new List<CareerSuitabilityScore>();

        foreach (var career in careers)
        {
            var totalWeight = 0m;
            var weightedScore = 0m;
            var meetsMinimums = true;

            // Interest weights
            foreach (var weight in career.InterestWeights)
            {
                var studentScore = interestList
                    .FirstOrDefault(s => s.InterestCategoryId == weight.InterestCategoryId);
                if (studentScore == null) continue;

                totalWeight += weight.Weight;
                weightedScore += weight.Weight * studentScore.PercentileScore;

                if (weight.MinPercentile > 0 && studentScore.PercentileScore < weight.MinPercentile)
                    meetsMinimums = false;
            }

            // Aptitude weights
            foreach (var weight in career.AptitudeWeights)
            {
                var studentScore = aptitudeList
                    .FirstOrDefault(s => s.AptitudeCategoryId == weight.AptitudeCategoryId);
                if (studentScore == null) continue;

                totalWeight += weight.Weight;
                weightedScore += weight.Weight * studentScore.PercentileScore;

                if (weight.MinPercentile > 0 && studentScore.PercentileScore < weight.MinPercentile)
                    meetsMinimums = false;
            }

            var suitabilityPct = totalWeight > 0
                ? Math.Round(weightedScore / totalWeight, 2)
                : 0;

            var isRecommended = meetsMinimums && suitabilityPct >= career.SuitabilityCutoffPct;

            // "Can be considered" — close to cutoff (within 15%) but fails some minimums
            var isCanBeConsidered = !isRecommended
                && suitabilityPct >= (career.SuitabilityCutoffPct - 15);

            suitabilityScores.Add(new CareerSuitabilityScore
            {
                AttemptId = attemptId,
                CareerId = career.Id,
                SuitabilityPct = suitabilityPct,
                IsRecommended = isRecommended,
                IsCanBeConsidered = isCanBeConsidered
            });
        }

        // Rank by suitability percentage
        var ranked = suitabilityScores.OrderByDescending(s => s.SuitabilityPct).ToList();
        for (int i = 0; i < ranked.Count; i++)
            ranked[i].RankOrder = i + 1;

        // Save
        var existing = await _context.CareerSuitabilityScores
            .Where(s => s.AttemptId == attemptId).ToListAsync();
        _context.CareerSuitabilityScores.RemoveRange(existing);
        await _context.CareerSuitabilityScores.AddRangeAsync(ranked);
        await _context.SaveChangesAsync();

        return ranked;
    }
}
