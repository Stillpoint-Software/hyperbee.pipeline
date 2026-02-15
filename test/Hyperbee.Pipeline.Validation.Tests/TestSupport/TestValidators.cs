using FluentValidation;

namespace Hyperbee.Pipeline.Validation.Tests.TestSupport;

public class TestOutputValidator : AbstractValidator<TestOutput>
{
    public TestOutputValidator()
    {
        RuleFor( x => x.Name ).NotEmpty();
        RuleFor( x => x.ProcessedAge ).GreaterThan( 0 );
    }
}

public class AlwaysFailValidator : AbstractValidator<TestOutput>
{
    public AlwaysFailValidator()
    {
        RuleFor( x => x.Name ).Must( _ => false ).WithMessage( "Always fails" );
    }
}

public class RuleSetModelValidator : AbstractValidator<RuleSetModel>
{
    public RuleSetModelValidator()
    {
        RuleFor( x => x.Name ).NotEmpty();

        RuleSet( "Create", () =>
        {
            RuleFor( x => x.Value ).LessThanOrEqualTo( 1000 );
        } );

        RuleSet( "Update", () =>
        {
            RuleFor( x => x.Id ).NotEmpty();
            RuleFor( x => x.VersionTag ).NotEmpty();
        } );
    }
}
