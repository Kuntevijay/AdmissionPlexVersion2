using FluentValidation;
using AdmissionPlex.Shared.DTOs.Tests;

namespace AdmissionPlex.Shared.Validators;

public class QuestionCreateValidator : AbstractValidator<QuestionCreateDto>
{
    private static readonly string[] ValidQuestionTypes = { "Mcq", "Likert", "TrueFalse", "Ranking", "Open" };
    private static readonly string[] ValidSectionTypes = { "StreamSelector", "Interest", "Aptitude" };

    public QuestionCreateValidator()
    {
        RuleFor(x => x.QuestionText)
            .NotEmpty().WithMessage("Question text is required.")
            .MaximumLength(2000);

        RuleFor(x => x.QuestionType)
            .Must(t => ValidQuestionTypes.Contains(t))
            .WithMessage("Question type must be: Mcq, Likert, TrueFalse, Ranking, or Open.");

        RuleFor(x => x.SectionType)
            .Must(t => ValidSectionTypes.Contains(t))
            .WithMessage("Section type must be: StreamSelector, Interest, or Aptitude.");

        RuleFor(x => x.InterestCategoryId)
            .NotNull().When(x => x.SectionType == "Interest")
            .WithMessage("Interest category is required for Interest section questions.");

        RuleFor(x => x.AptitudeCategoryId)
            .NotNull().When(x => x.SectionType == "Aptitude")
            .WithMessage("Aptitude category is required for Aptitude section questions.");

        RuleFor(x => x.Options)
            .NotEmpty().When(x => x.QuestionType != "Open")
            .WithMessage("Options are required for non-open questions.");

        RuleForEach(x => x.Options).ChildRules(option =>
        {
            option.RuleFor(o => o.OptionText)
                .NotEmpty().WithMessage("Option text is required.");
        });
    }
}
