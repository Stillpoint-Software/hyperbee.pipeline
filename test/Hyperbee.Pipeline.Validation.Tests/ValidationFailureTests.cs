namespace Hyperbee.Pipeline.Validation.Tests;

[TestClass]
public class ValidationFailureTests
{
    [TestMethod]
    public void ApplicationValidationFailure_should_store_property_and_message()
    {
        var failure = new ApplicationValidationFailure( "Email", "Email is required." );

        Assert.AreEqual( "Email", failure.PropertyName );
        Assert.AreEqual( "Email is required.", failure.ErrorMessage );
    }

    [TestMethod]
    public void ForbiddenValidationFailure_should_have_forbidden_error_code()
    {
        var failure = new ForbiddenValidationFailure( "Resource", "Access denied." );

        Assert.AreEqual( "Forbidden", failure.ErrorCode );
        Assert.AreEqual( "Resource", failure.PropertyName );
        Assert.AreEqual( "Access denied.", failure.ErrorMessage );
    }

    [TestMethod]
    public void NotFoundValidationFailure_should_have_not_found_error_code()
    {
        var failure = new NotFoundValidationFailure( "Item", "Item not found." );

        Assert.AreEqual( "NotFound", failure.ErrorCode );
        Assert.AreEqual( "Item", failure.PropertyName );
        Assert.AreEqual( "Item not found.", failure.ErrorMessage );
    }

    [TestMethod]
    public void UnauthorizedValidationFailure_should_have_unauthorized_error_code()
    {
        var failure = new UnauthorizedValidationFailure( "User", "Not authenticated." );

        Assert.AreEqual( "Unauthorized", failure.ErrorCode );
        Assert.AreEqual( "User", failure.PropertyName );
        Assert.AreEqual( "Not authenticated.", failure.ErrorMessage );
    }

    [TestMethod]
    public void ValidationFailure_Create_should_create_with_error_code()
    {
        var failure = ValidationFailure.Create( "Field", "Invalid value.", "ERR001" );

        Assert.AreEqual( "Field", failure.PropertyName );
        Assert.AreEqual( "Invalid value.", failure.ErrorMessage );
        Assert.AreEqual( "ERR001", failure.ErrorCode );
    }

    [TestMethod]
    public void ValidationFailure_Create_should_create_without_error_code()
    {
        var failure = ValidationFailure.Create( "Field", "Invalid value." );

        Assert.AreEqual( "Field", failure.PropertyName );
        Assert.AreEqual( "Invalid value.", failure.ErrorMessage );
        Assert.IsNull( failure.ErrorCode );
    }

    [TestMethod]
    public void ForbiddenValidationFailure_should_derive_from_ApplicationValidationFailure()
    {
        var failure = new ForbiddenValidationFailure( "X", "Y" );

        Assert.IsInstanceOfType( failure, typeof( ApplicationValidationFailure ) );
        Assert.IsInstanceOfType( failure, typeof( ValidationFailure ) );
    }

    [TestMethod]
    public void NotFoundValidationFailure_should_derive_from_ApplicationValidationFailure()
    {
        var failure = new NotFoundValidationFailure( "X", "Y" );

        Assert.IsInstanceOfType( failure, typeof( ApplicationValidationFailure ) );
    }

    [TestMethod]
    public void UnauthorizedValidationFailure_should_derive_from_ApplicationValidationFailure()
    {
        var failure = new UnauthorizedValidationFailure( "X", "Y" );

        Assert.IsInstanceOfType( failure, typeof( ApplicationValidationFailure ) );
    }
}
