    [TestMethod]
    public void Create_should_throw_with_null_provider()
    {
        Assert.ThrowsExactly<System.ArgumentNullException>( () =>
            PipelineFactory.Create<string, string>( null, builder =>
                builder
                    .Pipe( ( ctx, arg ) => arg + "1" )
            )
        );
    }