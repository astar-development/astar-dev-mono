// using System.Diagnostics.CodeAnalysis;
//
// namespace AStar.Dev.Functional.Extensions.Tests.Unit;
//
// [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly")]
// public class ResultAsyncLinqExtensionsTests
// {
//     private static Result<int, string>.Ok Ok(int value) => new(value);
//
//     private static Result<int, string>.Error Err(string error) => new(error);
//
//     [Fact]
//     public async Task AsyncLinq_ComposesCorrectly()
//     {
//         var result = from a in new ValueTask<Result<int, string>>(Ok(2))
//                      from b in new ValueTask<Result<int, string>>(Ok(3))
//                      select a + b;
//
//         var output = await result.MatchAsync(
//                                              success => success,
//                                              error => -1);
//
//         Assert.Equal(5, output);
//     }
//
//     [Fact]
//     public async Task AsyncLinq_PropagatesOuterError()
//     {
//         var result = from a in new ValueTask<Result<int, string>>(Err("outer"))
//                      from b in new ValueTask<Result<int, string>>(Ok(3))
//                      select a + b;
//
//         var output = await result.MatchAsync(
//                                              success => success,
//                                              error => -1);
//
//         Assert.Equal(-1, output);
//     }
//
//     [Fact]
//     public async Task AsyncLinq_PropagatesInnerError()
//     {
//         var result = from a in new ValueTask<Result<int, string>>(Ok(2))
//                      from b in new ValueTask<Result<int, string>>(Err("inner"))
//                      select a + b;
//
//         var output = await result.MatchAsync(
//                                              success => success,
//                                              error => -1);
//
//         Assert.Equal(-1, output);
//     }
// }


