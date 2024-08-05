# todo

## Builder generic type arg names

Consider refactoring the builder generic type args to be more clear. It is a difficult naming problem
because the inputs of the current builder and the inputs of the builder being created overlap. Naming of
type args that make sense in both contexts has proven difficult. The compositional nature of pipelines
is also a complicating factor; `TInput`, for instance, isn't the input to the current builder but is the
first input to the pipeline.

The current argument convention is [`TInput`, `TOutput`, `TNext`] where:

- `TInput` is always the initial pipeline input
- `TOutput` is the _current_ builder output (the next function's input)
- `TNext` is the next function output

Should `TInput` be renamed to `TStart` or `TFirst`?
Should `TNext` be renamed to make it clear that it is the next `TOutput`?
